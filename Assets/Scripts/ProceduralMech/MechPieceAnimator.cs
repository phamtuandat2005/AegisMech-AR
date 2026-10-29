using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MechChess.Chess3D;

namespace MechChess.ProceduralMech
{
    /// <summary>
    /// Handles idle hovering/breathing animations, movement particle sparks,
    /// EMP shock reaction, and explosion/disintegration effects upon capture.
    /// </summary>
    public class MechPieceAnimator : MonoBehaviour
    {
        [Header("Idle Hover Settings")]
        [SerializeField] private float hoverAmplitude = 0.045f;
        [SerializeField] private float hoverFrequency = 1.6f;
        [SerializeField] private float swayAngle = 1.2f;

        [Header("State")]
        private PieceColor _pieceColor;
        private Color _glowColor = Color.cyan;
        private Vector3 _baseLocalPosition;
        private Quaternion _baseLocalRotation = Quaternion.identity;
        private float _timeOffset;
        private bool _isMoving = false;
        private bool _isDestroyed = false;

        // Selection Visual Polish
        private bool _isSelected = false;
        private float _selectedLift = 0f;
        private GameObject _selectionHologramObj;

        // ── Sub-Part Animation System ──────────────────────────────────────────
        public enum CombatRole { Attacker, Defender }
        public enum CombatPhase { Idle, Dash, Clash, Finisher, Aftermath }

        // Part type constants — used to select the right combat & locomotion overrides per part.
        private const byte PT_GENERIC  = 0;
        private const byte PT_LEG      = 1;  // thighs, shins, bipedal & hydraulic legs
        private const byte PT_FOOT     = 2;  // feet, toes, heels
        private const byte PT_ARM      = 3;  // upper arm, forearm, gauntlet
        private const byte PT_HEAD     = 4;  // head, neck, helmet, visor
        private const byte PT_WING     = 5;  // wings (Queen)
        private const byte PT_SHOULDER = 6;  // pauldrons
        private const byte PT_CANNON   = 7;  // twin cannons, barrels (Rook)
        private const byte PT_WEAPON   = 8;  // Greatsword, arm blades, mane blades
        private const byte PT_FIN      = 9;  // radiator fins (Pawn)
        private const byte PT_CORE     = 10; // reactor core, resonance core

        private struct PartAnim
        {
            public Transform  target;
            public Vector3    baseLocalPos;
            public Quaternion baseLocalRot;
            public byte       partType;
            public float      sideSign; // -1 for right, +1 for left, 0 for center

            // Rotation oscillation
            public Vector3 rotAxis;   // axis (local-space)
            public float   rotAmp;    // degrees amplitude  (< 0 = continuous spin)
            public float   rotFreq;   // Hz
            public float   rotPhase;  // phase offset (radians)

            // Position oscillation (zero = disabled)
            public Vector3 posAxis;
            public float   posAmp;
            public float   posFreq;
            public float   posPhase;
        }

        private readonly List<PartAnim> _partAnims = new List<PartAnim>();
        [System.NonSerialized] private bool _subPartsInitialized;

        // Piece type caching
        private PieceType _pieceType = PieceType.Pawn;
        private bool _hasPieceType = false;
        public PieceType PieceType
        {
            get
            {
                if (_hasPieceType) return _pieceType;
                ChessPiece3D p = GetComponent<ChessPiece3D>();
                if (p != null) { _pieceType = p.pieceType; _hasPieceType = true; }
                return _pieceType;
            }
            set { _pieceType = value; _hasPieceType = true; }
        }

        // ── Active Locomotion State (0..1, driven during piece movement) ───────
        private float _locoWeight = 0f;
        private float _locoPhase  = 0f;

        // ── Combat Choreography State (driven by cutscene & attack routines) ───
        private bool        _inCombat       = false;
        private CombatRole  _cbtRole        = CombatRole.Attacker;
        private CombatPhase _cbtPhase       = CombatPhase.Idle;
        private float       _cbtProgress    = 0f;
        private PieceType   _cbtPieceType   = PieceType.Pawn;

        /// <summary>
        /// Drives the full-body locomotion stride (legs running, arms swinging, wings flapping).
        /// </summary>
        public void SetLocomotion(float weight, float strideCycle)
        {
            _locoWeight = Mathf.Clamp01(weight);
            _locoPhase  = strideCycle;
        }

        /// <summary>
        /// Drives dynamic full-body combat poses matching the current cutscene or attack phase.
        /// </summary>
        public void DriveCombatChoreography(PieceType pieceType, CombatRole role, CombatPhase phase, float progress)
        {
            _inCombat     = true;
            _cbtPieceType = pieceType;
            _cbtRole      = role;
            _cbtPhase     = phase;
            _cbtProgress  = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Clears all combat and locomotion poses back to neutral idle.
        /// </summary>
        public void ResetCombatPoses()
        {
            _inCombat     = false;
            _cbtPhase     = CombatPhase.Idle;
            _cbtProgress  = 0f;
            _locoWeight   = 0f;
        }


        // Skippable Animation References
        private Coroutine _activeAnimationRoutine = null;
        private Action _onImpactCallback = null;
        private Action _onCompleteCallback = null;
        private Vector3 _pendingTargetPos;
        private GameObject _activeBladeObj = null;

        private ParticleSystem _moveSparks;

        public void Initialize(PieceColor color, Color glowColor)
        {
            _pieceColor = color;
            _glowColor = glowColor;
            _timeOffset = UnityEngine.Random.Range(0f, 10f);
            _baseLocalPosition = transform.localPosition;
            _baseLocalRotation = transform.localRotation;

            CreateMoveSparkParticles();
        }

        public void SetBaseOrientation(Quaternion rot)
        {
            _baseLocalRotation = rot;
            transform.localRotation = rot;
        }

        private void Start()
        {
            if (_baseLocalPosition == Vector3.zero)
            {
                _baseLocalPosition = transform.localPosition;
            }
            if (_baseLocalRotation == Quaternion.identity && transform.localRotation != Quaternion.identity)
            {
                _baseLocalRotation = transform.localRotation;
            }
        }

        // ── Sub-Part Animation Discovery ───────────────────────────────────────
        /// <summary>
        /// Scans child GameObjects by name and registers oscillation descriptors
        /// so each recognizable mech part moves independently on its own rhythm.
        /// Called once lazily on first Update so all child parts are already built.
        /// </summary>
        private void InitializeSubPartAnimations()
        {
            _subPartsInitialized = true;
            _partAnims.Clear();

            float rng = _timeOffset;

            Transform[] children = GetComponentsInChildren<Transform>(includeInactive: false);
            foreach (Transform t in children)
            {
                if (t == transform) continue; // skip root

                string n = t.name.ToLower();
                PartAnim pa = new PartAnim
                {
                    target       = t,
                    baseLocalPos = t.localPosition,
                    baseLocalRot = t.localRotation,
                };

                // Side detection (left vs right vs center)
                bool isLeft  = n.EndsWith("_l") || n.Contains("_l_") || n.Contains("_0") || n.Contains("dish_l") || n.Contains("antenna_l") || n.Contains("wing_l") || n.Contains("blade_l");
                bool isRight = n.EndsWith("_r") || n.Contains("_r_") || n.Contains("_1") || n.Contains("dish_r") || n.Contains("antenna_r") || n.Contains("wing_r") || n.Contains("blade_r");
                pa.sideSign  = isLeft ? 1f : (isRight ? -1f : 0f);

                bool registered = false;

                // ── LEGS: thighs, shins, bipedal & hydraulic legs ──
                if (n.Contains("thigh") || n.Contains("shin") || n.Contains("hydraulicleg") || n.Contains("leg_") || n.Contains("talon"))
                {
                    float side   = pa.sideSign != 0f ? pa.sideSign : 1f;
                    pa.rotAxis   = new Vector3(side, 0, 0);
                    pa.rotAmp    = 6f;
                    pa.rotFreq   = 1.2f;
                    pa.rotPhase  = isLeft ? 0f : Mathf.PI;
                    pa.partType  = PT_LEG;
                    registered   = true;
                }
                // ── FEET: subtle rock front/back ──
                else if (n.Contains("foot") || n.Contains("toe") || n.Contains("heel"))
                {
                    pa.rotAxis   = Vector3.right;
                    pa.rotAmp    = 3.5f;
                    pa.rotFreq   = 1.2f;
                    pa.rotPhase  = rng;
                    pa.partType  = PT_FOOT;
                    registered   = true;
                }
                // ── ARMS: upper arm, forearm, gauntlet ──
                else if (n.Contains("upperarm") || n.Contains("upper_arm") || n.Contains("forearm") || n.Contains("gauntlet"))
                {
                    float side   = pa.sideSign != 0f ? pa.sideSign : 1f;
                    pa.rotAxis   = new Vector3(0, 0, side);
                    pa.rotAmp    = 4.5f;
                    pa.rotFreq   = 0.9f;
                    pa.rotPhase  = isLeft ? 0f : Mathf.PI;
                    pa.partType  = PT_ARM;
                    registered   = true;
                }
                // ── PAULDRONS / SHOULDER ARMOR: subtle up/down bob ──
                else if (n.Contains("pauldron") || n.Contains("shoulder"))
                {
                    pa.rotAxis   = Vector3.forward;
                    pa.rotAmp    = 3.5f;
                    pa.rotFreq   = 0.8f;
                    pa.rotPhase  = isLeft ? 0f : Mathf.PI;
                    pa.partType  = PT_SHOULDER;
                    registered   = true;
                }
                // ── HEAD / HELMET / NECK ──
                else if ((n.Contains("head") || n.Contains("neck") || n.Contains("helmet") || n.Contains("steed")) && !n.Contains("crown"))
                {
                    pa.rotAxis   = Vector3.up;
                    pa.rotAmp    = 7f;
                    pa.rotFreq   = 0.55f;
                    pa.rotPhase  = rng * 0.7f;
                    pa.partType  = PT_HEAD;
                    registered   = true;
                }
                // ── VISOR / OPTICS / SENSORS ──
                else if (n.Contains("visor") || n.Contains("sensor") || n.Contains("optic") || n.Contains("eye"))
                {
                    pa.rotAxis   = Vector3.zero;
                    pa.posAxis   = Vector3.forward;
                    pa.posAmp    = 0.012f;
                    pa.posFreq   = 2.2f;
                    pa.posPhase  = rng;
                    pa.partType  = PT_HEAD;
                    registered   = true;
                }
                // ── WINGS (Queen) ──
                else if (n.Contains("wing"))
                {
                    float side   = pa.sideSign != 0f ? pa.sideSign : 1f;
                    pa.rotAxis   = new Vector3(0, 0, side);
                    pa.rotAmp    = 6f;
                    pa.rotFreq   = 1.4f;
                    pa.rotPhase  = isLeft ? 0f : Mathf.PI;
                    pa.partType  = PT_WING;
                    registered   = true;
                }
                // ── CANNONS / MISSILES (Rook) ──
                else if (n.Contains("cannon") || n.Contains("barrel") || n.Contains("missile"))
                {
                    pa.rotAxis   = Vector3.right;
                    pa.rotAmp    = 2.5f;
                    pa.rotFreq   = 0.6f;
                    pa.rotPhase  = rng * 0.4f;
                    pa.partType  = PT_CANNON;
                    registered   = true;
                }
                // ── WEAPONS: Greatsword, arm blades, mane blades ──
                else if (n.Contains("sword") || n.Contains("blade") || n.Contains("mane"))
                {
                    pa.rotAxis   = new Vector3(0, 1f, 0);
                    pa.rotAmp    = 4f;
                    pa.rotFreq   = 0.85f;
                    pa.rotPhase  = rng * 0.9f;
                    pa.partType  = PT_WEAPON;
                    registered   = true;
                }
                // ── RADIATOR FINS (Pawn) ──
                else if (n.Contains("fin") || n.Contains("radiator"))
                {
                    float side   = pa.sideSign != 0f ? pa.sideSign : 1f;
                    pa.rotAxis   = new Vector3(0, side, 0);
                    pa.rotAmp    = 4.5f;
                    pa.rotFreq   = 1.1f;
                    pa.rotPhase  = isLeft ? 0f : Mathf.PI;
                    pa.partType  = PT_FIN;
                    registered   = true;
                }
                // ── REACTOR CORE / RESONANCE CORE ──
                else if (n.Contains("reactor") || n.Contains("resonance") || n.Contains("core"))
                {
                    pa.rotAxis   = Vector3.zero;
                    pa.posAxis   = Vector3.forward;
                    pa.posAmp    = 0.015f;
                    pa.posFreq   = 1.8f;
                    pa.posPhase  = rng * 1.3f;
                    pa.partType  = PT_CORE;
                    registered   = true;
                }
                // ── ANTENNA / DISH (Bishop) ──
                else if (n.Contains("antenna") || n.Contains("dish"))
                {
                    float side   = pa.sideSign != 0f ? pa.sideSign : 1f;
                    pa.rotAxis   = new Vector3(0, 0, side);
                    pa.rotAmp    = 5f;
                    pa.rotFreq   = 0.7f;
                    pa.rotPhase  = isLeft ? rng : rng + Mathf.PI;
                    pa.partType  = PT_GENERIC;
                    registered   = true;
                }
                // ── ORBIT RINGS (Bishop): continuous spin ──
                else if (n.Contains("orbitring") || n.Contains("orbit_ring"))
                {
                    pa.rotAxis   = GetOrbitRingAxis(n);
                    pa.rotAmp    = -1f; // sentinel: continuous spin
                    pa.rotFreq   = GetOrbitRingSpeed(n);
                    pa.rotPhase  = 0f;
                    pa.partType  = PT_GENERIC;
                    registered   = true;
                }
                // ── HOLOGRAM RING (King base): continuous spin ──
                else if (n.Contains("holo") && n.Contains("ring"))
                {
                    pa.rotAxis   = Vector3.up;
                    pa.rotAmp    = -1f;
                    pa.rotFreq   = 35f;
                    pa.rotPhase  = 0f;
                    pa.partType  = PT_GENERIC;
                    registered   = true;
                }
                // ── CROWN SPIRES ──
                else if (n.Contains("crownspire") || n.Contains("crown_spire"))
                {
                    pa.rotAxis   = new Vector3(1f, 0, 0);
                    pa.rotAmp    = 2.5f;
                    pa.rotFreq   = 0.9f;
                    pa.rotPhase  = rng + t.GetSiblingIndex() * 0.4f;
                    pa.partType  = PT_GENERIC;
                    registered   = true;
                }

                if (registered) _partAnims.Add(pa);
            }
        }

        private static Vector3 GetOrbitRingAxis(string name)
        {
            if (name.Contains("_1")) return Vector3.up;
            if (name.Contains("_2")) return new Vector3(0.5f, 0.5f, 0).normalized;
            return new Vector3(0, 0.3f, 0.7f).normalized;
        }

        private static float GetOrbitRingSpeed(string name)
        {
            if (name.Contains("_1")) return 55f;
            if (name.Contains("_2")) return -40f;
            return 70f;
        }

        /// <summary>
        /// Advances all registered sub-part animations by one frame.
        /// Seamlessly blends between idle breathing, active locomotion strides,
        /// and dynamic piece-specific combat choreographies.
        /// </summary>
        private void UpdatePartAnims()
        {
            float t = Time.time + _timeOffset;

            for (int i = 0; i < _partAnims.Count; i++)
            {
                PartAnim pa = _partAnims[i];
                if (pa.target == null) continue;

                // ── Continuous spin (orbit rings, holo rings) ──
                if (pa.rotAmp < 0f)
                {
                    float speedMult = _inCombat ? 3.5f : (_locoWeight > 0.05f ? 2.2f : 1.0f);
                    pa.target.Rotate(pa.rotAxis, pa.rotFreq * speedMult * Time.deltaTime, Space.Self);
                    continue;
                }

                // ── Base idle sine-wave ──
                float idleAngle = 0f;
                if (pa.rotAxis != Vector3.zero && pa.rotAmp > 0f)
                    idleAngle = Mathf.Sin(t * pa.rotFreq * Mathf.PI * 2f + pa.rotPhase) * pa.rotAmp;

                float finalAngle = idleAngle;
                Vector3 finalPosOffset = Vector3.zero;
                float side = pa.sideSign != 0f ? pa.sideSign : 1f;

                // ── 1. ACTIVE LOCOMOTION OVERRIDES ──
                if (_locoWeight > 0.001f && !_inCombat)
                {
                    float stride    = Mathf.Sin(_locoPhase);
                    float strideCos = Mathf.Cos(_locoPhase);

                    switch (pa.partType)
                    {
                        case PT_LEG:
                            // Alternating fluid walk/run stride
                            float legStride = side * stride * 24f;
                            if (pa.target.name.ToLower().Contains("shin"))
                            {
                                float kneeFlex = Mathf.Clamp01(-side * stride) * 22f;
                                legStride += kneeFlex;
                            }
                            finalAngle = Mathf.Lerp(idleAngle, legStride, _locoWeight);
                            break;

                        case PT_FOOT:
                            float footRock = -side * strideCos * 14f;
                            finalAngle = Mathf.Lerp(idleAngle, footRock, _locoWeight);
                            break;

                        case PT_ARM:
                            // Counter-swing opposite to legs
                            float armSwing = -side * stride * 18f;
                            finalAngle = Mathf.Lerp(idleAngle, armSwing, _locoWeight);
                            break;

                        case PT_SHOULDER:
                            float shoulderBob = side * strideCos * 5f;
                            finalAngle = Mathf.Lerp(idleAngle, shoulderBob, _locoWeight);
                            break;

                        case PT_WING:
                            // Graceful flapping and aerodynamic spread
                            float flap = side * (22f + Mathf.Sin(_locoPhase * 1.5f) * 16f);
                            finalAngle = Mathf.Lerp(idleAngle, flap, _locoWeight);
                            break;

                        case PT_FIN:
                            // Sweep back into streamlined cruise shape
                            float finSweep = -side * 28f;
                            finalAngle = Mathf.Lerp(idleAngle, finSweep, _locoWeight);
                            break;

                        case PT_CANNON:
                            float cannonPitch = Mathf.Sin(_locoPhase * 2f) * 3f;
                            finalAngle = Mathf.Lerp(idleAngle, cannonPitch, _locoWeight);
                            break;

                        case PT_WEAPON:
                            float weaponSway = side * stride * 8f;
                            finalAngle = Mathf.Lerp(idleAngle, weaponSway, _locoWeight);
                            break;
                    }
                }

                // ── 2. DYNAMIC COMBAT CHOREOGRAPHY OVERRIDES ──
                if (_inCombat)
                {
                    float u = _cbtProgress;

                    if (_cbtRole == CombatRole.Attacker)
                    {
                        // ── ATTACKER CHOREOGRAPHY ──
                        switch (_cbtPhase)
                        {
                            case CombatPhase.Dash:
                            {
                                // SPRINT CHARGE: legs pump 3× speed, arms & wings stream back
                                switch (pa.partType)
                                {
                                    case PT_LEG:
                                        finalAngle = Mathf.Sin(u * 28f + (side > 0 ? 0 : Mathf.PI)) * 26f;
                                        break;
                                    case PT_ARM:
                                        finalAngle = -38f * u; // arms swept back in slipstream
                                        break;
                                    case PT_WING:
                                        finalAngle = -side * 25f * u;
                                        break;
                                    case PT_FIN:
                                        finalAngle = -side * 32f * u;
                                        break;
                                    case PT_WEAPON:
                                        finalAngle = 25f * u;
                                        break;
                                }
                                break;
                            }

                            case CombatPhase.Clash:
                            {
                                // CLASH & BRACE: weapon strikes forward, limbs brace against impact
                                switch (pa.partType)
                                {
                                    case PT_LEG:
                                        finalAngle = side * 15f; // braced wide stance
                                        break;
                                    case PT_ARM:
                                        finalAngle = (side > 0) ? -20f : 55f; // right arm strikes forward
                                        break;
                                    case PT_SHOULDER:
                                        finalAngle = side * 12f;
                                        break;
                                    case PT_WEAPON:
                                        finalAngle = 65f;
                                        break;
                                }
                                break;
                            }

                            case CombatPhase.Finisher:
                            {
                                // SIGNATURE FINISHER PER PIECE TYPE
                                switch (_cbtPieceType)
                                {
                                    case PieceType.Pawn: // Scout Drone: high-speed ram & laser eye blast
                                        if (pa.partType == PT_FIN)
                                        {
                                            float spread = (u < 0.6f) ? (side * 35f * (u / 0.6f)) : (-side * 30f);
                                            finalAngle = spread;
                                        }
                                        else if (pa.partType == PT_HEAD)
                                        {
                                            finalPosOffset = Vector3.forward * (Mathf.Sin(u * Mathf.PI) * 0.04f);
                                        }
                                        break;

                                    case PieceType.Knight: // Strike Strider: vault leap & mane-blade execution
                                        if (pa.partType == PT_LEG)
                                        {
                                            float legAng = (u < 0.5f) ? (-40f * (u / 0.5f)) : (Mathf.Lerp(-40f, 55f, (u - 0.5f) / 0.5f));
                                            finalAngle = legAng;
                                        }
                                        else if (pa.partType == PT_HEAD)
                                        {
                                            float headAng = (u < 0.5f) ? (-25f * (u / 0.5f)) : (Mathf.Lerp(-25f, 35f, (u - 0.5f) / 0.5f));
                                            finalAngle = headAng;
                                        }
                                        else if (pa.partType == PT_WEAPON)
                                        {
                                            finalAngle = Mathf.Lerp(20f, 85f, u);
                                        }
                                        break;

                                    case PieceType.Bishop: // Tactical Relay: orbital convergence & beam focus
                                        if (pa.partType == PT_GENERIC && (pa.target.name.ToLower().Contains("antenna") || pa.target.name.ToLower().Contains("dish")))
                                        {
                                            finalAngle = 35f * Mathf.Clamp01(u * 1.5f);
                                        }
                                        else if (pa.partType == PT_CORE)
                                        {
                                            finalPosOffset = Vector3.forward * (Mathf.Sin(u * Mathf.PI) * 0.05f);
                                        }
                                        break;

                                    case PieceType.Rook: // Siege Bastion: anchor protocol & artillery recoil
                                        if (pa.partType == PT_LEG)
                                        {
                                            finalAngle = side * 22f; // anchor out
                                        }
                                        else if (pa.partType == PT_CANNON)
                                        {
                                            if (u < 0.55f)
                                            {
                                                finalAngle = 18f * (u / 0.55f); // elevate
                                            }
                                            else
                                            {
                                                float recoilT = (u - 0.55f) / 0.45f;
                                                finalPosOffset = -Vector3.forward * (Mathf.Sin(recoilT * Mathf.PI) * 0.22f); // violent backward jerk
                                                finalAngle = 18f - 8f * recoilT;
                                            }
                                        }
                                        break;

                                    case PieceType.Queen: // Valkyrie: aerial ascent & dual-blade X-cross execution
                                        if (pa.partType == PT_WING)
                                        {
                                            finalAngle = side * (35f + Mathf.Sin(u * Mathf.PI) * 15f); // full glorious wingspan
                                        }
                                        else if (pa.partType == PT_ARM)
                                        {
                                            if (u < 0.5f)
                                            {
                                                // Raise dual blades high in X-cross
                                                finalAngle = -55f * (u / 0.5f);
                                            }
                                            else
                                            {
                                                // Violent diagonal downward execution
                                                float slashT = (u - 0.5f) / 0.5f;
                                                finalAngle = Mathf.Lerp(-55f, 65f, slashT * slashT);
                                            }
                                        }
                                        break;

                                    case PieceType.King: // Overlord Titan: two-handed Greatsword earth splitter
                                        if (pa.partType == PT_LEG)
                                        {
                                            finalAngle = side * 20f; // massive titan power stance
                                        }
                                        else if (pa.partType == PT_SHOULDER)
                                        {
                                            finalAngle = 15f * Mathf.Sin(u * Mathf.PI);
                                        }
                                        else if (pa.partType == PT_ARM || pa.partType == PT_WEAPON)
                                        {
                                            if (u < 0.7f)
                                            {
                                                // Hoist Greatsword overhead with both arms
                                                finalAngle = -75f * (u / 0.7f);
                                            }
                                            else
                                            {
                                                // Catastrophic ground-cleaving slam
                                                float strikeT = (u - 0.7f) / 0.3f;
                                                finalAngle = Mathf.Lerp(-75f, 65f, strikeT * strikeT);
                                            }
                                        }
                                        break;
                                }
                                break;
                            }

                            case CombatPhase.Aftermath:
                            {
                                // Settle smoothly from combat pose back to idle
                                finalAngle = Mathf.Lerp(finalAngle, idleAngle, u);
                                break;
                            }
                        }
                    }
                    else
                    {
                        // ── DEFENDER CHOREOGRAPHY ──
                        switch (_cbtPhase)
                        {
                            case CombatPhase.Dash:
                            {
                                // Defensive crouch & guard
                                if (pa.partType == PT_LEG) finalAngle = 14f * u;
                                else if (pa.partType == PT_ARM) finalAngle = -28f * u; // raise guard
                                break;
                            }

                            case CombatPhase.Clash:
                            {
                                // Bracing against attacker
                                if (pa.partType == PT_LEG) finalAngle = 18f;
                                else if (pa.partType == PT_ARM) finalAngle = -35f;
                                else if (pa.partType == PT_HEAD) finalAngle = -8f;
                                break;
                            }

                            case CombatPhase.Finisher:
                            {
                                if (u >= 0.7f)
                                {
                                    // CRITICAL IMPACT REACTION: body thrown backward, limbs blown outward
                                    float hitT = (u - 0.7f) / 0.3f;
                                    float hitCurve = Mathf.SmoothStep(0f, 1f, hitT);
                                    if (pa.partType == PT_ARM) finalAngle = -45f * hitCurve;
                                    else if (pa.partType == PT_LEG) finalAngle = 26f * hitCurve;
                                    else if (pa.partType == PT_HEAD) finalAngle = -25f * hitCurve;
                                    else if (pa.partType == PT_SHOULDER) finalAngle = -side * 18f * hitCurve;
                                }
                                break;
                            }

                            case CombatPhase.Aftermath:
                            {
                                // Staggered and reeling into disintegration
                                if (pa.partType == PT_ARM) finalAngle = -45f;
                                else if (pa.partType == PT_LEG) finalAngle = 26f;
                                break;
                            }
                        }
                    }
                }

                // Apply rotation
                pa.target.localRotation = pa.rotAxis != Vector3.zero
                    ? pa.baseLocalRot * Quaternion.AngleAxis(finalAngle, pa.rotAxis)
                    : pa.baseLocalRot;

                // Apply position oscillation / recoil
                Vector3 basePos = pa.baseLocalPos + finalPosOffset;
                if (pa.posAxis != Vector3.zero && pa.posAmp > 0f)
                {
                    float posOsc = Mathf.Sin(t * pa.posFreq * Mathf.PI * 2f + pa.posPhase) * pa.posAmp;
                    pa.target.localPosition = basePos + pa.posAxis * posOsc;
                }
                else
                {
                    pa.target.localPosition = basePos;
                }
            }
        }

        public void SetSelected(bool isSelected)
        {
            if (_isSelected == isSelected) return;
            _isSelected = isSelected;

            if (_isSelected)
            {
                CreateSelectionHologram();
            }
            else
            {
                if (_selectionHologramObj != null)
                {
                    Destroy(_selectionHologramObj);
                    _selectionHologramObj = null;
                }
            }
        }

        private void CreateSelectionHologram()
        {
            if (_selectionHologramObj != null) Destroy(_selectionHologramObj);

            _selectionHologramObj = new GameObject("SelectionHologramBeacon");
            _selectionHologramObj.transform.SetParent(transform, false);
            _selectionHologramObj.transform.localPosition = new Vector3(0, 0.02f, 0);

            // Outer glowing hologram ring
            GameObject ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "HoloRing";
            ring.transform.SetParent(_selectionHologramObj.transform, false);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0.78f, 0.015f, 0.78f);

            Collider col = ring.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer rend = ring.GetComponent<Renderer>();
            if (rend != null)
            {
                rend.sharedMaterial = GetSelectionHoloMaterial(_pieceColor);
                rend.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                rend.receiveShadows = false;
            }
        }

        private static Material _whiteHoloMat;
        private static Material _blackHoloMat;

        private static Material GetSelectionHoloMaterial(PieceColor color)
        {
            if (color == PieceColor.White)
            {
                if (_whiteHoloMat != null) return _whiteHoloMat;
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                _whiteHoloMat = new Material(s) { name = "SelectionHoloMat_White" };
                Color c = new Color(0.1f, 0.95f, 1.0f);
                if (_whiteHoloMat.HasProperty("_BaseColor")) _whiteHoloMat.SetColor("_BaseColor", c);
                if (_whiteHoloMat.HasProperty("_Color")) _whiteHoloMat.SetColor("_Color", c);
                _whiteHoloMat.EnableKeyword("_EMISSION");
                if (_whiteHoloMat.HasProperty("_EmissionColor")) _whiteHoloMat.SetColor("_EmissionColor", c * 3.0f);
                _whiteHoloMat.enableInstancing = true;
                return _whiteHoloMat;
            }
            else
            {
                if (_blackHoloMat != null) return _blackHoloMat;
                Shader s = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard") ?? Shader.Find("Diffuse");
                _blackHoloMat = new Material(s) { name = "SelectionHoloMat_Black" };
                Color c = new Color(1.0f, 0.25f, 0.35f);
                if (_blackHoloMat.HasProperty("_BaseColor")) _blackHoloMat.SetColor("_BaseColor", c);
                if (_blackHoloMat.HasProperty("_Color")) _blackHoloMat.SetColor("_Color", c);
                _blackHoloMat.EnableKeyword("_EMISSION");
                if (_blackHoloMat.HasProperty("_EmissionColor")) _blackHoloMat.SetColor("_EmissionColor", c * 3.0f);
                _blackHoloMat.enableInstancing = true;
                return _blackHoloMat;
            }
        }

        private void Update()
        {
            if (_isDestroyed) return;

            // Lazy-initialize sub-part animations on first tick
            // (done here so all child GameObjects are guaranteed to exist)
            if (!_subPartsInitialized) InitializeSubPartAnimations();

            // Sub-part animations run ALWAYS (idle + moving + attacking).
            // Attack/move coroutines only touch the ROOT transform and the
            // dynamically-created blade object — never the registered child parts —
            // so there is zero conflict and sub-parts keep moving during combat.
            UpdatePartAnims();

            if (_isMoving)
            {
                // Root hover/sway is suppressed while a coroutine drives the root.
                // Hologram ring also skipped during movement (not visible anyway).
                return;
            }

            // Idle Hovering / Breathing Sine Wave with preserved base orientation
            float t = (Time.time + _timeOffset) * hoverFrequency;
            float yOffset = Mathf.Sin(t) * hoverAmplitude;
            float roll    = Mathf.Sin(t * 0.75f) * swayAngle;
            float pitch   = Mathf.Cos(t * 0.5f) * (swayAngle * 0.6f);

            // Smooth physical elevation when selected
            float targetLift = _isSelected ? 0.16f : 0f;
            _selectedLift = Mathf.MoveTowards(_selectedLift, targetLift, Time.deltaTime * 1.8f);

            transform.localPosition = _baseLocalPosition + new Vector3(0, yOffset + _selectedLift, 0);
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(pitch, 0, roll);

            // Smooth rotation of hologram focus ring
            if (_selectionHologramObj != null)
            {
                _selectionHologramObj.transform.Rotate(0, 90f * Time.deltaTime, 0);
            }
        }

        /// <summary>
        /// Instantly skips active move or capture animation and cleanly finalizes board state.
        /// </summary>
        public void SkipCurrentAnimation()
        {
            if (!_isMoving || _activeAnimationRoutine == null) return;

            StopCoroutine(_activeAnimationRoutine);
            _activeAnimationRoutine = null;

            // Trigger impact immediately if pending
            if (_onImpactCallback != null)
            {
                var impact = _onImpactCallback;
                _onImpactCallback = null;
                impact.Invoke();
            }

            if (_activeBladeObj != null)
            {
                Destroy(_activeBladeObj);
                _activeBladeObj = null;
            }

            transform.position = _pendingTargetPos;
            _baseLocalPosition = transform.localPosition;
            transform.localRotation = _baseLocalRotation;

            if (_moveSparks != null) _moveSparks.Stop();
            _isMoving = false;

            var done = _onCompleteCallback;
            _onCompleteCallback = null;
            done?.Invoke();
        }

        /// <summary>
        /// Moves the mech smoothly from current position to target board position with an arc.
        /// </summary>
        public void AnimateMoveTo(Vector3 targetWorldPos, float duration, Action onComplete = null)
        {
            SetSelected(false);
            if (_activeAnimationRoutine != null) StopCoroutine(_activeAnimationRoutine);
            _pendingTargetPos = targetWorldPos;
            _onImpactCallback = null;
            _onCompleteCallback = onComplete;
            _activeAnimationRoutine = StartCoroutine(MoveRoutine(targetWorldPos, duration, onComplete));
        }

        private IEnumerator MoveRoutine(Vector3 targetWorldPos, float duration, Action onComplete)
        {
            _isMoving = true;
            ResetCombatPoses();
            if (_moveSparks != null) _moveSparks.Play();

            Vector3 startPos = transform.position;
            Vector3 delta = targetWorldPos - startPos;
            delta.y = 0f;
            float dist = delta.magnitude;
            Vector3 moveDir = dist > 0.001f ? delta.normalized : transform.forward;
            Quaternion targetFacing = Quaternion.LookRotation(moveDir, Vector3.up);
            Quaternion initialRot = transform.localRotation;

            float elapsed = 0f;
            float jumpHeight = 0.55f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Smooth step trajectory with parabolic arc
                float smoothT = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 current = Vector3.Lerp(startPos, targetWorldPos, smoothT);
                current.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;
                transform.position = current;

                // Dynamic facing and forward lean while moving
                float forwardLean = Mathf.Sin(progress * Mathf.PI) * 15f;
                Quaternion facing = Quaternion.Slerp(initialRot, targetFacing, Mathf.Clamp01(progress * 2.5f));
                transform.localRotation = facing * Quaternion.Euler(forwardLean, 0f, 0f);

                // Drive full-body locomotion (running legs, swinging arms, flapping wings)
                float locoWeight = Mathf.Sin(progress * Mathf.PI);
                float strideCycle = progress * (dist * 2.5f + 2.0f) * Mathf.PI * 2f;
                SetLocomotion(locoWeight, strideCycle);

                yield return null;
            }

            transform.position = targetWorldPos;
            _baseLocalPosition = transform.localPosition;
            transform.localRotation = _baseLocalRotation;

            ResetCombatPoses();

            if (_moveSparks != null) _moveSparks.Stop();
            _isMoving = false;
            _activeAnimationRoutine = null;

            var done = _onCompleteCallback;
            _onCompleteCallback = null;
            done?.Invoke();
        }

        /// <summary>
        /// Plays an in-place 3D sci-fi combat clash directly on the chessboard.
        /// The attacking piece charges forward, brandishes its signature weapon, executes its finisher,
        /// and settles cleanly into the captured square.
        /// </summary>
        public void AnimateAttackAndCapture(Vector3 defenderWorldPos, MechPieceAnimator defender, Action onImpact, Action onComplete)
        {
            ChessPiece3D selfPiece = GetComponent<ChessPiece3D>();
            ChessPiece3D defPiece = defender != null ? defender.GetComponent<ChessPiece3D>() : null;
            if (CaptureCutsceneDirector.TryPlay(selfPiece, defPiece, onImpact, onComplete)) return;
            AnimateAttackAndCaptureClassic(defenderWorldPos, defender, onImpact, onComplete);
        }

        public void AnimateAttackAndCaptureClassic(Vector3 defenderWorldPos, MechPieceAnimator defender, Action onImpact, Action onComplete)
        {
            SetSelected(false);
            if (_activeAnimationRoutine != null) StopCoroutine(_activeAnimationRoutine);
            _pendingTargetPos = defenderWorldPos;
            _onImpactCallback = onImpact;
            _onCompleteCallback = onComplete;
            _activeAnimationRoutine = StartCoroutine(AttackAndCaptureRoutine(defenderWorldPos, defender, onImpact, onComplete));
        }

        private IEnumerator AttackAndCaptureRoutine(Vector3 defenderWorldPos, MechPieceAnimator defender, Action onImpact, Action onComplete)
        {
            _isMoving = true;
            ResetCombatPoses();
            if (_moveSparks != null) _moveSparks.Play();

            PieceType attType = PieceType;
            PieceType defType = defender != null ? defender.PieceType : PieceType.Pawn;

            Vector3 startPos = transform.position;
            Vector3 toTarget = defenderWorldPos - startPos;
            toTarget.y = 0;
            Vector3 targetDir = toTarget.normalized;
            if (targetDir == Vector3.zero) targetDir = transform.forward;

            Quaternion attackFacingRot = Quaternion.LookRotation(targetDir, Vector3.up);

            // ── Phase 1: Charge towards defender ─────────────────────────────────
            float approachDist = Mathf.Max(0.1f, toTarget.magnitude - 0.42f);
            Vector3 meleePos = startPos + targetDir * approachDist;
            float approachDuration = 0.25f;
            float elapsed = 0f;

            Quaternion initialRot = transform.localRotation;
            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / approachDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                DriveCombatChoreography(attType, CombatRole.Attacker, CombatPhase.Dash, t);
                if (defender != null) defender.DriveCombatChoreography(defType, CombatRole.Defender, CombatPhase.Dash, t);

                Vector3 curPos = Vector3.Lerp(startPos, meleePos, smoothT);
                curPos.y += Mathf.Sin(t * Mathf.PI) * 0.15f;
                transform.position = curPos;

                Quaternion tilt = Quaternion.Euler(14f * Mathf.Sin(t * Mathf.PI), 0, 0);
                transform.localRotation = Quaternion.Slerp(initialRot, attackFacingRot, smoothT) * tilt;

                yield return null;
            }

            transform.position = meleePos;
            transform.localRotation = attackFacingRot;

            // ── Phase 2: Weapon deployment & clash ────────────────────────────────
            GameObject bladeObj = (attType != PieceType.Bishop && attType != PieceType.Rook) ? CreateEnergyBlade() : null;
            if (bladeObj != null)
            {
                bladeObj.transform.SetParent(transform, false);
                _activeBladeObj = bladeObj;
            }

            PlayCombatAudio(CombatAudioType.Slash);

            // ── Phase 3: Signature Finisher (per piece type) ─────────────────────
            float slashDuration = 0.28f;
            elapsed = 0f;
            bool impactTriggered = false;

            Vector3 windupBladePos = new Vector3(0.25f, 0.45f, 0.15f);
            Quaternion windupBladeRot = Quaternion.Euler(-45f, 35f, 25f);
            Vector3 finishBladePos = new Vector3(-0.35f, 0.25f, 0.55f);
            Quaternion finishBladeRot = Quaternion.Euler(45f, -65f, -60f);

            while (elapsed < slashDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / slashDuration);

                DriveCombatChoreography(attType, CombatRole.Attacker, CombatPhase.Finisher, t);
                if (defender != null) defender.DriveCombatChoreography(defType, CombatRole.Defender, CombatPhase.Finisher, t);

                if (bladeObj != null)
                {
                    if (t < 0.30f)
                    {
                        float wT = t / 0.30f;
                        bladeObj.transform.localPosition = Vector3.Lerp(Vector3.zero, windupBladePos, wT);
                        bladeObj.transform.localRotation = Quaternion.Slerp(Quaternion.identity, windupBladeRot, wT);
                        transform.localRotation = attackFacingRot * Quaternion.Euler(-8f * wT, 15f * wT, 0);
                    }
                    else
                    {
                        float sT = (t - 0.30f) / 0.70f;
                        float powerCurve = sT * sT;
                        bladeObj.transform.localPosition = Vector3.Lerp(windupBladePos, finishBladePos, powerCurve);
                        bladeObj.transform.localRotation = Quaternion.Slerp(windupBladeRot, finishBladeRot, powerCurve);
                        transform.localRotation = attackFacingRot * Quaternion.Euler(18f * powerCurve, -25f * powerCurve, 0);
                    }
                }

                if (!impactTriggered && t >= 0.65f)
                {
                    impactTriggered = true;
                    Vector3 hitPoint = Vector3.Lerp(transform.position, defenderWorldPos, 0.65f) + Vector3.up * 0.40f;
                    SpawnSlashImpact(hitPoint, _glowColor);
                    PlayCombatAudio(CombatAudioType.Impact);
                    var impact = onImpact ?? _onImpactCallback;
                    _onImpactCallback = null;
                    impact?.Invoke();
                }

                yield return null;
            }

            if (!impactTriggered)
            {
                impactTriggered = true;
                Vector3 hitPoint = Vector3.Lerp(transform.position, defenderWorldPos, 0.65f) + Vector3.up * 0.40f;
                SpawnSlashImpact(hitPoint, _glowColor);
                PlayCombatAudio(CombatAudioType.Impact);
                var impact = onImpact ?? _onImpactCallback;
                _onImpactCallback = null;
                impact?.Invoke();
            }

            if (bladeObj != null) Destroy(bladeObj);
            _activeBladeObj = null;

            // ── Phase 4: Settle cleanly into square ──────────────────────────────
            float settleDuration = 0.22f;
            elapsed = 0f;
            Vector3 preSettlePos = transform.position;
            Quaternion preSettleRot = transform.localRotation;

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                DriveCombatChoreography(attType, CombatRole.Attacker, CombatPhase.Aftermath, t);
                if (defender != null) defender.DriveCombatChoreography(defType, CombatRole.Defender, CombatPhase.Aftermath, t);

                transform.position = Vector3.Lerp(preSettlePos, defenderWorldPos, smoothT);
                transform.localRotation = Quaternion.Slerp(preSettleRot, _baseLocalRotation, smoothT);
                yield return null;
            }

            ResetCombatPoses();
            if (defender != null) defender.ResetCombatPoses();

            transform.position = defenderWorldPos;
            _baseLocalPosition = transform.localPosition;
            transform.localRotation = _baseLocalRotation;

            if (_moveSparks != null) _moveSparks.Stop();
            _isMoving = false;
            _activeAnimationRoutine = null;

            var done = _onCompleteCallback;
            _onCompleteCallback = null;
            _onImpactCallback = null;
            done?.Invoke();
        }

        internal GameObject CreateEnergyBlade()
        {
            GameObject bladeRoot = new GameObject("CombatEnergyBlade");

            // Glowing Laser Blade Beam
            GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cube);
            beam.name = "LaserBeam";
            beam.transform.SetParent(bladeRoot.transform, false);
            beam.transform.localPosition = new Vector3(0, 0.38f, 0);
            beam.transform.localScale = new Vector3(0.045f, 0.76f, 0.022f);

            Collider col = beam.GetComponent<Collider>();
            if (col != null) Destroy(col);

            MeshRenderer rend = beam.GetComponent<MeshRenderer>();
            Material beamMat = GetBladeGlowMaterial(_glowColor);
            if (rend != null && beamMat != null) rend.material = beamMat;

            // Cyber Hilt
            GameObject hilt = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hilt.name = "BladeHilt";
            hilt.transform.SetParent(bladeRoot.transform, false);
            hilt.transform.localPosition = new Vector3(0, -0.05f, 0);
            hilt.transform.localScale = new Vector3(0.06f, 0.12f, 0.06f);

            Collider hCol = hilt.GetComponent<Collider>();
            if (hCol != null) Destroy(hCol);

            MeshRenderer hRend = hilt.GetComponent<MeshRenderer>();
            Material pMat = GetParticleMaterial();
            if (hRend != null && pMat != null) hRend.material = pMat;

            return bladeRoot;
        }

        private static readonly Dictionary<Color, Material> _bladeMatCache = new Dictionary<Color, Material>();

        private Material GetBladeGlowMaterial(Color color)
        {
            if (_bladeMatCache.TryGetValue(color, out Material cached) && cached != null)
                return cached;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Universal Render Pipeline/Unlit")
                         ?? Shader.Find("Standard")
                         ?? Shader.Find("Sprites/Default");
            Material mat = new Material(shader) { name = "CombatLaserBlade" };
            mat.enableInstancing = true;
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_EmissionColor"))
            {
                mat.EnableKeyword("_EMISSION");
                mat.SetColor("_EmissionColor", color * 2.8f);
            }
            _bladeMatCache[color] = mat;
            return mat;
        }

        internal void SpawnSlashImpact(Vector3 hitPoint, Color color)
        {
            GameObject impactObj = new GameObject("CombatSlashImpact");
            impactObj.transform.position = hitPoint;

            ParticleSystem ps = impactObj.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.35f;
            main.startLifetime = 0.35f;
            main.startSpeed = 5.5f;
            main.startSize = 0.14f;
            main.startColor = color;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.25f;

            var renderer = impactObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                Material mat = GetParticleMaterial();
                if (mat != null) renderer.material = mat;
            }

            ps.Play();
            Destroy(impactObj, 0.55f);
        }

        internal enum CombatAudioType { Slash, Impact }
        private static AudioSource _combatAudioSource;
        private static AudioClip _slashClip;
        private static AudioClip _impactClip;

        internal static void PlayCombatAudio(CombatAudioType type)
        {
            try
            {
                if (_combatAudioSource == null)
                {
                    GameObject audioObj = new GameObject("Combat_SFX_Source");
                    GameObject.DontDestroyOnLoad(audioObj);
                    _combatAudioSource = audioObj.AddComponent<AudioSource>();
                    _combatAudioSource.playOnAwake = false;
                    _combatAudioSource.spatialBlend = 0f;
                    _combatAudioSource.volume = 0.55f;

                    _slashClip = SynthesizeTone(1600f, 320f, 0.14f, 0.6f, isNoise: true);
                    _impactClip = SynthesizeTone(120f, 850f, 0.20f, 0.7f, isNoise: false);
                }

                AudioClip clip = (type == CombatAudioType.Slash) ? _slashClip : _impactClip;
                if (_combatAudioSource != null && clip != null)
                {
                    _combatAudioSource.PlayOneShot(clip);
                }
            }
            catch { }
        }

        private static AudioClip SynthesizeTone(float startFreq, float endFreq, float duration, float volume, bool isNoise)
        {
            int samples = (int)(44100 * duration);
            AudioClip clip = AudioClip.Create("CombatSfx", samples, 1, 44100, false);
            float[] data = new float[samples];
            for (int i = 0; i < samples; i++)
            {
                float t = (float)i / samples;
                float freq = Mathf.Lerp(startFreq, endFreq, t);
                float env = Mathf.Sin(t * Mathf.PI);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * ((float)i / 44100f));
                if (isNoise)
                {
                    float noise = (UnityEngine.Random.value * 2f - 1f) * 0.35f;
                    data[i] = (tone * 0.65f + noise) * env * volume;
                }
                else
                {
                    float sub = Mathf.Sin(2f * Mathf.PI * (freq * 0.5f) * ((float)i / 44100f)) * 0.4f;
                    data[i] = (tone + sub) * env * volume;
                }
            }
            clip.SetData(data, 0);
            return clip;
        }

        /// <summary>
        /// Triggers sci-fi explosion and energy disintegration upon piece capture.
        /// Immediately disables colliders so the dying piece cannot receive raycasts or selection.
        /// </summary>
        public void PlayCaptureDestruction(Action onDone = null)
        {
            if (_isDestroyed) return;
            _isDestroyed = true;

            // Immediately disable all colliders so piece stops receiving clicks/taps
            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null) colliders[i].enabled = false;
            }

            ChessPiece3D piece = GetComponent<ChessPiece3D>();
            if (piece != null) piece.isCaptured = true;

            StartCoroutine(DestructionRoutine(onDone));
        }

        private void OnDestroy()
        {
            if (_activeBladeObj != null) Destroy(_activeBladeObj);
            if (_selectionHologramObj != null) Destroy(_selectionHologramObj);
            _onCompleteCallback = null;
            _onImpactCallback = null;
        }

        private IEnumerator DestructionRoutine(Action onDone)
        {
            // Spawn procedural explosion particle burst
            SpawnExplosionBurst(transform.position, _glowColor);

            // Disintegrate pieces: shrink and shatter
            float elapsed = 0f;
            float duration = 0.55f;
            Vector3 originalScale = transform.localScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                transform.localScale = Vector3.Lerp(originalScale, Vector3.zero, t * t);
                transform.Rotate(Vector3.up, 360f * Time.deltaTime * 3f);
                yield return null;
            }

            onDone?.Invoke();
            Destroy(gameObject);
        }

        /// <summary>
        /// EMP Shockwave reaction (flickering energy and shudder).
        /// </summary>
        public void TriggerEMPShock()
        {
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(EMPShockRoutine());
            }
        }

        private IEnumerator EMPShockRoutine()
        {
            Vector3 originalPos = transform.localPosition;
            float elapsed = 0f;
            float duration = 1.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float shakeMagnitude = (1f - (elapsed / duration)) * 0.08f;
                Vector3 shake = UnityEngine.Random.insideUnitSphere * shakeMagnitude;
                shake.y = Mathf.Abs(shake.y) * 0.5f;
                transform.localPosition = originalPos + shake;
                yield return null;
            }

            transform.localPosition = originalPos;
        }

        public Color GlowColor { get { return _glowColor; } }
        public Quaternion BaseLocalRotation { get { return _baseLocalRotation; } }

        /// <summary>Hands control of this piece's transform to the capture cutscene.</summary>
        public void BeginCutscene(bool thrusters)
        {
            SetSelected(false);
            if (_activeAnimationRoutine != null) { StopCoroutine(_activeAnimationRoutine); _activeAnimationRoutine = null; }
            _onImpactCallback = null;
            _onCompleteCallback = null;
            _isMoving = true;
            if (thrusters && _moveSparks != null) _moveSparks.Play();
        }

        /// <summary>Returns control to the idle animation and rests the piece on the given square.</summary>
        public void EndCutscene(Vector3 restWorldPos)
        {
            if (this == null) return;
            transform.position = restWorldPos;
            _baseLocalPosition = transform.localPosition;
            transform.localRotation = _baseLocalRotation;
            if (_moveSparks != null) _moveSparks.Stop();
            _isMoving = false;
        }

        private void CreateMoveSparkParticles()
        {
            GameObject pObj = new GameObject("ThrusterSparks");
            pObj.transform.SetParent(transform, false);
            pObj.transform.localPosition = new Vector3(0, 0.15f, 0);

            _moveSparks = pObj.AddComponent<ParticleSystem>();
            var main = _moveSparks.main;
            main.startLifetime = 0.35f;
            main.startSpeed = 1.2f;
            main.startSize = 0.08f;
            main.startColor = _glowColor;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.playOnAwake = false;
            main.stopAction = ParticleSystemStopAction.None;

            var emission = _moveSparks.emission;
            emission.rateOverTime = 35;

            var shape = _moveSparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var renderer = pObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material mat = GetParticleMaterial();
            if (mat != null) renderer.material = mat;

            // Make sure it's not playing at idle — only plays during movement
            _moveSparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }


        private static Material _particleMat;
        internal static Material GetParticleMaterial()
        {
            if (_particleMat == null)
            {
                Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit")
                             ?? Shader.Find("Universal Render Pipeline/Unlit")
                             ?? Shader.Find("Particles/Standard Unlit")
                             ?? Shader.Find("Sprites/Default");
                if (shader != null)
                {
                    _particleMat = new Material(shader);
                }
            }
            return _particleMat;
        }

        private void SpawnExplosionBurst(Vector3 worldPos, Color color)
        {
            GameObject explosion = new GameObject("MechExplosionEffect");
            explosion.transform.position = worldPos;

            ParticleSystem ps = explosion.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false;
            main.duration = 0.6f;
            main.startLifetime = 0.8f;
            main.startSpeed = 4.5f;
            main.startSize = 0.18f;
            main.startColor = color;
            main.stopAction = ParticleSystemStopAction.Destroy;

            var emission = ps.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 60) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.4f;

            var renderer = explosion.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
            {
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                Material mat = GetParticleMaterial();
                if (mat != null) renderer.material = mat;
            }

            ps.Play();
            Destroy(explosion, 1.2f);
        }
    }
}

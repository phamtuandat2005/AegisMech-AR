using System;
using System.Collections;
using UnityEngine;
using MechChess.Chess3D;
using MechChess.Mobile;

namespace MechChess.ProceduralMech
{
    /// <summary>
    /// Cinematic capture cutscene (about 4 seconds). The attacker dashes in, the defender counter-attacks,
    /// the blades clash, then the attacker performs a finisher unique to its piece type.
    /// The camera zooms in, time slows before and after the impact, then the camera returns to the player view.
    /// Click/tap during the cutscene to skip it. Toggle with CaptureCutsceneDirector.Enabled.
    /// </summary>
    public class CaptureCutsceneDirector : MonoBehaviour
    {
        public const string PrefKey = "Aegis_CaptureCutscene";

        public static bool Enabled
        {
            get { return PlayerPrefs.GetInt(PrefKey, 1) == 1; }
            set { PlayerPrefs.SetInt(PrefKey, value ? 1 : 0); PlayerPrefs.Save(); }
        }

        private static CaptureCutsceneDirector _instance;
        public static bool IsPlaying { get { return _instance != null && _instance._playing; } }

        // Timeline in scene seconds (real time is longer because of the slow motion windows)
        private const float A_END = 0.60f;   // dash in + camera push
        private const float B_END = 1.35f;   // defender counter-attack and blade clash
        private const float C_END = 2.30f;   // signature finisher, impact at the end
        private const float D_END = 2.65f;   // aftermath
        private const float TOTAL = 3.10f;   // camera returns

        private bool _playing;
        private Coroutine _routine;
        private ChessPiece3D _attP, _defP;
        private Transform _att, _def;
        private MechPieceAnimator _attAnim, _defAnim;
        private Action _onImpact, _onComplete;
        private bool _impactFired, _clashFired;
        private int _miniMask;
        private float _savedTimeScale = 1f;
        private float _hitStop, _shake, _jolt;

        private Vector3 _f, _r, _attStart, _attEngage, _defPos, _mid, _lastAttPos;
        private Quaternion _attStartRot, _defStartRot, _face, _defFace, _lastAttRot;
        private float _S = 1f, _restY;
        private PieceType _type;
        private GameObject _attBlade, _defBlade, _orb, _beam;

        private Camera _cam;
        private MobileTouchInput _touch;
        private bool _touchWasEnabled;
        private Vector3 _camPos0;
        private Quaternion _camRot0;
        private float _fov0;
        private float[] _kT;
        private Vector3[] _kPos;
        private Quaternion[] _kRot;
        private float[] _kFov;

        // ------------------------------------------------------------------
        public static bool TryPlay(ChessPiece3D attacker, ChessPiece3D defender, Action onImpact, Action onComplete)
        {
            if (!Application.isPlaying || !Enabled) return false;
            if (attacker == null || defender == null) return false;
            if (attacker.Animator == null || defender.Animator == null) return false;
            if (_instance == null)
            {
                GameObject go = new GameObject("CaptureCutsceneDirector");
                _instance = go.AddComponent<CaptureCutsceneDirector>();
            }
            if (_instance._playing) return false;
            _instance.Begin(attacker, defender, onImpact, onComplete);
            return true;
        }

        public static void SkipActive()
        {
            if (_instance != null && _instance._playing) _instance.Finish(true);
        }

        private void OnDisable()
        {
            if (_playing)
            {
                Time.timeScale = _savedTimeScale;
                if (_touch != null) _touch.enabled = _touchWasEnabled;
                if (_attAnim != null) _attAnim.ResetCombatPoses();
                if (_defAnim != null) _defAnim.ResetCombatPoses();
                _playing = false;
            }
        }

        // ------------------------------------------------------------------
        private void Begin(ChessPiece3D attacker, ChessPiece3D defender, Action onImpact, Action onComplete)
        {
            _attP = attacker; _defP = defender;
            _att = attacker.transform; _def = defender.transform;
            _attAnim = attacker.Animator; _defAnim = defender.Animator;
            _onImpact = onImpact; _onComplete = onComplete;
            _type = attacker.pieceType;
            _impactFired = false; _clashFired = false; _miniMask = 0;
            _hitStop = 0f; _shake = 0f; _jolt = 0f;

            _S = 1f;
            if (_att.parent != null) _S = Mathf.Max(0.01f, Mathf.Abs(_att.parent.lossyScale.y));
            _defPos = _def.position;
            _restY = _defPos.y;
            _attStart = _att.position;
            Vector3 flat = _defPos - _attStart; flat.y = 0f;
            if (flat.sqrMagnitude < 0.0001f) { flat = _att.forward; flat.y = 0f; }
            if (flat.sqrMagnitude < 0.0001f) flat = Vector3.forward;
            _f = flat.normalized;
            _r = Vector3.Cross(Vector3.up, _f);
            _attEngage = _defPos - _f * (1.15f * _S);
            _attEngage.y = _restY;
            _attStartRot = _att.rotation; _defStartRot = _def.rotation;
            _face = Quaternion.LookRotation(_f, Vector3.up);
            _defFace = Quaternion.LookRotation(-_f, Vector3.up);
            _mid = (_attEngage + _defPos) * 0.5f;
            _lastAttPos = _attStart; _lastAttRot = _attStartRot;

            _attAnim.BeginCutscene(true);
            _defAnim.BeginCutscene(false);

            _cam = Camera.main;
            _touch = (_cam != null) ? _cam.GetComponent<MobileTouchInput>() : null;
            if (_cam != null && _touch != null && !_cam.orthographic)
            {
                _camPos0 = _cam.transform.position;
                _camRot0 = _cam.transform.rotation;
                _fov0 = _cam.fieldOfView;
                _touchWasEnabled = _touch.enabled;
                _touch.enabled = false;
                BuildCameraKeys();
            }
            else { _cam = null; _touch = null; }

            _savedTimeScale = Time.timeScale;
            if (_savedTimeScale < 0.5f) _savedTimeScale = 1f;
            _playing = true;
            _routine = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            MechPieceAnimator.PlayCombatAudio(MechPieceAnimator.CombatAudioType.Slash);
            float t = 0f;
            float rate = 1f;
            while (t < TOTAL)
            {
                if (_att == null || _attAnim == null) { Finish(true); yield break; }
                float udt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);

                if (_hitStop > 0f)
                {
                    _hitStop -= udt;
                    Time.timeScale = 0.03f;
                    PoseCamera(t);
                    yield return null;
                    continue;
                }

                rate = Mathf.MoveTowards(rate, SlowRate(t), udt * 10f);
                Time.timeScale = rate;
                t = Mathf.Min(TOTAL, t + udt * rate);
                _jolt = Mathf.MoveTowards(_jolt, 0f, udt * 0.8f);

                PosePieces(t);
                PoseCamera(t);
                yield return null;
            }
            Finish(false);
        }

        private static float SlowRate(float t)
        {
            if (t >= 1.15f && t < 1.30f) return 0.4f;   // blade clash
            if (t >= 2.05f && t < 2.50f) return 0.4f;   // finisher + impact
            return 1f;
        }

        private void Finish(bool skipped)
        {
            if (!_playing) return;
            _playing = false;
            if (_routine != null) { StopCoroutine(_routine); _routine = null; }
            Time.timeScale = _savedTimeScale;

            if (!_impactFired)
            {
                _impactFired = true;
                try { if (_onImpact != null) _onImpact(); } catch (Exception ex) { Debug.LogException(ex); }
            }
            _onImpact = null;

            CleanupFx();
            if (_attAnim != null)
            {
                _attAnim.ResetCombatPoses();
                _attAnim.EndCutscene(_defPos);
            }
            if (_defAnim != null) _defAnim.ResetCombatPoses();

            if (_cam != null)
            {
                _cam.transform.SetPositionAndRotation(_camPos0, _camRot0);
                _cam.fieldOfView = _fov0;
            }
            if (_touch != null) _touch.enabled = _touchWasEnabled;

            Action done = _onComplete;
            _onComplete = null;
            try { if (done != null) done(); } catch (Exception ex) { Debug.LogException(ex); }
        }

        // ------------------------------------------------------------------
        // Camera
        // ------------------------------------------------------------------
        private void BuildCameraKeys()
        {
            _kT = new float[] { 0f, A_END, B_END, C_END, D_END, TOTAL };
            _kPos = new Vector3[6]; _kRot = new Quaternion[6]; _kFov = new float[6];
            _kPos[0] = _camPos0; _kRot[0] = _camRot0; _kFov[0] = _fov0;
            SetKey(1, 55f, 3.6f, 1.5f, 52f);
            SetKey(2, 80f, 2.5f, 1.0f, 44f);
            SetKey(3, 105f, 1.9f, 0.85f, 36f);
            SetKey(4, 125f, 2.8f, 1.3f, 46f);
            _kPos[5] = _camPos0; _kRot[5] = _camRot0; _kFov[5] = _fov0;
        }

        private void SetKey(int i, float angle, float dist, float height, float fov)
        {
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * (-_f);
            Vector3 pos = _mid + dir * (dist * _S) + Vector3.up * (height * _S);
            Vector3 look = _mid + Vector3.up * (0.5f * _S);
            _kPos[i] = pos;
            _kRot[i] = Quaternion.LookRotation(look - pos, Vector3.up);
            _kFov[i] = fov;
        }

        private void PoseCamera(float t)
        {
            if (_cam == null) return;
            int i = 0;
            while (i < 4 && t > _kT[i + 1]) i++;
            float seg = _kT[i + 1] - _kT[i];
            float u = seg > 0f ? Mathf.Clamp01((t - _kT[i]) / seg) : 1f;
            float e = Mathf.SmoothStep(0f, 1f, u);
            Vector3 p = Vector3.Lerp(_kPos[i], _kPos[i + 1], e);
            Quaternion q = Quaternion.Slerp(_kRot[i], _kRot[i + 1], e);
            float fov = Mathf.Lerp(_kFov[i], _kFov[i + 1], e);
            if (_shake > 0.001f)
            {
                p += UnityEngine.Random.insideUnitSphere * (_shake * _S);
                _shake = Mathf.MoveTowards(_shake, 0f, Time.unscaledDeltaTime * 0.5f);
            }
            _cam.transform.SetPositionAndRotation(p, q);
            _cam.fieldOfView = fov;
        }

        // ------------------------------------------------------------------
        // Choreography
        // ------------------------------------------------------------------
        private static float Sm(float x) { x = Mathf.Clamp01(x); return x * x * (3f - 2f * x); }

        private void PosePieces(float t)
        {
            if (t < A_END) PoseA(t / A_END);
            else if (t < B_END) PoseB((t - A_END) / (B_END - A_END));
            else if (t < C_END) PoseC((t - B_END) / (C_END - B_END));
            else
            {
                if (!_impactFired) FireImpact();
                if (t < D_END) PoseD((t - C_END) / (D_END - C_END));
                else PoseEnd();
            }
        }

        private void SetDefender(float offset, float up)
        {
            if (_def == null) return;
            Vector3 p = _defPos + _f * ((offset + _jolt) * _S) + Vector3.up * (up * _S);
            _def.SetPositionAndRotation(p, _defFace);
        }

        private void EnsureBlades()
        {
            if (_attBlade == null) { _attBlade = _attAnim.CreateEnergyBlade(); _attBlade.transform.SetParent(_att, false); }
            if (_defBlade == null && _defAnim != null) { _defBlade = _defAnim.CreateEnergyBlade(); _defBlade.transform.SetParent(_def, false); }
        }

        private static void SetBlade(GameObject b, float pitch, float yaw, float scale)
        {
            if (b == null) return;
            b.transform.localPosition = new Vector3(0.22f, 0.55f, 0.28f);
            b.transform.localRotation = Quaternion.Euler(pitch, yaw, 0f);
            b.transform.localScale = Vector3.one * scale;
        }

        // Phase A: dash in, defender turns to face the attacker
        private void PoseA(float u)
        {
            float e = Sm(u);
            Vector3 p = Vector3.Lerp(_attStart, _attEngage, e);
            p.y = Mathf.Lerp(_attStart.y, _restY, e) + Mathf.Sin(u * Mathf.PI) * 0.18f * _S;
            float turn = Sm(u * 1.6f);
            Quaternion r = Quaternion.Slerp(_attStartRot, _face, turn) * Quaternion.Euler(14f * Mathf.Sin(u * Mathf.PI), 0f, 0f);
            _att.SetPositionAndRotation(p, r);
            _def.SetPositionAndRotation(_defPos, Quaternion.Slerp(_defStartRot, _defFace, turn));

            if (_attAnim != null) _attAnim.DriveCombatChoreography(_type, MechPieceAnimator.CombatRole.Attacker, MechPieceAnimator.CombatPhase.Dash, u);
            if (_defAnim != null) _defAnim.DriveCombatChoreography(_defP != null ? _defP.pieceType : PieceType.Pawn, MechPieceAnimator.CombatRole.Defender, MechPieceAnimator.CombatPhase.Dash, u);
        }

        // Phase B: the defender counter-attacks and the blades clash
        private void PoseB(float u)
        {
            EnsureBlades();
            float dOff;
            if (u < 0.45f) dOff = Mathf.Lerp(0f, 0.25f, Sm(u / 0.45f));
            else if (u < 0.78f) dOff = Mathf.Lerp(0.25f, -0.40f, Sm((u - 0.45f) / 0.33f));
            else dOff = Mathf.Lerp(-0.40f, 0f, Sm((u - 0.78f) / 0.22f));

            float aOff;
            if (u < 0.40f) aOff = Mathf.Lerp(0f, -0.08f, Sm(u / 0.40f));
            else if (u < 0.78f) aOff = Mathf.Lerp(-0.08f, 0.40f, Sm((u - 0.40f) / 0.38f));
            else aOff = Mathf.Lerp(0.40f, 0f, Sm((u - 0.78f) / 0.22f));

            _att.SetPositionAndRotation(_attEngage + _f * (aOff * _S), _face);
            SetDefender(dOff, 0f);

            float ap, ay, dp, dy, k;
            if (u < 0.40f) { ap = 55f; ay = 0f; }
            else if (u < 0.78f) { k = Sm((u - 0.40f) / 0.38f); ap = Mathf.Lerp(55f, 95f, k); ay = Mathf.Lerp(0f, 25f, k); }
            else { k = Sm((u - 0.78f) / 0.22f); ap = Mathf.Lerp(95f, 70f, k); ay = Mathf.Lerp(25f, 0f, k); }

            if (u < 0.45f) { k = Sm(u / 0.45f); dp = Mathf.Lerp(70f, 130f, k); dy = Mathf.Lerp(0f, 20f, k); }
            else if (u < 0.78f) { k = Sm((u - 0.45f) / 0.33f); dp = Mathf.Lerp(130f, 95f, k); dy = Mathf.Lerp(20f, -25f, k); }
            else { k = Sm((u - 0.78f) / 0.22f); dp = Mathf.Lerp(95f, 70f, k); dy = Mathf.Lerp(-25f, 0f, k); }

            SetBlade(_attBlade, ap, ay, 1f);
            SetBlade(_defBlade, dp, dy, 1f);

            if (_attAnim != null) _attAnim.DriveCombatChoreography(_type, MechPieceAnimator.CombatRole.Attacker, MechPieceAnimator.CombatPhase.Clash, u);
            if (_defAnim != null) _defAnim.DriveCombatChoreography(_defP != null ? _defP.pieceType : PieceType.Pawn, MechPieceAnimator.CombatRole.Defender, MechPieceAnimator.CombatPhase.Clash, u);

            if (!_clashFired && u >= 0.78f) { _clashFired = true; OnClash(); }
        }

        // Phase C: the attacker's signature finisher
        private void PoseC(float u)
        {
            EnsureBlades();
            float fo = 0f, lo = 0f, yo = 0f, tilt = 0f, spin = 0f, bp = 70f, by = 0f, bs = 1f;
            float orbScale = 0f, beamW = 0f;
            bool aim = false;

            switch (_type)
            {
                case PieceType.Pawn:
                {
                    float w = Sm(u / 0.6f);
                    float s = Mathf.Clamp01((u - 0.6f) / 0.4f); s = s * s;
                    fo = -0.35f * w + 1.15f * s;
                    bp = Mathf.Lerp(Mathf.Lerp(70f, 115f, w), 88f, s);
                    tilt = 10f * s;
                    break;
                }
                case PieceType.Knight:
                {
                    if (u < 0.5f)
                    {
                        float v = u / 0.5f, sv = Sm(v);
                        fo = -0.6f * sv; yo = 1.25f * Mathf.Sin(v * Mathf.PI * 0.5f);
                        spin = 360f * sv; tilt = -12f * sv; bp = Mathf.Lerp(70f, 15f, sv);
                    }
                    else
                    {
                        float v = (u - 0.5f) / 0.5f, e = v * v;
                        fo = Mathf.Lerp(-0.6f, 0.75f, e); yo = 1.25f * (1f - e);
                        tilt = Mathf.Lerp(-12f, 28f, e); bp = Mathf.Lerp(15f, 150f, e);
                    }
                    break;
                }
                case PieceType.Bishop:
                {
                    if (u < 0.7f)
                    {
                        float v = u / 0.7f;
                        yo = 0.35f * Sm(v); fo = -0.2f * Sm(v);
                        orbScale = 0.05f + 0.32f * v * (1f + 0.15f * Mathf.Sin(u * 50f));
                    }
                    else
                    {
                        float v = (u - 0.7f) / 0.3f;
                        yo = 0.35f; fo = -0.2f - 0.25f * Sm(v);
                        orbScale = 0.37f; beamW = Mathf.Lerp(0.22f, 0.10f, v);
                    }
                    break;
                }
                case PieceType.Rook:
                {
                    if (u < 0.55f)
                    {
                        float v = Sm(u / 0.55f);
                        fo = -0.95f * v; tilt = -6f * v; bp = Mathf.Lerp(70f, 120f, v);
                        yo = 0.035f * Mathf.Abs(Mathf.Sin(u * 38f)) * (1f - v);
                    }
                    else
                    {
                        float v = (u - 0.55f) / 0.45f, e = v * v * v;
                        fo = Mathf.Lerp(-0.95f, 0.85f, e); tilt = Mathf.Lerp(-6f, 24f, v); bp = Mathf.Lerp(120f, 100f, v);
                    }
                    break;
                }
                case PieceType.Queen:
                {
                    aim = true;
                    if (u < 0.28f)
                    {
                        float v = u / 0.28f, sv = Sm(v);
                        lo = -0.9f * sv; fo = 0.25f * sv; bp = Mathf.Lerp(70f, 85f, sv); by = Mathf.Lerp(0f, -60f, sv);
                        if (v > 0.85f) MiniHit(1);
                    }
                    else if (u < 0.56f)
                    {
                        float v = (u - 0.28f) / 0.28f, sv = Sm(v);
                        lo = Mathf.Lerp(-0.9f, 0.9f, sv); fo = Mathf.Lerp(0.25f, 0.30f, sv); bp = 85f; by = Mathf.Lerp(-60f, 60f, sv);
                        if (v > 0.85f) MiniHit(2);
                    }
                    else if (u < 0.80f)
                    {
                        float v = (u - 0.56f) / 0.24f, sv = Sm(v);
                        lo = Mathf.Lerp(0.9f, 0f, sv); fo = Mathf.Lerp(0.30f, -0.10f, sv);
                        yo = 0.9f * Mathf.Sin(v * Mathf.PI * 0.5f); spin = 360f * sv;
                        bp = Mathf.Lerp(85f, 20f, sv); by = Mathf.Lerp(60f, 0f, sv);
                    }
                    else
                    {
                        float v = (u - 0.80f) / 0.20f, e = v * v;
                        fo = Mathf.Lerp(-0.10f, 0.80f, e); yo = 0.9f * (1f - e);
                        tilt = Mathf.Lerp(0f, 28f, e); bp = Mathf.Lerp(20f, 150f, e);
                    }
                    break;
                }
                case PieceType.King:
                {
                    if (u < 0.7f)
                    {
                        float v = Sm(u / 0.7f);
                        yo = 0.55f * v; fo = -0.3f * v; tilt = -10f * v;
                        bp = Mathf.Lerp(70f, 12f, v); bs = Mathf.Lerp(1f, 1.7f, v);
                    }
                    else
                    {
                        float v = (u - 0.7f) / 0.3f, e = v * v * v;
                        yo = 0.55f * (1f - e); fo = Mathf.Lerp(-0.3f, 0.75f, e);
                        tilt = Mathf.Lerp(-10f, 30f, e); bp = Mathf.Lerp(12f, 165f, e); bs = 1.7f;
                    }
                    break;
                }
            }

            Vector3 pos = _attEngage + _f * (fo * _S) + _r * (lo * _S) + Vector3.up * (yo * _S);
            Quaternion rot = _face;
            if (aim)
            {
                Vector3 d = _defPos - pos; d.y = 0f;
                if (d.sqrMagnitude > 0.0001f) rot = Quaternion.LookRotation(d.normalized, Vector3.up);
            }
            rot = rot * Quaternion.Euler(tilt, spin, 0f);
            _att.SetPositionAndRotation(pos, rot);
            _lastAttPos = pos; _lastAttRot = rot;

            if (_attBlade != null) _attBlade.SetActive(_type != PieceType.Bishop);
            SetBlade(_attBlade, bp, by, bs);

            float fl = Sm((u - 0.8f) / 0.2f);
            SetDefender(0.18f * fl, 0f);
            SetBlade(_defBlade, Mathf.Lerp(70f, 35f, fl), 0f, 1f);

            if (_type == PieceType.Bishop)
            {
                EnsureOrb();
                Vector3 orbPos = pos + rot * (new Vector3(0.08f, 0.85f, 0.6f) * _S);
                _orb.transform.position = orbPos;
                _orb.transform.localScale = Vector3.one * (orbScale * _S);
                if (beamW > 0f)
                {
                    EnsureBeam();
                    Vector3 end = _defPos + Vector3.up * (0.6f * _S);
                    Vector3 dir = end - orbPos;
                    _beam.transform.position = orbPos + dir * 0.5f;
                    _beam.transform.rotation = Quaternion.LookRotation(dir.normalized, Vector3.up);
                    _beam.transform.localScale = new Vector3(beamW * _S, beamW * _S, dir.magnitude);
                }
            }

            if (_attAnim != null) _attAnim.DriveCombatChoreography(_type, MechPieceAnimator.CombatRole.Attacker, MechPieceAnimator.CombatPhase.Finisher, u);
            if (_defAnim != null) _defAnim.DriveCombatChoreography(_defP != null ? _defP.pieceType : PieceType.Pawn, MechPieceAnimator.CombatRole.Defender, MechPieceAnimator.CombatPhase.Finisher, u);
        }

        // Phase D: aftermath, defender is knocked back while it disintegrates
        private void PoseD(float u)
        {
            if (_attBlade != null) { Destroy(_attBlade); _attBlade = null; }
            if (_orb != null) { Destroy(_orb); _orb = null; }
            if (u > 0.4f && _beam != null) { Destroy(_beam); _beam = null; }

            float e = Sm(u);
            _att.SetPositionAndRotation(Vector3.Lerp(_lastAttPos, _defPos, e), Quaternion.Slerp(_lastAttRot, BaseWorldRot(), e));
            if (_def != null)
            {
                Vector3 p = _defPos + _f * (0.7f * _S * e) + Vector3.up * (Mathf.Sin(e * Mathf.PI) * 0.25f * _S);
                _def.position = p;
            }

            if (_attAnim != null) _attAnim.DriveCombatChoreography(_type, MechPieceAnimator.CombatRole.Attacker, MechPieceAnimator.CombatPhase.Aftermath, u);
            if (_defAnim != null) _defAnim.DriveCombatChoreography(_defP != null ? _defP.pieceType : PieceType.Pawn, MechPieceAnimator.CombatRole.Defender, MechPieceAnimator.CombatPhase.Aftermath, u);
        }

        private void PoseEnd()
        {
            CleanupFx();
            if (_attAnim != null) _attAnim.ResetCombatPoses();
            if (_defAnim != null) _defAnim.ResetCombatPoses();
            _att.SetPositionAndRotation(_defPos, BaseWorldRot());
        }

        private Quaternion BaseWorldRot()
        {
            Quaternion local = _attAnim.BaseLocalRotation;
            return _att.parent != null ? _att.parent.rotation * local : local;
        }

        // ------------------------------------------------------------------
        // Events and effects
        // ------------------------------------------------------------------
        private void OnClash()
        {
            Vector3 pt = (_att.position + _def.position) * 0.5f + Vector3.up * (0.6f * _S);
            _attAnim.SpawnSlashImpact(pt, Color.Lerp(_attAnim.GlowColor, Color.white, 0.5f));
            if (_defAnim != null) _defAnim.SpawnSlashImpact(pt, _defAnim.GlowColor);
            Flash(pt, Color.white, 6f, 0.25f);
            _shake = Mathf.Max(_shake, 0.10f);
            _hitStop = 0.08f;
            MechPieceAnimator.PlayCombatAudio(MechPieceAnimator.CombatAudioType.Slash);
            MechPieceAnimator.PlayCombatAudio(MechPieceAnimator.CombatAudioType.Impact);
        }

        private void MiniHit(int bit)
        {
            if ((_miniMask & bit) != 0) return;
            _miniMask |= bit;
            Vector3 p = _defPos + Vector3.up * (0.6f * _S) + _r * (UnityEngine.Random.Range(-0.15f, 0.15f) * _S);
            _attAnim.SpawnSlashImpact(p, _attAnim.GlowColor);
            MechPieceAnimator.PlayCombatAudio(MechPieceAnimator.CombatAudioType.Slash);
            _shake = Mathf.Max(_shake, 0.07f);
            _jolt = 0.12f;
        }

        private void FireImpact()
        {
            _impactFired = true;
            Vector3 chest = _defPos + Vector3.up * (0.6f * _S);
            Vector3 ground = _defPos + Vector3.up * (0.05f * _S);
            Color glow = _attAnim.GlowColor;
            float shake = 0.16f, stop = 0.10f, light = 8f;

            _attAnim.SpawnSlashImpact(chest, glow);
            switch (_type)
            {
                case PieceType.Knight: Ring(ground, glow, 4f, 40, 0.09f); shake = 0.20f; break;
                case PieceType.Bishop: _attAnim.SpawnSlashImpact(chest + Vector3.up * (0.2f * _S), Color.white); break;
                case PieceType.Rook: Ring(ground, glow, 6.5f, 70, 0.12f); shake = 0.30f; stop = 0.13f; break;
                case PieceType.Queen: _attAnim.SpawnSlashImpact(chest, Color.white); _attAnim.SpawnSlashImpact(chest + _r * (0.15f * _S), glow); shake = 0.22f; break;
                case PieceType.King:
                    Ring(ground, glow, 8f, 90, 0.14f);
                    Ring(ground + Vector3.up * (0.15f * _S), Color.white, 6f, 50, 0.10f);
                    shake = 0.40f; stop = 0.16f; light = 14f; break;
            }
            Flash(chest, glow, light, 0.35f);
            _shake = Mathf.Max(_shake, shake);
            _hitStop = stop;
            MechPieceAnimator.PlayCombatAudio(MechPieceAnimator.CombatAudioType.Impact);

            try { if (_onImpact != null) { Action a = _onImpact; _onImpact = null; a(); } }
            catch (Exception ex) { Debug.LogException(ex); }
        }

        private void Flash(Vector3 pos, Color c, float intensity, float life)
        {
            GameObject g = new GameObject("CutsceneFlash");
            g.transform.position = pos;
            Light l = g.AddComponent<Light>();
            l.type = LightType.Point; l.color = c; l.range = 5f * _S; l.intensity = intensity;
            StartCoroutine(FadeLight(l, intensity, life));
        }

        private IEnumerator FadeLight(Light l, float i0, float life)
        {
            float e = 0f;
            while (e < life && l != null)
            {
                e += Time.unscaledDeltaTime;
                l.intensity = Mathf.Lerp(i0, 0f, e / life);
                yield return null;
            }
            if (l != null) Destroy(l.gameObject);
        }

        private void Ring(Vector3 pos, Color c, float speed, int count, float size)
        {
            GameObject g = new GameObject("CutsceneRing");
            g.transform.position = pos;
            g.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
            ParticleSystem ps = g.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.loop = false; main.duration = 0.4f; main.startLifetime = 0.45f;
            main.startSpeed = speed * _S; main.startSize = size * _S; main.startColor = c;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.stopAction = ParticleSystemStopAction.Destroy;
            var emission = ps.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)count) });
            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.05f;
            ParticleSystemRenderer rend = g.GetComponent<ParticleSystemRenderer>();
            if (rend != null)
            {
                rend.renderMode = ParticleSystemRenderMode.Billboard;
                Material m = MechPieceAnimator.GetParticleMaterial();
                if (m != null) rend.material = m;
            }
            ps.Play();
            Destroy(g, 1.0f);
        }

        private static Material GlowMat(Color c)
        {
            Shader sh = Shader.Find("Universal Render Pipeline/Unlit");
            if (sh == null) sh = Shader.Find("Sprites/Default");
            if (sh == null) sh = Shader.Find("Standard");
            Material m = new Material(sh);
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
            if (m.HasProperty("_Color")) m.SetColor("_Color", c);
            return m;
        }

        private GameObject MakePrimitive(PrimitiveType type, string name, Color c)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            Collider col = go.GetComponent<Collider>();
            if (col != null) { col.enabled = false; Destroy(col); }
            MeshRenderer mr = go.GetComponent<MeshRenderer>();
            if (mr != null) mr.material = GlowMat(c);
            return go;
        }

        private void EnsureOrb()
        {
            if (_orb == null) _orb = MakePrimitive(PrimitiveType.Sphere, "CutsceneOrb", Color.Lerp(_attAnim.GlowColor, Color.white, 0.35f));
        }

        private void EnsureBeam()
        {
            if (_beam == null) _beam = MakePrimitive(PrimitiveType.Cube, "CutsceneBeam", Color.Lerp(_attAnim.GlowColor, Color.white, 0.5f));
        }

        private void CleanupFx()
        {
            if (_attBlade != null) Destroy(_attBlade);
            if (_defBlade != null) Destroy(_defBlade);
            if (_orb != null) Destroy(_orb);
            if (_beam != null) Destroy(_beam);
            _attBlade = null; _defBlade = null; _orb = null; _beam = null;
        }
    }
}

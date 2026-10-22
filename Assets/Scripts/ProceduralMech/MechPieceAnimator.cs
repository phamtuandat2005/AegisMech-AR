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
            if (_isMoving || _isDestroyed) return;

            // Idle Hovering / Breathing Sine Wave with preserved base orientation
            float t = (Time.time + _timeOffset) * hoverFrequency;
            float yOffset = Mathf.Sin(t) * hoverAmplitude;
            float roll = Mathf.Sin(t * 0.75f) * swayAngle;
            float pitch = Mathf.Cos(t * 0.5f) * (swayAngle * 0.6f);

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
            if (_moveSparks != null) _moveSparks.Play();

            Vector3 startPos = transform.position;
            float elapsed = 0f;
            float jumpHeight = 0.65f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float progress = Mathf.Clamp01(elapsed / duration);

                // Smooth step trajectory with parabolic arc
                float smoothT = Mathf.SmoothStep(0f, 1f, progress);
                Vector3 current = Vector3.Lerp(startPos, targetWorldPos, smoothT);
                current.y += Mathf.Sin(progress * Mathf.PI) * jumpHeight;

                transform.position = current;
                yield return null;
            }

            transform.position = targetWorldPos;
            _baseLocalPosition = transform.localPosition;
            transform.localRotation = _baseLocalRotation;

            if (_moveSparks != null) _moveSparks.Stop();
            _isMoving = false;
            _activeAnimationRoutine = null;

            var done = _onCompleteCallback;
            _onCompleteCallback = null;
            done?.Invoke();
        }

        /// <summary>
        /// Plays an in-place 3D sci-fi combat clash directly on the chessboard.
        /// The attacking piece charges forward, brandishes an energy blade, slashes the target,
        /// and settles cleanly into the captured square.
        /// </summary>
        public void AnimateAttackAndCapture(Vector3 defenderWorldPos, MechPieceAnimator defender, Action onImpact, Action onComplete)
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
            if (_moveSparks != null) _moveSparks.Play();

            Vector3 startPos = transform.position;
            Vector3 toTarget = defenderWorldPos - startPos;
            toTarget.y = 0;
            Vector3 targetDir = toTarget.normalized;
            if (targetDir == Vector3.zero) targetDir = transform.forward;

            Quaternion attackFacingRot = Quaternion.LookRotation(targetDir, Vector3.up);

            // Phase 1: Charge towards defender (stopping at melee slash distance)
            float approachDist = Mathf.Max(0.1f, toTarget.magnitude - 0.42f);
            Vector3 meleePos = startPos + targetDir * approachDist;
            float approachDuration = 0.22f;
            float elapsed = 0f;

            Quaternion initialRot = transform.localRotation;
            while (elapsed < approachDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / approachDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                Vector3 curPos = Vector3.Lerp(startPos, meleePos, smoothT);
                curPos.y += Mathf.Sin(t * Mathf.PI) * 0.15f;
                transform.position = curPos;

                Quaternion tilt = Quaternion.Euler(14f * Mathf.Sin(t * Mathf.PI), 0, 0);
                transform.localRotation = Quaternion.Slerp(initialRot, attackFacingRot, smoothT) * tilt;

                yield return null;
            }

            transform.position = meleePos;
            transform.localRotation = attackFacingRot;

            // Phase 2: Brandish energy blade
            GameObject bladeObj = CreateEnergyBlade();
            bladeObj.transform.SetParent(transform, false);
            _activeBladeObj = bladeObj;

            PlayCombatAudio(CombatAudioType.Slash);

            // Phase 3: Wind-up & Slash across target
            float slashDuration = 0.24f;
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

                    if (!impactTriggered && sT >= 0.45f)
                    {
                        impactTriggered = true;
                        Vector3 hitPoint = Vector3.Lerp(transform.position, defenderWorldPos, 0.65f) + Vector3.up * 0.40f;
                        SpawnSlashImpact(hitPoint, _glowColor);
                        PlayCombatAudio(CombatAudioType.Impact);
                        var impact = onImpact ?? _onImpactCallback;
                        _onImpactCallback = null;
                        impact?.Invoke();
                    }
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

            // Phase 4: Settle cleanly into the target square
            float settleDuration = 0.20f;
            elapsed = 0f;
            Vector3 preSettlePos = transform.position;
            Quaternion preSettleRot = transform.localRotation;

            while (elapsed < settleDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / settleDuration);
                float smoothT = Mathf.SmoothStep(0f, 1f, t);

                transform.position = Vector3.Lerp(preSettlePos, defenderWorldPos, smoothT);
                transform.localRotation = Quaternion.Slerp(preSettleRot, _baseLocalRotation, smoothT);
                yield return null;
            }

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

        private GameObject CreateEnergyBlade()
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

        private void SpawnSlashImpact(Vector3 hitPoint, Color color)
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

        private enum CombatAudioType { Slash, Impact }
        private static AudioSource _combatAudioSource;
        private static AudioClip _slashClip;
        private static AudioClip _impactClip;

        private static void PlayCombatAudio(CombatAudioType type)
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

            var emission = _moveSparks.emission;
            emission.rateOverTime = 35;

            var shape = _moveSparks.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.15f;

            var renderer = pObj.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            Material mat = GetParticleMaterial();
            if (mat != null) renderer.material = mat;
        }

        private static Material _particleMat;
        private static Material GetParticleMaterial()
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

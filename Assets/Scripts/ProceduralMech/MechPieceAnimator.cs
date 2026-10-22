using System;
using System.Collections;
using UnityEngine;

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

        private void Update()
        {
            if (_isMoving || _isDestroyed) return;

            // Idle Hovering / Breathing Sine Wave with preserved base orientation
            float t = (Time.time + _timeOffset) * hoverFrequency;
            float yOffset = Mathf.Sin(t) * hoverAmplitude;
            float roll = Mathf.Sin(t * 0.75f) * swayAngle;
            float pitch = Mathf.Cos(t * 0.5f) * (swayAngle * 0.6f);

            transform.localPosition = _baseLocalPosition + new Vector3(0, yOffset, 0);
            transform.localRotation = _baseLocalRotation * Quaternion.Euler(pitch, 0, roll);
        }

        /// <summary>
        /// Moves the mech smoothly from current position to target board position with an arc.
        /// </summary>
        public void AnimateMoveTo(Vector3 targetWorldPos, float duration, Action onComplete = null)
        {
            StartCoroutine(MoveRoutine(targetWorldPos, duration, onComplete));
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

            onComplete?.Invoke();
        }

        /// <summary>
        /// Triggers sci-fi explosion and energy disintegration upon piece capture.
        /// </summary>
        public void PlayCaptureDestruction(Action onDone = null)
        {
            if (_isDestroyed) return;
            _isDestroyed = true;
            StartCoroutine(DestructionRoutine(onDone));
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
        }
    }
}

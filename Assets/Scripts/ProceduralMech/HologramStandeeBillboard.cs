using UnityEngine;

namespace MechChess.ProceduralMech
{
    /// <summary>
    /// Smoothly rotates a 2.5D Hologram Standee around the Y-axis to face the active camera,
    /// keeping the character standing perfectly upright at all times in 3D and AR views.
    /// </summary>
    public class HologramStandeeBillboard : MonoBehaviour
    {
        [SerializeField] private float turnSpeed = 12f;
        private Camera _mainCamera;

        private void Start()
        {
            _mainCamera = Camera.main;
        }

        private void LateUpdate()
        {
            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
                if (_mainCamera == null) return;
            }

            Vector3 camPos = _mainCamera.transform.position;
            Vector3 lookTarget = new Vector3(camPos.x, transform.position.y, camPos.z);
            Vector3 direction = lookTarget - transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * turnSpeed);
            }
        }
    }
}

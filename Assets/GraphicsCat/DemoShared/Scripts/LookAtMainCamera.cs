using UnityEngine;

namespace GraphicsCat
{
    [ExecuteAlways]
    public class LookAtMainCamera : MonoBehaviour
    {
        public enum AlignMode { Full, YAxisOnly }

        [Tooltip("Full: face camera on all axes. YAxisOnly: only rotate around world Y to face camera.")]
        public AlignMode alignMode = AlignMode.Full;

        [Tooltip("Local self-rotation speed in degrees per second on each axis.")]
        public Vector3 selfRotationDegreesPerSecond = Vector3.zero;

        [Tooltip("Optional euler offset (applied after aligning to camera).")]
        public Vector3 rotationOffsetEuler = Vector3.zero;

        [Tooltip("Use LateUpdate for camera-following (recommended if camera moves in Update).")]
        public bool useLateUpdate = true;

        [Tooltip("If true, the look direction will be inverted (useful if your mesh faces -Z by default).")]
        public bool invertLookDirection = true;

        [Tooltip("If set, this camera will be used instead of Camera.main.")]
        public Camera overrideCamera;

        // cached camera transform (will be updated if main camera changes)
        Transform targetCameraTransform;

        void OnEnable()
        {
            TryCacheCamera();
        }

        void TryCacheCamera()
        {
            if (overrideCamera != null)
            {
                targetCameraTransform = overrideCamera.transform;
                return;
            }

            if (Camera.main != null)
                targetCameraTransform = Camera.main.transform;
        }

        void Update()
        {
            if (!useLateUpdate)
                AlignToCameraAndSelfRotate();
        }

        void LateUpdate()
        {
            if (useLateUpdate)
                AlignToCameraAndSelfRotate();
        }

        void AlignToCameraAndSelfRotate()
        {
            if (targetCameraTransform == null)
                TryCacheCamera();

            // 1) Align to camera
            if (targetCameraTransform != null)
            {
                Vector3 camPos = targetCameraTransform.position;
                Vector3 toCam = camPos - transform.position;

                // optionally invert direction if front faces -Z
                if (invertLookDirection)
                    toCam = -toCam;

                if (alignMode == AlignMode.Full)
                {
                    if (toCam.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                }
                else // YAxisOnly
                {
                    toCam.y = 0f;
                    if (toCam.sqrMagnitude > 0.0001f)
                        transform.rotation = Quaternion.LookRotation(toCam.normalized, Vector3.up);
                }

                // apply an optional rotation offset after facing the camera
                if (rotationOffsetEuler != Vector3.zero)
                    transform.rotation *= Quaternion.Euler(rotationOffsetEuler);
            }

            // 2) Apply local self-rotation (spin) in local space
            if (selfRotationDegreesPerSecond != Vector3.zero)
            {
                transform.Rotate(selfRotationDegreesPerSecond * Time.deltaTime, Space.Self);
            }
        }
    }
}

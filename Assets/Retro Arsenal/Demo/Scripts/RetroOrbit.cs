using UnityEngine;

namespace RetroArsenal
{
    public class RetroOrbit : MonoBehaviour
    {
        public Transform target;
        public Vector3 cameraOffset = new Vector3(-2f, 0.5f, 0f);
        public float defaultDistance = 5.0f;
        private float _currentDistance = 5.0f;
        public float xSpeed = 1.0f;
        public float ySpeed = 1.0f;
        public float yMinLimit = -20f;
        public float yMaxLimit = 80f;
        public float distanceMin = .5f;
        public float distanceMax = 15f;
        public float zoomLerpSpeed = 2f;
        public float smoothingFactor = 2f;
        public float collisionOffset = 0.2f; // Optional offset from collision point towards the target
        float rotationYAxis = 0.0f;
        float rotationXAxis = 0.0f;
        float velocityX = 0.0f;
        float velocityY = 0.0f;
        private Vector3 originalTargetPosition;

        void Start()
        {
            Vector3 angles = transform.eulerAngles;
            rotationYAxis = angles.y;
            rotationXAxis = angles.x;

            if (GetComponent<Rigidbody>())
                GetComponent<Rigidbody>().freezeRotation = true;

            _currentDistance = defaultDistance;
            originalTargetPosition = target.position;
        }

        void LateUpdate()
        {
            if (!target) return;

            if (Input.GetMouseButton(1))
            {
                velocityX += xSpeed * Input.GetAxis("Mouse X") * _currentDistance * 0.02f;
                velocityY += ySpeed * Input.GetAxis("Mouse Y") * 0.02f;
            }

            rotationYAxis += velocityX;
            rotationXAxis -= velocityY;
            rotationXAxis = Mathf.Clamp(rotationXAxis, yMinLimit, yMaxLimit);

            Quaternion toRotation = Quaternion.Euler(rotationXAxis, rotationYAxis, 0);
            Quaternion rotation = toRotation;

            defaultDistance = Mathf.Clamp(defaultDistance - Input.GetAxis("Mouse ScrollWheel") * 10, distanceMin, distanceMax);
            float targetDistance = defaultDistance;

            RaycastHit hit;
            Vector3 cameraTargetDirection = transform.position - target.position;
            if (Physics.Raycast(target.position, cameraTargetDirection, out hit, defaultDistance))
            {
                if (!(hit.collider.gameObject.name.Contains("Missile") || hit.collider.gameObject.name.Contains("Obstacle")))
                {
                    targetDistance = hit.distance - collisionOffset;
                }
            }

            _currentDistance = Mathf.Lerp(_currentDistance, targetDistance, Time.deltaTime * zoomLerpSpeed);

            Vector3 negDistance = new Vector3(0.0f, 0.0f, -_currentDistance);
            Vector3 position = rotation * negDistance + target.position + (rotation * cameraOffset);

            transform.rotation = rotation;
            transform.position = position;
            velocityX = Mathf.Lerp(velocityX, 0, Time.deltaTime * smoothingFactor);
            velocityY = Mathf.Lerp(velocityY, 0, Time.deltaTime * smoothingFactor);
        }
    }
}
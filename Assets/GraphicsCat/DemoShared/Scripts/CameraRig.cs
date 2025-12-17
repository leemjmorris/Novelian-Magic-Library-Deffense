using UnityEngine;

namespace GraphicsCat
{
    public partial class CameraRig : MonoBehaviour, IMGUIDockable
    {
        public enum RigType
        {
            Free,
            Topdown,
        }

        const int k_SpaceHeight = 6;

        [Space(k_SpaceHeight)]
        public RigType rigType = RigType.Free;

        [Space(k_SpaceHeight)]
        public float moveSpeed = 10f;
        public float rotateSpeed = 0.1f;
        public float scrollSpeed = 100f;
        public float dragSpeed = 15f;

        [Space(k_SpaceHeight)]
        public Transform freeCameraTarget;

        [Space(k_SpaceHeight)]
        public bool guiEnabled = false;

        private bool m_EnableCtrl = true;

        private Transform m_CacheTransform;
        private Vector3 m_DefaultPos;
        private Quaternion m_DefaultRotation;

        // Touch variables needed for initialization
        private Touch m_MoveTouch_Start;
        private Touch m_MoveTouch_Current;

        private Vector3 m_LastRotateMousePos;
        private Vector3 m_LastDragMousePos;
        private float m_MoveSpeedBuff = 0;

        private Vector3 m_TopDownCameraMoveDelta;

        private void Start()
        {
            m_CacheTransform = transform;

            m_DefaultPos = m_CacheTransform.position;
            m_DefaultRotation = m_CacheTransform.rotation;

            m_MoveTouch_Start.fingerId = int.MinValue;

            IMGUIDock.topRight.DockGUI(this);
        }

        private void Update()
        {
            if (m_EnableCtrl == false)
                return;

            if (Application.isMobilePlatform)
                UpdateMobile();
            else
                UpdateDesktop();
        }

        public void OnDockGUI()
        {
            if (!guiEnabled)
                return;

            GUILayout.BeginHorizontal();
            {
                var btnTex = "";
                if (m_EnableCtrl)
                {
                    switch (rigType)
                    {
                        case RigType.Topdown: btnTex = "TopdownCamera"; break;
                        case RigType.Free: btnTex = "FreeCamera"; break;
                    }
                }
                else
                {
                    btnTex = "Lock";
                }

                if (GUILayout.Button(btnTex))
                {
                    if (m_EnableCtrl == false)
                    {
                        m_EnableCtrl = true;
                        rigType = RigType.Topdown;
                    }
                    else if (rigType == RigType.Topdown)
                        rigType = RigType.Free;
                    else if (rigType == RigType.Free)
                        m_EnableCtrl = false;
                }

                bool defaultStatus = true;
                defaultStatus &= (m_CacheTransform.position == m_DefaultPos);
                defaultStatus &= (m_CacheTransform.rotation == m_DefaultRotation);
                GUI.enabled = !defaultStatus;
                if (GUILayout.Button("Reset"))
                {
                    m_CacheTransform.position = m_DefaultPos;
                    m_CacheTransform.rotation = m_DefaultRotation;
                }
                GUI.enabled = true;
            }
            GUILayout.EndHorizontal();
        }

        private void UpdateDesktop()
        {
            var moveLeft = false;
            var moveRight = false;
            var moveUp = false;
            var moveDown = false;
            var moveForward = false;
            var moveBackward = false;
            var moveHorizontalForward = false;
            var moveHorizontalBackward = false;

            var isTopDownType = rigType == RigType.Topdown;
            var isFreeType = (rigType == RigType.Free);

            if (isFreeType && freeCameraTarget != null)
            {
                moveLeft = Input.GetKey(KeyCode.A);
                moveRight = Input.GetKey(KeyCode.D);
                moveForward = Input.GetKey(KeyCode.W);
                moveBackward = Input.GetKey(KeyCode.S);
                moveUp = Input.GetKey(KeyCode.E);
                moveDown = Input.GetKey(KeyCode.Q);
            }
            else if (isTopDownType)
            {
                moveLeft = Input.GetKey(KeyCode.A);
                moveRight = Input.GetKey(KeyCode.D);
                moveForward = Input.GetKey(KeyCode.Q);
                moveBackward = Input.GetKey(KeyCode.E);
                moveHorizontalForward = Input.GetKey(KeyCode.W);
                moveHorizontalBackward = Input.GetKey(KeyCode.S);
            }

            var moveDir = Vector3.zero;
            if (moveForward)
                moveDir = (moveDir.normalized + m_CacheTransform.forward).normalized;
            else if (moveBackward)
                moveDir = (moveDir.normalized - m_CacheTransform.forward).normalized;
            if (moveLeft)
                moveDir = (moveDir.normalized - m_CacheTransform.right).normalized;
            else if (moveRight)
                moveDir = (moveDir.normalized + m_CacheTransform.right).normalized;
            if (moveUp)
                moveDir = (moveDir.normalized + Vector3.up).normalized;
            else if (moveDown)
                moveDir = (moveDir.normalized - Vector3.up).normalized;

            if (moveHorizontalForward)
                moveDir = (moveDir.normalized + GetHorizontalForward()).normalized;
            else if (moveHorizontalBackward)
                moveDir = (moveDir.normalized - GetHorizontalForward()).normalized;

            if (moveDir != Vector3.zero)
            {
                var deltaTime = Time.smoothDeltaTime;
                m_MoveSpeedBuff += deltaTime;

                if (isFreeType && freeCameraTarget != null)
                {
                    // For orbit camera, move the target object instead of the camera
                    // Calculate the offset between camera and target before moving the target
                    Vector3 targetToCam = m_CacheTransform.position - freeCameraTarget.position;

                    // Move the target object
                    freeCameraTarget.position += moveDir * moveSpeed * deltaTime * m_MoveSpeedBuff;

                    // Also move the camera to maintain the same relative position to target
                    m_CacheTransform.position = freeCameraTarget.position + targetToCam;
                }
                else
                {
                    // For other camera types, move the camera directly
                    m_CacheTransform.position += moveDir * moveSpeed * deltaTime * m_MoveSpeedBuff;
                }
            }
            else
                m_MoveSpeedBuff = 1;

            if (isFreeType && freeCameraTarget != null)
            {
                // Free camera specific controls
                if (Input.GetMouseButtonDown(0)) // Left mouse button for orbit rotation
                    m_LastRotateMousePos = Input.mousePosition;
                else if (Input.GetMouseButtonDown(1)) // Right mouse button for target rotation around camera
                    m_LastRotateMousePos = Input.mousePosition;
                else if (Input.GetMouseButtonDown(2)) // Middle mouse button for target movement
                    m_LastDragMousePos = Input.mousePosition;

                if (Input.GetMouseButton(0)) // Left mouse button held down for orbit rotation
                {
                    var rotateMouseDelta = (Input.mousePosition - m_LastRotateMousePos);
                    if (rotateMouseDelta.sqrMagnitude > float.Epsilon)
                    {
                        // Calculate the vector from target to camera
                        Vector3 targetToCamera = m_CacheTransform.position - freeCameraTarget.position;
                        float distance = targetToCamera.magnitude;

                        // Rotate around target
                        m_CacheTransform.RotateAround(freeCameraTarget.position, Vector3.up, rotateMouseDelta.x * rotateSpeed);
                        // Use negative right axis to fix vertical rotation direction
                        m_CacheTransform.RotateAround(freeCameraTarget.position, -m_CacheTransform.right, rotateMouseDelta.y * rotateSpeed);

                        // Maintain the same distance from target
                        m_CacheTransform.position = freeCameraTarget.position + (m_CacheTransform.position - freeCameraTarget.position).normalized * distance;

                        m_LastRotateMousePos = Input.mousePosition;
                    }
                }
                else if (Input.GetMouseButton(1)) // Right mouse button held down for target rotation around camera
                {
                    var rotateMouseDelta = (Input.mousePosition - m_LastRotateMousePos);
                    if (rotateMouseDelta.sqrMagnitude > float.Epsilon)
                    {
                        // Calculate rotation angles
                        float horizontalRotation = rotateMouseDelta.x * rotateSpeed;
                        // Add inversion to fix vertical rotation direction
                        float verticalRotation = -rotateMouseDelta.y * rotateSpeed;

                        // Get the vector from camera to target
                        Vector3 cameraToTarget = freeCameraTarget.position - m_CacheTransform.position;

                        // Create a rotation matrix using camera's local coordinate system
                        // First, create a reference up vector that's perpendicular to camera's forward
                        Vector3 referenceUp = Vector3.up;
                        if (Vector3.Dot(m_CacheTransform.forward, Vector3.up) > 0.99f || Vector3.Dot(m_CacheTransform.forward, Vector3.up) < -0.99f)
                        {
                            // Use camera's right as reference when looking straight up or down
                            referenceUp = m_CacheTransform.right;
                        }

                        // Calculate camera's right and up vectors that are perpendicular
                        Vector3 cameraRight = Vector3.Cross(referenceUp, m_CacheTransform.forward).normalized;
                        Vector3 cameraUp = Vector3.Cross(m_CacheTransform.forward, cameraRight).normalized;

                        // Create quaternions for the rotations
                        Quaternion horizontalQuat = Quaternion.AngleAxis(horizontalRotation, cameraUp);
                        Quaternion verticalQuat = Quaternion.AngleAxis(verticalRotation, cameraRight);

                        // Combine rotations
                        Quaternion totalRotation = horizontalQuat * verticalQuat;

                        // Apply rotation to the direction vector
                        Vector3 newCameraToTarget = totalRotation * cameraToTarget;

                        // Update target position
                        freeCameraTarget.position = m_CacheTransform.position + newCameraToTarget;

                        // Make camera look at the target to keep it centered
                        m_CacheTransform.LookAt(freeCameraTarget.position);

                        m_LastRotateMousePos = Input.mousePosition;
                    }
                }
                else if (Input.GetMouseButton(2)) // Middle mouse button held down for target movement
                {
                    var mouseDelta = (Input.mousePosition - m_LastDragMousePos);
                    if (mouseDelta.sqrMagnitude > float.Epsilon)
                    {
                        // Calculate movement direction relative to camera plane
                        Vector3 moveDirection = Vector3.zero;

                        // Get camera's right direction (horizontal movement)
                        moveDirection -= m_CacheTransform.right * mouseDelta.x;

                        // Get camera's up direction (vertical movement)
                        moveDirection -= m_CacheTransform.up * mouseDelta.y;

                        // Normalize to ensure consistent movement speed
                        if (moveDirection.sqrMagnitude > 0)
                            moveDirection.Normalize();

                        // Calculate the offset between camera and target before moving the target
                        Vector3 targetToCam = m_CacheTransform.position - freeCameraTarget.position;

                        // Move the target object
                        freeCameraTarget.position += moveDirection * dragSpeed * Time.deltaTime;

                        // Also move the camera to maintain the same relative position to target
                        m_CacheTransform.position = freeCameraTarget.position + targetToCam;

                        m_LastDragMousePos = Input.mousePosition;
                    }
                }

                // Zoom in/out with mouse wheel - move towards/away from target
                if (Input.mouseScrollDelta.y != 0 && IsMouseOnScreen())
                {
                    Vector3 targetToCam = m_CacheTransform.position - freeCameraTarget.position;
                    float distance = targetToCam.magnitude;

                    // Calculate new distance based on scroll input
                    float newDistance = distance - (Input.mouseScrollDelta.y * scrollSpeed * Time.deltaTime);

                    // Ensure minimum distance to prevent camera from going inside the target
                    newDistance = Mathf.Max(newDistance, 0.1f);

                    // Update camera position while maintaining direction from target
                    m_CacheTransform.position = freeCameraTarget.position + targetToCam.normalized * newDistance;
                }
            }
            else if (isTopDownType)
            {
                // Topdown camera specific controls
                if (Input.mouseScrollDelta.y != 0 && IsMouseOnScreen()) // Mouse wheel for zoom
                    m_CacheTransform.position += m_CacheTransform.forward * scrollSpeed * Input.mouseScrollDelta.y * Time.deltaTime;
                // Left mouse button for panning (same as WASD keys)
                else if (Input.GetMouseButtonDown(0)) // Left mouse button down
                    m_LastDragMousePos = Input.mousePosition;
                else if (Input.GetMouseButton(0)) // Left mouse button held down for panning
                {
                    var mouseDelta = (Input.mousePosition - m_LastDragMousePos);
                    if (mouseDelta.sqrMagnitude > float.Epsilon)
                    {
                        // Calculate movement direction based on mouse delta (same logic as WASD keys)
                        Vector3 dragDirection = Vector3.zero;

                        // Horizontal movement (A/D keys equivalent)
                        dragDirection -= m_CacheTransform.right * mouseDelta.x;

                        // Forward/backward movement (W/S keys equivalent)
                        dragDirection -= GetHorizontalForward() * mouseDelta.y;

                        // Normalize and apply movement
                        if (dragDirection.sqrMagnitude > 0)
                            dragDirection.Normalize();

                        m_CacheTransform.position += dragDirection * moveSpeed * Time.smoothDeltaTime * m_MoveSpeedBuff;
                        m_LastDragMousePos = Input.mousePosition;
                    }
                }
                // Middle mouse button panning removed for Topdown camera
                // to avoid conflict with left mouse button panning
            }
        }

        private void UpdateMobile()
        {
            if (rigType == RigType.Topdown)
                UpdateMobile_TopDownCamera();
            else if (rigType == RigType.Free)
                UpdateMobile_FreeCamera();
        }

        private void UpdateMobile_FreeCamera()
        {
            if (freeCameraTarget == null)
                return;

            var touches = Input.touches;
            foreach (var touch in touches)
            {
                if (touch.phase == TouchPhase.Began)
                    m_MoveTouch_Start = touch;
                else if (touch.phase == TouchPhase.Ended)
                {
                    if (m_MoveTouch_Start.fingerId == touch.fingerId)
                        m_MoveTouch_Start.fingerId = int.MinValue;
                }
            }

            m_MoveTouch_Current.fingerId = int.MinValue;
            foreach (var touch in touches)
            {
                if (touch.fingerId == m_MoveTouch_Start.fingerId)
                    m_MoveTouch_Current = touch;
            }

            if (m_MoveTouch_Current.fingerId != int.MinValue)
            {
                var rotateStrength = (m_MoveTouch_Current.position - m_MoveTouch_Start.position) / Screen.width;
                if (rotateStrength != Vector2.zero)
                {
                    // Calculate the vector from target to camera
                    Vector3 targetToCam = m_CacheTransform.position - freeCameraTarget.position;
                    float distance = targetToCam.magnitude;

                    var deltaTime = Time.smoothDeltaTime;
                    rotateStrength *= rotateSpeed * deltaTime * 1000;

                    // Rotate around target
                    m_CacheTransform.RotateAround(freeCameraTarget.position, Vector3.up, rotateStrength.x);
                    m_CacheTransform.RotateAround(freeCameraTarget.position, -m_CacheTransform.right, rotateStrength.y);

                    // Maintain the same distance from target
                    m_CacheTransform.position = freeCameraTarget.position + (m_CacheTransform.position - freeCameraTarget.position).normalized * distance;
                }
            }
        }

        private void UpdateMobile_TopDownCamera()
        {
            var touches = Input.touches;
            foreach (var touch in touches)
            {
                if (touch.phase == TouchPhase.Began)
                    m_MoveTouch_Start = touch;
                else if (touch.phase == TouchPhase.Ended)
                {
                    if (m_MoveTouch_Start.fingerId == touch.fingerId)
                        m_MoveTouch_Start.fingerId = int.MinValue;
                }
            }

            m_MoveTouch_Current.fingerId = int.MinValue;
            foreach (var touch in touches)
            {
                if (touch.fingerId == m_MoveTouch_Start.fingerId)
                    m_MoveTouch_Current = touch;
            }

            if (m_MoveTouch_Current.fingerId != int.MinValue)
            {
                if (m_MoveTouch_Current.deltaPosition != Vector2.zero)
                {
                    var cam = GetComponent<Camera>();
                    if (cam)
                    {
                        // Convention:
                        // 1. Focus point: intersection of line of sight and ground
                        // 2. Ground height is 0 (height of focus point is 0)

                        // Function:
                        // Calculate the range of the field of view projected onto the ground (width and height of the ground within the camera's view)
                        // Move the camera by the same amount as the touch movement
                        // Ensure that the object at the touch start point will be exactly at the touch end point, i.e., slide to where you touch

                        var halfFOV = cam.fieldOfView / 2; // Returns vertical FOV
                        var camForward = m_CacheTransform.forward;
                        var camHeight = m_CacheTransform.position.y;
                        var camEulerX = Mathf.Acos(camForward.y) * Mathf.Rad2Deg;

                        var focalPos = m_CacheTransform.position + camForward * (camHeight / (-camForward.y)); // todo: possible division by zero
                        var focalDist = (m_CacheTransform.position - focalPos).magnitude;

                        // tan(fov/2) = (focalPlaneHeight / 2) / focalDist
                        // focalPlaneHeight = tan(fov/2) * focalDist * 2
                        var focalPlaneHeight = Mathf.Tan(Mathf.Deg2Rad * halfFOV) * focalDist * 2;
                        var projWidth = focalPlaneHeight * cam.aspect;

                        var projTop = camHeight * Mathf.Tan(Mathf.Deg2Rad * (camEulerX + halfFOV));
                        var projBottom = camHeight * Mathf.Tan(Mathf.Deg2Rad * (camEulerX - halfFOV));
                        var projHeight = (projTop - projBottom);

                        var movePercentX = m_MoveTouch_Current.deltaPosition.x / Screen.width;
                        var movePercentY = m_MoveTouch_Current.deltaPosition.y / Screen.height;
                        var moveDeltaLeftRight = -movePercentX * projWidth * m_CacheTransform.right;
                        var moveDeltaUpDown = -movePercentY * projHeight * GetScreenSpaceUpDir();
                        m_TopDownCameraMoveDelta = moveDeltaLeftRight + moveDeltaUpDown;

                        m_CacheTransform.position += m_TopDownCameraMoveDelta;
                    }
                }
            }
            else
            {
                m_TopDownCameraMoveDelta *= Mathf.Lerp(1f, 0f, Time.smoothDeltaTime * 5);
                m_CacheTransform.position += m_TopDownCameraMoveDelta;
            }
        }

        private bool IsMouseOnScreen()
        {
            if (Input.mousePosition.x < 0 || Input.mousePosition.x > Screen.width ||
                Input.mousePosition.y < 0 || Input.mousePosition.y > Screen.height)
            {
                return false;
            }
            return true;
        }

        private Vector3 GetScreenSpaceUpDir()
        {
            var forward = m_CacheTransform.forward;
            if (Mathf.Abs(forward.z) < 0.01)
                return m_CacheTransform.up;
            else
            {
                forward.y = 0;
                return forward.normalized;
            }
        }

        private Vector3 GetHorizontalForward()
        {
            var forward = m_CacheTransform.forward;
            forward.y = 0;
            return forward.normalized;
        }
    }
}
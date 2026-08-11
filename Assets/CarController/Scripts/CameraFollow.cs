using UnityEngine;



namespace CameraControlsforcar
{
    public class CameraFollow : MonoBehaviour
    {
        public float moveSmoothness;
        public float rotSmoothness;
        public float orbitSpeed = 5f;
        public float minVerticalAngle = -20f;
        public float maxVerticalAngle = 80f;
        public float zoomSpeed = 5f;
        public float minZoomDistance = 2f;
        public float maxZoomDistance = 15f;

        public Vector3 moveOffset;
        public Vector3 rotOffset;

        public Transform carTarget;

        private float currentHorizontalAngle;
        private float currentVerticalAngle = 30f;
        private float currentZoomDistance;
        private Vector3 baseOffsetDirection;
        private float baseOffsetMagnitude;

        void Start()
        {
            // Store initial zoom distance and offset direction
            currentZoomDistance = moveOffset.magnitude;
            baseOffsetMagnitude = currentZoomDistance;
            
            if (moveOffset != Vector3.zero)
            {
                baseOffsetDirection = moveOffset.normalized;
            }
            else
            {
                baseOffsetDirection = Vector3.back;
            }

            // Initialize orbit angles based on initial camera position
            Vector3 initialDirection = carTarget.position - transform.position;
            currentHorizontalAngle = Mathf.Atan2(initialDirection.x, initialDirection.z) * Mathf.Rad2Deg;
        }

        void Update()
        {
            HandleOrbitInput();
            HandleZoomInput();
        }

        void FixedUpdate()
        {
            FollowTarget();
        }

        void HandleOrbitInput()
        {
            if (Input.GetMouseButton(1)) // Right mouse button held
            {
                currentHorizontalAngle += Input.GetAxis("Mouse X") * orbitSpeed;
                currentVerticalAngle -= Input.GetAxis("Mouse Y") * orbitSpeed;
                currentVerticalAngle = Mathf.Clamp(currentVerticalAngle, minVerticalAngle, maxVerticalAngle);
            }
        }

        void HandleZoomInput()
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                currentZoomDistance -= scroll * zoomSpeed;
                currentZoomDistance = Mathf.Clamp(currentZoomDistance, minZoomDistance, maxZoomDistance);
            }
        }

        void FollowTarget()
        {
            HandleMovement();
            HandleRotation();
        }

        void HandleMovement()
        {
            // Calculate rotated offset direction
            Quaternion horizontalRotation = Quaternion.Euler(0, currentHorizontalAngle, 0);
            Vector3 rotatedDirection = horizontalRotation * baseOffsetDirection;
            
            // Apply vertical rotation to offset direction
            Quaternion verticalRotation = Quaternion.Euler(currentVerticalAngle, 0, 0);
            Vector3 finalDirection = verticalRotation * rotatedDirection;
            
            // Apply zoom distance to the offset
            Vector3 zoomOffset = finalDirection * currentZoomDistance;
            
            // Calculate target position
            Vector3 targetPos = carTarget.TransformPoint(zoomOffset);
            transform.position = Vector3.Lerp(transform.position, targetPos, moveSmoothness * Time.deltaTime);
        }

        void HandleRotation()
        {
            Vector3 lookTarget = carTarget.position + rotOffset;
            Vector3 lookDirection = lookTarget - transform.position;
            
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, rotSmoothness * Time.deltaTime);
        }

        // ✅ Added: SetTarget method
        public void SetTarget(Transform newTarget)
        {
            carTarget = newTarget;
        }

        // ✅ Added: ResetCameraPosition method
        public void ResetCameraPosition()
        {
            currentZoomDistance = baseOffsetMagnitude;

            Quaternion horizontalRotation = Quaternion.Euler(0, currentHorizontalAngle, 0);
            Vector3 rotatedDirection = horizontalRotation * baseOffsetDirection;

            Quaternion verticalRotation = Quaternion.Euler(currentVerticalAngle, 0, 0);
            Vector3 finalDirection = verticalRotation * rotatedDirection;

            Vector3 zoomOffset = finalDirection * currentZoomDistance;

            Vector3 targetPos = carTarget.TransformPoint(zoomOffset);
            transform.position = targetPos;
            transform.LookAt(carTarget.position + rotOffset);
        }
    }
}

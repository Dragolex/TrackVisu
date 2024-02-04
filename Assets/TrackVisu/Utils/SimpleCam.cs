using System;
using UnityEngine;

namespace TrackVisuUtils
{
    public class SimpleCam : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private float smoothTime = 0.25f;
        [SerializeField] private Vector3 offset;
        [SerializeField] private Quaternion rotation;

        [Header("Controls")]
        [SerializeField] private float pixel_limit = 50f; // stops major jump when loosing focus
        [SerializeField] private float mouse_rotation_speed = 5.0f;
        [SerializeField] private float move_speed = 5.0f;
        [SerializeField] private float scroll_speed = 5.0f;


        private Vector3 velocity = Vector3.zero;
        private Vector3 offset_by_controls = Vector3.zero;
        private Quaternion rotation_by_controls = Quaternion.identity;


        internal Transform GetTarget()
        {
            return target;
        }
        internal void SetTarget(Transform target)
        {
            this.target = target;
        }
        internal void ResetVelocity()
        {
            velocity = Vector3.zero;
        }
        public void ResetCameraOffset()
        {
            offset_by_controls = Vector3.zero;
            rotation_by_controls = Quaternion.identity;
        }

        private void Update()
        {
            if (!target)
                return;

            OrientViaMouse();
            MoveViaKeyboard();

            Vector3 targetPosition = target.position + offset + offset_by_controls;
            transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);
            transform.rotation = rotation * rotation_by_controls;
        }

        Vector2 last_mouse_pos;

        private void OrientViaMouse()
        {
            Vector2 mouse_pos = Input.mousePosition;
            Vector2 mouse_movement = mouse_pos - last_mouse_pos;
            mouse_movement.x = Mathf.Min(pixel_limit, Mathf.Max(-pixel_limit, mouse_movement.x));
            mouse_movement.y = Mathf.Min(pixel_limit, Mathf.Max(-pixel_limit, mouse_movement.y));

            // Calculate rotation angles
            float rotationX = mouse_movement.y * Time.deltaTime * mouse_rotation_speed;
            float rotationY = mouse_movement.x * Time.deltaTime * mouse_rotation_speed;

            last_mouse_pos = mouse_pos;

            if (Input.GetMouseButton(1)) // right mouse button only
            {
                // Create quaternion rotations
                Quaternion rotationXQuaternion = Quaternion.AngleAxis(rotationX, Vector3.left);
                Quaternion rotationYQuaternion = Quaternion.AngleAxis(rotationY, Vector3.up);

                // Apply rotations
                rotation_by_controls *= rotationXQuaternion;
                rotation_by_controls *= rotationYQuaternion;
            }
        }
        private void MoveViaKeyboard()
        {
            float x_input = (Input.GetKey(KeyCode.A) ? -1f : 0f) + (Input.GetKey(KeyCode.D) ? 1f : 0f); // Input.GetAxis("Horizontal");
            float y_input = (Input.GetKey(KeyCode.S) ? -1f : 0f) + (Input.GetKey(KeyCode.W) ? 1f : 0f); // Input.GetAxis("Vertical");

            // Calculate movement in X and Y directions
            Vector3 movement = new Vector3(x_input, 0, y_input) * move_speed * Time.deltaTime;

            // Get input for scrolling
            float scrollInput = Input.GetAxis("Mouse ScrollWheel");
            movement += new Vector3(0.0f, 0.0f, scrollInput * scroll_speed * Time.deltaTime);
            movement = transform.rotation * movement;

            offset_by_controls += movement;
        }
    }
}
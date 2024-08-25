using UnityEngine;
using UnityEngine.InputSystem;


namespace AiWorldGeneration.Desktop
{
    /// <summary>
    /// Control a camera that can move and rotate in the space.
    /// </summary>
    public class FreeCameraMovement : MonoBehaviour
    {
        [Tooltip("Camera movement speed")]
        [SerializeField]
        float moveSpeed = 1f;

        [Tooltip("Camera rotation speed")]
        [SerializeField]
        float rotateSpeed = 0.5f;

        [SerializeField] InputActionReference moveActionReference;

        [SerializeField] InputActionReference rotateActionReference;

        Vector2 rotation;

        // <summary>
        /// Moves the camera in the specified direction.
        /// </summary>
        /// <param name="direction">A Vector2 containing the x and z coordinates of the direction to move the camera.</param>
        void Move(Vector2 direction)
        {
            Vector3 moveDirection = new(direction.x, 0, direction.y);
            transform.Translate(moveDirection);
        }

        /// <summary>
        /// Rotates the camera based on the input Mouse X and Mouse Y axes.
        /// </summary>
        /// <param name="angles">A Vector2 containing the angles to rotate the camera on the X and Y axes.</param>
        void Rotate(Vector2 angles)
        {
            rotation += angles;
            rotation.y = Mathf.Clamp(rotation.y, -85, 85);
            var xQuat = Quaternion.AngleAxis(rotation.x, Vector3.up);
            var yQuat = Quaternion.AngleAxis(rotation.y, Vector3.left);

            transform.localRotation = xQuat * yQuat;
        }

        /// <summary>
        /// This method is used to move and rotate the camera based on the input from the player.
        /// </summary>
        void LateUpdate()
        {
            // Move the camera
            Move(
                moveSpeed * Time.deltaTime *
                moveActionReference.action.ReadValue<Vector2>()
            );

            // Rotate the camera
            Rotate(
                rotateSpeed *
                rotateActionReference.action.ReadValue<Vector2>()
            );
        }
    }
}

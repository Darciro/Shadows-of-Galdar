using UnityEngine;
using UnityEngine.InputSystem;

namespace DungeonMaster
{
    public class PlayerInputHandler : MonoBehaviour, PlayerInputActions.IPlayerActions
    {
        private PlayerInputActions inputActions;

        private void Awake()
        {
            inputActions = new PlayerInputActions();
            inputActions.Player.SetCallbacks(this);
        }

        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }

        // Input callbacks from IPlayerActions interface
        public void OnCameraMovement(InputAction.CallbackContext context)
        {
            Vector2 movement = context.ReadValue<Vector2>();
            Debug.Log("Camera Movement: " + movement);
            // Apply movement to camera logic here
        }

        public void OnCameraRotate(InputAction.CallbackContext context)
        {
            float rotation = context.ReadValue<float>();
            Debug.Log("Camera Rotate: " + rotation);
            // Apply rotation to camera logic here
        }

        public void OnCameraZoom(InputAction.CallbackContext context)
        {
            float zoom = context.ReadValue<float>();
            Debug.Log("Camera Zoom: " + zoom);
            // Apply zoom to camera logic here
        }

        public void OnClick(InputAction.CallbackContext context)
        {
            if (context.performed)
            {
                Debug.Log("Click!");
                // Handle click behavior here (e.g. selection, shooting)
            }
        }
    }
}

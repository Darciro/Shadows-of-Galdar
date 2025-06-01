using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Cinemachine;

namespace DungeonMaster
{
    public class CameraController : MonoBehaviour
    {
        private const float MIN_ORTHO_SIZE = 1f;
        private const float MAX_ORTHO_SIZE = 10f;

        [SerializeField] private CinemachineCamera cineCam; // New camera type in Cinemachine 3
        [SerializeField] private float moveSpeed = 5f;
        [SerializeField] private float zoomSpeed = 10f;

        private void Update()
        {
            HandleMovement();
            HandleZoom();
        }

        private void HandleMovement()
        {
            Vector2 inputMoveDir = InputManager.Instance.GetCameraMoveVector();

            Vector3 move = new Vector3(inputMoveDir.x, inputMoveDir.y, 0f) * moveSpeed * Time.deltaTime;
            transform.position += move;
        }

        private void HandleZoom()
        {
            if (InputManager.Instance.GetCameraZoomAmount() != 0f)
            {
                float currentZoom = cineCam.Lens.OrthographicSize;
                currentZoom += InputManager.Instance.GetCameraZoomAmount() * zoomSpeed * Time.deltaTime;
                currentZoom = Mathf.Clamp(currentZoom, MIN_ORTHO_SIZE, MAX_ORTHO_SIZE);
                cineCam.Lens.OrthographicSize = currentZoom;
            }
        }
    }

}
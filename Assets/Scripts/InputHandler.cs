using System.Collections;
using System.Collections.Generic;
using UnityEngine;


    public class InputHandler : MonoBehaviour
    {
        public float horizontal;
        public float vertical;
        public float moveAmount;
        public float mouseX;
        public float mouseY;

        PlayerControls inputActions;

        public bool isSwap;
        public bool mapIsOpen = false;
        Vector2 movementInput;
        Vector2 cameraInput;
    
    public void OnEnable()
        {
            if (inputActions == null)
            {
                inputActions = new PlayerControls();
                inputActions.PlayerMovement.Movement.performed += inputActions => movementInput = inputActions.ReadValue<Vector2>();
                inputActions.PlayerMovement.Camera.performed += i => cameraInput = i.ReadValue<Vector2>();
        }

            inputActions.Enable();
        }

        private void OnDiable()
        {
            inputActions.Disable();
        }

        public void TickInput(float delta)
        {
            MoveInput(delta);
            HandleSwap(delta);
        }

        private void MoveInput(float delta)
        {
            horizontal = movementInput.x;
            vertical = movementInput.y;
            moveAmount = Mathf.Clamp01(Mathf.Abs(horizontal) + Mathf.Abs(vertical));
            mouseX = cameraInput.x;
            mouseY = cameraInput.y;
        }

        private void HandleSwap(float delta)
        {
            inputActions.PlayerMovement.Swap.performed += ctx => isSwap = true;
        }

        private void HandleMap(float delta)
        {
        inputActions.PlayerMovement.Map.performed += ctx => mapIsOpen = true;

        }
}

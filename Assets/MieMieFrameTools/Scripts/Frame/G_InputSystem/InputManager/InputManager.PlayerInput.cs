using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MieMieFrameWork.M_InputSystem
{
    /// <summary>
    /// InputManager Player Map 读取与事件
    /// </summary>
    public partial class InputManager
    {
        #region Player 连续输入

        public Vector2 MoveInput =>
            inputActions != null ? inputActions.Player.Move.ReadValue<Vector2>() : Vector2.zero;

        public bool IsMovePressed => MoveInput.sqrMagnitude > 1e-12f;

        public Vector2 LookInput =>
            inputActions != null ? inputActions.Player.Look.ReadValue<Vector2>() : Vector2.zero;

        public bool IsLookPressed => LookInput.sqrMagnitude > 0.01f;

        #endregion

        #region Player 离散输入

        public bool IsJumpPressed => ReadButtonPressed(inputActions?.Player.Jump);
        public bool IsJumpHeld => ReadButtonHeld(inputActions?.Player.Jump);
        public bool IsJumpReleased => ReadButtonReleased(inputActions?.Player.Jump);

        public bool IsAttackPressed => ReadButtonPressed(inputActions?.Player.Attack);

        public bool IsCrouchPressed => ReadButtonPressed(inputActions?.Player.Crouch);
        public bool IsCrouchHeld => ReadButtonHeld(inputActions?.Player.Crouch);
        public bool IsCrouchReleased => ReadButtonReleased(inputActions?.Player.Crouch);

        public bool IsSprintHeld => ReadButtonHeld(inputActions?.Player.Sprint);
        public bool IsSprintPressed => ReadButtonPressed(inputActions?.Player.Sprint);
        public bool IsSprintReleased => ReadButtonReleased(inputActions?.Player.Sprint);

        /// <summary>
        /// Interact Hold 按下瞬间
        /// </summary>
        public bool IsInteractStarted => ReadButtonPressed(inputActions?.Player.Interact);

        /// <summary>
        /// Interact Hold 达到时长完成
        /// </summary>
        public bool IsInteractHoldCompleted => ReadButtonPerformed(inputActions?.Player.Interact);

        public bool IsInteractHeld => ReadButtonHeld(inputActions?.Player.Interact);
        public bool IsInteractCanceled => ReadButtonReleased(inputActions?.Player.Interact);

        #endregion

        #region Player 输入事件

        public Action OnJumpPressed_Event;
        public Action OnJumpReleased_Event;
        public Action OnInteractStarted_Event;
        public Action OnInteractHoldCompleted_Event;
        public Action OnInteractCanceled_Event;
        public Action OnAttackPressed_Event;
        public Action OnCrouchPressed_Event;
        public Action OnCrouchReleased_Event;
        public Action OnSprintPressed_Event;
        public Action OnSprintReleased_Event;

        #endregion

        #region Player 事件订阅

        private void SubscribePlayerEvents()
        {
            MmInputAciton.PlayerActions playerActions = inputActions.Player;

            playerActions.Jump.started += OnJumpStarted;
            playerActions.Jump.canceled += OnJumpCanceled;

            playerActions.Interact.started += OnInteractStartedCallback;
            playerActions.Interact.performed += OnInteractPerformed;
            playerActions.Interact.canceled += OnInteractCanceledCallback;

            playerActions.Attack.started += OnAttackStarted;

            playerActions.Crouch.started += OnCrouchStarted;
            playerActions.Crouch.canceled += OnCrouchCanceled;

            playerActions.Sprint.started += OnSprintStarted;
            playerActions.Sprint.canceled += OnSprintCanceled;
        }

        private void UnsubscribePlayerEvents()
        {
            if (inputActions == null)
                return;

            MmInputAciton.PlayerActions playerActions = inputActions.Player;

            playerActions.Jump.started -= OnJumpStarted;
            playerActions.Jump.canceled -= OnJumpCanceled;

            playerActions.Interact.started -= OnInteractStartedCallback;
            playerActions.Interact.performed -= OnInteractPerformed;
            playerActions.Interact.canceled -= OnInteractCanceledCallback;

            playerActions.Attack.started -= OnAttackStarted;

            playerActions.Crouch.started -= OnCrouchStarted;
            playerActions.Crouch.canceled -= OnCrouchCanceled;

            playerActions.Sprint.started -= OnSprintStarted;
            playerActions.Sprint.canceled -= OnSprintCanceled;
        }

        private void OnJumpStarted(InputAction.CallbackContext context)
        {
            OnJumpPressed_Event?.Invoke();
        }

        private void OnJumpCanceled(InputAction.CallbackContext context)
        {
            OnJumpReleased_Event?.Invoke();
        }

        private void OnInteractStartedCallback(InputAction.CallbackContext context)
        {
            OnInteractStarted_Event?.Invoke();
        }

        private void OnInteractPerformed(InputAction.CallbackContext context)
        {
            OnInteractHoldCompleted_Event?.Invoke();
        }

        private void OnInteractCanceledCallback(InputAction.CallbackContext context)
        {
            OnInteractCanceled_Event?.Invoke();
        }

        private void OnAttackStarted(InputAction.CallbackContext context)
        {
            OnAttackPressed_Event?.Invoke();
        }

        private void OnCrouchStarted(InputAction.CallbackContext context)
        {
            OnCrouchPressed_Event?.Invoke();
        }

        private void OnCrouchCanceled(InputAction.CallbackContext context)
        {
            OnCrouchReleased_Event?.Invoke();
        }

        private void OnSprintStarted(InputAction.CallbackContext context)
        {
            OnSprintPressed_Event?.Invoke();
        }

        private void OnSprintCanceled(InputAction.CallbackContext context)
        {
            OnSprintReleased_Event?.Invoke();
        }

        #endregion

        private static bool ReadButtonPressed(InputAction action)
        {
            return action != null && action.WasPressedThisFrame();
        }

        private static bool ReadButtonHeld(InputAction action)
        {
            return action != null && action.IsPressed();
        }

        private static bool ReadButtonReleased(InputAction action)
        {
            return action != null && action.WasReleasedThisFrame();
        }

        private static bool ReadButtonPerformed(InputAction action)
        {
            return action != null && action.WasPerformedThisFrame();
        }
    }
}

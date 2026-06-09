using System;
using UnityEngine;
using UnityEngine.InputSystem;
using static MieMieFrameWork.ModuleHub;
namespace MieMieFrameWork.M_InputSystem
{   
/// <summary>
/// 输入管理器 - 封装玩家输入系统
/// </summary>
[ManagerAttribute(9)]
public class InputManager : MonoBehaviour, IManagerBase
{   

    private InputSystem_Default inputActions;
    private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
    private const string RebindsPlayerPrefsKey = "Input_Rebinds";
    
    // 移动输入 (连续输入 - 直接读取) x:ad y:ws
    public Vector2 MoveInput => inputActions.Player.Move.ReadValue<Vector2>();
    public bool IsMovePressed => MoveInput.magnitude >  1e-6f;
    
    // 鼠标/手势输入 (连续输入 - 直接读取)
    public Vector2 LookInput => inputActions.Player.Look.ReadValue<Vector2>();
    public bool IsLookPressed => LookInput.magnitude > 0.1f;
    
    // 按键输入状态 (离散输入 - 事件驱动)
    public bool IsJumpPressed { get; private set; }
    public bool IsJumpHeld { get; private set; }
    public bool IsInteractPressed { get; private set; }
    public bool IsAttackPressed { get; private set; }
    public bool IsCrouchPressed { get; private set; }
    public bool IsCrouchHeld { get; private set; }
    public bool IsSprintHeld { get; private set; }
    
    // 输入事件
    public Action<Vector2> OnMoveInput;
    public Action<Vector2> OnLookInput;
    public Action OnJumpPressed_Event;
    public Action OnJumpReleased_Event;
    public Action OnInteractPressed_Event;
    public Action OnAttackPressed_Event;
    public Action OnCrouchPressed_Event;
    public Action OnCrouchReleased_Event;
    public Action OnSprintPressed_Event;
    public Action OnSprintReleased_Event;

    public void Init()
    {
        // 创建输入系统实例
        inputActions = new InputSystem_Default();
        
        // 加载本地重绑
        LoadRebinds();
        
        // 订阅输入事件
        SubscribeToInputEvents();
        
        // 启用输入
        inputActions.Enable();
    }

    private void SubscribeToInputEvents()
    {
        var playerActions = inputActions.Player;
        
        // // 移动输入事件 (可选 - 用于触发其他逻辑)
        // playerActions.Move.performed += OnMove;
        // playerActions.Move.canceled += OnMoveStop;
        
        // // 视角输入事件 (可选 - 用于触发其他逻辑)
        // playerActions.Look.performed += OnLook;
        // playerActions.Look.canceled += OnLookStop;
        
        // 跳跃输入 (离散输入)
        playerActions.Jump.started += OnJumpStart;
        playerActions.Jump.canceled += OnJumpStop;
        
        // 交互输入 (离散输入)
        playerActions.Interact.started += OnInteract;
        
        // 攻击输入 (离散输入)
        playerActions.Attack.started += OnAttack;
        
        // // 蹲伏输入 (离散输入)
        // playerActions.Crouch.started += OnCrouchStart;
        // playerActions.Crouch.canceled += OnCrouchStop;
        
        // 冲刺输入 (离散输入)
        playerActions.Sprint.started += OnSprintStart;
        playerActions.Sprint.canceled += OnSprintStop;
    }

    #region 输入事件处理
    
    private void OnMove(InputAction.CallbackContext context)
    {
        Vector2 moveInput = context.ReadValue<Vector2>();
        OnMoveInput?.Invoke(moveInput);
    }
    
    private void OnMoveStop(InputAction.CallbackContext context)
    {
        OnMoveInput?.Invoke(Vector2.zero);
    }
    
    private void OnLook(InputAction.CallbackContext context)
    {
        Vector2 lookInput = context.ReadValue<Vector2>();
        OnLookInput?.Invoke(lookInput);
    }
    
    private void OnLookStop(InputAction.CallbackContext context)
    {
        OnLookInput?.Invoke(Vector2.zero);
    }
    
    private void OnJumpStart(InputAction.CallbackContext context)
    {
        IsJumpPressed = true;
        IsJumpHeld = true;
        OnJumpPressed_Event?.Invoke();
    }
    
    private void OnJumpStop(InputAction.CallbackContext context)
    {
        IsJumpHeld = false;
        OnJumpReleased_Event?.Invoke();
    }
    
    private void OnInteract(InputAction.CallbackContext context)
    {
        IsInteractPressed = true;
        OnInteractPressed_Event?.Invoke();
    }
    
    private void OnAttack(InputAction.CallbackContext context)
    {
        IsAttackPressed = true;
        OnAttackPressed_Event?.Invoke();
    }
    
    private void OnCrouchStart(InputAction.CallbackContext context)
    {
        IsCrouchPressed = true;
        IsCrouchHeld = true;
        OnCrouchPressed_Event?.Invoke();
    }
    
    private void OnCrouchStop(InputAction.CallbackContext context)
    {
        IsCrouchHeld = false;
        OnCrouchReleased_Event?.Invoke();
    }
    
    private void OnSprintStart(InputAction.CallbackContext context)
    {
        IsSprintHeld = true;
        OnSprintPressed_Event?.Invoke();
    }
    
    private void OnSprintStop(InputAction.CallbackContext context)
    {
        IsSprintHeld = false;
        OnSprintReleased_Event?.Invoke();
    }
    
    #endregion

    #region 公共方法

    /// <summary>
    /// 重置一次性按键状态 (每帧调用)
    /// </summary>
    public void ResetFrameInputs()
    {
        IsJumpPressed = false;
        IsInteractPressed = false;
        IsAttackPressed = false;
        IsCrouchPressed = false;
    }

    /// <summary>
    /// 启用输入
    /// </summary>
    public void EnableInput()
    {
        inputActions.Enable();
    }

    /// <summary>
    /// 禁用输入
    /// </summary>
    public void DisableInput()
    {
        inputActions.Disable();
    }

   
    #endregion

    #region 生命周期

    public void OnDestroy()
    {
        CancelRebind();
        DisableInput();
        
        if (inputActions != null)
        {
            // 取消订阅所有事件
            UnsubscribeFromInputEvents();
            inputActions.Dispose();
            inputActions = null;
        }
    }

    private void UnsubscribeFromInputEvents()
    {
        if (inputActions == null) return;
        
        var playerActions = inputActions.Player;
        
        playerActions.Move.performed -= OnMove;
        playerActions.Move.canceled -= OnMoveStop;
        playerActions.Look.performed -= OnLook;
        playerActions.Look.canceled -= OnLookStop;
        playerActions.Jump.started -= OnJumpStart;
        playerActions.Jump.canceled -= OnJumpStop;
        playerActions.Interact.started -= OnInteract;
        // playerActions.Attack.started -= OnAttack;
        // playerActions.Crouch.started -= OnCrouchStart;
        // playerActions.Crouch.canceled -= OnCrouchStop;
        // playerActions.Sprint.started -= OnSprintStart;
        // playerActions.Sprint.canceled -= OnSprintStop;
    }

    #endregion

    #region 改键支持

    /// <summary>
    /// 交互式开始改键（监听下一次输入）。
    /// </summary>
    public void StartRebind(string actionName, int bindingIndex = 0, Action onComplete = null, Action onCancel = null)
    {
        if (inputActions == null || inputActions.asset == null) return;
        var action = inputActions.asset.FindAction(actionName, throwIfNotFound: false);
        if (action == null) return;

        CancelRebind();

        rebindingOperation = action.PerformInteractiveRebinding(bindingIndex)
            .OnComplete(op =>
            {
                op.Dispose();
                rebindingOperation = null;
                SaveRebinds();
                onComplete?.Invoke();
            })
            .OnCancel(op =>
            {
                op.Dispose();
                rebindingOperation = null;
                onCancel?.Invoke();
            })
            .Start();
    }

    /// <summary>
    /// 取消当前改键流程。
    /// </summary>
    public void CancelRebind()
    {
        if (rebindingOperation != null)
        {
            rebindingOperation.Cancel();
            rebindingOperation.Dispose();
            rebindingOperation = null;
        }
    }

    /// <summary>
    /// 直接用控制路径覆写某个动作的绑定。
    /// 例如 controlPath: "<Keyboard>/space" 或 "<Gamepad>/buttonSouth"。
    /// </summary>
    public void ApplyBindingOverride(string actionName, int bindingIndex, string controlPath)
    {
        if (inputActions == null || inputActions.asset == null) return;
        var action = inputActions.asset.FindAction(actionName, throwIfNotFound: false);
        if (action == null) return;
        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count) return;
        action.ApplyBindingOverride(bindingIndex, new InputBinding { overridePath = controlPath });
        SaveRebinds();
    }

    /// <summary>
    /// 清除所有重绑并恢复默认。
    /// </summary>
    public void ResetAllRebinds()
    {
        if (inputActions == null || inputActions.asset == null) return;
        inputActions.asset.RemoveAllBindingOverrides();
        PlayerPrefs.DeleteKey(RebindsPlayerPrefsKey);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 获取绑定显示文本（用于 UI）。
    /// </summary>
    public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
    {
        if (inputActions == null || inputActions.asset == null) return string.Empty;
        var action = inputActions.asset.FindAction(actionName, throwIfNotFound: false);
        if (action == null) return string.Empty;
        return action.GetBindingDisplayString(bindingIndex, out _, out _);
    }

    /// <summary>
    /// 保存所有重绑到本地。
    /// </summary>
    public void SaveRebinds()
    {
        if (inputActions == null || inputActions.asset == null) return;
        string json = inputActions.asset.SaveBindingOverridesAsJson();
        PlayerPrefs.SetString(RebindsPlayerPrefsKey, json);
        PlayerPrefs.Save();
    }

    /// <summary>
    /// 从本地加载重绑。
    /// </summary>
    public void LoadRebinds()
    {
        if (inputActions == null || inputActions.asset == null) return;
        string json = PlayerPrefs.GetString(RebindsPlayerPrefsKey, string.Empty);
        if (!string.IsNullOrEmpty(json))
        {
            inputActions.asset.LoadBindingOverridesFromJson(json);
        }
    }

    #endregion
}
}

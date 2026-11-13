using UnityEngine;
using UnityEngine.InputSystem;
using static MieMieFrameWork.ModuleHub;

namespace MieMieFrameWork.M_InputSystem
{
    /// <summary>
    /// 当前启用的 Input Action Map
    /// </summary>
    public enum E_InputMapMode
    {
        None,
        Player,
        UI
    }

    /// <summary>
    /// 输入管理器 生命周期与 Map 切换
    /// </summary>
    [ManagerAttribute(9)]
    public partial class InputManager : MonoBehaviour, IManagerBase
    {
        private MmInputAciton inputActions;
        private InputActionRebindingExtensions.RebindingOperation rebindingOperation;
        private const string RebindsPlayerPrefsKey = "Input_Rebinds";

        /// <summary>
        /// 当前 Map 模式
        /// </summary>
        public E_InputMapMode CurrentMapMode { get; private set; } = E_InputMapMode.None;

        public void Init()
        {
            inputActions = new MmInputAciton();
            LoadRebinds();
            SubscribePlayerEvents();
            SetupUiCommandMap();
            EnablePlayerInput();
        }

        #region Map 切换

        /// <summary> UI 指针叠加是否开启 Player 仍可走 </summary>
        private bool uiPointerOverlayEnabled;

        /// <summary>
        /// 启用 Player Map 关闭 UI Map
        /// </summary>
        public void EnablePlayerInput()
        {
            if (inputActions == null)
                return;

            inputActions.UI.Disable();
            inputActions.Player.Enable();
            uiCommandMap?.Enable();
            CurrentMapMode = E_InputMapMode.Player;
            uiPointerOverlayEnabled = false;
        }

        /// <summary>
        /// 启用 UI Map 关闭 Player Map
        /// </summary>
        public void EnableUIInput()
        {
            if (inputActions == null)
                return;

            inputActions.Player.Disable();
            inputActions.UI.Enable();
            uiCommandMap?.Enable();
            CurrentMapMode = E_InputMapMode.UI;
            uiPointerOverlayEnabled = true;
        }

        /// <summary>
        /// 叠加启用 UI Map 指针 不关 Player 供开背包等可走动 UI
        /// </summary>
        public void EnableUiPointerOverlay()
        {
            if (inputActions == null || uiPointerOverlayEnabled)
                return;

            inputActions.UI.Enable();
            uiPointerOverlayEnabled = true;
        }

        /// <summary>
        /// 关闭 UI 指针叠加 全 UI 模式时不关 UI Map
        /// </summary>
        public void DisableUiPointerOverlay()
        {
            if (inputActions == null || !uiPointerOverlayEnabled)
                return;

            if (CurrentMapMode != E_InputMapMode.UI)
                inputActions.UI.Disable();

            uiPointerOverlayEnabled = false;
        }

        /// <summary>
        /// 关闭全部 Map
        /// </summary>
        public void DisableAllInput()
        {
            if (inputActions == null)
                return;

            inputActions.Player.Disable();
            inputActions.UI.Disable();
            uiCommandMap?.Disable();
            CurrentMapMode = E_InputMapMode.None;
            uiPointerOverlayEnabled = false;
        }

        /// <summary>
        /// 启用 Player 输入
        /// </summary>
        public void EnableInput()
        {
            EnablePlayerInput();
        }

        /// <summary>
        /// 关闭全部输入
        /// </summary>
        public void DisableInput()
        {
            DisableAllInput();
        }

        #endregion

        #region 生命周期

        /// <summary>
        /// 销毁输入管理器
        /// </summary>
        public void OnDestroy()
        {
            CancelRebind();
            DisableAllInput();
            TeardownUiCommandMap();

            if (inputActions == null)
                return;

            UnsubscribePlayerEvents();
            inputActions.Dispose();
            inputActions = null;
        }

        #endregion
    }
}

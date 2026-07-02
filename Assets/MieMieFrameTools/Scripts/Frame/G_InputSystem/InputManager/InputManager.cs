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
            EnablePlayerInput();
        }

        #region Map 切换

        /// <summary>
        /// 启用 Player Map 关闭 UI Map
        /// </summary>
        public void EnablePlayerInput()
        {
            if (inputActions == null)
                return;

            inputActions.UI.Disable();
            inputActions.Player.Enable();
            CurrentMapMode = E_InputMapMode.Player;
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
            CurrentMapMode = E_InputMapMode.UI;
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
            CurrentMapMode = E_InputMapMode.None;
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

            if (inputActions == null)
                return;

            UnsubscribePlayerEvents();
            inputActions.Dispose();
            inputActions = null;
        }

        #endregion
    }
}

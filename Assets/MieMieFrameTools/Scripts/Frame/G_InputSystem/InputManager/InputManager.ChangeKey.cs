using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MieMieFrameWork.M_InputSystem
{
    public partial class InputManager
    {
        #region 改键支持
        /// <summary>
        /// 交互式开始改键（监听下一次输入）
        /// </summary>
        /// <param name="actionName">动作名称</param>
        /// <param name="bindingIndex">绑定索引</param>
        /// <param name="onComplete">完成回调</param>
        /// <param name="onCancel">取消回调</param>
        public void StartRebind(string actionName,
                                int bindingIndex = 0,
                                Action onComplete = null,
                                Action onCancel = null)
        {
            if (inputActions == null || inputActions.asset == null) return;
            var action = inputActions.asset.FindAction(actionName, throwIfNotFound: false);
            if (action == null) return;

            CancelRebind();

            // 这个是Unity提供的核心方法
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
        /// 取消当前改键流程
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
        /// 直接用控制路径覆写某个动作的绑定
        /// 例如 controlPath: "<Keyboard>/space" 或 "<Gamepad>/buttonSouth"
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
        /// 清除所有重绑并恢复默认
        /// </summary>
        public void ResetAllRebinds()
        {
            if (inputActions == null || inputActions.asset == null) return;
            inputActions.asset.RemoveAllBindingOverrides();
            PlayerPrefs.DeleteKey(RebindsPlayerPrefsKey);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 获取绑定显示文本（用于 UI）
        /// </summary>
        public string GetBindingDisplayString(string actionName, int bindingIndex = 0)
        {
            if (inputActions == null || inputActions.asset == null) return string.Empty;
            var action = inputActions.asset.FindAction(actionName, throwIfNotFound: false);
            if (action == null) return string.Empty;
            return action.GetBindingDisplayString(bindingIndex, out _, out _);
        }

        /// <summary>
        /// 保存所有重绑到本地
        /// </summary>
        public void SaveRebinds()
        {
            if (inputActions == null || inputActions.asset == null) return;
            string json = inputActions.asset.SaveBindingOverridesAsJson();
            PlayerPrefs.SetString(RebindsPlayerPrefsKey, json);
            PlayerPrefs.Save();
        }

        /// <summary>
        /// 从本地加载重绑
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
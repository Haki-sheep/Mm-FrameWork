using MieMieFrameWork.M_InputSystem;
using Sirenix.OdinInspector;
using UnityEngine;
using static MieMieFrameWork.ModuleHub;

namespace MieMieFrameWork.Interaction
{
    /// <summary>
    /// 交互管理器 聚焦检测与 Hold 触发交互
    /// </summary>
    [ManagerAttribute(11)]
    public class InteractionManager : MonoBehaviour, IManagerBase
    {
        [SerializeField, LabelText("射线检测")]
        private InteractionDetector detector = new();

        /// <summary>
        /// 当前聚焦目标
        /// </summary>
        public IInteractable CurrentFocus { get; private set; }

        /// <summary>
        /// 当前提示文案
        /// </summary>
        public string CurrentPrompt =>
            CurrentFocus != null ? CurrentFocus.GetPromptText() : string.Empty;

        private InputManager inputManager;
        private InteractionContext currentContext;
        private bool hasCurrentContext;

        public void Init()
        {
            detector.EnsureRayOrigin();
            inputManager = ModuleHub.Instance.GetManager<InputManager>();
            ModuleHub.Instance.GetManager<MonoManager>().AddUpdateListener(Tick);
        }

        private void OnDestroy()
        {
            if (ModuleHub.Instance != null)
            {
                MonoManager monoManager = ModuleHub.Instance.GetManager<MonoManager>();
                monoManager.RemoveUpdateListener(Tick);
            }

            ClearFocus();
        }

        /// <summary>
        /// 每帧检测聚焦与交互输入
        /// </summary>
        private void Tick()
        {
            if (inputManager == null)
                return;

            UpdateFocus();

            if (CurrentFocus == null || !hasCurrentContext)
                return;

            if (!CurrentFocus.CanInteract(currentContext))
                return;

            if (!inputManager.IsInteractHoldCompleted)
                return;

            CurrentFocus.Interact(currentContext);
        }

        /// <summary>
        /// 更新聚焦目标
        /// </summary>
        private void UpdateFocus()
        {
            // 通过射线检测获取聚焦目标
            if (!detector.TryDetect(out RaycastHit hit, out IInteractable newFocus))
            {
                if (CurrentFocus != null)
                    ClearFocus();
                return;
            }

            // 计算交互器与聚焦目标的距离
            Transform interactorTransform = detector.RayOrigin;
            float distance = interactorTransform != null
                ? Vector3.Distance(interactorTransform.position, hit.point)
                : 0f;

            // 创建交互上下文
            var newContext = new InteractionContext(interactorTransform, hit.point, distance);

            // 如果当前聚焦目标与新聚焦目标相同，则更新交互上下文
            if (ReferenceEquals(CurrentFocus, newFocus))
            {
                currentContext = newContext;
                hasCurrentContext = true;
                return;
            }

            // 如果当前聚焦目标不为空，则退出当前聚焦目标
            if (CurrentFocus != null)
            {
                CurrentFocus.OnFocusExit(currentContext);
                CurrentFocus = null;
                hasCurrentContext = false;
            }

            // 设置新的聚焦目标
            CurrentFocus = newFocus;
            currentContext = newContext;
            hasCurrentContext = true;
            CurrentFocus.OnFocusEnter(currentContext);
        }

        /// <summary>
        /// 清除当前聚焦
        /// </summary>
        private void ClearFocus()
        {
            if (CurrentFocus == null)
                return;

            if (hasCurrentContext)
                CurrentFocus.OnFocusExit(currentContext);

            CurrentFocus = null;
            hasCurrentContext = false;
        }
    }
}

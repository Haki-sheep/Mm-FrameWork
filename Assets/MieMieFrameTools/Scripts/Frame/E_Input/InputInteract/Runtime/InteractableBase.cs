using MieMieFrameWork.Tools.Inspector;
using UnityEngine;
using UnityEngine.Events;

namespace MieMieFrameWork.Interaction
{
    /// <summary>
    /// 可交互物体基类 关卡物体继承或挂脚本即可
    /// </summary>
    public class InteractableBase : MonoBehaviour, IInteractable
    {
        [SerializeField]
        [InspectorLabel("提示文案")]
        private string promptText = "交互";

        [SerializeField]
        [InspectorLabel("允许交互")]
        private bool allowInteract = true;

        [SerializeField]
        [InspectorLabel("交互事件")]
        private UnityEvent onInteractEvent;

        public bool CanInteract(InteractionContext ctx)
        {
            return allowInteract;
        }

        public void OnFocusEnter(InteractionContext ctx)
        {
        }

        public void OnFocusExit(InteractionContext ctx)
        {
        }

        public virtual void Interact(InteractionContext ctx)
        {
            onInteractEvent?.Invoke();
        }

        public string GetPromptText()
        {
            return promptText;
        }
    }
}

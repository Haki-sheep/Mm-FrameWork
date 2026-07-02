namespace MieMieFrameWork.Interaction
{
    /// <summary>
    /// 可交互物体契约
    /// </summary>
    public interface IInteractable
    {
        /// <summary>
        /// 当前是否允许交互
        /// </summary>
        bool CanInteract(InteractionContext ctx);

        /// <summary>
        /// 进入聚焦
        /// </summary>
        void OnFocusEnter(InteractionContext ctx);

        /// <summary>
        /// 离开聚焦
        /// </summary>
        void OnFocusExit(InteractionContext ctx);

        /// <summary>
        /// Hold 完成时执行交互
        /// </summary>
        void Interact(InteractionContext ctx);

        /// <summary>
        /// 提示文案
        /// </summary>
        string GetPromptText();
    }
}

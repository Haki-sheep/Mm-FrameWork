
namespace MieMieFrameWork.FSM
{
    using System;
    using UnityEngine;

    /// <summary>
    /// 状态基类 - 所有状态都应该继承此类
    /// </summary>
    public abstract class StateBase 
    {
        //黑板数据引用
        protected IFsmBlackboard blackboard;

        //状态名称
        public virtual string StateName => GetType().Name;


        #region 生命周期方法

        //初始化状态 - 在状态被添加到状态机时调用
        public virtual void Init(IFsmBlackboard blackboard = null)
        {
            this.blackboard = blackboard;
            OnInit();
        }

        /// <summary>
        /// 反初始化状态 - 在状态从状态机中移除时调用
        /// </summary>
        public virtual void UnInit()
        {
            OnUnInit();
            blackboard = null;
        }

        #endregion

        #region 虚方法 - 子类可重写

        /// <summary>
        /// 状态初始化时调用 - 子类可重写
        /// </summary>
        public virtual void OnInit() { }

        /// <summary>
        /// 状态反初始化时调用 - 子类可重写
        /// </summary>
        public virtual void OnUnInit() { }

        /// <summary>
        /// 状态进入时调用 - 子类可重写
        /// </summary>
        public virtual void OnEnter() { }

        /// <summary>
        /// 状态退出时调用 - 子类可重写
        /// </summary>
        public virtual void OnExit() { }

        /// <summary>
        /// 状态更新时调用 - 子类可重写
        /// </summary>
        public virtual void OnUpdate() { }

        /// <summary>
        /// 状态延迟更新时调用 - 子类可重写
        /// </summary>
        public virtual void OnLateUpdate() { }

        /// <summary>
        /// 状态物理更新时调用 - 子类可重写
        /// </summary>
        public virtual void OnFixedUpdate() { }

        /// <summary>
        #endregion

        #region 黑板数据操作辅助方法
 
        /// <summary>
        /// 设置黑板数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="value">值</param>
        protected void SetBlackboardValue<T>(EBlockBoardParme key, T value)
        {
            blackboard?.SetValue(key, value);
        }

        /// <summary>
        /// 获取黑板数据
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="key">键名</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>数据值</returns>
        protected T GetBlackboardValue<T>(EBlockBoardParme key, T defaultValue = default)
        {
            return blackboard != null ? blackboard.GetValue(key, defaultValue) : defaultValue;
        }

        /// <summary>
        /// 检查黑板是否包含指定键
        /// </summary>
        /// <param name="key">键名</param>
        /// <returns>是否包含</returns>
        protected bool HasBlackboardKey(EBlockBoardParme key)
        {
            return blackboard?.HasKey(key) ?? false;
        }

        /// <summary>
        /// 移除黑板数据
        /// </summary>
        /// <param name="key">键名</param>
        protected void RemoveBlackboardValue(EBlockBoardParme key)
        {
            blackboard?.RemoveValue(key);
        }

        #endregion


        #region 调试和工具方法

        /// <summary>
        /// 获取状态调试信息
        /// </summary>
        /// <returns>调试信息字符串</returns>
        public virtual string GetDebugInfo()
        {
            // var info = new System.Text.StringBuilder();
            // info.AppendLine($"状态名称: {StateName}");
            // info.AppendLine($"拥有者: {owner?.GetType().Name ?? "None"}");
            // info.AppendLine($"黑板数据: {(blackboard != null ? "Available" : "None")}");

            return StateName;
        }


        #endregion
    }

}

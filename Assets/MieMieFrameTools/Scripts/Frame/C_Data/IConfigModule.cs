using System;

namespace MieMieFrameWork.Data
{
    /// <summary>
    /// 配置组件生命周期 由框架根节点唯一持有并调用
    /// </summary>
    public interface IConfigModule : IDisposable
    {
        /// <summary>
        /// 加载配置数据 仅在框架启动阶段且管理器注册前调用一次
        /// </summary>
        public void InitComponents();
    }
}

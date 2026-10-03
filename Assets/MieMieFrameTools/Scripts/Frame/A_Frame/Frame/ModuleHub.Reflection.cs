namespace MieMieFrameWork
{
    using System;
    using MieMieFrameWork.Diagnostics;
    using Sirenix.OdinInspector;
    using UnityEngine;
    using UnityEngine.Serialization;

    /// <summary>
    /// 游戏根节点管理器 反射部分
    /// 只反射发现不可直接编译引用的可选 UI 模块 生命周期通过管理器接口调用
    /// </summary>
    public partial class ModuleHub
    {
        #region UI 管理器钩子

        /// <summary>
        /// UI管理器场景引用
        /// </summary>
        [FormerlySerializedAs("uICoreMgr")]
        [SerializeField, LabelText("UI管理器")]
        private MonoBehaviour uiCoreMgrBehaviour;

        /// <summary>
        /// UI 管理器实例 未安装 com.hakisheep.mm-uiframe 时为 null
        /// </summary>
        private IManagerBase uiCoreMgr;

        /// <summary>
        /// 绑定可选 UI 管理器 实际初始化由运行时管理器阶段统一执行
        /// </summary>
        internal void BindUIHub()
        {
            var UIType = ResolveUIHubType();
            if (UIType == null)
            {
                FrameLog.Warning("UI 模块未安装或 MieMieUIFrameWork.UI 程序集未编译 跳过 UIHub 初始化", "Module");
                return;
            }

            Component UIComponent = uiCoreMgrBehaviour;
            if (UIComponent == null || !UIType.IsInstanceOfType(UIComponent))
                UIComponent = GetComponent(UIType);

            // UIRoot 与 FrameRoot 常为场景内两个物体 同物体找不到时全局搜一次
            if (UIComponent == null)
                UIComponent = FindAnyObjectByType(UIType, FindObjectsInactive.Include) as Component;

            if (UIComponent == null)
            {
                FrameLog.Warning("已安装 UI 模块但未找到 UIHub 组件 请挂到场景 UIRoot 或拖入序列化槽", "Module");
                return;
            }

            uiCoreMgr = UIComponent as IManagerBase
                ?? throw new InvalidOperationException($"[ModuleHub] UI 组件 {UIComponent.GetType().FullName} 必须实现 IManagerBase");
            uiCoreMgrBehaviour = UIComponent as MonoBehaviour;
        }

        /// <summary>
        /// 解析 MmUIFrameWork.Core.UIHub 类型
        /// </summary>
        private static Type ResolveUIHubType()
        {
            const string UITypeName = "MmUIFrameWork.Core.UIHub";
            const string AssemblyName = "MieMieUIFrameWork.UI";

            var UIType = Type.GetType($"{UITypeName}, {AssemblyName}");
            if (UIType != null)
                return UIType;

            foreach (var Assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (Assembly.GetName().Name != AssemblyName)
                    continue;

                UIType = Assembly.GetType(UITypeName);
                if (UIType != null)
                    return UIType;
            }

            return null;
        }

        /// <summary>
        /// 是否已安装并绑定 UI 模块
        /// </summary>
        public bool HasUI => uiCoreMgr != null;

        /// <summary>
        /// 获取 UI 管理器 需引用 MieMieUIFrameWork.UI 后使用 UIHub 类型
        /// </summary>
        public T GetUI<T>() where T : class => uiCoreMgr as T;

        #endregion

    }
}

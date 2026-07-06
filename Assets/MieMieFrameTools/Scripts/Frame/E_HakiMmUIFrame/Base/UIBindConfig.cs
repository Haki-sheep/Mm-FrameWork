namespace MieMieFrameWork.UI
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    /// <summary>
    /// UI 自动绑定配置
    /// </summary>
    [DisallowMultipleComponent]
    public class UIBindConfig : MonoBehaviour
    {
        /// <summary>
        /// 绑定项目列表
        /// </summary>
        [SerializeField]
        private List<UIBindItem> bindItemList = new List<UIBindItem>();

        public IReadOnlyList<UIBindItem> BindItemList => bindItemList;

#if UNITY_EDITOR
        public List<UIBindItem> EditorBindItemList => bindItemList;
#endif
    }

    /// <summary>
    /// UI 自动绑定项目
    /// </summary>
    [Serializable]
    public class UIBindItem
    {
        /// <summary>
        /// 节点路径
        /// </summary>
        public string nodePath;

        /// <summary>
        /// 节点名
        /// </summary>
        public string nodeName;

        /// <summary>
        /// 组件类型名
        /// </summary>
        public string componentTypeName;

        /// <summary>
        /// 组件完整类型名
        /// </summary>
        public string componentFullTypeName;

        /// <summary>
        /// 组件程序集名
        /// </summary>
        public string componentAssemblyName;

        /// <summary>
        /// 字段名
        /// </summary>
        public string fieldName;
    }
}

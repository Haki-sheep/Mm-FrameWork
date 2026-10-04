using MieMieFrameWork.Asset.DynamicAtlas.Editor;
using UnityEngine;

namespace MieMieFrameWork.Editor.ToolsCenter
{
    /// <summary>
    /// 单向适配动态图集窗口 不让资源程序集依赖工具中枢
    /// </summary>
    public sealed class DynamicAtlasToolsPage : MieMieToolsPage
    {
        /// <summary>
        /// 本页面持有的隐藏调试窗口
        /// </summary>
        private DynamicAtlasWindow window;

        /// <summary>
        /// 注册资源分类页面
        /// </summary>
        public DynamicAtlasToolsPage() : base("资源/动态图集调试", "动态图集调试", "观察运行时图集并试装图片 不执行离线打包")
        {
        }

        /// <summary>
        /// 页面选中时创建唯一窗口实例
        /// </summary>
        public override void OnOpen()
        {
            if (window != null)
                return;
            window = ScriptableObject.CreateInstance<DynamicAtlasWindow>();
            window.hideFlags = HideFlags.HideAndDontSave;
        }

        /// <summary>
        /// 页面离开时触发窗口清理 只释放试装引用
        /// </summary>
        public override void OnClose()
        {
            if (window == null)
                return;
            Object.DestroyImmediate(window);
            window = null;
        }

        /// <summary>
        /// 将调试窗口内容绘制到工具中枢
        /// </summary>
        public override void DrawGUI()
        {
            DrawPageTitle();
            window.DrawEmbeddedGUI();
        }
    }
}

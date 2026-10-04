using System;
using System.Collections.Generic;
using System.Linq;
using MieMieFrameWork.Editor.ToolsCenter;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace MieMieFrameWork.Editor.StaticAtlas
{
    /// <summary>
    /// 工具中枢静态图集页面 显式扫描写入与原生预览
    /// </summary>
    public sealed class StaticAtlasWindow : EditorWindow, IMieMieToolsEmbeddedWindow
    {
        /// <summary>
        /// 递归扫描的源目录
        /// </summary>
        private readonly List<DefaultAsset> FolderList = new List<DefaultAsset>();
        /// <summary>
        /// 最近扫描结果
        /// </summary>
        private List<Texture2D> TextureList = new List<Texture2D>();
        /// <summary>
        /// 显式写入参数
        /// </summary>
        private StaticAtlasOptions Options = new StaticAtlasOptions();
        /// <summary>
        /// 更新目标图集
        /// </summary>
        private SpriteAtlas Target;
        /// <summary>
        /// 原生预览编辑器
        /// </summary>
        private UnityEditor.Editor PreviewEditor;
        /// <summary>
        /// 最近检查结果
        /// </summary>
        private StaticAtlasReport Report;
        /// <summary>
        /// 最近操作结果或完整错误
        /// </summary>
        private string Message;
        /// <summary>
        /// 操作结果类型
        /// </summary>
        private MessageType eMessageType;
        /// <summary>
        /// 页面滚动位置
        /// </summary>
        private Vector2 Scroll;
        /// <summary>
        /// 源图列表折叠状态
        /// </summary>
        private bool ShowSources;
        /// <summary>
        /// Unity 支持的常见平台名称
        /// </summary>
        private static readonly string[] PlatformNameList = { "Android", "iPhone", "Standalone" };
        /// <summary>
        /// 图集支持的尺寸
        /// </summary>
        private static readonly int[] SizeList = { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 };

        #region 生命周期与界面

        /// <summary>
        /// 页面关闭时释放仅属于本工具的预览编辑器
        /// </summary>
        private void OnDisable()
        {
            if (PreviewEditor != null)
                DestroyImmediate(PreviewEditor);
        }

        /// <summary>
        /// 绘制窗口内容
        /// </summary>
        private void OnGUI()
        {
            DrawEmbeddedGUI();
        }

        /// <summary>
        /// 绘制中枢内的图集参数 来源 检查与原生预览
        /// </summary>
        public void DrawEmbeddedGUI()
        {
            Scroll = EditorGUILayout.BeginScrollView(Scroll);
            EditorGUILayout.HelpBox("生成构建前的 SpriteAtlas 不修改原图导入设置 GUID 或 YooAsset 采集配置 更新会替换全部收录", MessageType.Info);
            DrawMode();
            var Selected = (SpriteAtlas)EditorGUILayout.ObjectField("已有图集 可选", Target, typeof(SpriteAtlas), false);
            if (Selected != Target)
                Run(() => SelectTarget(Selected));
            EditorGUILayout.BeginHorizontal();
            using (new EditorGUI.DisabledScope(Target == null))
            {
                if (GUILayout.Button("重新读取图集参数"))
                    Run(() => SelectTarget(Target));
                if (GUILayout.Button("在 Project 中定位"))
                    EditorGUIUtility.PingObject(Target);
            }
            EditorGUILayout.EndHorizontal();
            DrawFolders();
            DrawOptions();
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                if (GUILayout.Button("扫描来源并检查"))
                    Run(Scan);
                using (new EditorGUI.DisabledScope(EditorSettings.spritePackerMode == SpritePackerMode.Disabled))
                {
                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("创建新图集"))
                        Run(() => Save(true));
                    using (new EditorGUI.DisabledScope(Target == null))
                    {
                        if (GUILayout.Button("更新当前图集"))
                            Run(() => Save(false));
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
            DrawReport();
            DrawPreview();
            if (!string.IsNullOrEmpty(Message))
                EditorGUILayout.HelpBox(Message, eMessageType);
            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 显示全局图集模式 仅明确确认后启用 V2
        /// </summary>
        private void DrawMode()
        {
            EditorGUILayout.LabelField("Sprite Packer 模式", EditorSettings.spritePackerMode.ToString());
            if (EditorSettings.spritePackerMode != SpritePackerMode.Disabled)
                return;
            EditorGUILayout.HelpBox("项目当前关闭 Sprite Packer 创建与预览不可用 启用 V2 会改变项目全局设置并触发重导入", MessageType.Warning);
            using (new EditorGUI.DisabledScope(EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                if (GUILayout.Button("显式启用 Sprite Atlas V2") && EditorUtility.DisplayDialog("改变项目图集模式", "将启用全局 Sprite Atlas V2 并触发相关资产重新导入 是否继续", "启用", "取消"))
                    Run(() => EditorSettings.spritePackerMode = SpritePackerMode.SpriteAtlasV2);
            }
        }

        /// <summary>
        /// 编辑目录列表 目录重叠会在扫描时去重
        /// </summary>
        private void DrawFolders()
        {
            EditorGUILayout.LabelField("来源目录 递归扫描 Sprite 纹理", EditorStyles.boldLabel);
            for (int Index = 0; Index < FolderList.Count; Index++)
            {
                EditorGUILayout.BeginHorizontal();
                FolderList[Index] = (DefaultAsset)EditorGUILayout.ObjectField(FolderList[Index], typeof(DefaultAsset), false);
                if (GUILayout.Button("移除", GUILayout.Width(50)))
                    FolderList.RemoveAt(Index--);
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("添加来源目录"))
                FolderList.Add(null);
        }

        /// <summary>
        /// 编辑通用参数及单一平台覆盖 其余平台保留
        /// </summary>
        private void DrawOptions()
        {
            EditorGUILayout.LabelField("图集参数", EditorStyles.boldLabel);
            Options.MaxSize = EditorGUILayout.IntPopup("最大尺寸", Options.MaxSize, SizeList.Select(Size => Size.ToString()).ToArray(), SizeList);
            Options.Padding = EditorGUILayout.IntPopup("Padding", Options.Padding, new[] { "2", "4", "8" }, new[] { 2, 4, 8 });
            Options.Rotation = EditorGUILayout.Toggle("允许旋转 UI 建议关闭", Options.Rotation);
            Options.TightPacking = EditorGUILayout.Toggle("Tight Packing UI 建议关闭", Options.TightPacking);
            Options.IncludeInBuild = EditorGUILayout.Toggle("Include In Build", Options.IncludeInBuild);
            EditorGUILayout.HelpBox("由 YooAsset 分发时默认不随 Player 隐式包含 业务仍需使用现有资源加载与句柄释放流程", MessageType.None);
            Options.Compression = (TextureImporterCompression)EditorGUILayout.EnumPopup("压缩", Options.Compression);
            Options.Srgb = EditorGUILayout.Toggle("sRGB", Options.Srgb);
            Options.Filter = (FilterMode)EditorGUILayout.EnumPopup("过滤", Options.Filter);
            Options.WritePlatformOverride = EditorGUILayout.Toggle("同时写入指定平台覆盖", Options.WritePlatformOverride);
            if (!Options.WritePlatformOverride)
                return;
            int PlatformIndex = Array.IndexOf(PlatformNameList, Options.PlatformName);
            int NextIndex = EditorGUILayout.Popup("平台", PlatformIndex, PlatformNameList);
            if (NextIndex != PlatformIndex)
            {
                Options.PlatformName = PlatformNameList[NextIndex];
                ReadPlatform();
            }
            Options.PlatformOverrideEnabled = EditorGUILayout.Toggle("启用该平台覆盖", Options.PlatformOverrideEnabled);
            Options.PlatformFormat = (TextureImporterFormat)EditorGUILayout.EnumPopup("该平台格式", Options.PlatformFormat);
            EditorGUILayout.HelpBox("默认参数与选中平台共用最大尺寸和压缩 其他平台不会被重置 自定义格式须适用于所选平台", MessageType.None);
        }

        /// <summary>
        /// 显示最近扫描结果与完整来源路径
        /// </summary>
        private void DrawReport()
        {
            EditorGUILayout.LabelField($"最近扫描源图 {TextureList.Count} 张 保存前会重新扫描");
            if (Report != null)
            {
                foreach (string Error in Report.ErrorList)
                    EditorGUILayout.HelpBox(Error, MessageType.Error);
                foreach (string Warning in Report.WarningList)
                    EditorGUILayout.HelpBox(Warning, MessageType.Warning);
                if (Report.ErrorList.Count == 0)
                    EditorGUILayout.HelpBox("最近静态检查无阻断项 不代表平台构建或 Bundle 依赖已经验收", MessageType.Info);
            }
            ShowSources = EditorGUILayout.Foldout(ShowSources, "查看扫描来源列表", true);
            if (ShowSources)
            {
                foreach (var Texture in TextureList)
                    EditorGUILayout.SelectableLabel(AssetDatabase.GetAssetPath(Texture), GUILayout.Height(EditorGUIUtility.singleLineHeight));
            }
        }

        /// <summary>
        /// 按当前构建平台显式打包预览 不反射 Unity 内部 API
        /// </summary>
        private void DrawPreview()
        {
            if (Target == null)
                return;
            EditorGUILayout.LabelField("打包预览", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("当前平台", EditorUserBuildSettings.activeBuildTarget.ToString());
            using (new EditorGUI.DisabledScope(EditorSettings.spritePackerMode == SpritePackerMode.Disabled || EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling || EditorApplication.isUpdating))
            {
                if (GUILayout.Button("打包并刷新原生预览"))
                    Run(() =>
                    {
                        SpriteAtlasUtility.PackAtlases(new[] { Target }, EditorUserBuildSettings.activeBuildTarget);
                        RefreshPreview();
                        Message = $"已执行 {EditorUserBuildSettings.activeBuildTarget} 预览打包 Sprite 数量 {Target.spriteCount} 不等于 Player 或 Bundle 构建验收";
                    });
            }
            if (PreviewEditor != null && PreviewEditor.HasPreviewGUI())
            {
                PreviewEditor.OnPreviewSettings();
                PreviewEditor.OnInteractivePreviewGUI(GUILayoutUtility.GetRect(240, 320, GUILayout.ExpandWidth(true)), EditorStyles.helpBox);
            }
            else
                EditorGUILayout.HelpBox("打包后显示 Unity 原生预览 也可在 Project 中选择图集打开原生 Inspector", MessageType.None);
        }

        #endregion

        #region 数据读取与显式操作

        /// <summary>
        /// 选择目标并读取原有参数 不自动推断或扩大来源目录
        /// </summary>
        private void SelectTarget(SpriteAtlas atlas)
        {
            Target = atlas;
            Options = new StaticAtlasOptions();
            if (Target != null)
            {
                var Packing = Target.GetPackingSettings();
                var Texture = Target.GetTextureSettings();
                var Platform = Target.GetPlatformSettings("DefaultTexturePlatform");
                Options.Padding = Packing.padding;
                Options.Rotation = Packing.enableRotation;
                Options.TightPacking = Packing.enableTightPacking;
                Options.MaxSize = Platform.maxTextureSize;
                Options.Compression = Platform.textureCompression;
                Options.Srgb = Texture.sRGB;
                Options.Filter = Texture.filterMode;
                Options.IncludeInBuild = Target.IsIncludeInBuild();
                ReadPlatform();
            }
            Report = null;
            RefreshPreview();
        }

        /// <summary>
        /// 读取选中平台的原有格式与覆盖状态
        /// </summary>
        private void ReadPlatform()
        {
            if (Target == null)
            {
                Options.PlatformFormat = TextureImporterFormat.Automatic;
                Options.PlatformOverrideEnabled = true;
                return;
            }
            var Platform = Target.GetPlatformSettings(Options.PlatformName);
            Options.PlatformFormat = Platform.format;
            Options.PlatformOverrideEnabled = Platform.overridden;
        }

        /// <summary>
        /// 重新扫描检查 不复用旧目录结果
        /// </summary>
        private void Scan()
        {
            Report = null;
            TextureList = StaticAtlasBuilder.CollectTextureList(FolderList);
            Report = StaticAtlasDiagnostics.Check(TextureList, Options, AssetDatabase.GetAssetPath(Target));
            Message = "扫描完成 非 Sprite 图片已排除 重叠目录已去重";
        }

        /// <summary>
        /// 确认更新或选择新文件后再次检查并写入
        /// </summary>
        private void Save(bool create)
        {
            string Path = AssetDatabase.GetAssetPath(Target);
            if (create)
            {
                string Extension = EditorSettings.spritePackerMode.ToString().Contains("AtlasV2") ? "spriteatlasv2" : "spriteatlas";
                Path = EditorUtility.SaveFilePanelInProject("创建静态图集", "UIAtlas", Extension, "请选择现有资源采集目录下的输出位置");
                if (string.IsNullOrEmpty(Path))
                    return;
            }
            else if (!EditorUtility.DisplayDialog("替换图集收录", $"将按当前来源目录替换全部 Packables 并保存参数 保持图集 GUID 未列入来源的旧图将被移除\n{Path}", "更新", "取消"))
                return;
            TextureList = StaticAtlasBuilder.CollectTextureList(FolderList);
            Report = StaticAtlasDiagnostics.Check(TextureList, Options, Path);
            if (Report.ErrorList.Count > 0)
                throw new InvalidOperationException(string.Join("\n", Report.ErrorList));
            var Atlas = StaticAtlasBuilder.Save(Path, TextureList, Options, create);
            SelectTarget(Atlas);
            Report = StaticAtlasDiagnostics.Check(TextureList, Options, Path);
            Message = $"已保存 {Path}\n" + string.Join("\n", StaticAtlasDiagnostics.GetCollectorHintList(Path));
            EditorGUIUtility.PingObject(Target);
        }

        /// <summary>
        /// 替换预览编辑器并释放旧实例
        /// </summary>
        private void RefreshPreview()
        {
            if (PreviewEditor != null)
                DestroyImmediate(PreviewEditor);
            PreviewEditor = Target != null ? UnityEditor.Editor.CreateEditor(Target) : null;
        }

        /// <summary>
        /// 显式显示操作异常 不降级或伪造成功
        /// </summary>
        private void Run(Action action)
        {
            Message = null;
            eMessageType = MessageType.Info;
            try
            {
                action();
            }
            catch (Exception Exception)
            {
                Message = Exception.ToString();
                eMessageType = MessageType.Error;
            }
        }

        #endregion
    }
}

namespace MieMieFrameWork.Editor.MmAssets
{
    /// <summary>
    /// MieMieFrameTools 编辑器与运行时资源路径
    /// </summary>
    public static class MmEditorPaths
    {
        /// <summary>
        /// 框架根目录
        /// </summary>
        public const string MieMieRoot = "Assets/MieMieFrameTools";

        /// <summary>
        /// 编辑器根目录
        /// </summary>
        public const string EditorRoot = MieMieRoot + "/Editor";

        /// <summary>
        /// 运行时 Frame 根目录
        /// </summary>
        public const string FrameRoot = MieMieRoot + "/Scripts/Frame";

        /// <summary>
        /// 框架事件 Token 定义
        /// </summary>
        public const string MmGameEventsAsset = FrameRoot + "/D_EventCenter/MmGameEvents.cs";

        /// <summary>
        /// Protobuf 设置 JSON
        /// </summary>
        public const string ProtobufSettingsAsset = EditorRoot + "/Protobuf/protobuf_settings.json";

        /// <summary>
        /// protoc 可执行文件
        /// </summary>
        public const string ProtocExeAsset = EditorRoot + "/SaveForEditor/Protoc/protoc.exe";

        /// <summary>
        /// proto 源文件目录
        /// </summary>
        public const string ProtoSourceFolderAsset = MieMieRoot + "/ARequired/GameSave";

        /// <summary>
        /// proto 生成 C# 输出目录
        /// </summary>
        public const string ProtoOutputFolderAsset = MieMieRoot + "/ARequired/GameSave/Generated";
    }
}

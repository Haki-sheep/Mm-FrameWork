// Assets/Editor/Protobuf/ProtobufMenu.cs
// Tools 菜单：仅保留入口，具体操作在窗口内完成

using UnityEditor;

namespace Editor.Protobuf
{
    public static class ProtobufMenu
    {
        private const string MENU_ROOT = "Tools/MieMieFrameWork/Protobuf/";

        [MenuItem(MENU_ROOT + "Protobuf 生成器", priority = 1000)]
        public static void OpenGenerator()
        {
            ProtobufGeneratorWindow.ShowWindow();
        }

        [MenuItem(MENU_ROOT + "依赖下载链接", priority = 1001)]
        public static void OpenDownloads()
        {
            ProtobufDownloadWindow.Open();
        }
    }
}

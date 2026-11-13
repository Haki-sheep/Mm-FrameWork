using UnityEngine;
using UnityEngine.Scripting;

namespace MmHotUpdate.Demo
{
    [Preserve]
    public sealed class DemoView : MonoBehaviour
    {
        /// <summary> 本次实际执行代码与加载资源的结果 </summary>
        public string result;

        [Preserve]
        private void OnGUI()
        {
            var Style = new GUIStyle(GUI.skin.label) { fontSize = 28, wordWrap = true };
            GUILayout.BeginArea(new Rect(40, 40, Screen.width - 80, Screen.height - 80));
            GUILayout.Label("MmFramework / HybridCLR", Style);
            GUILayout.Space(24);
            GUILayout.Label(result, Style);
            GUILayout.Space(24);
            GUILayout.Label("Publish changes in Unity, then restart this EXE.\nThe same Player automatically downloads the latest code and assets.", Style);
            GUILayout.Space(24);
            if (GUILayout.Button("Exit", GUILayout.Height(48))) Application.Quit();
            GUILayout.EndArea();
        }
    }
}

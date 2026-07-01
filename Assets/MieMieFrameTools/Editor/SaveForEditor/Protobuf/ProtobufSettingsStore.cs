using System;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace Editor.Protobuf
{
    /// <summary>
    /// 编辑器 Protobuf 配置（JSON 持久化，路径见 <see cref="SettingsFilePath"/>）
    /// </summary>
    [Serializable]
    public class ProtobufSettingsData
    {
        public string protocPath = "";
        public string protoDirectory = "";
        public string outputDirectory = "";
        public string csharpNamespace = "Game.Save";
    }

    public static class ProtobufSettingsStore
    {
        public static string SettingsFilePath =>
            System.IO.Path.Combine(UnityEngine.Application.dataPath, "MieMieFrameTools/Editor/Protobuf/protobuf_settings.json");

        private static ProtobufSettingsData _data;
        private static bool _loaded;

        public static ProtobufSettingsData Data
        {
            get
            {
                EnsureLoaded();
                return _data;
            }
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;

            bool createdNewFile = !File.Exists(SettingsFilePath);

            if (!createdNewFile)
            {
                try
                {
                    string json = File.ReadAllText(SettingsFilePath);
                    _data = JsonConvert.DeserializeObject<ProtobufSettingsData>(json) ?? new ProtobufSettingsData();
                }
                catch
                {
                    _data = new ProtobufSettingsData();
                    createdNewFile = true;
                }
            }
            else
            {
                _data = new ProtobufSettingsData();
                MigrateFromEditorPrefs();
            }

            ApplyEmptyDefaults(_data);

            if (createdNewFile)
                Save();
        }

        private static void MigrateFromEditorPrefs()
        {
            string oldProtoc = EditorPrefsHelper.GetString("ProtobufProtocPath", "");
            if (!string.IsNullOrEmpty(oldProtoc))
                _data.protocPath = oldProtoc;

            string oldProto = EditorPrefsHelper.GetString("ProtobufProtoPath", "");
            if (!string.IsNullOrEmpty(oldProto))
                _data.protoDirectory = oldProto;

            string oldOut = EditorPrefsHelper.GetString("ProtobufOutputPath", "");
            if (!string.IsNullOrEmpty(oldOut))
                _data.outputDirectory = oldOut;

            string oldNs = EditorPrefsHelper.GetString("ProtobufNamespace", "");
            if (!string.IsNullOrEmpty(oldNs))
                _data.csharpNamespace = oldNs;
        }

        private static void ApplyEmptyDefaults(ProtobufSettingsData d)
        {
            if (string.IsNullOrEmpty(d.protoDirectory))
                d.protoDirectory = Path.Combine(Application.dataPath, "MieMieFrameTools/ARequired/GameSave");
            if (string.IsNullOrEmpty(d.outputDirectory))
                d.outputDirectory = Path.Combine(Application.dataPath, "MieMieFrameTools/ARequired/GameSave/Generated");
            if (string.IsNullOrEmpty(d.csharpNamespace))
                d.csharpNamespace = "Game.Save";
            if (string.IsNullOrEmpty(d.protocPath))
                d.protocPath = Path.Combine(Application.dataPath, "MieMieFrameTools/Editor/SaveForEditor/Protoc/protoc.exe");
        }

        public static void Save()
        {
            EnsureLoaded();
            string dir = Path.GetDirectoryName(SettingsFilePath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            string json = JsonConvert.SerializeObject(_data, Formatting.Indented);
            File.WriteAllText(SettingsFilePath, json);
        }

    }
}

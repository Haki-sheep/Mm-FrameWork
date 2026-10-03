#if UNITY_EDITOR
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace FakeUnityCLI.EditorBridge
{
    [InitializeOnLoad]
    internal static class BridgeBootstrap
    {
        internal const string BridgeVersion = "1.20.0";
        private static readonly string Root = Path.Combine("Library", "FakeUnityCLI");
        private static readonly string OwnerPath = Path.Combine(Root, "bridge-owner.json");
        private static FileStream _owner;
        private static bool _started;

        internal static readonly int EditorPid = Process.GetCurrentProcess().Id;
        internal static readonly long ProcessStartedAtTicks = Process.GetCurrentProcess().StartTime.ToUniversalTime().Ticks;
        internal static readonly string EditorInstanceId = EditorPid + "-" + ProcessStartedAtTicks;

        static BridgeBootstrap()
        {
            EnsureStarted();
        }

        internal static bool EnsureStarted()
        {
            if (_started) return true;
            if (AssetDatabase.IsAssetImportWorkerProcess()) return false;

            Directory.CreateDirectory(Root);
            try
            {
                _owner = new FileStream(OwnerPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
            }
            catch (IOException exception)
            {
                ReleaseOwner();
                UnityEngine.Debug.LogWarning("FakeUnityCLI Bridge did not start because another process owns the project bridge: " + exception.Message);
                return false;
            }

            try
            {
                var owner = JsonUtility.ToJson(new OwnerState
                {
                    bridge_version = BridgeVersion,
                    process_role = "main_editor",
                    project_path = Path.GetFullPath(Path.Combine(Application.dataPath, "..")),
                    editor_pid = EditorPid,
                    process_started_at_ticks = ProcessStartedAtTicks,
                    editor_instance_id = EditorInstanceId,
                    acquired_at = DateTime.UtcNow.ToString("o")
                }, true);
                var bytes = new UTF8Encoding(false).GetBytes(owner);
                _owner.SetLength(0);
                _owner.Write(bytes, 0, bytes.Length);
                _owner.Flush();

                LiveBridgeUpdate.Initialize();
                OnlineProviderRegistry.Refresh();
                EditorActivityBridge.Start();
                LogCaptureBridge.Start();
                RoslynRequestBridge.Start();
                AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
                AssemblyReloadEvents.beforeAssemblyReload += BeforeAssemblyReload;
                EditorApplication.quitting -= BeforeQuit;
                EditorApplication.quitting += BeforeQuit;
                _started = true;
                return true;
            }
            catch (Exception exception)
            {
                Stop("bridge_error", "Bridge startup failed");
                UnityEngine.Debug.LogError("FakeUnityCLI Bridge startup failed: " + exception);
                return false;
            }
        }

        private static void BeforeAssemblyReload()
        {
            EditorActivityBridge.BeforeReload();
            Stop("reloading", "Unity assembly reload started");
        }

        private static void BeforeQuit()
        {
            Stop("stopping", "Unity Editor is quitting");
        }

        private static void Stop(string state, string detail)
        {
            RoslynRequestBridge.Stop(state, detail);
            LogCaptureBridge.Stop();
            EditorActivityBridge.Stop();
            LiveBridgeUpdate.Stop();
            AssemblyReloadEvents.beforeAssemblyReload -= BeforeAssemblyReload;
            EditorApplication.quitting -= BeforeQuit;
            _started = false;
            ReleaseOwner();
        }

        private static void ReleaseOwner()
        {
            if (_owner == null) return;
            try { _owner.Dispose(); }
            catch { }
            _owner = null;
        }

        [Serializable]
        private sealed class OwnerState
        {
            public string bridge_version;
            public string process_role;
            public string project_path;
            public int editor_pid;
            public long process_started_at_ticks;
            public string editor_instance_id;
            public string acquired_at;
        }
    }
}
#endif

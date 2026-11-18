using System;
using System.Collections.Generic;
using System.IO;
using MieMieFrameWork;
using MiMieSaver;
using UnityEditor;
using UnityEngine;

namespace MieMieFrameWork.Editor.SaveForEditor
{
    /// <summary>
    /// 存档系统编辑器管理窗口
    /// </summary>
    public class ArchiveEditorWindow : EditorWindow, MieMieFrameWork.Editor.ToolsCenter.IMieMieToolsEmbeddedWindow
    {
        private const float MinWindowWidth = 620f;
        private const float MinWindowHeight = 420f;
        private const long BytesPerKilobyte = 1024L;
        private const long BytesPerMegabyte = BytesPerKilobyte * 1024L;

        private enum ETab
        {
            Slots,
            Modules
        }

        private ETab currentTab;
        private Vector2 scrollPosition;
        private string createSlotName = "New Slot";
        private string selectedSlotId;
        private string renameSlotName = string.Empty;
        private string statusMessage = string.Empty;

        #region 生命周期

        /// <summary>
        /// 打开存档系统编辑器窗口
        /// </summary>
        public static void Open()
        {
            ArchiveEditorWindow window = GetWindow<ArchiveEditorWindow>("存档系统");
            window.minSize = new Vector2(MinWindowWidth, MinWindowHeight);
            window.Show();
        }

        /// <summary>
        /// 注册编辑器生命周期回调
        /// </summary>
        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            EditorApplication.update += OnEditorUpdate;
        }

        /// <summary>
        /// 注销编辑器生命周期回调
        /// </summary>
        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            EditorApplication.update -= OnEditorUpdate;
        }

        /// <summary>
        /// 处理播放模式变化
        /// </summary>
        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
                statusMessage = "已进入 Play 模式 可以管理运行时存档";
            else if (state == PlayModeStateChange.ExitingPlayMode)
                statusMessage = "正在退出 Play 模式";

            Repaint();
        }

        /// <summary>
        /// 刷新运行时存档状态
        /// </summary>
        private void OnEditorUpdate()
        {
            if (Application.isPlaying)
                Repaint();
        }

        /// <summary>
        /// 绘制窗口内容
        /// </summary>
        public void DrawEmbeddedGUI()
        {
            OnGUI();
        }

        private void OnGUI()
        {
            DrawHeader();

            ArchiveMgr archiveMgr = TryGetArchiveMgr();
            if (archiveMgr == null)
            {
                EditorGUILayout.HelpBox("请进入 Play 模式并确保场景中存在 ModuleHub", MessageType.Info);
                return;
            }

            DrawArchivePath(archiveMgr);
            EditorGUILayout.Space(4f);

            currentTab = (ETab)GUILayout.Toolbar((int)currentTab, new[] { "槽位管理", "模块诊断" });
            EditorGUILayout.Space(6f);

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
            if (currentTab == ETab.Slots)
                DrawSlotsTab(archiveMgr);
            else
                DrawModulesTab(archiveMgr);
            EditorGUILayout.EndScrollView();

            if (!string.IsNullOrEmpty(statusMessage))
            {
                EditorGUILayout.Space(4f);
                EditorGUILayout.HelpBox(statusMessage, MessageType.None);
            }
        }

        #endregion

        #region 界面绘制

        /// <summary>
        /// 绘制窗口标题
        /// </summary>
        private void DrawHeader()
        {
            EditorGUILayout.LabelField("MieMie 存档系统", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("槽位管理 · 读写控制 · 存档诊断", EditorStyles.miniLabel);
        }

        /// <summary>
        /// 绘制存档路径信息
        /// </summary>
        private void DrawArchivePath(ArchiveMgr archiveMgr)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginDisabledGroup(true);
                EditorGUILayout.TextField("存档根目录", archiveMgr.RootPath);
                EditorGUI.EndDisabledGroup();

                if (GUILayout.Button("打开", GUILayout.Width(52f)))
                    OpenArchiveFolder(archiveMgr.RootPath);
            }
        }

        /// <summary>
        /// 绘制槽位管理页
        /// </summary>
        private void DrawSlotsTab(ArchiveMgr archiveMgr)
        {
            DrawArchiveActions(archiveMgr);
            EditorGUILayout.Space(6f);
            DrawCreateSlot(archiveMgr);
            DrawRenameSlot(archiveMgr);
            EditorGUILayout.Space(6f);
            DrawSlotList(archiveMgr);
        }

        /// <summary>
        /// 绘制存档读写操作
        /// </summary>
        private void DrawArchiveActions(ArchiveMgr archiveMgr)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存当前槽位"))
                    SaveCurrentArchive(archiveMgr);

                if (GUILayout.Button("加载当前槽位"))
                    LoadCurrentArchive(archiveMgr);

                if (GUILayout.Button("刷新"))
                    RefreshWindow("已刷新存档状态");
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("清理孤立槽位"))
                    CleanupOrphanedSlots(archiveMgr);

                if (GUILayout.Button("清理孤立文件"))
                    CleanupOrphanedFiles(archiveMgr);
            }
        }

        /// <summary>
        /// 绘制创建槽位区域
        /// </summary>
        private void DrawCreateSlot(ArchiveMgr archiveMgr)
        {
            EditorGUILayout.LabelField("创建存档槽", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                createSlotName = EditorGUILayout.TextField("显示名称", createSlotName);
                if (GUILayout.Button("创建", GUILayout.Width(52f)))
                    CreateSlot(archiveMgr);
            }
        }

        /// <summary>
        /// 绘制重命名槽位区域
        /// </summary>
        private void DrawRenameSlot(ArchiveMgr archiveMgr)
        {
            if (string.IsNullOrEmpty(selectedSlotId))
                return;

            ISlot slot = FindSlot(archiveMgr.GetAllSlotIndex().Slots, selectedSlotId);
            if (slot == null)
            {
                selectedSlotId = null;
                renameSlotName = string.Empty;
                return;
            }

            EditorGUILayout.LabelField("重命名存档槽", EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope(EditorStyles.helpBox))
            {
                renameSlotName = EditorGUILayout.TextField("显示名称", renameSlotName);
                if (GUILayout.Button("保存", GUILayout.Width(52f)))
                    RenameSlot(archiveMgr);

                if (GUILayout.Button("取消", GUILayout.Width(52f)))
                {
                    selectedSlotId = null;
                    renameSlotName = string.Empty;
                }
            }
        }

        /// <summary>
        /// 绘制槽位列表
        /// </summary>
        private void DrawSlotList(ArchiveMgr archiveMgr)
        {
            ISlotIndex slotIndex = archiveMgr.GetAllSlotIndex();
            ISlot currentSlot = slotIndex.CurrentSlot;
            IReadOnlyList<ISlot> slotList = slotIndex.Slots;

            EditorGUILayout.LabelField($"存档槽位  {slotList.Count}", EditorStyles.boldLabel);
            if (slotList.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有存档槽位", MessageType.None);
                return;
            }

            for (int i = 0; i < slotList.Count; i++)
            {
                ISlot slot = slotList[i];
                bool isCurrent = currentSlot != null && currentSlot.SlotId == slot.SlotId;
                DrawSlotRow(archiveMgr, slot, isCurrent);
                EditorGUILayout.Space(4f);
            }
        }

        /// <summary>
        /// 绘制单个槽位信息
        /// </summary>
        private void DrawSlotRow(ArchiveMgr archiveMgr, ISlot slot, bool isCurrent)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(isCurrent ? "当前槽位" : "存档槽", GUILayout.Width(64f));
                    EditorGUILayout.LabelField(slot.DisplayName, EditorStyles.boldLabel);

                    if (!isCurrent && GUILayout.Button("切换", GUILayout.Width(52f)))
                        SwitchSlot(archiveMgr, slot.SlotId);

                    if (GUILayout.Button("重命名", GUILayout.Width(64f)))
                    {
                        selectedSlotId = slot.SlotId;
                        renameSlotName = slot.DisplayName;
                    }

                    if (GUILayout.Button("删除", GUILayout.Width(52f)))
                        DeleteSlot(archiveMgr, slot);
                }

                EditorGUILayout.LabelField("ID", slot.SlotId, EditorStyles.miniLabel);
                EditorGUILayout.LabelField("创建时间", FormatTimestamp(slot.CreateTime), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("最后保存", FormatTimestamp(slot.LastSaveTime), EditorStyles.miniLabel);
                EditorGUILayout.LabelField("文件大小", GetSlotFileSizeText(archiveMgr, slot.SlotId), EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 绘制模块诊断页
        /// </summary>
        private void DrawModulesTab(ArchiveMgr archiveMgr)
        {
            IReadOnlyList<IArchiveModule> moduleList = archiveMgr.GetModules();
            EditorGUILayout.LabelField($"已注册模块  {moduleList.Count}", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox("模块按注册顺序参与 Save 与 Load", MessageType.Info);

            if (moduleList.Count == 0)
            {
                EditorGUILayout.HelpBox("当前没有注册存档模块", MessageType.None);
                return;
            }

            for (int i = 0; i < moduleList.Count; i++)
            {
                IArchiveModule module = moduleList[i];
                using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
                {
                    EditorGUILayout.LabelField($"{i + 1}. {module.ModuleName}", EditorStyles.boldLabel);
                    EditorGUILayout.LabelField(module.GetType().FullName, EditorStyles.miniLabel);
                }
            }
        }

        #endregion

        #region 存档操作

        /// <summary>
        /// 创建存档槽位
        /// </summary>
        private void CreateSlot(ArchiveMgr archiveMgr)
        {
            if (string.IsNullOrWhiteSpace(createSlotName))
            {
                EditorUtility.DisplayDialog("创建存档槽", "请输入存档槽显示名称", "确定");
                return;
            }

            try
            {
                ISlot slot = archiveMgr.CreatSlot(createSlotName.Trim());
                selectedSlotId = slot.SlotId;
                renameSlotName = slot.DisplayName;
                RefreshWindow($"已创建存档槽 {slot.DisplayName}");
            }
            catch (Exception exception)
            {
                ShowOperationException("创建存档槽失败", exception);
            }
        }

        /// <summary>
        /// 重命名存档槽位
        /// </summary>
        private void RenameSlot(ArchiveMgr archiveMgr)
        {
            if (string.IsNullOrWhiteSpace(renameSlotName))
            {
                EditorUtility.DisplayDialog("重命名存档槽", "请输入存档槽显示名称", "确定");
                return;
            }

            try
            {
                archiveMgr.RenameSlot(selectedSlotId, renameSlotName.Trim());
                RefreshWindow("存档槽已重命名");
            }
            catch (Exception exception)
            {
                ShowOperationException("重命名存档槽失败", exception);
            }
        }

        /// <summary>
        /// 切换当前存档槽位
        /// </summary>
        private void SwitchSlot(ArchiveMgr archiveMgr, string slotId)
        {
            try
            {
                archiveMgr.SwitchSlot(slotId);
                RefreshWindow("当前存档槽已切换");
            }
            catch (Exception exception)
            {
                ShowOperationException("切换存档槽失败", exception);
            }
        }

        /// <summary>
        /// 删除存档槽位
        /// </summary>
        private void DeleteSlot(ArchiveMgr archiveMgr, ISlot slot)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "删除存档槽",
                $"确定删除存档槽 {slot.DisplayName} 及其存档文件吗",
                "删除",
                "取消");
            if (!confirmed)
                return;

            try
            {
                archiveMgr.DeleteSlot(slot.SlotId);
                if (selectedSlotId == slot.SlotId)
                {
                    selectedSlotId = null;
                    renameSlotName = string.Empty;
                }

                RefreshWindow("存档槽已删除");
            }
            catch (Exception exception)
            {
                ShowOperationException("删除存档槽失败", exception);
            }
        }

        /// <summary>
        /// 保存当前存档
        /// </summary>
        private void SaveCurrentArchive(ArchiveMgr archiveMgr)
        {
            if (archiveMgr.GetAllSlotIndex().CurrentSlot == null)
            {
                RefreshWindow("当前没有选中的存档槽位");
                return;
            }

            try
            {
                archiveMgr.Save();
                RefreshWindow("当前存档已保存");
            }
            catch (Exception exception)
            {
                ShowOperationException("保存存档失败", exception);
            }
        }

        /// <summary>
        /// 加载当前存档
        /// </summary>
        private void LoadCurrentArchive(ArchiveMgr archiveMgr)
        {
            if (!archiveMgr.HasSaveData())
            {
                RefreshWindow("当前槽位没有可加载的存档文件");
                return;
            }

            try
            {
                archiveMgr.Load();
                RefreshWindow("当前存档已加载");
            }
            catch (Exception exception)
            {
                ShowOperationException("加载存档失败", exception);
            }
        }

        /// <summary>
        /// 清理索引中不存在文件的槽位
        /// </summary>
        private void CleanupOrphanedSlots(ArchiveMgr archiveMgr)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "清理孤立槽位",
                "这会删除索引中找不到存档文件的槽位记录 确定继续吗",
                "清理",
                "取消");
            if (!confirmed)
                return;

            try
            {
                archiveMgr.CleanupOrphanedSlots();
                RefreshWindow("孤立槽位清理完成");
            }
            catch (Exception exception)
            {
                ShowOperationException("清理孤立槽位失败", exception);
            }
        }

        /// <summary>
        /// 清理目录中不存在索引的文件
        /// </summary>
        private void CleanupOrphanedFiles(ArchiveMgr archiveMgr)
        {
            bool confirmed = EditorUtility.DisplayDialog(
                "清理孤立文件",
                "这会删除没有对应槽位记录的存档文件 确定继续吗",
                "清理",
                "取消");
            if (!confirmed)
                return;

            try
            {
                archiveMgr.CleanupOrphanedFiles();
                RefreshWindow("孤立文件清理完成");
            }
            catch (Exception exception)
            {
                ShowOperationException("清理孤立文件失败", exception);
            }
        }

        #endregion

        #region 查询辅助

        /// <summary>
        /// 获取当前运行时存档管理器
        /// </summary>
        private static ArchiveMgr TryGetArchiveMgr()
        {
            if (!Application.isPlaying || ModuleHub.Instance == null)
                return null;

            return ModuleHub.Instance.GetArchive<ArchiveMgr>();
        }

        /// <summary>
        /// 查找指定 ID 的存档槽
        /// </summary>
        private static ISlot FindSlot(IReadOnlyList<ISlot> slotList, string slotId)
        {
            for (int i = 0; i < slotList.Count; i++)
            {
                if (slotList[i].SlotId == slotId)
                    return slotList[i];
            }

            return null;
        }

        /// <summary>
        /// 获取槽位文件大小文本
        /// </summary>
        private static string GetSlotFileSizeText(ArchiveMgr archiveMgr, string slotId)
        {
            string path = Path.Combine(archiveMgr.RootPath, $"{slotId}_SaveData.msgpack");
            if (!File.Exists(path))
                return "文件不存在";

            long byteCount = new FileInfo(path).Length;
            if (byteCount >= BytesPerMegabyte)
                return $"{(double)byteCount / BytesPerMegabyte:0.00} MB";

            return $"{(double)byteCount / BytesPerKilobyte:0.00} KB";
        }

        /// <summary>
        /// 格式化 Unix 时间戳
        /// </summary>
        private static string FormatTimestamp(long timestamp)
        {
            if (timestamp <= 0)
                return "未记录";

            return DateTimeOffset
                .FromUnixTimeSeconds(timestamp)
                .ToLocalTime()
                .ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 打开存档目录
        /// </summary>
        private static void OpenArchiveFolder(string path)
        {
            if (!Directory.Exists(path))
            {
                EditorUtility.DisplayDialog("存档目录", "存档目录不存在", "确定");
                return;
            }

            EditorUtility.RevealInFinder(path);
        }

        /// <summary>
        /// 更新窗口状态信息
        /// </summary>
        private void RefreshWindow(string message)
        {
            statusMessage = message;
            Repaint();
        }

        /// <summary>
        /// 显示存档操作异常
        /// </summary>
        private void ShowOperationException(string title, Exception exception)
        {
            Debug.LogException(exception);
            EditorUtility.DisplayDialog(title, exception.Message, "确定");
        }

        #endregion
    }
}

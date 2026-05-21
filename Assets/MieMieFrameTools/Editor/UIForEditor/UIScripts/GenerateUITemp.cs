using System.Collections.Generic;
using MieMieUITools.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// UI 脚本生成器窗口（独立工具，不依赖 FrameSetting）。
/// </summary>
public class GenerateUITemp : EditorWindow
{
    [SerializeField]
    private VisualTreeAsset m_VisualTreeAsset = default;

    private VisualElement root;

    private TextField classNameField;
    private UnityEditor.UIElements.ObjectField prefabFieldGameObject;
    private TextField checkPathField;
    private Button selectButton;
    private Button defaultPathButton;

    private ScrollView mappingScrollView;
    private Button addMappingButton;
    private Button resetMappingButton;
    private Button setNewDefaultButton;
    private Button copyMappingButton;
    private Button saveMappingButton;
    private Button creatButton;

    private TextField defaultGenFolderField;
    private Button saveDefaultFolderButton;
    private Button clearPathRecordsButton;

    private string className;
    private GameObject prefab;

    [MenuItem("Tools/UI/GenerateUITemp")]
    public static void ShowExample()
    {
        GenerateUITemp wnd = GetWindow<GenerateUITemp>();
        wnd.titleContent = new GUIContent("GenerateUITemp");
    }

    public void CreateGUI()
    {
        root = rootVisualElement;
        if (m_VisualTreeAsset == null)
        {
            string uxmlPath = UIToolLocator.TryFindGenerateUITempUxmlPath(out string p) ? p : null;
            m_VisualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(uxmlPath);
            if (m_VisualTreeAsset == null)
            {
                Debug.LogError(
                    $"GenerateUITemp.uxml 无法加载: {(uxmlPath ?? "(null)")}。请确认 UIForEditor/UIToolkits/GenerateUITemp.uxml 存在。");
                return;
            }
        }

        root.Add(m_VisualTreeAsset.CloneTree());

        InitBaseInfoElement();
        InitPathSettingsSection();
        InitMappingSection();
    }

    #region 基础信息栏

    private void InitBaseInfoElement()
    {
        className = string.Empty;
        prefab = null;

        classNameField = root.Q<TextField>("ClassName");
        prefabFieldGameObject = root.Q<UnityEditor.UIElements.ObjectField>("PrefabGameObject");
        checkPathField = root.Q<TextField>("CheckPath");
        selectButton = root.Q<Button>("SelectButton");
        defaultPathButton = root.Q<Button>("DefaultPahtButton");

        prefabFieldGameObject.RegisterValueChangedCallback(OnPrefabFieldGameObjectChanged);
        defaultPathButton.RegisterCallback<ClickEvent>(OnDefaultPathButtonClicked);
        selectButton.RegisterCallback<ClickEvent>(OnSelectButtonClicked);

        if (checkPathField != null && string.IsNullOrEmpty(checkPathField.value))
            checkPathField.value = UIGenPathSettings.GetDefaultFolder();
    }

    private void InitPathSettingsSection()
    {
        defaultGenFolderField = root.Q<TextField>("DefaultGenFolder");
        saveDefaultFolderButton = root.Q<Button>("SaveDefaultFolderButton");
        clearPathRecordsButton = root.Q<Button>("ClearPathRecordsButton");

        if (defaultGenFolderField != null)
            defaultGenFolderField.value = UIGenPathSettings.GetDefaultFolder();

        saveDefaultFolderButton?.RegisterCallback<ClickEvent>(OnSaveDefaultFolderClicked);
        clearPathRecordsButton?.RegisterCallback<ClickEvent>(OnClearPathRecordsClicked);
    }

    private void OnSaveDefaultFolderClicked(ClickEvent evt)
    {
        string folder = defaultGenFolderField != null
            ? defaultGenFolderField.value
            : checkPathField?.value;
        UIGenPathSettings.SetDefaultFolder(folder);
        if (defaultGenFolderField != null)
            defaultGenFolderField.value = UIGenPathSettings.GetDefaultFolder();
        EditorUtility.DisplayDialog("提示", $"默认生成目录已保存:\n{UIGenPathSettings.GetDefaultFolder()}", "确定");
    }

    private void OnClearPathRecordsClicked(ClickEvent evt)
    {
        if (!EditorUtility.DisplayDialog("确认", "清空所有预制体的「上次生成路径」记忆？", "确定", "取消"))
            return;
        UIGenPathSettings.ClearPrefabFolderRecords();
        EditorUtility.DisplayDialog("提示", "路径记忆已清空", "确定");
    }

    private void OnPrefabFieldGameObjectChanged(ChangeEvent<UnityEngine.Object> evt)
    {
        if (evt.newValue == null)
        {
            if (classNameField != null)
                classNameField.value = default(string);
            return;
        }

        if (classNameField != null)
            className = classNameField.value = evt.newValue.name;
        else
            Debug.LogError("classNameField 为空");

        prefab = evt.newValue as GameObject;

        if (prefab != null)
        {
            string lastPath = GetRecordedPathForPrefab(prefab);
            if (!string.IsNullOrEmpty(lastPath))
            {
                checkPathField.value = lastPath;
                Debug.Log($"[GenerateUITemp] 已自动填入上次生成路径: {prefab.name} -> {lastPath}");
            }
        }
    }

    private void OnDefaultPathButtonClicked(ClickEvent evt)
    {
        checkPathField.value = GenerateUITool.GetDefaultUIGenScriptPath();
    }

    private void OnSelectButtonClicked(ClickEvent evt)
    {
        string startDir = UIGenPathSettings.ToAbsoluteFolderForDialog(checkPathField.value);
        string path = EditorUtility.OpenFolderPanel("选择生成文件夹", startDir, "");
        if (string.IsNullOrEmpty(path)) return;

        string assetPath = UIGenPathSettings.NormalizeFolderPath(path);
        checkPathField.value = assetPath;
        if (prefab != null)
            RecordPrefabPath(prefab, assetPath);
    }

    private string GetRecordedPathForPrefab(GameObject targetPrefab)
    {
        if (targetPrefab == null) return null;
        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        if (string.IsNullOrEmpty(prefabPath)) return null;
        string guid = AssetDatabase.AssetPathToGUID(prefabPath);
        return UIGenPathSettings.GetLastFolderForPrefab(guid);
    }

    private void RecordPrefabPath(GameObject targetPrefab, string generatePath)
    {
        if (targetPrefab == null || string.IsNullOrEmpty(generatePath)) return;
        string prefabPath = AssetDatabase.GetAssetPath(targetPrefab);
        if (string.IsNullOrEmpty(prefabPath)) return;
        string guid = AssetDatabase.AssetPathToGUID(prefabPath);
        UIGenPathSettings.SetFolderForPrefab(guid, targetPrefab.name, generatePath);
    }

    #endregion

    #region 映射表区域

    private void InitMappingSection()
    {
        mappingScrollView = root.Q<ScrollView>("MappingScrollView");
        addMappingButton = root.Q<Button>("AddMappingButton");
        resetMappingButton = root.Q<Button>("ResetMappingButton");
        setNewDefaultButton = root.Q<Button>("SetNewDefaultButton");
        copyMappingButton = root.Q<Button>("CopyMappingButton");
        saveMappingButton = root.Q<Button>("SaveMappingButton");
        creatButton = root.Q<Button>("CreatButton");

        addMappingButton.RegisterCallback<ClickEvent>(OnAddMappingClicked);
        resetMappingButton.RegisterCallback<ClickEvent>(OnResetMappingClicked);
        setNewDefaultButton.RegisterCallback<ClickEvent>(OnSetNewDefaultClicked);
        copyMappingButton.RegisterCallback<ClickEvent>(OnCopyMappingClicked);
        saveMappingButton.RegisterCallback<ClickEvent>(OnSaveMappingClicked);
        creatButton.RegisterCallback<ClickEvent>(OnCreatButtonClicked);

        RefreshMappingTable();
    }

    private void RefreshMappingTable()
    {
        if (mappingScrollView == null) return;

        mappingScrollView.Clear();
        foreach (var pair in UIGenPathSettings.GetPrefixMappings())
            AddMappingRow(pair.prefix, pair.componentType);
    }

    private void AddMappingRow(string prefixValue, string typeValue)
    {
        var row = new VisualElement();
        row.AddToClassList("mapping-row");

        var prefixField = new TextField();
        prefixField.AddToClassList("mapping-prefix-field");
        prefixField.value = prefixValue ?? string.Empty;
        prefixField.RegisterValueChangedCallback(_ => MarkMappingDirty());

        var arrowLabel = new Label("→");
        arrowLabel.AddToClassList("mapping-arrow");

        var typeField = new TextField();
        typeField.AddToClassList("mapping-type-field");
        typeField.value = typeValue ?? string.Empty;
        typeField.RegisterValueChangedCallback(_ => MarkMappingDirty());

        var deleteBtn = new Button(() => OnDeleteMappingRow(row));
        deleteBtn.AddToClassList("mapping-delete-btn");
        deleteBtn.text = "删除";

        row.Add(prefixField);
        row.Add(arrowLabel);
        row.Add(typeField);
        row.Add(deleteBtn);
        mappingScrollView.Add(row);
    }

    private void MarkMappingDirty()
    {
        saveMappingButton.style.backgroundColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
    }

    private void OnAddMappingClicked(ClickEvent evt)
    {
        AddMappingRow("NewPrefix", "ComponentType");
        MarkMappingDirty();
    }

    private void OnDeleteMappingRow(VisualElement row)
    {
        mappingScrollView.Remove(row);
        MarkMappingDirty();
    }

    private void OnResetMappingClicked(ClickEvent evt)
    {
        if (!EditorUtility.DisplayDialog("确认重置", "确定要将映射表重置为内置默认？", "确定", "取消"))
            return;

        UIGenPathSettings.ResetPrefixMappingsToBuiltIn();
        UIGenPathSettings.InvalidateCache();
        GenerateUITool.RefreshPrefixToTypeMap();
        RefreshMappingTable();
        EditorUtility.DisplayDialog("提示", "映射表已重置（已写入 UIGenPathSettings.json）", "确定");
    }

    private void OnSetNewDefaultClicked(ClickEvent evt)
    {
        if (!EditorUtility.DisplayDialog("确认", "将当前映射表保存到 UIGenPathSettings.json？", "确定", "取消"))
            return;

        SaveMappingFromUI();
        EditorUtility.DisplayDialog("提示", "映射已保存到 UIGenPath/UIGenPathSettings.json", "确定");
    }

    private void OnCopyMappingClicked(ClickEvent evt)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var pair in UIGenPathSettings.GetPrefixMappings())
            sb.AppendLine($"{pair.prefix} -> {pair.componentType}");

        EditorGUIUtility.systemCopyBuffer = sb.ToString();
        EditorUtility.DisplayDialog("提示", "映射表已复制到剪切板", "确定");
    }

    private void OnSaveMappingClicked(ClickEvent evt)
    {
        SaveMappingFromUI();
        saveMappingButton.style.backgroundColor = Color.clear;
        EditorUtility.DisplayDialog("提示", "映射表已保存到 UIGenPathSettings.json", "确定");
    }

    private void SaveMappingFromUI()
    {
        var list = new List<UIPrefixMappingEntry>();
        foreach (var child in mappingScrollView.Children())
        {
            var prefixField = child.Q<TextField>(className: "mapping-prefix-field");
            var typeField = child.Q<TextField>(className: "mapping-type-field");
            if (prefixField == null || typeField == null) continue;

            string prefix = prefixField.value.Trim();
            string type = typeField.value.Trim();
            if (string.IsNullOrEmpty(prefix) || string.IsNullOrEmpty(type)) continue;

            list.Add(new UIPrefixMappingEntry { prefix = prefix, componentType = type });
        }

        UIGenPathSettings.SetPrefixMappings(list);
        UIGenPathSettings.InvalidateCache();
        GenerateUITool.RefreshPrefixToTypeMap();
    }

    private void OnCreatButtonClicked(ClickEvent evt)
    {
        if (string.IsNullOrEmpty(className))
        {
            EditorUtility.DisplayDialog("错误", "请输入类名", "确定");
            return;
        }

        if (prefab == null)
        {
            EditorUtility.DisplayDialog("错误", "请拖入UI预制体", "确定");
            return;
        }

        if (string.IsNullOrEmpty(checkPathField.value))
        {
            EditorUtility.DisplayDialog("错误", "请选择生成路径", "确定");
            return;
        }

        SaveMappingFromUI();

        string outputFolder = UIGenPathSettings.NormalizeFolderPath(checkPathField.value);
        if (string.IsNullOrEmpty(outputFolder))
        {
            EditorUtility.DisplayDialog("错误", "生成路径无效，请使用工程内 Assets/ 目录", "确定");
            return;
        }

        checkPathField.value = outputFolder;
        RecordPrefabPath(prefab, outputFolder);

        GenerateUITool.GenerateUITemplates(className, prefab, outputFolder);
    }

    #endregion
}

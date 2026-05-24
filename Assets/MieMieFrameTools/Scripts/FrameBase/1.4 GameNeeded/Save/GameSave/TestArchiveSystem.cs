// Assets/Scripts/GameSave/TestArchiveSystem.cs
// 存档系统测试用例
// 测试内容：槽位管理、存档读写、模块注册

using UnityEngine;
using System.Linq;
using MieMieFrameTools;
using Game.Save;

public class TestArchiveSystem : MonoBehaviour
{
    private ArchiveMgr archiveMgr;

    private void Start()
    {
        string rootPath = System.IO.Path.Combine(Application.persistentDataPath, "Archives", "TestPlayer");
        archiveMgr = new ArchiveMgr(rootPath);

        archiveMgr.RegisterModule(new TestPlayerModule());
        archiveMgr.RegisterModule(new TestEquipmentModule());

        if (archiveMgr.TryGetModule(out TestPlayerModule playerMod))
            Debug.Log($"[泛型查找] 玩家模块: {playerMod.ModuleName}");
        if (archiveMgr.HasModule<TestEquipmentModule>())
            Debug.Log("[泛型查找] 装备模块已注册");

        Debug.Log($"存档根路径: {archiveMgr.RootPath}");

        RunAllTests();
    }

    // ==================== 测试用例 ====================

    private void RunAllTests()
    {
        Debug.Log("========== 开始存档系统测试 ==========");

        // 清理已有槽位，确保每次测试都从干净状态开始
        var existingSlots = archiveMgr.GetAllSlotIndex().Slots.ToList();
        foreach (var slot in existingSlots)
            archiveMgr.DeleteSlot(slot.SlotId);

        TestCreateSlot();
        TestSwitchSlot();
        TestRenameSlot();
        TestSaveAndLoad();
        TestHasSaveData();
        TestDeleteSlot(); 

        Debug.Log("========== 测试完成 ==========");
    }

    /// <summary>测试1: 创建存档槽</summary>
    private void TestCreateSlot()
    {
        Debug.Log("--- 测试: 创建存档槽 ---");

        var slot1 = archiveMgr.CreatSlot("存档槽1-主号");
        Debug.Log($"创建槽位1: ID={slot1.SlotId}, 名称={slot1.DisplayName}");

        var slot2 = archiveMgr.CreatSlot("存档槽2-小号");
        Debug.Log($"创建槽位2: ID={slot2.SlotId}, 名称={slot2.DisplayName}");

        var slotIndex = archiveMgr.GetAllSlotIndex();
        Debug.Log($"当前槽位数量: {slotIndex.Count}");
        Debug.Log($"当前选中槽位: {slotIndex.CurrentSlot?.DisplayName}");
    }

    /// <summary>测试2: 切换存档槽</summary>
    private void TestSwitchSlot()
    {
        Debug.Log("--- 测试: 切换存档槽 ---");

        var slotIndex = archiveMgr.GetAllSlotIndex();
        var slots = slotIndex.Slots;

        if (slots.Count >= 2)
        {
            archiveMgr.SwitchSlot(slots[1].SlotId);
            Debug.Log($"已切换到: {slotIndex.CurrentSlot?.DisplayName}");

            archiveMgr.SwitchSlot(slots[0].SlotId);
            Debug.Log($"已切换到: {slotIndex.CurrentSlot?.DisplayName}");
        }
    }

    /// <summary>测试3: 重命名存档槽</summary>
    private void TestRenameSlot()
    {
        Debug.Log("--- 测试: 重命名存档槽 ---");

        var slotIndex = archiveMgr.GetAllSlotIndex();
        var currentSlot = slotIndex.CurrentSlot;

        if (currentSlot != null)
        {
            Debug.Log($"重命名前: {currentSlot.DisplayName}");
            archiveMgr.RenameSlot(currentSlot.SlotId, "新名字-测试");
            Debug.Log($"重命名后: {slotIndex.CurrentSlot?.DisplayName}");
        }
    }

    /// <summary>测试4: 保存和加载</summary>
    private void TestSaveAndLoad()
    {
        Debug.Log("--- 测试: 保存和加载 ---");

        var slotIndex = archiveMgr.GetAllSlotIndex();
        Debug.Log($"当前槽位: {slotIndex.CurrentSlot?.DisplayName}");

        var slot = slotIndex.CurrentSlot;
        if (slot == null) return;

        var saveData = archiveMgr.GetArchive() ?? new SaveData();

        // 各模块创建新存档
        foreach (var module in archiveMgr.GetModules())
            module.CreateArchive(saveData);

        // 保存
        archiveMgr.Save();
        Debug.Log("存档已保存!");

        // 修改数据后再次保存
        ModifyTestData();
        archiveMgr.Save();
        Debug.Log("修改后存档已保存!");

        // 读取验证
        var loadedData = archiveMgr.GetArchive();
        if (loadedData != null)
        {
            Debug.Log($"读取成功! 元信息: Version={loadedData.Meta?.Version}, LastSaveTime={loadedData.Meta?.LastSaveTime}");
        }
    }

    /// <summary>测试5: 判断存档是否存在</summary>
    private void TestHasSaveData()
    {
        Debug.Log("--- 测试: 判断存档是否存在 ---");
        Debug.Log($"当前槽位是否存在存档: {archiveMgr.HasSaveData()}");
    }

    /// <summary>测试6: 删除存档槽</summary>
    private void TestDeleteSlot()
    {
        Debug.Log("--- 测试: 删除存档槽 ---");

        var slotIndex = archiveMgr.GetAllSlotIndex();
        var slots = slotIndex.Slots;

        // 删除最后一个测试槽位
        if (slots.Count >= 2)
        {
            var toDelete = slots[^1];
            Debug.Log($"删除前槽位数量: {slotIndex.Count}");
            archiveMgr.DeleteSlot(toDelete.SlotId);
            Debug.Log($"已删除槽位: {toDelete.DisplayName}");
            Debug.Log($"删除后槽位数量: {slotIndex.Count}");
            Debug.Log($"当前选中槽位: {slotIndex.CurrentSlot?.DisplayName}");
        }

        // 清理历史遗留的孤立 .dat 文件
        archiveMgr.CleanupOrphanedFiles();
    }

    // ==================== 辅助方法 ====================

    private void ModifyTestData()
    {
        var saveData = archiveMgr.GetArchive();
        if (saveData?.Player != null)
        {
            Debug.Log("模拟修改玩家数据...");
        }
    }
}

// ==================== 测试用存档模块 ====================

public class TestPlayerModule : IArchiveModule
{
    public string ModuleName => "TestPlayerModule";

    public void CreateArchive(SaveData saveData)
    {
        saveData.Player = new PlayerModuleSave
        {
            PlayerId = System.Guid.NewGuid().ToString(),
            PlayerName = "测试玩家",
            CreateTime = System.DateTimeOffset.UtcNow.ToUnixTimeSeconds()
        };
        Debug.Log($"[{ModuleName}] 创建新存档: PlayerId={saveData.Player.PlayerId}");
    }

    public void FromArchive(SaveData saveData)
    {
        if (saveData.Player != null)
        {
            Debug.Log($"[{ModuleName}] 加载存档: PlayerId={saveData.Player.PlayerId}, Name={saveData.Player.PlayerName}");
        }
        else
        {
            Debug.Log($"[{ModuleName}] 存档中无玩家数据");
        }
    }

    public void ToArchive(SaveData saveData)
    {
        Debug.Log($"[{ModuleName}] 写入存档");
    }
}

public class TestEquipmentModule : IArchiveModule
{
    public string ModuleName => "TestEquipmentModule";

    public void CreateArchive(SaveData saveData)
    {
        saveData.Equpment = new EquipmentModuleSave { ActiveSetIndex = 0 };
        saveData.Equpment.Items.Add(new EquipmentItem { InstanceId = "item_001", Level = 1, SlotType = 1 });
        saveData.Equpment.Items.Add(new EquipmentItem { InstanceId = "item_002", Level = 5, SlotType = 2 });
        Debug.Log($"[{ModuleName}] 创建装备数据，共 {saveData.Equpment.Items.Count} 件");
    }

    public void FromArchive(SaveData saveData)
    {
        if (saveData.Equpment != null)
        {
            Debug.Log($"[{ModuleName}] 加载装备: {saveData.Equpment.Items.Count} 件, ActiveSetIndex={saveData.Equpment.ActiveSetIndex}");
            foreach (var item in saveData.Equpment.Items)
                Debug.Log($"  - {item.InstanceId}: Level={item.Level}, Slot={item.SlotType}");
        }
    }

    public void ToArchive(SaveData saveData)
    {
        Debug.Log($"[{ModuleName}] 写入装备存档");
    }
}

# Luban 运行时结构（最小说明）

生成代码在 `Luban/Generated/` 手写加载逻辑不要放进该目录  
改表只改 `DataTables/` 再跑 `DataTables/gen.bat`  
工具链见 `DataTables/README.md`

---

## 1 三层各是什么

| 角色 | 类 | 对应 |
|---|---|---|
| 总表 | `cfg.Tables` | 所有配置表的入口 |
| 子表 | `cfg.demo.Tbitem` | Excel 整张 `#demo.item.xlsx` |
| 一行 | `cfg.demo.item` | 表里的一条记录（Bean） |

Bean = 一条有类型的记录
`vector2` 也是 Bean 只是嵌在字段里的复合结构 不一定对应一张表

命名：`demo` 是模块 `Tb` 是 Table `item` 是行类型  
JSON 文件名由同一规则生成：`模块_表名` 全小写 → `demo_tbitem.json`

---

## 2 数据从哪来

```text
DataTables/Data/#demo.item.xlsx
    ↓ gen.bat
代码  Luban/Generated/Tables.cs  Tbitem.cs  item.cs
JSON  StreamingAssets/DataTables/demo_tbitem.json
```

哪份 JSON 进哪张子表 由生成代码写死 不是运行时猜：

```csharp
Tbitem = new demo.Tbitem(loader("demo_tbitem"));
```

`loader` 只按文件名读磁盘 不负责认表  
技能表若存在 会另有 `loader("demo_tbskill")` → `new Tbskill` 不会和 `Tbitem` 混绑

---

## 3 什么时候进内存 / 什么时候进对象

分两阶段 都发生在 `new cfg.Tables(loader)` 里

```text
阶段1 灌数据（进内存 = 进对象 同时发生）
  loader("demo_tbitem")     读 JSON → JArray
  new Tbitem(jarray)        逐行 Deserializeitem → new item
                            Add 进 DataList 与 DataMap

阶段2 接线（不读文件 不 new 行）
  ResolveRef                把外键 Id 换成另一张表的对象引用
                            当前 item 无跨表引用 此步为空
```

行对象在阶段1已经构造完成 `Id/Name/Desc/Count` 已填好  
`ResolveRef` 不是加载 是事后填引用指针

读硬盘不在 Generated 里 由传入的 `Func<string, JArray>` 负责  
当前工程由 `LubanTablesDataModule` 接入 `new cfg.Tables(loader)` 本地文件加载器按生成表名读取 JSON

---

## 4 基础 API

最小加载（Editor / 能直接读 StreamingAssets 的平台）：

```csharp
public static cfg.Tables LoadTables()
{
    return new cfg.Tables(LoadJsonArray);
}

private static Newtonsoft.Json.Linq.JArray LoadJsonArray(string eFileName)
{
    string ePath = System.IO.Path.Combine(
        UnityEngine.Application.streamingAssetsPath,
        "DataTables",
        eFileName + ".json");
    string eText = System.IO.File.ReadAllText(ePath);
    return Newtonsoft.Json.Linq.JArray.Parse(eText);
}
```

查询：

```csharp
cfg.demo.item eItem = eTables.Tbitem.Get(1001);           // 没有则抛 KeyNotFoundException
cfg.demo.item eMaybe = eTables.Tbitem.GetOrDefault(1001); // 没有则 null
cfg.demo.item eSame = eTables.Tbitem[1001];               // 等同 Get

foreach (var eRow in eTables.Tbitem.DataList)
{
    int eId = eRow.Id;
    string eName = eRow.Name;
}
```

| API | 用途 |
|---|---|
| `Get(id)` / `this[id]` | 主键必存在 |
| `GetOrDefault(id)` | 主键可能不存在 |
| `DataList` | 按表顺序遍历 |
| `DataMap` | 只读字典 |

`item` 字段全是 `readonly` 这是配置真值  
运行时数量背包等不要改 `item` 放自己的 RuntimeData

热更配表应整份替换 `Tables` 不要改已有行对象  
框架通过 `LubanConfigModule` 接入启动与释放 由 `LubanTablesDataModule` 直接管理 Init/Reload/Dispose 和当前 `cfg.Tables` 不再依赖通用数据基类

FrameRoot 可以预挂配置组件 ModuleHub 优先复用同物体接口 没有组件时通过通用 GetOrAdd 扩展补挂已安装的 Luban 组件 无需拖入配置槽位 业务等待 `ModuleHub.Instance.ReadyTask` 后通过 `GetConfig<LubanConfigModule>()` 获取配置

启动 所有权 平台限制 重载失败与退出规则统一见 [数据模块生命周期](README.md)

# HybridCLR 热更原理简明理解

先记住一句话：**首包提前装好执行能力 后续下载新 DLL 游戏就能执行其中的新业务代码**

本文结合官方 9.x 文档解释原理 当前项目使用开源社区版 不包含商业版 DHE 和热重载能力

## 1 先分清四个东西

| 名称 | 是什么 |
|---|---|
| C# 源码 | 开发者写的 `.cs` 文件 |
| IL | C# 编译后生成的中间指令 |
| DLL | 本例中保存类型定义和方法 IL 的程序集文件 |
| Player | Unity 打包出来的整套游戏 包括 EXE 原生库和数据目录 |

**游戏运行时不读取 `.cs` 源码 CPU 也不认识 C# 类型 它只执行机器指令**

## 2 打包和运行是两个不同阶段

| 代码 | 构建时 | 游戏运行时 |
|---|---|---|
| 首包普通代码 | C# 编译成 IL 再由 IL2CPP 转成 C++ 并编译成机器码 | CPU 执行已经生成的机器码 |
| 本演示的热更业务代码 | C# 编译成 IL 保存到 DLL 并发布到服务器 | HybridCLR 加载 DLL 转换方法指令并解释执行 调用已有 AOT 功能时可以进入机器码 |

HybridCLR 扩展了 IL2CPP 运行时 使它成为 **AOT + Interpreter 混合运行时** 支持通过 `Assembly.Load` 动态加载程序集

**它的解释发生在游戏运行时 不属于 C# 转 IL 或 IL 转 C++ 的构建阶段**

更准确地说 它会把 DLL 方法中的 IL 转成自己的寄存器指令 再由寄存器解释器执行 CPU 运行的是解释器本身的机器码 调用 Unity 等首包功能时可以进入已有机器码

因此热更 IL 可以运行 **不要求先为它生成新的 C++ 和机器码**

### 它究竟是不是编译器

**HybridCLR 是运行时 其中包含编译器和解释器** 内部编译器负责 IL 到自定义寄存器指令的转换 不是 C# 编译器 也不是把热更代码编译成 CPU 机器码的 JIT

之前把它简单说成“只有解释器 没有编译器”不够准确

### DHE 是另一种执行方式

官方商业旗舰版提供 **DHE 差分混合执行** 首次打包时将指定程序集也编译为 AOT 后续结合新 DLL 和差分文件 让未变化的方法继续执行原生 AOT 实现 变化或新增的方法解释执行

所以不能把所有版本的热更方法都说成只能解释执行 DHE 的实际性能取决于变化范围和调用情况 **当前开源演示没有接入 DHE**

## 3 NewEnemyData 到底怎么进入游戏

假设首次打包时没有 `NewEnemyData` 后来在热更代码里新增了它

1. 发布时 C# 编译器把新类的定义 字段和方法 IL 写进热更 DLL
2. 游戏启动下载 DLL 调用 `Assembly.Load(DllBytes)` 加载程序集 运行时登记其中的新类型
3. **程序集和所需依赖加载成功后** 才调用热更入口 执行 `new NewEnemyData()` 分配对象内存并执行构造函数

此时 `NewEnemyData` 已经是运行时登记的类型 创建出来的对象是内存中的数据 **不是一段仍在等待编译的 C# 源码**

### 首包 A 如何调用新类

首包 A 如果当时不知道 `NewEnemyData` 就不能直接写 `new NewEnemyData()` 否则首次编译会失败

可以在首包提前定义接口

```csharp
public interface IEnemyData
{
    public int Health { get; }
}
```

后来热更 DLL 新增实现

```csharp
public sealed class NewEnemyData : IEnemyData
{
    public int Health => 100;
}
```

热更入口创建 `NewEnemyData` 并通过既有调用入口把对象传给 A A 按 `IEnemyData` 使用它

**A 的机器码没有被拼接或替换 它继续按原有接口调用 新类的方法由 HybridCLR 执行** 接口只是这里采用的调用方式 也可以通过反射 委托等入口协作 HybridCLR 不强制使用这个接口

## 4 AOT 补充元数据负责什么

新类的定义来自 **热更 DLL** 不来自 AOT 补充文件

核心作用是 **解决 AOT 泛型实例化时缺少方法实现的问题**

运行时认识 `List<某类型>` 不代表一定有它的构造函数和 `Add` 等方法实现 如果首包没有相应 AOT 实现 也没有可用的泛型共享实现 就可能出现“类型能找到 方法却不能执行”的问题

补充元数据提供原始泛型方法的 IL 等信息 让 HybridCLR 能实例化并解释执行这些缺失的方法 **补的是泛型方法信息 不是凭空补出新业务类型**

例如新业务使用 `List<NewEnemyData>`

- `NewEnemyData` 的定义来自新 DLL
- `List<T>` 属于首包基础库
- 如果已有可用的 AOT 泛型共享实现 HybridCLR 优先使用它 不必每个新组合都解释执行
- 没有可用实现时 补充 `List<T>` 所在的 `mscorlib.dll` 元数据 便可支持相关泛型方法的解释执行

`NewEnemyData` 是引用类型 某些情况下可以复用已有泛型共享代码 热更新增的结构体例如 `List<NewEnemyStats>` 更容易体现补充元数据的必要性 因为普通泛型共享对值类型存在限制

本演示归档对应首包裁剪后的 AOT DLL 将其作为 `.bytes` 发布 在执行业务前调用 `RuntimeApi.LoadMetadataForAOTAssembly` 一次完成补充 并不是重新生成原生机器码

官方要求保存首包构建时的 AOT DLL 不同 BuildTarget 的文件不可混用 后续补丁不要随意换成重新生成的 AOT DLL

官方 API 的 `Consistent` 模式要求与首包裁剪 DLL 精确一致 `SuperSet` 模式也接受对应程序集的兼容超集 本演示使用 `SuperSet` 但发布工具仍按归档哈希绑定首包

**补充文件必须与首包相容 它不能替换首包机器码 也不能凭空补出任意缺失的底层能力**

## 5 当前演示怎么对应

**本节描述项目自定义流程 不是 HybridCLR 强制规定的目录 组件或版本字段**

- `Bootstrap` 是本项目编写的首包启动代码 负责下载 校验和启动入口 不是 HybridCLR 内置组件
- `HotLogic` 里的代码编译成热更 DLL 由 HybridCLR 执行
- `Data` 中的已采集资源由 YooAsset 打包 下载并按地址加载
- 点击发布会重新编译热更代码并打包资源 因此代码和资源可以同时修改
- `codeVersion` 和资源发布版本是项目自己的管理字段 前者记录代码发布 后者标记整个更新包 每次构建使用未占用的新资源版本
- 发布成功后完全退出并重启 EXE 才会加载新代码 当前实现不在运行中替换已加载 DLL

目录名字可以自定 **代码是否参与热更看程序集配置 资源是否参与更新看 YooAsset 采集配置**

具体配置和操作以已有的 [资源与热更文档](../Assets/MieMieFrameTools/Scripts/Frame/B_Assets/Yooasset/RuntimeAdapter/README.md#启动与代码热更) 为准

官方商业热重载版提供程序集卸载与重新加载能力 需满足旧对象不再使用 旧逻辑退出和依赖顺序等条件 部分 Unity 类型元数据还有保留限制 不能理解为任意程序集随时无条件卸载 当前社区版演示采用重启 EXE 的方式

## 三句话记住

1. **编译器在发布时把新 C# 变成 DLL 中的 IL**
2. **HybridCLR 在运行时加载新类型 将相关方法 IL 转成寄存器指令并解释执行**
3. **CPU 始终执行机器码 已有 AOT 实现可以直接运行 DHE 还可保留未变化业务方法的 AOT 执行**

## 官方文档

以下链接于 2026-10-10 核对 用于区分社区版原理 商业版能力和项目自己的实现

- [HybridCLR 介绍与工作原理](https://www.hybridclr.cn/docs/intro)
- [源码结构与 IL 指令转换](https://www.hybridclr.cn/docs/basic/sourceinspect)
- [使用泛型与补充元数据操作](https://www.hybridclr.cn/docs/beginner/generic)
- [AOT 泛型 泛型共享与补充元数据原理](https://www.hybridclr.cn/docs/basic/aotgeneric)
- [商业版本功能区别](https://www.hybridclr.cn/docs/business/intro)
- [DHE 差分混合执行](https://www.hybridclr.cn/docs/business/differentialhybridexecution)
- [热重载技术与使用限制](https://www.hybridclr.cn/docs/business/reload/hotreloadassembly)

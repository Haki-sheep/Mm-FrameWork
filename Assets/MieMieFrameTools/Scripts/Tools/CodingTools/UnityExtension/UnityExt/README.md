# Unity 通用扩展

`MmUnityExtension` 位于 `MieMieFrameWork` 命名空间 支持 GameObject 和 Component 两种调用入口

## 获取或添加组件

```csharp
var Collider = gameObject.GetOrAddComponent<BoxCollider>();
var SameCollider = transform.GetOrAddComponent<BoxCollider>();
```

只查询同物体 已存在时复用 不存在时调用 Unity AddComponent 不查询父物体或子物体

有程序集引用时可直接使用具体组件类型

```csharp
var Config = gameObject.GetOrAddComponent<LubanConfigModule>();
```

不能直接引用具体组件程序集时 使用接口加已经解析的具体组件 Type

```csharp
var Config = gameObject.GetOrAddComponent<IConfigModule>(ConfigType);
```

传入类型必须继承 Component 并实现或继承泛型契约 不兼容时抛出异常 已有同物体实现优先复用

扩展只负责组件获取与创建 不调用 Init 或 InitComponents Unity 自身的 Awake 与 OnEnable 仍按 AddComponent 生命周期执行

接口本身不能作为 AddComponent 的创建类型 接口调用必须提供具体组件 Type 不扫描所有程序集猜测实现

框架配置补挂与默认目录规则见 [配置生命周期](../../../../Frame/C_Data/README.md)

# 运行中 Unity Editor 结构化操作与 Roslyn 在线线路

在线线路用于 Editor 内存态、画面或 Unity/第三方 API，也可作为离线契约不足时的受约束兜底。
普通资产创作优先离线语义命令；在线结构化操作优先，最后才使用 Roslyn。
对象/引用/Curve/Gradient/Prefab 见 [editor-inspector-prefab](editor-inspector-prefab.md)，
UGUI 观察/点击见 [editor-ui](editor-ui.md)。精确接口以实时 schema 为准。

## 状态与安装

“初始化 FakeUnityCLI”是明确的 Package 更新触发词：先执行 `<fuc> editor bridge-update --project <root> --dry-run --json`。运行中的 Bridge 1.8.0+ 使用 `bridge-update --yes` 热更新并只触发一次正常 Domain Reload；无 Bridge 或旧 Bridge 才关闭 Unity 执行一次 `bridge-install` bootstrap。

```text
<fuc> editor status --project <root> --json
<fuc> editor compile-diagnose --project <root> --json
<fuc> editor bridge-install --project <root> --json
<fuc> editor bridge-update --project <root> --dry-run --json
<fuc> editor bridge-update --project <root> --yes --timeout 180 --json
<fuc> editor provider-list --project <root> --json
<fuc> editor template-list --project <root> --json
```

每个 Unity 工程首次使用在线线路时离线安装一次 Bridge。后续先执行 `editor status`；运行中 Bridge 具备 `bridge_live_update` 时使用 `editor bridge-update`，由 Bridge 暂停 Auto Refresh/程序集 reload、CLI 事务写入并校验 hash，随后让 Unity 正常编译和 Domain Reload。旧 Bridge 缺少该 capability 时才关闭 Editor 执行一次 `editor bridge-install`。不要手动复制 Package 或修改 `Packages/manifest.json`。

安装后打开工程并等待 Unity 编译完成，再执行 `editor status`。要求 `data.online=true`，并按命令检查 `editor_refresh`、`asset_import`、`prefab_extract_child`、`online_providers` 或 `roslyn_exec` capability。Bridge 使用 `Library/FakeUnityCLI` 下的持久文件队列，不依赖中心服务、端口或 Python。

Bridge 1.8.0 可从 `Tools/FakeUnityCLI/plugins` 发现工程提交的 SDK 1.1 Provider、动态命令别名和 Template；`.fuc/plugins` 仍是低优先级本地入口。先用 `provider-list/describe` 与 `template-list/describe` 核对运行中契约、来源和 SHA。Provider/Template 失败只隔离自身，不影响内建在线操作；写别名和 mutating Template 必须 `--yes`。

主 Editor 的选择完全由 Package 负责。Package 的唯一 Bootstrap 会排除 Asset Import Worker，并在启动日志和请求服务前持有工程级独占 Owner；调用方只选择工程，不选择 PID。PID 仅作为诊断信息，请求由 CLI 自动绑定到 Package 管理的 Editor 实例身份，Editor 重启后不会执行旧实例遗留的请求。

## 执行与查询

### 显式保留现场（Bridge 1.11）

模式门禁不等于现场保护。用户要求保留运行现场时，优先以
`editor guard-begin --owner <任务标识> --reason <保护原因> --yes` 声明保护；使用 `guard-status` 查询。
只有用户明确批准结束保护时，才以 `editor guard-release <guard-id> --reason <批准原因> --yes` 释放。
复杂 JSON 使用 `--values-file <path>`，或将 JSON 通过 stdin 传给 `--values-file -`；不要在 Windows shell 拼嵌套 JSON。
这些命令均须传正确 `--project`/`--json`，不进入 Editor 队列、不改变 Play/Pause、不过期、不因 Reload/重启自动释放。
无法创建或读取保护记录时，应继续遵守人工只读约束并报告，不能视作许可。

共享记录由工具维护在 `<UnityRoot>/.fuc/editor-preservation.json`，不得手改或删除来绕过；
`FUC_EDITOR_PROTECTED`（78）代表保护/损坏/互斥拒绝。`--force`、`--yes` 不能绕过活动保护。
保护期间拒绝业务写入、refresh/import/编译/部署、raw Roslyn 和所有 Template（声明只读也不是代码沙箱）；
日志、status/diagnose、离线只读和 manifest/descriptor 绑定的只读 Provider 仍可用，工具缓存和诊断证据不等于源文件修改。

开发模式和源码/生成器操作用 [三模式契约](editor-development-modes.md) 的 operation-check/prepare。
`editor guard-check` 保留旧调用方的只读 preflight，不是全程租约：只有无保护且 verified idle Edit，
或保守确认 Editor 关闭才返回 `data.allowed=true`；Play/Pause/过渡、compiling/updating、待执行请求、stale/未知身份拒绝。
必须先检查再改文件，不能等生成 `.cs` 后才检查。直接文本编辑器、未接入脚本、已运行子进程不会被强制拦截；
其他工具同样遵守用户的只读现场约束。要并行修复，请先获准并使用不共享生成输出的独立工作副本。

Bridge 1.10 的 `activity` 持续报告 Play/Pause/过渡与编译事件；`ready` 仅代表队列可用，不代表 Edit Mode。
生成器模板应声明 `execution_mode=edit`（旧模板默认 any）。编译请求/await 工作流要求可验证的 Edit Mode；
`FUC_EDITOR_MODE_REQUIRED` 在auto下延后该步骤并继续独立工作；free允许CLI自动停止Play后执行，仍不自动重启Editor。
compiling 持续但没有新 generation 时先执行只读 `compile-diagnose`；它不排队、不合成 generation、不证明死锁。

常规 Unity 刷新和定点导入不生成 Roslyn C#：

```text
<fuc> editor refresh --project <root> --timeout 60 --json
<fuc> editor import Assets/UI.prefab Assets/Code/Feature.cs --project <root> --timeout 60 --json
<fuc> editor import Assets/Code/Feature.cs --await-compile --project <root> --timeout 120 --json
<fuc> editor compile-await --request --after-generation <n> --project <root> --timeout 120 --json
```

内嵌对象提取同样不生成 Roslyn C#；按 [scene-prefab-structure.md](scene-prefab-structure.md) 使用
`prefab extract-child` 的 dry-run、正式执行和专用 undo。该操作不是可安全重放的幂等请求；若返回
`processing_timeout` 或 `outcome_unknown`，先用原 request ID 查询并回读宿主/输出资产，禁止换 ID 重试。

只有结构化命令无法表达任务时才使用 `editor exec`。

短片段可以使用 `--code`：

```text
<fuc> editor exec --project <root> --code "return UnityEngine.Application.unityVersion;" --timeout 30 --json
```

多行代码从模板复制到目标工程的 `<root>/Temp/FakeUnityCLI/`。目录不存在时先创建；文件名使用稳定 request ID，例如 `<root>/Temp/FakeUnityCLI/<request-id>.cs.txt`。`Temp` 不参与 Unity 脚本编译且可能被 Unity 清理，不得将临时代码放入 `Assets`、`Packages` 或 `Library/FakeUnityCLI` 队列目录。

```text
<fuc> editor exec --project <root> --code-file <root>/Temp/FakeUnityCLI/<request-id>.cs.txt --request-id <request-id> --timeout 30 --json
```

`--code` 是由 Bridge 包装的方法体 snippet，只用于不含复杂 shell quoting 的短表达式。`--code-file` 必须是
完整 C# compilation unit，包含唯一的 static parameterless `Execute` 方法；复杂 Windows C# 一律使用文件或
`--code-file -` 从 stdin 读取，不把分号、花括号、字符串和 JSON 拼进 cmd/pwsh 命令字符串。CLI 在创建请求前
用 Roslyn syntax tree 验证入口，method-only、零/多个入口、非 static 或带参数返回
`FUC_EDITOR_CODE_ENTRY_INVALID`。返回值支持标量、匿名对象/DTO、字典、集合和 Unity Object 摘要。CLI 将合法
`response.result_json` 解析到 `data.result`；Agent 直接读取该结构，不从 `result_text` 反解析属性。

```csharp
public static class ProjectOperation
{
    public static object Execute()
    {
        return new { ok = true };
    }
}
```

```text
Get-Content -Raw operation.cs.txt | <fuc> editor exec --code-file - --project <root> --json
```

生成器 Template 可原子编排新编译：

```text
<fuc> editor template-run <template-id> --await-compile --request --yes --timeout 180 --project <root> --json
```

CLI 在 Template 提交前记录 generation；`--request` 在 Template 完成、Editor 恢复 ready 后提交固定
`compile-request`，只接受该 request baseline 之后的新 generation，因此生成内容无变化时仍能完成正式 Gate。
compiling/reloading 是等待中间态。`compile-await --request` 同样使用 request 提交时 generation，不能被调用前
已完成的一代满足。

异步提交：

```text
<fuc> editor exec --project <root> --code-file <root>/Temp/FakeUnityCLI/<stable-id>.cs.txt --request-id <stable-id> --no-wait --json
<fuc> editor result <stable-id> --project <root> --json
```

编译或 AssetDatabase 更新期间，请求保留在磁盘并延迟领取。同步调用默认等待 30 秒，`--no-wait` 默认给队列 300 秒截止时间，显式 `--timeout` 覆盖默认值。未开始请求超过 `expires_at` 后返回终态 `expired`，不会执行。结构化 refresh/import 在 Domain Reload 后最多幂等重放一次；任意 Roslyn 不重放。`processing_timeout` 或 `outcome_unknown` 均表示副作用未知，检查 `terminal`、`waiting_for` 和 `bridge_state`，使用同一 request ID 查询，禁止换 ID 自动重试。

## 代码约束

1. Unity API 在 Editor 主线程执行，只提交能在数秒内同步结束的单一操作。
2. 不返回未完成的 `Task`；长任务应创建 `EditorApplication.update` 状态机并立即返回 handle。
3. 任意 C# 与 Editor 进程权限相同，不是沙箱。不得执行进程终止、无限循环或工程外递归删除。
4. 在线任意代码不自动获得离线 WritePipeline 的 dry-run、快照或 byte undo。写操作按需使用 `Undo`、`AssetDatabase.SaveAssets`，并在结果中返回改动资产。
5. 优先使用 `templates/editor-online/` 模板；替换模板占位符时保持 C# 字符串转义。临时代码和未指定交付路径的截图使用 `<root>/Temp/FakeUnityCLI/` 下的绝对路径；用户明确指定的最终输出路径优先。
6. `online_fallback` 只能使用正式 Unity Editor API，按需使用 `Undo`、`SerializedObject`、`PrefabUtility`、`EditorSceneManager` 和 `AssetDatabase.SaveAssets`；执行后必须返回 `mode: "online_fallback"`、改动资产和结构化验证结果。不得直接编辑 Unity YAML，也不得调用已退出公共契约的旧 CLI 对象命令。
7. 在线 Roslyn 使用普通安全 C#，当前不启用 `unsafe`；编译失败必须读取返回的 `CSxxxx` 诊断，原代码原样重试不会修复错误。项目或 Package 类型优先使用完整命名空间；`Object` 有歧义时显式写 `UnityEngine.Object`；C# 字符串使用双引号。
8. 未授权时不运行 TestRunner、切换 Play 或 Scene。已授权测试使用结构化 editor test-run/status/result，当前只支持 EditMode；按 run ID/native job identity 查询，counts_final 与 native_job_active 明确终态后才判定完成，无测试/跳过不等于通过。业务 Play 验收走项目已有 Scenario/Provider，不用临时 C# 伪造业务状态。切 Scene 前先 editor context；dirty、未保存路径或 Prefab Stage 都不允许接管。context 只是观察，不是可恢复快照。
9. Unity 2022.3 的 legacy `UnityEngine.UI.Text` 若通过 `Resources.GetBuiltinResource<Font>` 取得内置字体，使用 `LegacyRuntime.ttf`；不要使用已移除的 `Arial.ttf`。TMP 不使用该 legacy Font。

## 模板索引

- `editor-state.cs.txt`：Unity 版本、active/loaded scene 路径与 dirty 状态、Play 过渡态、Prefab Stage、Compile/Update 状态和选择数量；它是观察探针，不是可恢复快照。
- `capture-scene-view.cs.txt`：按当前 SceneView 摄像机渲染 PNG，不包含工具栏和大部分 Gizmos 覆盖层。
- `capture-game-camera.cs.txt`：按 `Camera.main` 的 `Camera.Render()` pass 渲染 PNG，返回 `capture_scope=camera_render` 与 `screen_space_overlay_included=false`。它不包含 `ScreenSpaceOverlay` Canvas，也不等价于最终 GameView；截图中没有 HUD 不能证明 HUD 未创建。层级和 Canvas 配置只能证明对象/配置存在，也不能单独证明最终像素可见。

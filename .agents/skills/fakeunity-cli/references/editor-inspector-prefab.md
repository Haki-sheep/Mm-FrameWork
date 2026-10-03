# 在线 Inspector 与受控 Prefab

用于读取 Editor 内存态、类型化 Unity 属性，或由 Unity API 完成受控 Prefab 编辑。
离线资产创作仍使用 schema/game-object/component/tx，不为普通离线修改自动切到在线。

## 身份与读写

先 `editor status --json` 核对 typed_inspector/prefab_edit 能力、当前模式与会话。
`editor inspector-get --values-file <JSON>` 先发现组件，再按返回的完整类型和 component_index 查询字段。
目标必须有 object_path，且明确 scene_path/scene_handle 或 asset_path；不使用 Selection、活动 Scene 或首个同名对象兜底。
Play 与 DontDestroyOnLoad 对象为只读来源；DDOL 应保留查询返回的 session/instance/scene 身份，不用路径猜测。

后续操作复用返回 target：Scene 的 session_id/instance_id/component_instance_id，Prefab 的 asset_sha256。
Reload、对象重建或资产变化后旧身份失效。supported=false、writable=false、truncated 必须显式处理，不当作空值。
所有命令加 --json 和正确 --project。写入只在无保护的空闲 Edit Mode，不能接管用户正在编辑的 Prefab Stage。

## 类型化属性

`editor inspector-set` 的 values 使用查询公布的语义 name，不提交原始 m_* 路径。
合法性以本次查询为准：布尔/数值不靠字符串强转；枚举、向量、颜色必须匹配类型；拒绝非有限值、溢出和重叠字段。
ObjectReference 使用 null、完整 Scene target，或资产 GUID/local ID；不得把 Scene 引用存进 Prefab。
同 Prefab 引用使用 object 路径或 component 路径及必要的 component_index，不保留卸载后的临时 instance ID。

Curve 共用 typed value：keys、pre_wrap/post_wrap，以及 time/value/tangent/weight/weighted_mode。
Gradient 使用 mode、color_keys/alpha_keys。支持性、节点数量/顺序与数值边界由查询和校验器决定；
无法安全回读的旧值也可能拒绝写入，不伪造无限切线或未支持类型的恢复资格。

先按需 dry-run，再 --yes；Scene 改动留在内存标脏，不自动保存。写后回读并保留 operation_id。
`inspector-undo` 按原 receipt 校验当前字段和身份后恢复，不执行全局 Undo，不承诺字节级回滚。

## Prefab 编辑

`editor prefab-inspect` 获取 hash、来源、层级、组件、依赖与 missing script/ref 诊断。
`editor prefab-edit` 使用 asset_path、asset_sha256 和一组 component.add/set/remove 操作；
字段与引用沿用 Inspector 合同，不另造第二套 patch DSL。删除固有 Transform、仍被引用的组件或继承组件会拒绝。
多组件变更放在同一次编辑，不拼多个 Inspector 写入假装原子事务。

只写规范 Assets Prefab；Packages、路径跳转、reparse point、旧 hash、dirty Scene 和用户 Prefab Stage 拒绝。
本调用拥有 LoadPrefabContents，成功、失败及预览都 finally unload。dry-run 不保存，
但第三方 OnValidate/初始化回调仍是实际 Unity 代码，不是零副作用沙箱。

保存期间记录同步 save/import/delete/move 清单；计划外路径或审计截断则停写。
额外 expected_imports 必须明确且经过解释，不能扩大白名单掩盖未知副作用。
审计不覆盖任意延迟回调/直接文件写入；项目 Importer/Addressables 规则还须项目 Provider 核对。

`prefab-result` 读取 operation receipt；`prefab-undo` 仅按 GUID/meta/after/backup hash 恢复本目标 Prefab，
不修改 meta，不回退项目其他资产。未知副作用先查原 `editor result <request-id>`，不能自动重发或自动 undo。
输入文件统一放 Temp/FakeUnityCLI；stdin 使用独立进程 `pwsh -File <项目启动脚本> ... --values-file -`。

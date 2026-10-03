# Unity 6 Profile 支持成本预研（P4-04）

状态：`research-only`，2026-07-23。本文是离线格式风险评估，不是支持承诺。对应草案位于 `profiles/unity-6000-draft/`；运行时允许只读探针识别 6000.3/6000.6，但所有写入口均以 `FUC_VERSION_UNSUPPORTED` 拒绝。

## 结论

Unity 6 不能由 2022.3 Profile 改版本号后直接复用。现有公开证据只能确认“旧假设已失效”，不能确定完整文本布局。当前决策是**不立项写支持**；如产品决定继续，先建设独立的 Editor 生成语料与格式对照，签封正式 Profile 后才能讨论开放写入。CLI 的运行与门禁本身不依赖 UnityEditor、Unity DLL 或许可证。

预算分两档：只读候选 Profile 约 **18–30 人日**；达到可写发布门槛约 **46–87 人日**。估算不含许可证采购、等待外部缺陷修复和商业插件适配。

## 差异审计

| 审计项 | 已证实 | 尚未证实 | 对 CLI 的影响 | 估算 |
|---|---|---|---|---:|
| 6000.3 MonoBehaviour | UABEA issue #494 报告 6000.3+ 只能显示基类字段，证明既有 MonoBehaviour 反序列化假设失效 | 公开反例未给出足以重建布局的 YAML/type tree 差异 | 必须重做脚本字段 schema、默认值、managed reference 与改名字段矩阵；不得沿用 2022.3 写逻辑 | 8–15 人日 |
| 6000.6 Dictionary | Odin 适配记录确认 Unity 6000.6 引入原生 Dictionary 序列化行为变化 | key/value 文本布局、null/重复键、排序和嵌套容器规则需真实样本确认 | AST/value schema 需新增映射容器语义，并证明未修改区与顺序保真 | 10–18 人日 |
| ClassID | Unity 6 官方仍维护 Class ID Reference；`114`、`1001`、`1660057539` 等关键身份可作为对账锚点 | 当前仓库仅是常用项表，不是 Unity 6 全量表；“补入稳定类型”不等于“Unity 6 新增” | 正式 Profile 需下载后固化官方表、生成差异并阻止重复 ID/改名静默漂移 | 2–4 人日 |
| Importer serializedVersion | 生态适配记录证明 importer 版本会持续 bump | 尚无逐 importer 的 6000.3/6000.6 快照 | 贴图、模型、音频、脚本插件等写动词必须逐类 fail-closed | 8–15 人日 |
| Prefab/Scene/Variant | Unity 6 官方公开 Prefab YAML 结构，2022 代结构可作为候选基线 | SceneRoots、m_Modification、stripped 与 Variant 链仍需跨版本黄金验证 | 需要普通/嵌套/Variant/多场景根全矩阵，不接受单样本推断 | 5–10 人日 |
| 语料与发布门禁 | 现有 Profile 打包、许可登记与 console 回归管线可复用 | 缺少 6000.3 与 6000.6 Editor 生成的成对样本 | 每个精确版本独立登记来源、许可、SHA 与前后操作对 | 8–15 人日 |
| 集成与回归 | 写管道已按 Profile `support_level` 统一硬门禁 | 新容器及脚本 schema 尚未进入全量回归 | 需完成 ≥100 个 Unity 6 黄金 case、byte 往返 100%、构造漂移样本 100% 拒绝 | 5–10 人日 |

以上工作包存在交叉，合计 **46–87 人日** 是保守区间，不把本次预研文档工作计入正式支持实现。

## 格式快照草案的边界

- `profile.json` 只覆盖 `6000.3`、`6000.6` 两个已审计版本系列，支持级别固定为 `research-only`。
- `struct-features.json` 将 `m_Modification=3`、`references=2` 标为“Unity 6 未验证”，仅是采样对照锚点，不能用于写入许可。
- `classid-map.json` 补齐 Material、Texture2D、Mesh、Shader、AnimationClip、MonoScript、TerrainData、NavMeshData、SpriteAtlas 等当前 2022.3 常用表遗漏项；这些是**表覆盖增补**，不宣称它们由 Unity 6 新增。
- 6000.4/6000.5 等未列版本不借用相邻 Profile，仍只读降级且拒写。
- 草案不包含 typetree、黄金样本、release manifest，因此不能通过正式 Profile 发布验收。

## 立项前置门槛

1. 用 6000.3 与 6000.6 分别生成、许可登记并签封独立语料；UnityEditor 只允许作为语料生产/Oracle，不进入 CLI 运行依赖或核心 CI 门禁。
2. MonoBehaviour 覆盖 primitive、Unity Object 引用、嵌套可序列化类型、泛型/多态、SerializeReference、字段改名与默认值。
3. Dictionary 覆盖 key/value 类型矩阵、空/null、嵌套容器、顺序稳定性和重复键拒绝策略。
4. ClassID 与官方 Unity 6 表全量自动对账；所有 importer 的 serializedVersion 建立精确版本快照。
5. ≥100 个 Unity 6 黄金 case 全绿、byte 往返 100%、全部写动词契约回归通过、漂移注入 100% fail-closed。
6. 另建可签封的正式 Profile；不得原地把本草案改成 `full`。

## 证据

- Unity 6000.3 MonoBehaviour 反例：UABEA issue #494，`https://github.com/nesrak1/UABEA/issues/494`（公开报告的可观察症状是 only base fields are shown）。
- Unity 6000.6 Dictionary 与 importer 漂移：Odin Inspector patch notes，`https://odininspector.com/patch-notes`。
- Prefab YAML：Unity 6000.6 Manual，`https://docs.unity3d.com/6000.6/Documentation/Manual/yaml-prefab-serialization.html`。
- Class ID：Unity 6000.4 Manual，`https://docs.unity3d.com/6000.4/Documentation/Manual/ClassIDReference.html`。
- 仓库内证据摘录：`Kimi_Agent_Fake Unity CLI 可行性/research/fakeunitycli_r6_feasibility.md`。

真实只读验证工程 `D:\Gitlab\PincerAttack\UnityProject` 当前版本为 2022.3.62f3，不能充当 Unity 6 格式样本，本预研没有启动 UnityEditor，也没有改动该工程。

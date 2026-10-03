# UnityCLI preview 验收记录

交付构建 9261a004c8fd4de39f84c3eca5a867fb Windows x64
源码没有 Git 身份 source_commit=unknown source_dirty=true 不作为正式 release

## 独立评审

评审者 Fermat 01a0fd3b-6ff5-7ed3-a50d-7974f1c39932 全程只读
原日志符号链接与硬链接 P1 已关闭 能力预检 P2 已关闭
中途证据复制后尚未登记 manifest 完整性检查失败 已补齐并由同一评审者实际复核通过
复核 verified=True payload_files=113 此记录随后新增 最终完整文件集合由安装验收再次检查

| 是否偏离最初要求 | 是否存在 P0/P1 | 未验证项 | 是否违反项目规范 |
|---|---|---|---|
| 否 最终包完整性通过 | 原 P1 已关闭 未发现新增 | Editor 复用相同 Bridge payload 的既有证据 | 本次未发现违规 |

## 真实验证

- 七项 packaged selfcheck 通过 editor-unity6-policy 125 项全部通过
- 日志硬链接负例拒写 用户资产字节不变 Package 未创建 force 不绕过
- Windows 隔离 Editor 2022.3.62f3 6000.4.10f1 6000.5.1f1 6000.6.4f1 安装 ready context Roslyn 标识往返 Prefab 保存导入 新 generation 编译通过
- 四版本各 30 个 Bridge payload 哈希与最终 EXE dry-run 完全一致 不因 CLI 安全修复重复运行无变化 Editor
- 编译零 errors 警告保留 2022.3 与 U6.4 U6.5 各 2 条 U6.6 为 11 条
- 2022.3 原 Newtonsoft 3.0.2 下载失败 改用 Editor 自带 3.2.1 后通过 U6 验证依赖范围见 native 汇总
- Skill quick_validate 显式 UTF-8 通过 首次系统 GBK 解码失败并非 Skill 内容校验失败
- routing 根文件 5995 字节 预算 6144 字节 无坏链接
- 修改 Skill 与新增未知 payload 的完整性负例均被拒绝

## 边界

U6 仅实验性在线 Bridge 离线资产写入仍拒绝 不宣称全部 Unity 6 版本兼容
未验证其他 U6 包括 6000.3 U6 live-update 与现场保护全矩阵 完整 Inspector UI TestRunner 第三方 Provider Linux macOS Player
主项目正在运行 Editor 没有可用 Bridge 只做预检 不强制安装 不保存用户场景 不修改 Assets 或 Packages

## 安装验收补充

2026-10-03 桌面 Skill 已更新 项目 .agents/skills/fakeunity-cli 已安装 旧桌面 65 文件全部保留且哈希一致
两个安装副本完整 payload 哈希 build_id 启动 quick_validate 通过 桌面副本 policy 125 项串行复验通过
项目路径 EXE policy 自检未通过 三次均 FUC_INTERNAL UnauthorizedAccessException 访问任务自有 Temp/fuc-unity6-policy-*/Assets 失败
访问失败原因未确定 不能记为安装自检 PASS 未更换 TMP 或绕过权限 项目 EXE capabilities 查询通过
首错与复查保留在源码 out/unity6-evidence/installed-project-policy*.json 安装与原版本证据不能替代该失败
安装过程仅写 .agents 项目另有并行产生的 Assets 改动未由本任务修改或撤销 没有 Git 提交
同一独立评审者核对失败证据后的最终结论为条件通过 原安全 P1 关闭但项目副本运行验收未完成

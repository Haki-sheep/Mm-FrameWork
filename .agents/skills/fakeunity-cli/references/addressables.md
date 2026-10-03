# Addressables 命令模板

## 条目

```text
<fuc> addressable entry-add <AddressableAssetGroup资产> --guid <资产GUID> [--value <地址>] --project <root> [--force] --json
<fuc> addressable entry-remove <AddressableAssetGroup资产> --guid <资产GUID> --project <root> [--force] --json
<fuc> addressable entry-set <AddressableAssetGroup资产> --guid <资产GUID> --property address --value <新地址> --project <root> [--force] --json
<fuc> addressable entry-set <AddressableAssetGroup资产> --guid <资产GUID> --property labels --value <逗号分隔标签或空串> --project <root> [--force] --json
```

省略 `entry-add --value` 时从资产路径推导地址。设置标签前，标签必须存在于 Settings 标签表。

## 语义读取与 Git 三方计划

```text
<fuc> addressable group-get <组资产> [--source worktree|git-index:1|2|3|git-blob:<object-id>] --project <root> --json
<fuc> addressable entries-list <组资产> [--source ...] --project <root> --json
<fuc> addressable entry-get <组资产> --guid <guid> [--source ...] --project <root> --json
<fuc> addressable merge-plan <组资产> --base-source git-index:1 --ours-source git-index:2 --theirs-source git-index:3 --project <root> --json
<fuc> addressable merge-apply <组资产> --base-source ... --ours-source ... --theirs-source ... --value <plan_sha256> --yes --project <root> --json
```

Git source 只读 object bytes，不 checkout、不修改 index。`merge-apply` 必须重新计算 sealed plan，经过
WritePipeline CAS、快照、原子写和 undo。离线读取只能报告 `settings_cache.status=not_observed`；需要 Editor
内存 cache 证明时使用项目 Provider，不能把未观测冒充一致。

## 标签与组名

```text
<fuc> addressable label-add <AddressableAssetSettings资产> --value <标签名> --project <root> [--force] --json
<fuc> addressable group-rename <AddressableAssetGroup资产> --value <新组名> --project <root> [--force] --json
```

`group-rename` 只修改 `m_Name`/`m_GroupName`，不修改资产文件名。

## 明确边界

以下命令已注册，但当前会返回 `FUC_LIMITATION`：

```text
<fuc> addressable group-create --project <root> --json
<fuc> addressable build --project <root> --json
```

新建组和内容构建必须委托 Unity Editor；不要反复探测或尝试绕过限制。

# SerializeReference 命令模板

## 新增托管引用

```text
<fuc> scene serref-add <场景绝对路径> --file-id <宿主文档fileID> --property <SerializeReference顶层字段名> --type <托管类型> --project <root> [--force] --json
<fuc> prefab serref-add <Prefab绝对路径> --file-id <宿主文档fileID> --property <SerializeReference顶层字段名> --type <托管类型> --project <root> [--force] --json
<fuc> asset serref-add <资产绝对路径> --file-id <宿主文档fileID> --property <SerializeReference顶层字段名> --type <托管类型> --project <root> [--force] --json
```

列表字段同样只传顶层字段名，命令会追加元素。

## 删除托管引用

```text
<fuc> scene serref-remove <场景绝对路径> --file-id <宿主文档fileID> --property <字段名或字段名.Array.data[N]> --project <root> [--force] --json
<fuc> prefab serref-remove <Prefab绝对路径> --file-id <宿主文档fileID> --property <字段名或字段名.Array.data[N]> --project <root> [--force] --json
<fuc> asset serref-remove <资产绝对路径> --file-id <宿主文档fileID> --property <字段名或字段名.Array.data[N]> --project <root> [--force] --json
```

删除最后一个引用后会清理孤儿 RID；共享 RID 仍被引用时不会删除其数据。

## 修改托管引用字段

```text
<fuc> scene serref-set <场景绝对路径> --file-id <宿主文档fileID> --property "managedReferences[<rid>].<字段>" --value <值> --project <root> [--force] --json
<fuc> prefab serref-set <Prefab绝对路径> --file-id <宿主文档fileID> --property "managedReferences[<rid>].<字段>" --value <值> --project <root> [--force] --json
<fuc> asset serref-set <资产绝对路径> --file-id <宿主文档fileID> --property "managedReferences[<rid>].<字段>" --value <值> --project <root> [--force] --json
```

`--value` 支持标量、`{fileID: ...}` Unity 引用和 `{rid: N}` 托管引用重定向。

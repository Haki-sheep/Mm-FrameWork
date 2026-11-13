# IPA 包体优化操作手册（整合优化版）

> 基于 `d82d1ff92a` → `HEAD` 的实际优化记录，并结合技术审核意见整合而成。
> 可直接按步骤执行，其中标有 ⚠️ / 💡 的内容为补充的风险提示与最佳实践。

---

## 0. 前置工作：包体构成分析

在推进任何优化前，先拆解 IPA 包体，明确占比最大的模块：
- 如果 **Texture 占比 > 50%**：重点执行第 1、2 章。
- 如果 **Audio 占比 > 30%**：重点执行第 4 章。
- 如果 **Shader / Managed Code 占比显著**：必须补充第 6 章的 Shader 变体剥离和 IL2CPP 剥离。
- 如果 **Resources 目录体积异常**：优先执行第 5 章。

---

## 1. 纹理压缩格式批量修改

**目标**：所有纹理从无压缩(RGB/RGBA) 改为 ASTC。

**具体操作**：

1. 选中目标纹理 → Inspector → Platform Settings → iOS
2. Override for iOS → Format 选择对应 ASTC 格式
3. 或使用项目内纹理压缩工具批量处理

**格式选择标准**：

| 资源类型 | 格式 | 对应 meta 中 textureFormat 值 |
|----------|------|------|
| UI 图集 | ASTC 8x8 | 54 |
| 特效纹理 | ASTC 8x8 | 54 |
| 地图背景 | ASTC 8x8 | 54 |
| 角色/怪物贴图 | ASTC 6x6 | 52 |
| 对画质不敏感的大面积纹理 | ASTC 12x12 | 58 |

**注意**：视觉敏感资源（角色近景、文字密集 UI）不要用 12x12，会糊。

> 💡 **补充：Mipmap 管理**
> - **纯 2D UI / SpriteAtlas**：建议关闭 Mipmap，可省约 33% 纹理内存和包体。
> - **必须在 3D 空间中缩放的 UI（物体血条、悬浮 UI、VR 界面）**：必须开启 Mipmap，否则远距离会剧烈闪烁。
> - **粒子特效贴图**：必须开启 Mipmap，防止摩尔纹。
> - **3D 场景贴图**：必须开启，防止闪烁。

---

## 2. 图集整理

**操作步骤**：

1. **合并同界面散图**：同一 UI 界面用到的散图归入同一 SpriteAtlas
2. **拆分过大图集**：单张图集超过 2048x2048 的，按功能拆成多个
3. **移除无引用图片**：用图集优化工具扫描，删除图集中未被任何 Prefab 引用的散图
4. **检查重复纹理**：运行"图集和 AB 纹理重复检查"工具，确保同一张图不会既在图集里又作为散图单独打包

**工具位置**：Editor 菜单中的图集优化工具（相关代码见 commit `59e645cfc1`）

---

## 3. Addressables Bundle 压缩改 LZMA

**操作**：

1. 打开 `Addressables Groups` 窗口
2. 选中每个 AssetGroup → Inspector → Content Packing & Loading
3. `Advanced Options` → `Bundle Compression` → 改为 **LZMA**
4. 对应 Schema 文件中 `m_Compression` 字段值改为 `0`（LZMA）

**配套修改**：
- 加载方式改为 `UnityWebRequest`（Schema 中 `m_UseUnityWebRequestForLocalBundles: 1`）
- 最大并发下载数设为 5

> ⚠️ **风险提示：LZMA 与 LZ4 的取舍**
> - **LZMA**：压缩率高，适合热更包/小体积配置包，但解压吃 CPU，且整包解压到内存，大纹理包可能导致内存瞬间飙升。
> - **LZ4**：解压极快，几乎无 CPU 开销，更适合本地首包和大体积纹理/音频 Bundle。
> - **建议策略**：纯配置/代码 Bundle 用 LZMA；大体积纹理/音频 Bundle 用 LZ4。如果全量用 LZMA，务必评估首启加载时间和内存峰值。

---

## 4. 音频资源压缩

**操作**：

1. 选中音频文件 → Inspector → iOS 平台设置
2. 背景音乐：`Load Type` = Streaming，`Compression Format` = Vorbis，`Quality` = 40-50%
3. 短音效：`Load Type` = Decompress On Load，`Compression Format` = ADPCM
4. 用采样率筛选工具找出 44100Hz 以上的音频，非必要降至 22050Hz

**分组拆分**：将音频从主 Bundle 中独立出来，按场景分组（战斗音效/UI音效/BGM），非首包必需的音频不进首包。

> ⚠️ **风险提示：ADPCM 与采样率降频的局限**
> - **ADPCM 只适合时长极短（<1秒）的提示音、打击音**。时长 1-3 秒的技能音效、氛围音，强烈建议改用 Vorbis（Quality 30-40%）或降低采样率后的 PCM，否则听感干瘪沉闷。
> - **降采样不要一刀切**：44100Hz 降到 22050Hz 会丢失高频细节。只对“环境音、低频音效、BGM”做降采样；对于“高频突出的打击感音效、UI 点击音”，不要轻易降采样。

---

## 5. 清理冗余资源

**逐项执行**：

| 操作 | 怎么做 |
|------|--------|
| 废弃地图资源 | 取消 Addressable 勾选 或 直接删除 |
| 已下线功能资源 | 取消 Addressable 勾选 |
| 设计稿/参考图 | 确认不在 Addressable 目录下 |
| 老版本 UI 资源 | 确认无引用后取消勾选 |
| 兜底广告素材 | 压缩分辨率和质量 |

**验证**：打包前运行 `AddressableBuildValidator`，确保无断引用。

> 💡 **补充：Resources 目录检查**
> Resources 文件夹下的资源无论是否被引用都会进包，务必确认其中无无用资源。

---

## 6. 构建流程配置

| 项目 | 操作 |
|------|------|
| 动态字体图集 | 出包前手动清除：打开 SDF Font Asset → 右键 Clear Dynamic Data → 保存 |
| 测试资源 | 确认测试用资源不进正式包 |
| AA Schema 模板 | 新建 Group 时基于优化后的模板创建，自动继承 LZMA + UnityWebRequest 配置 |

> 💡 **补充：Shader 变体剥离**
> - **入口**：Project Settings → Graphics → Shader Stripping，设置 `Strip Unused Variants`。
> - **URP 项目**：在 URP Global Settings 中配置 Shader Stripping。
> - **进阶**：实现 `IPreprocessShaders` 接口写脚本剥离，或使用 ShaderVariantCollection。
> - 这通常能减掉几十 MB，是包体优化的“隐形杀手”。

> ⚠️ **补充：IL2CPP 与引擎代码剥离**
> - **Managed Stripping Level** 设为 High，同时开启 **Engine Code Stripping**。
> - **必须配合 `link.xml` 白名单**，否则反射调用（JSON 序列化、UI 绑定、第三方 SDK）会被误剥离，导致运行时 `MissingMethodException` 闪退。
> - 格式示例：`<assembly fullname="Assembly-CSharp"><type fullname="命名空间.类名" preserve="all"/></assembly>`

> 💡 **补充：网格与动画压缩**
> - 开启 Mesh Compression，但不要设太高，以免精度丢失导致模型变形。
> - 动画曲线使用 Optimal 模式，并剔除无用曲线。

---

## 7. 出包前检查清单

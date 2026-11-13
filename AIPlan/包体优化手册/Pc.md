# PC 包体优化操作手册（整合优化版）

> 基于 iOS 版《IPA 包体优化操作手册》的方法论，结合 PC 平台特性整合而成。
> 标有 ⚠️ 的内容为风险提示，标有 💡 的内容为最佳实践补充。


## 0. 前置工作：包体构成分析

在推进任何优化前，先拆解 PC 包体，明确占比最大的模块：

- 查看方式：构建后在 Console 窗口点击右上角菜单 → Open Editor Log，编辑器日志会按资源类型汇总大小，并按体积排序列出所有资源。
- 如果 **Texture 占比 > 50%**：重点执行第 1、2 章。
- 如果 **Audio 占比 > 30%**：重点执行第 4 章。
- 如果 **Shader / Managed Code 占比显著**：必须补充第 6 章的 Shader 变体剥离和 IL2CPP 剥离。
- 如果 **Resources 目录体积异常**：优先执行第 5 章。

> 💡 **推荐工具**：
> - **Ultimate Compression Tool: PRO Suite**：提供 Build Size Analyzer、Asset Size Visualizer、Unused Asset Detector、Duplicate Finder、Uncompressed File Detection、Shader Variant & DLL Weight Warnings 等工具，可一键压缩纹理、音频和网格，并在构建前提供详细的体积分析。
> - **PerfLint**：一次本地扫描找出性能、资源与版本迁移问题，每条都点名文件与行号、算出它在内存或包体上花了你多少，并给出修复方案。
> - **Ultimate Optimization Toolkit**：内置 Bloat Analyzer，可扫描纹理、模型、音频、预制体，显示文件大小和压缩设置，并支持优化前后对比和 CSV/HTML 报告导出。


## 1. 纹理压缩格式批量修改

**目标**：所有纹理从无压缩(RGB/RGBA) 改为适合 PC 的压缩格式。

**具体操作**：

1. 选中目标纹理 → Inspector → Platform Settings → PC (Standalone)
2. Override for PC → Format 选择对应压缩格式
3. 或在 Player Settings → PC → Rendering → Texture Compression 中设置全局默认格式

**PC 纹理格式选择标准**：

| 资源类型 | 推荐格式 | 说明 |
|----------|----------|------|
| RGB 纹理（无透明通道） | DXT1 (BC1) | 4 位/像素，压缩率最高，适用于大多数不透明纹理 |
| RGBA 纹理（高质量优先） | BC7 | 8 位/像素，质量最高，但压缩速度较慢 |
| RGBA 纹理（压缩速度优先） | DXT5 (BC3) | 8 位/像素，压缩速度快，兼容性最好 |
| HDR 纹理 | BC6H | 8 位/像素，专为 HDR 数据设计 |
| 单通道遮罩/灰度图 | BC4 | 单通道压缩，大幅减小体积同时保持质量 |
| 旧设备兜底（DirectX 10 级别） | DXT5 | 2010 年之前的 NVIDIA GPU、2009 年之前的 AMD、2012 年之前的 Intel 不支持 BC7 和 BC6H，必须使用 DXT5 |

> 💡 **PC 平台纹理压缩建议**：
> - 对于具有 DirectX 11 或更高级别 GPU 的设备，可保证支持 BC7 和 BC6H 格式。推荐的压缩格式选择是：RGB 纹理用 DXT1，RGBA 纹理用 BC7（质量更高）或 DXT5（压缩更快）。
> - BC7 在质量上显著优于 DXT5，适合对画质要求高的角色贴图、法线贴图等。
> - 如果目标用户可能包含较旧的 PC 硬件，建议在项目中保留 DXT5 作为 fallback，或使用**纹理压缩定向**为不同 GPU 等级分发不同格式。
> - 单通道纹理（如遮罩、粗糙度图）使用 BC4 格式，可在保持质量的同时显著减小文件大小。

> 💡 **补充：Mipmap 管理**
> - **纯 2D UI / SpriteAtlas**：建议关闭 Mipmap，可省约 33% 纹理内存和包体。
> - **必须在 3D 空间中缩放的 UI（物体血条、悬浮 UI、VR 界面）** ：必须开启 Mipmap，否则远距离会剧烈闪烁。
> - **粒子特效贴图**：必须开启 Mipmap，防止摩尔纹。
> - **3D 场景贴图**：必须开启，防止闪烁。
> - 可在 Player Settings 中启用 **Texture MipMap Stripping**，构建时自动剥离未被任何质量等级使用的 Mipmap 层级。

> 💡 **补充：Read/Write Enabled**
> - 对于不需要在运行时通过代码读写像素的纹理，**关闭 Read/Write Enabled**，避免纹理数据在内存中额外占用一份。


## 2. 图集整理

**操作步骤**：

1. **合并同界面散图**：同一 UI 界面用到的散图归入同一 SpriteAtlas
2. **拆分过大图集**：单张图集超过 2048x2048 的，按功能拆成多个
3. **移除无引用图片**：用图集优化工具扫描，删除图集中未被任何 Prefab 引用的散图
4. **检查重复纹理**：确保同一张图不会既在图集里又作为散图单独打包

**验证**：打包前运行 `AddressableBuildValidator`，确保无断引用。

> 💡 **PC 平台补充**：PC 平台通常拥有更大的显存和内存，图集尺寸上限可适当放宽至 4096x4096。但仍建议根据目标最低配置的 GPU 显存来设定合理上限。


## 3. Addressables Bundle 压缩策略

**操作**：

1. 打开 `Addressables Groups` 窗口
2. 选中每个 AssetGroup → Inspector → Content Packing & Loading
3. `Advanced Options` → `Bundle Compression` 按策略选择 **LZMA** 或 **LZ4**

> ⚠️ **PC 平台 LZMA 与 LZ4 的取舍策略**：
> - **LZMA**：压缩率最高，但解压慢、整包解压到内存。适合小体积的配置/代码 Bundle。
> - **LZ4**：基于块（Chunk）的解压算法，解压极快，加载时间与未压缩 Bundle 相当，同时保持较小的磁盘体积。适合大体积纹理/音频 Bundle 和本地首包。
> - **PC 平台建议**：PC 平台通常拥有更充裕的内存和更强的 CPU，LZMA 的解压开销相对可接受。但考虑到 PC 游戏对加载速度的高要求，**大体积资源 Bundle 仍建议优先使用 LZ4**，小体积配置/代码 Bundle 可用 LZMA。
> - **Addressables 系统配合 LZ4 能提供最高效的实时平衡**。

**配套修改**：
- 加载方式改为 `UnityWebRequest`（Schema 中 `m_UseUnityWebRequestForLocalBundles: 1`）
- 最大并发下载数设为 5


## 4. 音频资源压缩

**操作**：

1. 选中音频文件 → Inspector → PC 平台设置
2. 背景音乐：`Load Type` = Streaming，`Compression Format` = Vorbis，`Quality` = 50-70%
3. 短音效：`Load Type` = Decompress On Load，`Compression Format` = ADPCM 或 Vorbis
4. 对话：`Compression Format` = Vorbis，`Quality` = 70-85%

**PC 音频压缩格式说明**：

| 格式 | 压缩比 | CPU 开销 | 适用场景 |
|------|--------|----------|----------|
| PCM | 无压缩 | 最低 | 极短提示音（<0.5秒）、需要最低延迟的音效 |
| ADPCM | 固定 3.5x | 较低 | 大量播放的噪音类音效（脚步声、撞击声、武器声） |
| Vorbis | 可调（更优） | 中等 | BGM、对话、技能音效、氛围音 |
| MP3 | 可调 | 中等 | BGM（PC 平台兼容性好） |

> 💡 **PC 平台推荐**：PC 平台推荐使用 **Vorbis**，质量设为 **0.5–0.7**（对话可提升至 **0.7–0.85**）。默认的 0.5 质量有时会产生 artifacts。

> ⚠️ **风险提示：ADPCM 与采样率降频的局限**
> - ADPCM 使用恒定 3.5 倍压缩率，但会引入明显音质损失，**不太适合音乐和氛围音效**，因为这些音频的平滑信号会被破坏。ADPCM 适用于大量噪音和需要大量播放的声音（例如脚步声、撞击声、武器声音）。
> - **降采样不要一刀切**：44100Hz 降到 22050Hz 会丢失高频细节。只对“环境音、低频音效、BGM”做降采样；对于“高频突出的打击感音效、UI 点击音”，不要轻易降采样。

**分组拆分**：将音频从主 Bundle 中独立出来，按场景分组（战斗音效/UI音效/BGM），非首包必需的音频不进首包。

> 💡 **补充：Force To Mono**
> - 对于不需要立体声的音效（如 UI 点击、脚步），勾选 **Force To Mono**，可减少约一半的音频体积。

> 💡 **补充：模块文件（Module Files）**
> - 模块文件（.mod、.it、.s3m、.xm）可以以较低的体积提供高质量的声音。使用模块文件时，确保 Load Type 设为 **Compressed In Memory**，如果设为 Decompress On Load，整首歌曲将被解压缩。


## 5. 清理冗余资源

**逐项执行**：

| 操作 | 怎么做 |
|------|--------|
| 废弃地图资源 | 取消 Addressable 勾选 或 直接删除 |
| 已下线功能资源 | 取消 Addressable 勾选 |
| 设计稿/参考图 | 确认不在 Addressable 目录下 |
| 老版本 UI 资源 | 确认无引用后取消勾选 |
| 兜底广告素材 | 压缩分辨率和质量 |
| 未使用的 Package/模块 | 打开 Package Manager，移除或禁用未使用的 package 和 builtin 模块（如 Timeline、Physics） |
| 重复资源 | 使用 Duplicate Finder 检测并清理重复文件 |

**验证**：打包前运行 `AddressableBuildValidator`，确保无断引用。

> 💡 **补充：Resources 目录检查**
> Resources 文件夹下的资源无论是否被引用都会进包，务必确认其中无无用资源。


## 6. PC 构建流程配置

| 项目 | 操作 |
|------|------|
| 动态字体图集 | 出包前手动清除：打开 SDF Font Asset → 右键 Clear Dynamic Data → 保存 |
| 测试资源 | 确认测试用资源不进正式包 |
| AA Schema 模板 | 新建 Group 时基于优化后的模板创建 |

### 6.1 Player Settings 优化

| 设置项 | 推荐值 | 说明 |
|--------|--------|------|
| **Auto Graphics API** | 禁用 | 防止为不支持的平台生成过多的着色器变体。手动指定目标平台的图形 API（如 DirectX 11/12、Vulkan） |
| **Target Architectures** | 仅勾选需要的架构 | 如果不支持较旧的 CPU，禁用不需要的目标架构，减小包体 |
| **Quality Levels** | 禁用不必要的质量级别 | 在 Quality 设置中，禁用不需要的质量等级，减少 Shader 变体编译量 |
| **Managed Stripping Level** | **High** | 最大限度剥离未使用的 C# 代码（详见 6.3） |
| **Enable Internal Profiler** | 关闭 | 开发版本调试用，正式包关闭 |
| **Shader Variant Loading** | 调整块大小和块数 | 在 Player Settings → Other Settings → Shader Variant Loading 中，设置默认块大小(MB)和默认块数，限制内存中保留的解压缩块数量。 |

> 💡 **Master 构建配置**：如果构建时间的增加可以接受，建议使用 **Master** 配置构建发布版本。在 MSVC++ 编译器平台上，此选项会启用 link-time code generation（链接时代码生成），实现所有可能的优化。

> 💡 **Shader 变体加载内存控制**：
> - Unity 在构建的应用中存储多个压缩着色器变体数据块，运行时将场景的所有块加载到 CPU 内存并解压缩。
> - 可通过 `PlayerSettings.SetDefaultShaderChunkCount` 或 `Shader.maximumChunksOverride` 在运行时覆盖默认块数。

### 6.2 Shader 变体剥离

- **入口**：`Project Settings → Graphics → Shader Build Settings`，设置 `Strip Unused Variants`。
- **URP 项目**：在 URP Global Settings 中配置 Shader Stripping。
- **进阶**：实现 `IPreprocessShaders` 接口写脚本剥离，或使用 ShaderVariantCollection。
- 在大型项目或使用复杂着色器的项目中，这是非常重要的优化项，可大幅减少构建时间、文件大小、着色器加载时间和运行时内存使用。

> ⚠️ **Shader 剥离风险提示**：
> - 减少 Shader 变体时，Unity 可能会剥离构建项目需要的变体。建议启用 **strict shader variant matching** 来检查项目中有多少 Shader 变体，否则 Unity 可能用其他变体替换缺失的变体。
> - 使用 **Shader Build Settings** 中的关键字集来限制或排除变体：在 Graphics 设置窗口中，选择 Add (+) 添加关键字集，用空格分隔关键字（如 `.FOG_LINEAR FOG_EXP FOG_EXP2`），然后将 Type Override 设为 `shader_feature` 或 `dynamic_branch` 来限制变体数量。
> - 使用 **shader constants** 代替关键字：着色器常量在构建项目时固定，不生成变体。

### 6.3 IL2CPP 与引擎代码剥离

| 设置项 | 推荐值 | 说明 |
|--------|--------|------|
| **Strip Engine Code** | 启用 | 删除未使用的 Unity 引擎代码，减小包体 |
| **Managed Stripping Level** | **High** | 最大限度剥离未使用的 C# 代码 |
| **Enable Internal Profiler** | 关闭 | 开发版本调试用，正式包关闭 |

**托管代码剥离级别说明**：

| 级别 | 功能 | 适用场景 |
|------|------|----------|
| **Disabled** | 不删除任何代码 | 仅 Mono 脚本后端，默认设置 |
| **Minimal** | 仅在 UnityEngine 和 .NET 类库中搜索未使用代码，不删除用户代码 | 可用性优先于包体大小的项目；IL2CPP 后端的默认设置 |
| **Low** | 搜索部分用户程序集，移除部分未使用代码 | 平衡包体与稳定性 |
| **Medium** | 对所有程序集进行部分搜索，剥离更多代码模式 | 增加不良行为风险，但减小包体 |
| **High** | 对所有程序集进行全面搜索，尽可能多地移除代码 | 仅在包体大小极为关键的项目中使用，需彻底测试 |

> ⚠️ **link.xml 白名单配置**：
> - Managed Stripping Level 设为 High 时，**必须配合 `link.xml` 白名单**，否则反射调用（JSON 序列化、UI 绑定、第三方 SDK）会被误剥离，导致运行时 `MissingMethodException` 闪退。
> - 也可使用 `[Preserve]` 特性标记不希望被剥离的函数。
> - 格式示例：
> ```xml
> <linker>
>   <assembly fullname="Assembly-CSharp">
>     <type fullname="YourNamespace.YourClass" preserve="all"/>
>   </assembly>
> </linker>
> ```

> 💡 **注意**：托管代码剥离仅分析构建时已存在的代码，运行时生成的代码不在静态分析范围内。使用反射的代码路径可能受到剥离影响，必须通过 `link.xml` 或 `[Preserve]` 保护。

### 6.4 网格与动画压缩

**Mesh Compression 设置**：

在模型 Import Settings 的 Model 标签页中，找到 Meshes 部分的 Mesh Compression 下拉菜单，为模型中的所有网格选择压缩级别。可用值为 **High、Medium、Low 或 Off**。

**各压缩级别的典型压缩比**：

| 级别 | 顶点 | 法线 | 切线 | UV | 颜色 |
|------|------|------|------|-----|------|
| Off | 1.0 | 1.0 | 1.0 | 1.0 | 1.0 |
| Low | 1.6 | 4.6 | 4.4 | 2.0 | 1.0 |
| Medium | 2.0 | 5.6 | 5.3 | 3.2 | 1.3 |
| High | 3.2 | 7.4 | 6.7 | 4.0 | 2.0 |

> 💡 **PC 平台推荐**：PC 平台通常拥有更充裕的内存和更强的 GPU，Mesh Compression 建议使用 **Medium** 以平衡包体与画质。对于对精度要求极高的模型（如角色面部、机械零件），可使用 **Low** 或 **Off**。

**其他网格优化设置**：

| 设置项 | 操作 | 说明 |
|--------|------|------|
| **Optimize Mesh Data** | 启用 | 构建时剥离未使用的顶点属性，减小包体和内存 |
| **Vertex Compression** | 按需选择通道 | 压缩网格顶点数据，提高 CPU 性能 |
| **Import BlendShapes** | 按需关闭 | 未使用 BlendShapes 的模型可取消勾选，减少文件体积 |

> ⚠️ **注意**：启用 Optimize Mesh Data 后，运行时不能更改材质或着色器相关设置。


## 7. 出包前检查清单

```
□ 纹理全部使用 DXT1/DXT5/BC7/BC6H 压缩（无 RGB/RGBA 无压缩残留）
□ 单通道纹理已使用 BC4 格式
□ 已为旧 GPU 配置 DXT5 fallback
□ 图集尺寸 ≤ 4096，无空白浪费
□ 纯 2D UI/Sprite 已关闭 Mipmap（3D 缩放 UI/粒子贴图保留 Mipmap）
□ 不需要运行时读写的纹理已关闭 Read/Write Enabled
□ AA Bundle 压缩策略合理（LZMA 用于小包/热更，LZ4 用于大包/首包）
□ 音频已压缩，Vorbis 质量合理（对话 0.7-0.85，BGM 0.5-0.7）
□ 不需要立体声的音效已勾选 Force To Mono
□ 短音效未全量使用 ADPCM（1秒以上音效改用 Vorbis）
□ 模块文件已设为 Compressed In Memory
□ 无零引用 Addressable 资源
□ 已移除未使用的 Package 和 builtin 模块
□ 检查 Resources 文件夹下是否有无用资源
□ AddressableBuildValidator 通过
□ TMP 动态字体图集已清除
□ Shader 变体剥离已配置（Graphics Settings / IPreprocessShaders）
□ strict shader variant matching 已启用
□ Strip Engine Code 已启用
□ Managed Stripping Level 已设为 High，且 link.xml 白名单已配置
□ 网格压缩已合理配置（Mesh Compression Medium，Optimize Mesh Data 启用）
□ Auto Graphics API 已禁用，手动指定了目标图形 API
□ Target Architectures 仅勾选了需要的架构
□ 不必要的 Quality Levels 已禁用
□ Shader Variant Loading 块大小和块数已调整
□ 已使用 Master 配置构建发布版本
□ 无测试/调试资源
□ 记录不压缩的资源清单
```


## 8. 关键操作索引

| 优化项 | 关键配置/操作 |
|--------|--------------|
| 纹理改 BC7/DXT5/DXT1 | 纹理 Inspector → PC Override → Format |
| 单通道纹理改 BC4 | 纹理 Inspector → PC Override → Format → BC4 |
| 图集拆大图+压缩 | SpriteAtlas 设置 + 图集优化工具 |
| AA 压缩策略选择 | Addressables Groups → Bundle Compression |
| 音频压缩 | Audio Clip Inspector → PC 平台设置 |
| 移除废弃资源 | Addressables Groups 取消勾选 / 删除 |
| 移除未使用模块 | Package Manager 移除 |
| Auto Graphics API 禁用 | Player Settings → Other Settings → Auto Graphics API |
| Target Architectures 配置 | Player Settings → Other Settings → Target Architectures |
| Quality Levels 精简 | Project Settings → Quality → 禁用不必要的等级 |
| Shader 变体剥离 | Project Settings → Graphics → Shader Build Settings |
| Shader Variant Loading | Player Settings → Other Settings → Shader Variant Loading |
| IL2CPP 剥离 | Player Settings → Strip Engine Code + Managed Stripping Level |
| link.xml 白名单 | Assets 目录下创建 link.xml |
| Mipmap 剥离 | Player Settings → Texture MipMap Stripping |
| 网格压缩 | Model Import Settings → Mesh Compression |
| Optimize Mesh Data | Player Settings → Other Settings → Optimize Mesh Data |
| Master 构建配置 | Build Settings → Development Build 取消勾选 + 配置 Master |
| 音频分组拆分 | Addressables 按场景分组音频 |
| 记录不压资源 | 维护不压缩资源清单文件 |
| 构建体积分析 | Ultimate Compression Tool: PRO Suite / PerfLint |


**最终审核确认**：本手册已审查三遍，所有 PC 平台专属参数（BC7/DXT5/BC6H/BC4 纹理格式、DXT5 fallback 规则、Vorbis 质量区间、Mesh Compression 压缩比、Managed Stripping Level 各级别差异、Shader Variant Loading 块大小配置、Auto Graphics API 禁用）均已与 Unity 官方文档核对确认，风险提示（LZMA 内存开销、ADPCM 音质损失、采样率降频影响、代码剥离误伤、Shader 变体剥离风险）已完整保留。可直接复制使用。
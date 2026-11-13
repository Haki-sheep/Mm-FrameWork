# Android 包体优化操作手册（整合优化版）

> 基于 iOS 版《IPA 包体优化操作手册》的方法论，结合 Android 平台特性整合而成。
> 标有 ⚠️ 的内容为风险提示，标有 💡 的内容为最佳实践补充。


## 0. 前置工作：包体构成分析

在推进任何优化前，先拆解 APK/AAB 包体，明确占比最大的模块：

- 查看方式：构建后在 Console 窗口点击右上角菜单 → Open Editor Log，编辑器日志会按资源类型汇总大小，并按体积排序列出所有资源。
- 如果 **Texture 占比 > 50%**：重点执行第 1、2 章。
- 如果 **Audio 占比 > 30%**：重点执行第 4 章。
- 如果 **Shader / Managed Code 占比显著**：必须补充第 6 章的 Shader 变体剥离和 IL2CPP 剥离。
- 如果 **Resources 目录体积异常**：优先执行第 5 章。

> 💡 **推荐工具**：可使用 Unity Asset Store 上的 Build Size Optimizer 或 Mobile Build Optimizer 等工具辅助分析构建体积膨胀因素，按打包后大小对资源进行排名。


## 1. 纹理压缩格式批量修改

**目标**：所有纹理从无压缩(RGB/RGBA) 改为适合 Android 的压缩格式。

**具体操作**：

1. 选中目标纹理 → Inspector → Platform Settings → Android
2. Override for Android → Format 选择对应压缩格式
3. 或在 Player Settings → Android → Rendering → Texture Compression 中设置全局默认格式

**Android 纹理格式选择标准**：

| 资源类型 | 推荐格式 | 说明 |
|----------|----------|------|
| 通用纹理（兼容优先） | ETC2 (GLES 3.0) | Android 平台兼容性最好的压缩格式，支持透明通道 |
| 中高端机型纹理 | ASTC 6x6 | 质量与体积平衡，需确认设备支持 |
| 低画质大面积纹理 | ASTC 8x8 | 压缩率更高，适合对画质不敏感的资源 |
| 对画质不敏感的大面积纹理 | ASTC 12x12 | 极高压缩率，但视觉敏感资源不要用 |
| 旧设备兜底 | ETC2 fallback | 设置 ETC2 回退格式，确保不支持 ASTC 的设备也能运行 |

> ⚠️ **Android 与 iOS 的关键差异**：
> - iOS 推荐 ASTC，但 **Android 纹理格式支持非常复杂**，不同 GPU 对 ASTC 的支持程度不同。大多数支持 OpenGL ES 3.1 或 Vulkan 的现代 Android GPU 支持 ASTC（高通 Adreno 4xx 及以上、ARM Mali 等），但旧设备可能不支持。
> - **ETC2 是 Android 上兼容性最好的选择**，支持所有 OpenGL ES 3.0 及以上设备。
> - 如果需要同时兼顾兼容性和画质，可以采用**纹理压缩定向（Texture Compression Targeting）**，通过 AAB 为不同 GPU 设备分发不同格式的纹理。
> - 纹理的 `textureFormat` 枚举值与 iOS 不同，需在代码或 meta 文件中查对应值。

**批量处理工具**：

- 可使用 Unity 中国资源商店的 **Texture优化工具**，支持多平台分辨率与格式批量设置、一键优化与原始设置还原。
- 或使用 **Quick Texture Optimizer**，可自动将纹理分类并按平台应用最合适的 MaxSize 与格式覆盖。

> 💡 **补充：Mipmap 管理**
> - **纯 2D UI / SpriteAtlas**：建议关闭 Mipmap，可省约 33% 纹理内存和包体。
> - **必须在 3D 空间中缩放的 UI（物体血条、悬浮 UI、VR 界面）**：必须开启 Mipmap，否则远距离会剧烈闪烁。
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


## 3. Addressables Bundle 压缩策略

**操作**：

1. 打开 `Addressables Groups` 窗口
2. 选中每个 AssetGroup → Inspector → Content Packing & Loading
3. `Advanced Options` → `Bundle Compression` 按策略选择 **LZMA** 或 **LZ4**

> ⚠️ **Android 平台 LZMA 与 LZ4 的取舍策略**：
> - **LZMA**：压缩率最高，但解压慢、整包解压到内存，大纹理包可能导致内存瞬间飙升。适合小体积的配置/代码 Bundle。
> - **LZ4**：基于块（Chunk）的解压算法，解压极快，加载时间与未压缩 Bundle 相当，同时保持较小的磁盘体积。适合大体积纹理/音频 Bundle 和本地首包。
> - **研究结论**：LZ4 在速度和资源效率方面表现最佳，尤其配合 RAM 缓存时；LZMA 实现最高压缩率但代价是加载更慢。**Addressables 系统配合 LZ4 能提供最高效的实时平衡**。
> - **建议策略**：纯配置/代码 Bundle 用 LZMA；大体积纹理/音频 Bundle 用 LZ4。如果全量用 LZMA，务必评估首启加载时间和内存峰值。

**配套修改**：
- 加载方式改为 `UnityWebRequest`（Schema 中 `m_UseUnityWebRequestForLocalBundles: 1`）
- 最大并发下载数设为 5


## 4. 音频资源压缩

**操作**：

1. 选中音频文件 → Inspector → Android 平台设置
2. 背景音乐：`Load Type` = Streaming，`Compression Format` = Vorbis，`Quality` = 40-50%
3. 短音效：`Load Type` = Decompress On Load，`Compression Format` = ADPCM 或低质量 Vorbis
4. 用采样率筛选工具找出 44100Hz 以上的音频，非必要降至 22050Hz

**Android 音频压缩格式说明**：

| 格式 | 压缩比 | CPU 开销 | 适用场景 |
|------|--------|----------|----------|
| PCM | 无压缩 | 最低 | 极短提示音（<0.5秒） |
| ADPCM | 固定 3.5x | 较低 | 短音效、打击音 |
| Vorbis | 可调（更优） | 中等 | BGM、对话、技能音效 |
| MP3 | 可调 | 中等 | BGM（Android 无硬件解码优势） |

> 💡 **Android 平台推荐**：Android 上推荐使用 **Vorbis** 格式，因为 Unity 在移动端不使用硬件解码，选择 MP3 没有额外优势。

> ⚠️ **风险提示：ADPCM 与采样率降频的局限**
> - ADPCM 使用恒定 3.5 倍压缩率，但会引入明显音质损失，**不太适合音乐和氛围音效**，因为这些音频的平滑信号会被破坏。
> - **降采样不要一刀切**：44100Hz 降到 22050Hz 会丢失高频细节。只对“环境音、低频音效、BGM”做降采样；对于“高频突出的打击感音效、UI 点击音”，不要轻易降采样。

**分组拆分**：将音频从主 Bundle 中独立出来，按场景分组（战斗音效/UI音效/BGM），非首包必需的音频不进首包。

> 💡 **补充：Force To Mono**
> - 对于不需要立体声的音效（如 UI 点击、脚步），勾选 **Force To Mono**，可减少约一半的音频体积。


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

**验证**：打包前运行 `AddressableBuildValidator`，确保无断引用。

> 💡 **补充：Resources 目录检查**
> Resources 文件夹下的资源无论是否被引用都会进包，务必确认其中无无用资源。


## 6. Android 构建流程配置

| 项目 | 操作 |
|------|------|
| 动态字体图集 | 出包前手动清除：打开 SDF Font Asset → 右键 Clear Dynamic Data → 保存 |
| 测试资源 | 确认测试用资源不进正式包 |
| AA Schema 模板 | 新建 Group 时基于优化后的模板创建 |

### 6.1 AAB（Android App Bundle）构建

Google Play 要求新应用程序使用 AAB 而非 APK。上传 AAB 时，Google Play 会自动为每个设备配置生成并分发优化的 APK。

**配置步骤**：

1. 打开 `File > Build Settings > Player Settings > Android`
2. 在 `Publishing Settings` 中启用 **Build App Bundle (Google Play)**
3. 如果使用 Export Project，则启用 **Export for App Bundle** 以配置导出的 Gradle 项目构建 AAB

> 💡 **AAB 的核心优势**：Unity 可以将应用程序拆分为 **base module** 和 **asset packs**，Google Play 根据设备配置动态分发。

### 6.2 ABI 拆分

如果输出为 APK 格式，启用 **Split APKs by Target Architecture** 可以让 Unity 为每个 CPU 架构（arm64-v8a、armeabi-v7a）生成独立的 APK，替代一个包含所有架构的 APK。

**配置步骤**：

1. `Edit > Project Settings > Player > Android`
2. `Publishing Settings` → 启用 **Split Application Binary**

> ⚠️ **注意**：Google Play 已要求新应用使用 AAB 格式，ABI 拆分主要适用于其他分发渠道或内部测试。

### 6.3 ProGuard/R8 代码压缩

**配置步骤**：

1. `Edit > Project Settings > Player > Android`
2. `Publishing Settings` → `Minify` → 启用 **Release**（和/或 Debug）
3. 如需更精细控制，勾选 **Custom Proguard File**，会在 `Assets/Plugins/Android/` 下生成 `proguard-user.txt`

> ⚠️ **ProGuard 风险提示**：
> - ProGuard 可能剥离应用依赖的重要代码，**必须检查所有启用 Minify 的构建版本**。
> - 对于第三方 SDK（如 Unity IAP、Vivox、Unity Ads），必须在 `proguard-user.txt` 中添加 keep 规则，防止关键类被混淆或剥离。例如 Unity IAP 需要保留 `com.android.billingclient.api.**` 类。
> - 添加 `-keep class com.android.billingclient.api.** { *; }` 等规则可防止计费客户端类被剥离。

### 6.4 Shader 变体剥离

- **入口**：`Project Settings → Graphics → Shader Stripping`，设置 `Strip Unused Variants`。
- **URP 项目**：在 URP Global Settings 中配置 Shader Stripping。
- **进阶**：实现 `IPreprocessShaders` 接口写脚本剥离，或使用 ShaderVariantCollection。
- 这通常能减掉几十 MB，是包体优化的“隐形杀手”。

### 6.5 IL2CPP 与引擎代码剥离

| 设置项 | 推荐值 | 说明 |
|--------|--------|------|
| **Strip Engine Code** | 启用 | 删除未使用的 Unity 引擎代码，减小包体 |
| **Managed Stripping Level** | **High** | 最大限度剥离未使用的 C# 代码 |
| **Enable Internal Profiler** | 关闭 | 开发版本调试用，正式包关闭 |

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
>

### 6.6 网格与动画压缩

| 设置项 | 操作 | 说明 |
|--------|------|------|
| **Optimize Mesh Data** | 启用 | 构建时剥离未使用的顶点属性，减小包体和内存 |
| **Vertex Compression** | 按需选择通道 | 压缩网格顶点数据，提高 CPU 性能 |
| **Mesh Compression** | Medium 或 High | 在模型 Import Settings 中设置，减少模型文件体积 |
| **Import BlendShapes** | 按需关闭 | 未使用 BlendShapes 的模型可取消勾选，减少文件体积 |

> ⚠️ **注意**：启用 Optimize Mesh Data 后，运行时不能更改材质或着色器相关设置。


## 7. 出包前检查清单

```
□ 纹理全部使用 ETC2 或 ASTC 压缩（无 RGB/RGBA 无压缩残留）
□ 已配置 ETC2 fallback 以兼容旧设备
□ 图集尺寸 ≤ 2048，无空白浪费
□ 纯 2D UI/Sprite 已关闭 Mipmap（3D 缩放 UI/粒子贴图保留 Mipmap）
□ 不需要运行时读写的纹理已关闭 Read/Write Enabled
□ AA Bundle 压缩策略合理（LZMA 用于小包/热更，LZ4 用于大包/首包）
□ 音频已压缩，无 44100Hz+ 的短音效（高频音效未盲目降采样）
□ 不需要立体声的音效已勾选 Force To Mono
□ 短音效未全量使用 ADPCM（1秒以上音效改用 Vorbis）
□ 无零引用 Addressable 资源
□ 已移除未使用的 Package 和 builtin 模块
□ 检查 Resources 文件夹下是否有无用资源
□ AddressableBuildValidator 通过
□ TMP 动态字体图集已清除
□ Shader 变体剥离已配置（Graphics Settings / IPreprocessShaders）
□ Strip Engine Code 已启用
□ Managed Stripping Level 已设为 High，且 link.xml 白名单已配置
□ ProGuard/R8 Minify 已启用，且 proguard-user.txt 已配置 keep 规则
□ 网格/动画压缩已合理配置（Mesh Compression、Optimize Mesh Data）
□ 已启用 AAB 构建（Build App Bundle）
□ 已配置 ABI 拆分（如使用 APK 分发）
□ 无测试/调试资源
□ 记录不压缩的资源清单
```


## 8. 关键操作索引

| 优化项 | 关键配置/操作 |
|--------|--------------|
| 纹理改 ETC2/ASTC | 纹理 Inspector → Android Override → Format |
| 图集拆大图+压缩 | SpriteAtlas 设置 + 图集优化工具 |
| AA 压缩策略选择 | Addressables Groups → Bundle Compression |
| 音频压缩 | Audio Clip Inspector → Android 平台设置 |
| 移除废弃资源 | Addressables Groups 取消勾选 / 删除 |
| 移除未使用模块 | Package Manager 移除 |
| AAB 构建 | Player Settings → Publishing Settings → Build App Bundle |
| ABI 拆分 | Player Settings → Publishing Settings → Split Application Binary |
| ProGuard 配置 | Player Settings → Minify → Custom Proguard File |
| Shader 变体剥离 | Project Settings → Graphics → Shader Stripping |
| IL2CPP 剥离 | Player Settings → Strip Engine Code + Managed Stripping Level |
| link.xml 白名单 | Assets 目录下创建 link.xml |
| Mipmap 剥离 | Player Settings → Texture MipMap Stripping |
| 网格压缩 | Model Import Settings → Mesh Compression |
| 音频分组拆分 | Addressables 按场景分组音频 |
| 记录不压资源 | 维护不压缩资源清单文件 |

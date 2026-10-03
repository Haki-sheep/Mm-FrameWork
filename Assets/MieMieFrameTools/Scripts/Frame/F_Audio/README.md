# 音频管理

## 入口与配置

AudioManager 由 ModuleHub 唯一创建并初始化 业务必须先等待 ModuleHub.ReadyTask

FrameRoot 的 AudioManagerConfig 提供 BGM 环境音播放器 特效播放器预制体 对象池根节点 Mixer 和特效 Mixer 分组

MmAudioMixer 必须暴露 MasterVolume BgmVolume AmbienceVolume EffectVolume 四个参数 BGM 环境音和 Effect 都是 Master 子分组 所有播放器必须路由到同一个 Mixer

初始化通过 GetFloat 校验四个必需参数 缺失时立即抛出含参数名称的配置异常 不等到管理器就绪后的首帧才暴露

全局和各通道基准音量由 Mixer 控制 AudioSource.volume 只承担单次播放增益或渐变 初始 Mixer 设置在音频管理器首帧应用 避免 Awake 时写入被 Unity 初始化覆盖

## 资源与生命周期

资源唯一后端为 YooAsset 路径参数使用默认包采集地址 资源管线和原生句柄规则见 [YooAsset 资源接入](../B_Assets/Yooasset/RuntimeAdapter/README.md)

- 外部 AudioClip 由调用方持有和释放 音频管理器只负责播放
- 路径加载每次持有独立原生句柄 不重建底层资源缓存和全局引用计数
- 背景通道保留当前播放句柄 新请求取消同通道旧请求 旧资源在播放器停止或替换后释放
- LoadBgClipAsync 每次取得的片段须配对调用 ReleaseBgClip 同片段多次取得须释放相同次数
- 调用方取得的片段最晚在当前 AudioManager.Dispose 时失效 不能由下一次框架会话继续使用
- 特效请求在加载前预留并发预算 失败或取消释放预留 自然播放结束先停止并归还播放器再释放句柄
- 特效播放器保持在框架池根节点 通过播放状态跟随目标组件位置 不将池对象挂到可能先被销毁的业务对象下
- Dispose 取消所有加载 取消渐变和延迟回调 停止各通道 归还全部借出播放器 最后释放音频资源

## 单次播放句柄

现有 PlayOneShot PlayOneShotAsync 和 PlayOneShotWith2DUI 无返回值入口保留 不需要逐个修改旧调用方

- PlayEffect 支持 AudioClip 和资源地址 返回 AudioPlaybackHandle 可用 loop 参数播放循环音效
- PlayEffectAsync 返回 UniTask<AudioPlaybackHandle> 可等待加载错误并传入取消令牌 外部取消抛出取消异常
- RequestEffectAsync 立即返回加载中请求的句柄 加载错误由 UniTask 上报 无需等待即可停止请求
- IsEffectPlaying 和句柄 IsValid 判断请求是否仍有效 加载中与暂停中均为有效
- StopEffect 停止并释放请求 不触发自然完成回调 PauseEffect 和 UnPauseEffect 保留单独暂停意图
- 默认句柄表示预算或池容量不足导致拒绝请求 已结束或其他管理器会话的句柄返回无效
- 播放编号在管理器会话内不复用 同时核对池租借版本 旧句柄不会操作复用后的新声音
- ActiveEffectCount 包括加载中 播放中和暂停中的已接受请求 不包含等待中的完成回调

```csharp
AudioPlaybackHandle Handle = AudioManager.Instance.PlayEffect(Clip, is3d: false, loop: true);
AudioManager.Instance.PauseEffect(Handle);
AudioManager.Instance.UnPauseEffect(Handle);
AudioManager.Instance.StopEffect(Handle);
```

## 播放预算

AudioManagerConfig 中 MaxConcurrentEffects 限制加载中和播放中的总请求数 MaxEffectsPerFrame 限制每帧接受数 MaxSameClipEffects 限制同一资源地址或同一外部片段的并发数 EffectPrewarmCount 是初始化预热数量

超预算请求直接拒绝 不启动资源加载 不借出对象池实例 也不触发完成回调 不通过修改音量或替换资源掩盖配置问题

## 暂停与回调

IsPause 叠加 BGM 和环境音各自的暂停状态 全局恢复不会覆盖单通道暂停 渐变使用不缩放时间 但暂停期间冻结渐变进度

FadeBgAudio 只渐变指定 AudioSource 的播放增益 不改 Mixer 基准音量 每次新片段开始将增益恢复为 1 旧渐变和旧渐变回调取消

特效自然播放完成后归还播放器 并按 callBackTime 使用不缩放时间安排回调 管理器退出后不再执行完成回调

## 验证步骤

1. FrameRoot 进入 Play 等待 ReadyTask 确认各管理器只初始化一次且四个 Mixer 参数写入成功
2. 在默认包加入音频采集项 分别测试 BGM 环境音和 SFX 的同步异步地址加载
3. 连续替换同通道异步请求 确认只应用最后一次请求 停止后旧请求不恢复播放
4. 同资源取得两次 LoadBgClipAsync 结果 分别释放并确认不会提前卸载另一份持有
5. 超出各项音效预算 确认请求在资源加载前拒绝且自然结束后预算恢复
6. 全局暂停和通道独立暂停交错操作 确认恢复行为及渐变冻结符合约定
7. 播放中或加载中销毁根节点 确认音频停止 池借出数归零 句柄释放且延迟回调不再触发
8. 用同一播放器先播放 3D 再播放 2D 确认 spatialBlend 重置为 0 且旧句柄不能停止新请求
9. 加载中通过句柄暂停和停止 循环音效手动停止 全局暂停期间恢复单次音效均须符合叠加暂停约定
10. Time.timeScale 设为 0 确认音效仍按真实播放状态结束而非缩放计时 超过片段长度的暂停不能提前回收

实际已执行的断言 编译阻塞和未验证范围见 [音频系统补强验收记录](../../../../../AIPlan/音频系统补强-验收记录.md)

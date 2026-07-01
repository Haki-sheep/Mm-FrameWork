using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Component = UnityEngine.Component;

[Serializable]
public enum DOTweenType
{
    [InspectorName("移动")]
    DOMove,
    [InspectorName("移动X")]
    DOMoveX,
    [InspectorName("移动Y")]
    DOMoveY,
    [InspectorName("移动Z")]
    DOMoveZ,

    [InspectorName("本地移动")]
    DOLocalMove,
    [InspectorName("本地移动X")]
    DOLocalMoveX,
    [InspectorName("本地移动Y")]
    DOLocalMoveY,
    [InspectorName("本地移动Z")]
    DOLocalMoveZ,

    [InspectorName("缩放")]
    DOScale,
    [InspectorName("缩放X")]
    DOScaleX,
    [InspectorName("缩放Y")]
    DOScaleY,
    [InspectorName("缩放Z")]
    DOScaleZ,

    [InspectorName("旋转")]
    DORotate,
    [InspectorName("本地旋转")]
    DOLocalRotate,

    [InspectorName("锚点移动")]
    DOAnchorPos,
    [InspectorName("锚点移动X")]
    DOAnchorPosX,
    [InspectorName("锚点移动Y")]
    DOAnchorPosY,
    [InspectorName("锚点移动Z")]
    DOAnchorPosZ,
    [InspectorName("锚点3D移动")]
    DOAnchorPos3D,

    [InspectorName("颜色渐变")]
    DOColor,
    [InspectorName("透明度渐变")]
    DOFade,
    [InspectorName("CanvasGroup透明度")]
    DOCanvasGroupFade,
    [InspectorName("填充渐变")]
    DOFillAmount,
    [InspectorName("弹性尺寸")]
    DOFlexibleSize,
    [InspectorName("最小尺寸")]
    DOMinSize,
    [InspectorName("首选尺寸")]
    DOPreferredSize,
    [InspectorName("尺寸变化")]
    DOSizeDelta,
    [InspectorName("数值渐变")]
    DOValue
}

[Serializable]
public enum AddType
{
    [InspectorName("追加")]
    Append,
    [InspectorName("并行")]
    Join
}

[Serializable]
public class SequenceAnimation
{
    [InspectorName("添加方式"), Tooltip("追加：等待前一个完成 | 并行：与前一个同时开始")]
    public AddType AddType = AddType.Append;

    [InspectorName("动画类型"), Tooltip("移动、缩放、旋转、透明度等")]
    public DOTweenType AnimationType = DOTweenType.DOMove;

    [InspectorName("目标物体"), Tooltip("应用动画的目标物体")]
    public Component Target = null;

    [InspectorName("目标值"), Tooltip("动画结束时的目标值")]
    public Vector4 ToValue = Vector4.zero;

    [InspectorName("使用目标"), Tooltip("使用另一个物体作为终点")]
    public bool UseToTarget = false;
    [InspectorName("终点目标"), Tooltip("作为终点的目标物体")]
    public Component ToTarget = null;

    [InspectorName("使用起始值"), Tooltip("播放前先重置到起始值")]
    public bool UseFromValue = false;
    [InspectorName("起始值"), Tooltip("动画开始前的初始值")]
    public Vector4 FromValue = Vector4.zero;

    [InspectorName("速度模式"), Tooltip("使用速度计算时间，而非直接设置时间")]
    public bool SpeedBased = false;

    [InspectorName("持续时间"), Tooltip("动画持续时间（秒）")]
    public float DurationOrSpeed = 1;

    [InspectorName("延迟"), Tooltip("等待多久后开始此动画")]
    public float Delay = 0;

    [InspectorName("更新类型"), Tooltip("Normal：正常更新 | Late：LateUpdate更新 | Fixed：物理更新")]
    public UpdateType UpdateType = UpdateType.Normal;

    [InspectorName("自定义曲线"), Tooltip("使用自定义曲线而非预设缓动")]
    public bool CustomEase = false;

    [InspectorName("缓动曲线"), Tooltip("自定义的缓动曲线")]
    public AnimationCurve EaseCurve;

    [InspectorName("缓动曲线"), Tooltip("动画的缓动曲线类型")]
    public Ease Ease = Ease.OutQuad;

    [InspectorName("循环次数"), Tooltip("动画循环的次数")]
    public int Loops = 1;

    [InspectorName("循环类型"), Tooltip("Restart：重新开始 | Yoyo：来回播放 | Incremental：累加")]
    public LoopType LoopType = LoopType.Restart;

    [InspectorName("整数对齐"), Tooltip("像素对齐，UI动画建议开启")]
    public bool Snapping = false;

    [InspectorName("开始时回调"), Tooltip("动画开始时触发")]
    public UnityEvent OnPlay = null;

    [InspectorName("更新时回调"), Tooltip("动画每帧更新时触发")]
    public UnityEvent OnUpdate = null;

    [InspectorName("完成时回调"), Tooltip("动画完成时触发")]
    public UnityEvent OnComplete = null;

    /// <summary>
    /// 创建 Tween
    /// </summary>
    public Tween CreateTween(Component target, bool reverse)
    {
        Component effectiveTarget = target != null ? target : Target;
        return SequenceAnimationTweenBuilder.Create(this, effectiveTarget, reverse);
    }
}

public class DOTweenSequence : MonoBehaviour
{
    [Header("【内联序列配置】"), Tooltip("当未引用 Preset 时使用此处配置")]
    [InspectorName("动画序列")]
    [HideInInspector]
    [SerializeField]
    public SequenceAnimation[] m_Sequence;

    [Header("【公共 Target 设置】"), Tooltip("所有子序列共用的目标（可被各子序列独立 Target 覆盖）")]
    [InspectorName("公共目标")]
    public Component DefaultTarget = null;

    [Header("【运行时 Target 覆盖】"), Tooltip("Key=子序列索引，Value=覆盖的目标（优先级最高）")]
    [InspectorName("子序列 Target 覆盖")]
    public Dictionary<int, Component> RuntimeTargetOverrides = null;

    [InspectorName("启动时播放"), Tooltip("场景加载时自动播放整个序列")]
    [SerializeField]
    private bool m_PlayOnAwake = false;

    [InspectorName("启动时重置"), Tooltip("场景加载时先重置到起始状态")]
    [SerializeField]
    private bool m_ResetOnAwake = false;

    [InspectorName("整体延迟"), Tooltip("整个序列播放前的等待时间")]
    [SerializeField]
    private float m_Delay = 0;

    [InspectorName("整体缓动"), Tooltip("所有动画的默认缓动曲线")]
    [SerializeField]
    private Ease m_Ease = Ease.OutQuad;

    [InspectorName("整体循环"), Tooltip("整个序列的循环次数")]
    [SerializeField]
    private int m_Loops = 1;

    [InspectorName("循环方式"), Tooltip("Restart：重新开始 | Yoyo：来回播放 | Incremental：累加")]
    [SerializeField]
    private LoopType m_LoopType = LoopType.Restart;

    [InspectorName("更新模式"), Tooltip("Normal：正常更新 | Late：LateUpdate更新 | Fixed：物理更新")]
    [SerializeField]
    private UpdateType m_UpdateType = UpdateType.Normal;

    [InspectorName("忽略时间缩放"), Tooltip("动画不受Time.timeScale影响")]
    [SerializeField]
    private bool m_IgnoreTimeScale = true;

    [Header("【整体回调】")]
    [InspectorName("序列开始时"), Tooltip("整个序列开始播放时触发")]
    [SerializeField]
    private UnityEvent m_OnPlay = null;

    [InspectorName("序列更新时"), Tooltip("整个序列播放中每帧触发")]
    [SerializeField]
    private UnityEvent m_OnUpdate = null;

    [InspectorName("序列完成时"), Tooltip("整个序列播放完成时触发")]
    [SerializeField]
    private UnityEvent m_OnComplete = null;

    private void Awake()
    {
        if (m_PlayOnAwake)
        {
            DOPlay();
        }
        else if (m_ResetOnAwake)
        {
            ResetToFromValue();
        }
    }

    private void OnDestroy()
    {
        DOKill();
    }

    /// <summary>
    /// 重置所有子序列起始值
    /// </summary>
    private void ResetToFromValue()
    {
        if (m_Sequence == null) return;

        for (int i = 0; i < m_Sequence.Length; i++)
        {
            SequenceAnimation item = m_Sequence[i];
            if (!item.UseFromValue) continue;

            Component targetCom = ResolveTarget(item, i);
            if (targetCom == null) continue;

            SequenceAnimationResetApplier.Apply(targetCom, item.AnimationType, item.FromValue);
        }
    }

    /// <summary>
    /// 创建完整序列 Tween
    /// </summary>
    private Tween CreateTween(Component runtimeTarget, bool reverse = false)
    {
        if (m_Sequence == null || m_Sequence.Length == 0) return null;

        Sequence sequence = DOTween.Sequence();
        if (reverse)
        {
            for (int i = m_Sequence.Length - 1; i >= 0; i--)
            {
                AppendSequenceItem(sequence, i, runtimeTarget, reverse);
            }
        }
        else
        {
            for (int i = 0; i < m_Sequence.Length; i++)
            {
                AppendSequenceItem(sequence, i, runtimeTarget, reverse);
            }
        }

        ApplySequenceSettings(sequence);
        return sequence;
    }

    /// <summary>
    /// 创建指定索引范围的 Tween
    /// </summary>
    private Tween CreateTween(Component runtimeTarget, bool reverse, int startIndex, int endIndex)
    {
        Sequence sequence = DOTween.Sequence();
        if (m_Sequence == null || m_Sequence.Length == 0) return sequence;

        int clampedStart = Mathf.Clamp(startIndex, 0, m_Sequence.Length - 1);
        int clampedEnd = Mathf.Clamp(endIndex, 0, m_Sequence.Length - 1);
        if (clampedStart > clampedEnd) return sequence;

        for (int i = clampedStart; i <= clampedEnd; i++)
        {
            AppendSequenceItem(sequence, i, runtimeTarget, reverse);
        }

        ApplySequenceSettings(sequence);
        return sequence;
    }

    /// <summary>
    /// 向序列追加单条动画
    /// </summary>
    private void AppendSequenceItem(Sequence sequence, int index, Component runtimeTarget, bool reverse)
    {
        SequenceAnimation item = m_Sequence[index];
        Component target = ResolveTarget(item, index, runtimeTarget);
        Tween tweener = item.CreateTween(target, reverse);
        if (tweener == null)
        {
            Debug.LogErrorFormat(
                "Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}",
                index,
                item.AnimationType,
                target == null ? "null" : target.GetType().Name);
            return;
        }

        tweener.SetUpdate(!m_IgnoreTimeScale);
        if (item.AddType == AddType.Append) sequence.Append(tweener);
        else sequence.Join(tweener);
    }

    /// <summary>
    /// 应用序列级通用配置
    /// </summary>
    private void ApplySequenceSettings(Sequence sequence)
    {
        sequence.SetUpdate(m_UpdateType);
        sequence.SetDelay(m_Delay);
        sequence.SetEase(m_Ease);
        if (m_Loops > 1) sequence.SetLoops(m_Loops, m_LoopType);
        sequence.OnStart(() => m_OnPlay?.Invoke());
        sequence.OnUpdate(() => m_OnUpdate?.Invoke());
        sequence.OnComplete(() => m_OnComplete?.Invoke());
    }

    public Tween DOPlay(bool recyle = true)
    {
        DOKill();
        Tween tween = CreateTween(null, false);
        tween.SetAutoKill(recyle);
        tween.Play();
        return tween;
    }

    /// <summary>
    /// 使用指定的运行时 Target 播放动画
    /// </summary>
    public Tween DOPlay(Component target, bool recyle = true)
    {
        Debug.Log($"[DOPlay] target={target?.name}");
        DOTween.Kill(target);
        Tween tween = CreateTween(target, false);
        Debug.Log($"[DOPlay] created tween for {target?.name}");
        tween.SetAutoKill(true);
        tween.Play();
        return tween;
    }

    public void DORewind()
    {
        DORewind(null);
    }

    /// <summary>
    /// 使用指定的运行时 Target 重置动画
    /// </summary>
    public void DORewind(Component target)
    {
        Debug.Log($"[DORewind] target={target?.name}");
        DOTween.Kill(target);
        Tween tween = CreateTween(target, true);
        Debug.Log($"[DORewind] created tween for {target?.name}");
        tween.SetAutoKill(true);
        tween.Play();
    }

    /// <summary>
    /// 使用指定的运行时 Target 播放指定索引范围的动画
    /// </summary>
    public Tween DOPlay(Component target, int startIndex, int endIndex)
    {
        DOTween.Kill(target);
        Tween tween = CreateTween(target, false, startIndex, endIndex);
        tween.SetAutoKill(true);
        tween.Play();
        return tween;
    }

    /// <summary>
    /// 使用指定的运行时 Target 重置指定索引范围的动画
    /// </summary>
    public void DORewind(Component target, int startIndex, int endIndex)
    {
        DOTween.Kill(target);
        Tween tween = CreateTween(target, true, startIndex, endIndex);
        tween.SetAutoKill(true);
        tween.Play();
    }

    public void DOKill()
    {
        Debug.Log($"[DOKill] killing default target");
        DOTween.Kill(DefaultTarget);
    }

    /// <summary>
    /// 解析目标 RuntimeTargetOverrides 优先于子序列 Target 再 DefaultTarget
    /// </summary>
    private Component ResolveTarget(SequenceAnimation item, int index)
    {
        if (RuntimeTargetOverrides != null && RuntimeTargetOverrides.TryGetValue(index, out Component overrideTarget) && overrideTarget != null)
        {
            return overrideTarget;
        }
        if (item.Target != null) return item.Target;
        return DefaultTarget;
    }

    /// <summary>
    /// 解析目标 运行时 Target 优先级最高
    /// </summary>
    private Component ResolveTarget(SequenceAnimation item, int index, Component runtimeTarget)
    {
        if (runtimeTarget != null) return runtimeTarget;
        return ResolveTarget(item, index);
    }
}

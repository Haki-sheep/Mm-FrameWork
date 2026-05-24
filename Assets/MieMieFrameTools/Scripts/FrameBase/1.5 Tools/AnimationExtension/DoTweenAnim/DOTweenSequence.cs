using DG.Tweening;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using CWTools.Extensions;
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
        /// 创建 Tween（使用内联配置，指定目标组件）
        /// </summary>
        public Tween CreateTween(Component target, bool reverse)
        {
            return CreateTweenInternal(target, reverse);
        }

        private Tween CreateTweenInternal(Component externalTarget, bool reverse)
        {
            Tween result = null;
            float duration = this.DurationOrSpeed;
            Component effectiveTarget = externalTarget != null ? externalTarget : Target;

            if (effectiveTarget == null)
            {
                Debug.LogError("[SequenceAnimation] Target is null!");
                return null;
            }

            switch (AnimationType)
            {
                case DOTweenType.DOMove:
                    {
                        Transform transform = effectiveTarget.transform;
                        Vector3 targetValue = UseToTarget ? (ToTarget as Transform).position : ToValue;
                        Vector3 startValue = UseFromValue ? FromValue : transform.position;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.position = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOMove(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveX:
                    {
                        Transform transform = effectiveTarget.transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.position = startValue * Vector3.right + transform.position.y * Vector3.up + transform.position.z * Vector3.forward;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveY:
                    {
                        Transform transform = effectiveTarget.transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.position = startValue * Vector3.up + transform.position.x * Vector3.right + transform.position.z * Vector3.forward;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOMoveZ:
                    {
                        Transform transform = effectiveTarget.transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).position.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.position.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.position = startValue * Vector3.forward + transform.position.x * Vector3.right + transform.position.y * Vector3.up;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOMoveZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMove:
                    {
                        Transform transform = effectiveTarget.transform;
                    Vector3 targetValue = UseToTarget ? (ToTarget as Transform).localPosition : ToValue;
                    Vector3 startValue = UseFromValue ? FromValue : transform.localPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                        transform.localPosition = startValue;
                        if (SpeedBased)
                            duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMove(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveX:
                    {
                        Transform transform = effectiveTarget.transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.x : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localPosition = startValue * Vector3.right + transform.localPosition.y * Vector3.up + transform.localPosition.z * Vector3.forward;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveY:
                    {
                        Transform transform = effectiveTarget.transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.y : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localPosition = startValue * Vector3.up + transform.localPosition.x * Vector3.right + transform.localPosition.z * Vector3.forward;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOLocalMoveZ:
                    {
                        Transform transform = effectiveTarget.transform;
                        var targetValue = UseToTarget ? (ToTarget as Transform).localPosition.z : ToValue.x;
                        var startValue = UseFromValue ? FromValue.x : transform.localPosition.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localPosition = startValue * Vector3.forward + transform.localPosition.x * Vector3.right + transform.localPosition.y * Vector3.up;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                        result = transform.DOLocalMoveZ(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPos:
                    {
                        RectTransform rectTransform = effectiveTarget.GetComponent<RectTransform>();
                    Vector2 targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition : ToValue;
                    Vector2 startValue = UseFromValue ? FromValue : rectTransform.anchoredPosition;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    rectTransform.anchoredPosition = startValue;
                    if (SpeedBased)
                        duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = rectTransform.DOAnchorPos(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosX:
                    {
                        RectTransform rectTransform = effectiveTarget.GetComponent<RectTransform>();
                    var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.x : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    rectTransform.anchoredPosition = startValue * Vector2.right + rectTransform.anchoredPosition.y * Vector2.up;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                    result = rectTransform.DOAnchorPosX(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosY:
                    {
                        RectTransform rectTransform = effectiveTarget.GetComponent<RectTransform>();
                    var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition.y : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    rectTransform.anchoredPosition = startValue * Vector2.up + rectTransform.anchoredPosition.x * Vector2.right;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                    result = rectTransform.DOAnchorPosY(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOAnchorPosZ:
                    {
                        RectTransform rectTransform = effectiveTarget.GetComponent<RectTransform>();
                    var targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D.z : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : rectTransform.anchoredPosition3D.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    var currentPos = rectTransform.anchoredPosition3D;
                    currentPos.z = startValue;
                    rectTransform.anchoredPosition3D = currentPos;
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                    float endZ = targetValue;
                    Vector3 startPos = rectTransform.anchoredPosition3D;
                    result = DOTween.To(() => rectTransform.anchoredPosition3D, pos => {
                        var p = pos;
                        p.z = endZ;
                        rectTransform.anchoredPosition3D = p;
                    }, new Vector3(startPos.x, startPos.y, endZ), duration).SetTarget(rectTransform);
                    }
                    break;
                case DOTweenType.DOAnchorPos3D:
                    {
                        RectTransform rectTransform = effectiveTarget.GetComponent<RectTransform>();
                    Vector3 targetValue = UseToTarget ? (ToTarget as RectTransform).anchoredPosition3D : ToValue;
                    Vector3 startValue = UseFromValue ? FromValue : rectTransform.anchoredPosition3D;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    rectTransform.anchoredPosition3D = startValue;
                        if (SpeedBased)
                        duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = rectTransform.DOAnchorPos3D(targetValue, duration, Snapping);
                    } 
                    break;
                case DOTweenType.DOColor:
                    {
                        Graphic graphic = effectiveTarget as Graphic;
                    Color targetValue = UseToTarget ? (ToTarget as Graphic).color : FromColor(ToValue);
                    Color startValue = UseFromValue ? FromColor(FromValue) : graphic.color;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    graphic.color = startValue;
                    result = graphic.DOColor(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOFade:
                    {
                        Graphic graphic = effectiveTarget as Graphic;
                    var targetValue = UseToTarget ? (ToTarget as Graphic).color.a : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : graphic.color.a;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    graphic.color = (graphic.color).WithAlpha(startValue);
                    result = graphic.DOFade(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOCanvasGroupFade:
                    {
                        CanvasGroup canvasGroup = effectiveTarget as CanvasGroup;
                    var targetValue = UseToTarget ? (ToTarget as CanvasGroup).alpha : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : canvasGroup.alpha;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    canvasGroup.alpha = startValue;
                    result = canvasGroup.DOFade(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOValue:
                    {
                        Slider slider = effectiveTarget as Slider;
                    var targetValue = UseToTarget ? (ToTarget as Slider).value : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : slider.value;
                    if (reverse)
                    {
                        (targetValue, startValue) = (startValue, targetValue);
                    }
                    slider.value = startValue;
                    result = slider.DOValue(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOSizeDelta:
                    {
                        RectTransform rectTransform = effectiveTarget.GetComponent<RectTransform>();
                    Vector2 targetValue = UseToTarget ? (ToTarget as RectTransform).sizeDelta : ToValue;
                    Vector2 startValue = UseFromValue ? FromValue : rectTransform.sizeDelta;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    rectTransform.sizeDelta = startValue;
                        if (SpeedBased)
                        duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = rectTransform.DOSizeDelta(targetValue, duration, Snapping);
                    }
                    break;
                case DOTweenType.DOFillAmount:
                    {
                        Image image = effectiveTarget as Image;
                    var targetValue = UseToTarget ? (ToTarget as Image).fillAmount : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : image.fillAmount;
                    if (reverse)
                    {
                        (targetValue, startValue) = (startValue, targetValue);
                    }
                    image.fillAmount = startValue;
                    result = image.DOFillAmount(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOFlexibleSize:
                    {
                        LayoutElement layoutElement = effectiveTarget as LayoutElement;
                    Vector2 targetValue = UseToTarget ? (ToTarget as LayoutElement).GetFlexibleSize() : FromValue;
                    Vector2 startValue = UseFromValue ? FromValue : layoutElement.GetFlexibleSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    layoutElement.SetFlexibleSize(startValue);
                        if (SpeedBased)
                            duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = layoutElement.DOFlexibleSize(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOMinSize:
                    {
                        LayoutElement layoutElement = effectiveTarget as LayoutElement;
                    Vector2 targetValue = UseToTarget ? (ToTarget as LayoutElement).GetMinSize() : FromValue;
                    Vector2 startValue = UseFromValue ? FromValue : layoutElement.GetMinSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    layoutElement.SetMinSize(startValue);
                        if (SpeedBased)
                        duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = layoutElement.DOMinSize(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOPreferredSize:
                    {
                        LayoutElement layoutElement = effectiveTarget as LayoutElement;
                    Vector2 targetValue = UseToTarget ? (ToTarget as LayoutElement).GetPreferredSize() : FromValue;
                    Vector2 startValue = UseFromValue ? FromValue : layoutElement.GetPreferredSize();
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    layoutElement.SetPreferredSize(startValue);
                        if (SpeedBased)
                        duration = Vector2.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = layoutElement.DOPreferredSize(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScale:
                    {
                        Transform transform = effectiveTarget.transform;
                    Vector3 targetValue = UseToTarget ? (ToTarget as Transform).localScale : ToValue;
                    Vector3 startValue = UseFromValue ? FromValue : transform.localScale;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localScale = startValue;
                        if (SpeedBased)
                        duration = Vector3.Distance(targetValue, startValue) / this.DurationOrSpeed;
                    result = transform.DOScale(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleX:
                    {
                        Transform transform = effectiveTarget.transform;
                    var targetValue = UseToTarget ? (ToTarget as Transform).localScale.x : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : transform.localScale.x;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localScale = transform.localScale.ChangeX(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                    result = transform.DOScaleX(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleY:
                    {
                        Transform transform = effectiveTarget.transform;
                    var targetValue = UseToTarget ? (ToTarget as Transform).localScale.y : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : transform.localScale.y;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localScale = transform.localScale.ChangeY(startValue);
                        if (SpeedBased)
                            duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                    result = transform.DOScaleY(targetValue, duration);
                    }
                    break;
                case DOTweenType.DOScaleZ:
                    {
                        Transform transform = effectiveTarget.transform;
                    var targetValue = UseToTarget ? (ToTarget as Transform).localScale.z : ToValue.x;
                    var startValue = UseFromValue ? FromValue.x : transform.localScale.z;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localScale = transform.localScale.ChangeZ(startValue);
                        if (SpeedBased)
                        duration = Mathf.Abs(targetValue - startValue) / this.DurationOrSpeed;
                    result = transform.DOScaleZ(targetValue, duration);
                    }
                    break;
                case DOTweenType.DORotate:
                    {
                        Transform transform = effectiveTarget.transform;
                    Vector3 targetValue = UseToTarget ? (ToTarget as Transform).eulerAngles : ToValue;
                    Vector3 startValue = UseFromValue ? FromValue : transform.eulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.eulerAngles = startValue;
                        if (SpeedBased)
                        duration = GetAngleDistance(startValue, targetValue) / this.DurationOrSpeed;
                    result = transform.DORotate(targetValue, duration, RotateMode.Fast);
                    }
                    break;
                case DOTweenType.DOLocalRotate:
                    {
                        Transform transform = effectiveTarget.transform;
                    Vector3 targetValue = UseToTarget ? (ToTarget as Transform).localEulerAngles : ToValue;
                    Vector3 startValue = UseFromValue ? FromValue : transform.localEulerAngles;
                        if (reverse)
                        {
                            (targetValue, startValue) = (startValue, targetValue);
                        }
                    transform.localEulerAngles = startValue;
                        if (SpeedBased)
                        duration = GetAngleDistance(startValue, targetValue) / this.DurationOrSpeed;
                    result = transform.DOLocalRotate(targetValue, duration, RotateMode.Fast);
                    }
                    break;
            }

            if (result != null)
            {
            result.SetDelay(Delay);
            if (CustomEase)
                result.SetEase(EaseCurve);
            else
                result.SetEase(Ease);
            if (Loops > 1)
            {
                result.SetLoops(Loops, LoopType);
            }
            result.OnStart(() => OnPlay?.Invoke());
            result.OnUpdate(() => OnUpdate?.Invoke());
            result.OnComplete(() => OnComplete?.Invoke());
        }

            return result;
        }

    private static Color FromColor(Vector4 v)
    {
        return new Color(v.x, v.y, v.z, v.w);
    }

    private static float GetAngleDistance(Vector3 euler1, Vector3 euler2)
    {
        Vector3 delta;
            delta.x = Mathf.DeltaAngle(euler1.x, euler2.x);
            delta.y = Mathf.DeltaAngle(euler1.y, euler2.y);
            delta.z = Mathf.DeltaAngle(euler1.z, euler2.z);

            float angle = Mathf.Sqrt(delta.x * delta.x + delta.y * delta.y + delta.z * delta.z);
            return (angle + 360) % 360;
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
    public System.Collections.Generic.Dictionary<int, Component> RuntimeTargetOverrides = null;

    [InspectorName("启动时播放"), Tooltip("场景加载时自动播放整个序列")]
    [SerializeField]
    bool m_PlayOnAwake = false;

    [InspectorName("启动时重置"), Tooltip("场景加载时先重置到起始状态")]
    [SerializeField]
    bool m_ResetOnAwake = false;

    [InspectorName("整体延迟"), Tooltip("整个序列播放前的等待时间")]
    [SerializeField]
    float m_Delay = 0;

    [InspectorName("整体缓动"), Tooltip("所有动画的默认缓动曲线")]
    [SerializeField]
    Ease m_Ease = Ease.OutQuad;

    [InspectorName("整体循环"), Tooltip("整个序列的循环次数")]
    [SerializeField]
    int m_Loops = 1;

    [InspectorName("循环方式"), Tooltip("Restart：重新开始 | Yoyo：来回播放 | Incremental：累加")]
    [SerializeField]
    LoopType m_LoopType = LoopType.Restart;

    [InspectorName("更新模式"), Tooltip("Normal：正常更新 | Late：LateUpdate更新 | Fixed：物理更新")]
    [SerializeField]
    UpdateType m_UpdateType = UpdateType.Normal;

    [InspectorName("忽略时间缩放"), Tooltip("动画不受Time.timeScale影响")]
    [SerializeField]
    bool m_IgnoreTimeScale = true;

    [Header("【整体回调】")]
    [InspectorName("序列开始时"), Tooltip("整个序列开始播放时触发")]
    [SerializeField]
    UnityEvent m_OnPlay = null;

    [InspectorName("序列更新时"), Tooltip("整个序列播放中每帧触发")]
    [SerializeField]
    UnityEvent m_OnUpdate = null;

    [InspectorName("序列完成时"), Tooltip("整个序列播放完成时触发")]
    [SerializeField]
    UnityEvent m_OnComplete = null;

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

    private void ResetToFromValue()
    {
        if (m_Sequence == null) return;

        for (int i = 0; i < m_Sequence.Length; i++)
        {
            var item = m_Sequence[i];
            if (!item.UseFromValue) continue;

            Component targetCom = ResolveTarget(item, i);
            if (targetCom == null) continue;

            var resetValue = item.FromValue;
            switch (item.AnimationType)
            {
                case DOTweenType.DOMove:
                    (targetCom as Transform).position = resetValue;
                    break;
                case DOTweenType.DOMoveX:
                    (targetCom as Transform).SetPositionX(resetValue.x);
                    break;
                case DOTweenType.DOMoveY:
                    (targetCom as Transform).SetPositionY(resetValue.x);
                    break;
                case DOTweenType.DOMoveZ:
                    (targetCom as Transform).SetPositionZ(resetValue.x);
                    break;
                case DOTweenType.DOLocalMove:
                    (targetCom as Transform).localPosition = resetValue;
                    break;
                case DOTweenType.DOLocalMoveX:
                    (targetCom as Transform).SetLocalPositionX(resetValue.x);
                    break;
                case DOTweenType.DOLocalMoveY:
                    (targetCom as Transform).SetLocalPositionY(resetValue.x);
                    break;
                case DOTweenType.DOLocalMoveZ:
                    (targetCom as Transform).SetLocalPositionZ(resetValue.x);
                    break;
                case DOTweenType.DOAnchorPos:
                    (targetCom as RectTransform).anchoredPosition = resetValue;
                    break;
                case DOTweenType.DOAnchorPosX:
                    (targetCom as RectTransform).SetAnchoredPositionX(resetValue.x);
                    break;
                case DOTweenType.DOAnchorPosY:
                    (targetCom as RectTransform).SetAnchoredPositionY(resetValue.x);
                    break;
                case DOTweenType.DOAnchorPosZ:
                    (targetCom as RectTransform).SetAnchoredPosition3DZ(resetValue.x);
                    break;
                case DOTweenType.DOAnchorPos3D:
                    (targetCom as RectTransform).anchoredPosition3D = resetValue;
                    break;
                case DOTweenType.DOColor:
                    (targetCom as Graphic).color = resetValue;
                    break;
                case DOTweenType.DOFade:
                    (targetCom as Graphic).SetColorAlpha(resetValue.x);
                    break;
                case DOTweenType.DOCanvasGroupFade:
                    (targetCom as CanvasGroup).alpha = resetValue.x;
                    break;
                case DOTweenType.DOValue:
                    (targetCom as Slider).value = resetValue.x;
                    break;
                case DOTweenType.DOSizeDelta:
                    (targetCom as RectTransform).sizeDelta = resetValue;
                    break;
                case DOTweenType.DOFillAmount:
                    (targetCom as Image).fillAmount = resetValue.x;
                    break;
                case DOTweenType.DOFlexibleSize:
                    (targetCom as LayoutElement).SetFlexibleSize(resetValue);
                    break;
                case DOTweenType.DOMinSize:
                    (targetCom as LayoutElement).SetMinSize(resetValue);
                    break;
                case DOTweenType.DOPreferredSize:
                    (targetCom as LayoutElement).SetPreferredSize(resetValue);
                    break;
                case DOTweenType.DOScale:
                    (targetCom as Transform).localScale = resetValue;
                    break;
                case DOTweenType.DOScaleX:
                    (targetCom as Transform).SetLocalScaleX(resetValue.x);
                    break;
                case DOTweenType.DOScaleY:
                    (targetCom as Transform).SetLocalScaleY(resetValue.x);
                    break;
                case DOTweenType.DOScaleZ:
                    (targetCom as Transform).SetLocalScaleZ(resetValue.z);
                    break;
                case DOTweenType.DORotate:
                    (targetCom as Transform).eulerAngles = resetValue;
                    break;
                case DOTweenType.DOLocalRotate:
                    (targetCom as Transform).localEulerAngles = resetValue;
                    break;
            }
        }
    }

    private Tween CreateTween(bool reverse = false)
    {
        return CreateTween(null, reverse);
    }

    private Tween CreateTween(Component runtimeTarget, bool reverse = false)
    {
        if (m_Sequence == null || m_Sequence.Length == 0)
        {
            return null;
        }

        var sequence = DOTween.Sequence();
        if (reverse)
        {
            for (int i = m_Sequence.Length - 1; i >= 0; i--)
            {
                var item = m_Sequence[i];
                var tweener = item.CreateTween(ResolveTarget(item, i, runtimeTarget), reverse);
                if (tweener == null)
                {
                    Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i, item.AnimationType, ResolveTarget(item, i, runtimeTarget) == null ? "null" : ResolveTarget(item, i, runtimeTarget).GetType().Name);
                    continue;
                }
                tweener.SetUpdate(!m_IgnoreTimeScale);
                switch (item.AddType)
                {
                    case AddType.Append:
                        sequence.Append(tweener);
                        break;
                    case AddType.Join:
                        sequence.Join(tweener);
                        break;
                }
            }
        }
        else
        {
            for (int i = 0; i < m_Sequence.Length; i++)
            {
                var item = m_Sequence[i];
                var tweener = item.CreateTween(ResolveTarget(item, i, runtimeTarget), reverse);
                if (tweener == null)
                {
                    Debug.LogErrorFormat("Tweener is null. Index:{0}, Animation Type:{1}, Component Type:{2}", i, item.AnimationType, ResolveTarget(item, i, runtimeTarget) == null ? "null" : ResolveTarget(item, i, runtimeTarget).GetType().Name);
                    continue;
                }
                tweener.SetUpdate(!m_IgnoreTimeScale);
                switch (item.AddType)
                {
                    case AddType.Append:
                        sequence.Append(tweener);
                        break;
                    case AddType.Join:
                        sequence.Join(tweener);
                        break;
                }
            }
        }

        sequence.SetUpdate(m_UpdateType);
        sequence.SetDelay(m_Delay);
        sequence.SetEase(m_Ease);
        if (m_Loops > 1)
        {
            sequence.SetLoops(m_Loops, m_LoopType);
        }
        sequence.OnStart(() => m_OnPlay?.Invoke());
        sequence.OnUpdate(() => m_OnUpdate?.Invoke());
        sequence.OnComplete(() => m_OnComplete?.Invoke());
        return sequence;
    }

    public Tween DOPlay(bool recyle = true)
    {
        // 无参数版本：杀死所有现有动画
        DOKill();
        var tween = CreateTween(null, false);
        if (recyle)
        {
            tween.SetAutoKill(true);
        }
        else
        {
            tween.SetAutoKill(false);
        }
        tween.Play();
        return tween;
    }

    /// <summary>
    /// 使用指定的运行时 Target 播放动画
    /// </summary>
    /// <param name="target">动态指定的 Target</param>
    /// <param name="recyle">是否回收 Tween</param>
    /// <returns></returns>
    public Tween DOPlay(Component target, bool recyle = true)
    {
        Debug.Log($"[DOPlay] target={target?.name}");

        // DOTween.Kill(target) 会精确杀死这个目标上的所有 tween
        DOTween.Kill(target);

        var tween = CreateTween(target, false);
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

        // DOTween.Kill(target) 会精确杀死这个目标上的 tween，不影响其他目标
        DOTween.Kill(target);

        var tween = CreateTween(target, true);
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
        var tween = CreateTween(target, false, startIndex, endIndex);
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
        var tween = CreateTween(target, true, startIndex, endIndex);
        tween.SetAutoKill(true);
        tween.Play();
    }

    /// <summary>
    /// 创建指定索引范围的 Tween
    /// </summary>
    private Tween CreateTween(Component runtimeTarget, bool reverse, int startIndex, int endIndex)
    {
        var sequence = DOTween.Sequence();
        if (m_Sequence == null || m_Sequence.Length == 0) return sequence;

        startIndex = Mathf.Clamp(startIndex, 0, m_Sequence.Length - 1);
        endIndex = Mathf.Clamp(endIndex, 0, m_Sequence.Length - 1);
        if (startIndex > endIndex) return sequence;

        for (int i = startIndex; i <= endIndex; i++)
        {
            var item = m_Sequence[i];
            var tweener = item.CreateTween(ResolveTarget(item, i, runtimeTarget), reverse);
            if (tweener == null) continue;
            tweener.SetUpdate(!m_IgnoreTimeScale);
            if (item.AddType == AddType.Append)
                sequence.Append(tweener);
            else
                sequence.Join(tweener);
        }

        sequence.SetUpdate(m_UpdateType);
        sequence.SetDelay(m_Delay);
        sequence.SetEase(m_Ease);
        if (m_Loops > 1) sequence.SetLoops(m_Loops, m_LoopType);
        sequence.OnStart(() => m_OnPlay?.Invoke());
        sequence.OnUpdate(() => m_OnUpdate?.Invoke());
        sequence.OnComplete(() => m_OnComplete?.Invoke());
        return sequence;
    }

    public void DOKill()
    {
        Debug.Log($"[DOKill] killing default target");
        DOTween.Kill(DefaultTarget);
    }

    /// <summary>
    /// 解析目标：优先级：RuntimeTargetOverrides > 子序列内 Target > DefaultTarget
    /// </summary>
    private Component ResolveTarget(SequenceAnimation item, int index)
    {
        if (RuntimeTargetOverrides != null && RuntimeTargetOverrides.TryGetValue(index, out var overrideTarget) && overrideTarget != null)
        {
            return overrideTarget;
        }
        if (item.Target != null)
        {
            return item.Target;
        }
        return DefaultTarget;
    }

    /// <summary>
    /// 解析目标：优先级：运行时 Target > RuntimeTargetOverrides > 子序列内 Target > DefaultTarget
    /// </summary>
    private Component ResolveTarget(SequenceAnimation item, int index, Component runtimeTarget)
    {
        if (runtimeTarget != null)
        {
            return runtimeTarget;
        }
        return ResolveTarget(item, index);
    }
}

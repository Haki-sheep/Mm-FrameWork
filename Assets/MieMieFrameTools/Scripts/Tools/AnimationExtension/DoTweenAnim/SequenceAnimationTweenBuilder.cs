using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using CWTools.Extensions;
using Component = UnityEngine.Component;

/// <summary>
/// SequenceAnimation Tween 构建器
/// </summary>
internal static class SequenceAnimationTweenBuilder
{
    private enum ELayoutSizeType
    {
        Flexible,
        Min,
        Preferred
    }

    /// <summary>
    /// 创建 Tween
    /// </summary>
    public static Tween Create(SequenceAnimation anim, Component target, bool reverse)
    {
        if (target == null)
        {
            Debug.LogError("[SequenceAnimation] Target is null!");
            return null;
        }

        Tween tween = CreateRawTween(anim, target, reverse);
        return ApplyTweenSettings(anim, tween);
    }

    /// <summary>
    /// 按动画类型分发创建
    /// </summary>
    private static Tween CreateRawTween(SequenceAnimation anim, Component target, bool reverse)
    {
        var eType = anim.AnimationType;
        switch (eType)
        {
            case DOTweenType.DOMove: return CreateTransformMove(anim, target.transform, reverse, false);
            case DOTweenType.DOMoveX: return CreateTransformMoveAxis(anim, target.transform, reverse, false, 0);
            case DOTweenType.DOMoveY: return CreateTransformMoveAxis(anim, target.transform, reverse, false, 1);
            case DOTweenType.DOMoveZ: return CreateTransformMoveAxis(anim, target.transform, reverse, false, 2);
            case DOTweenType.DOLocalMove: return CreateTransformMove(anim, target.transform, reverse, true);
            case DOTweenType.DOLocalMoveX: return CreateTransformMoveAxis(anim, target.transform, reverse, true, 0);
            case DOTweenType.DOLocalMoveY: return CreateTransformMoveAxis(anim, target.transform, reverse, true, 1);
            case DOTweenType.DOLocalMoveZ: return CreateTransformMoveAxis(anim, target.transform, reverse, true, 2);
            case DOTweenType.DOScale: return CreateTransformScale(anim, target.transform, reverse);
            case DOTweenType.DOScaleX: return CreateTransformScaleAxis(anim, target.transform, reverse, 0);
            case DOTweenType.DOScaleY: return CreateTransformScaleAxis(anim, target.transform, reverse, 1);
            case DOTweenType.DOScaleZ: return CreateTransformScaleAxis(anim, target.transform, reverse, 2);
            case DOTweenType.DORotate: return CreateTransformRotate(anim, target.transform, reverse, false);
            case DOTweenType.DOLocalRotate: return CreateTransformRotate(anim, target.transform, reverse, true);
            case DOTweenType.DOAnchorPos: return CreateRectAnchorPos(anim, target, reverse);
            case DOTweenType.DOAnchorPosX: return CreateRectAnchorAxis(anim, target, reverse, 0);
            case DOTweenType.DOAnchorPosY: return CreateRectAnchorAxis(anim, target, reverse, 1);
            case DOTweenType.DOAnchorPosZ: return CreateRectAnchorPosZ(anim, target, reverse);
            case DOTweenType.DOAnchorPos3D: return CreateRectAnchorPos3D(anim, target, reverse);
            case DOTweenType.DOColor: return CreateGraphicColor(anim, target, reverse);
            case DOTweenType.DOFade: return CreateGraphicFade(anim, target, reverse);
            case DOTweenType.DOCanvasGroupFade: return CreateCanvasGroupFade(anim, target, reverse);
            case DOTweenType.DOValue: return CreateSliderValue(anim, target, reverse);
            case DOTweenType.DOSizeDelta: return CreateRectSizeDelta(anim, target, reverse);
            case DOTweenType.DOFillAmount: return CreateImageFillAmount(anim, target, reverse);
            case DOTweenType.DOFlexibleSize: return CreateLayoutElementSize(anim, target, reverse, ELayoutSizeType.Flexible);
            case DOTweenType.DOMinSize: return CreateLayoutElementSize(anim, target, reverse, ELayoutSizeType.Min);
            case DOTweenType.DOPreferredSize: return CreateLayoutElementSize(anim, target, reverse, ELayoutSizeType.Preferred);
            default: return null;
        }
    }

    /// <summary>
    /// Transform 全轴移动
    /// </summary>
    private static Tween CreateTransformMove(SequenceAnimation anim, Transform transform, bool reverse, bool local)
    {
        Vector3 current = local ? transform.localPosition : transform.position;
        ResolveVector3(anim, reverse, current, GetTransformPosition(local), (Vector3)anim.ToValue, out Vector3 start, out Vector3 end);

        if (local) transform.localPosition = start;
        else transform.position = start;

        float duration = ResolveDuration(anim, Vector3.Distance(end, start));
        return local
            ? transform.DOLocalMove(end, duration, anim.Snapping)
            : transform.DOMove(end, duration, anim.Snapping);
    }

    /// <summary>
    /// Transform 单轴移动
    /// </summary>
    private static Tween CreateTransformMoveAxis(SequenceAnimation anim, Transform transform, bool reverse, bool local, int axis)
    {
        Vector3 current = local ? transform.localPosition : transform.position;
        float currentAxis = axis == 0 ? current.x : axis == 1 ? current.y : current.z;
        ResolveFloat(anim, reverse, currentAxis, GetTransformAxis(local, axis), out float start, out float end);

        if (local)
        {
            if (axis == 0) transform.SetLocalPositionX(start);
            else if (axis == 1) transform.SetLocalPositionY(start);
            else transform.SetLocalPositionZ(start);
        }
        else
        {
            if (axis == 0) transform.SetPositionX(start);
            else if (axis == 1) transform.SetPositionY(start);
            else transform.SetPositionZ(start);
        }

        float duration = ResolveDuration(anim, Mathf.Abs(end - start));
        if (local)
        {
            if (axis == 0) return transform.DOLocalMoveX(end, duration, anim.Snapping);
            if (axis == 1) return transform.DOLocalMoveY(end, duration, anim.Snapping);
            return transform.DOLocalMoveZ(end, duration, anim.Snapping);
        }

        if (axis == 0) return transform.DOMoveX(end, duration, anim.Snapping);
        if (axis == 1) return transform.DOMoveY(end, duration, anim.Snapping);
        return transform.DOMoveZ(end, duration, anim.Snapping);
    }

    /// <summary>
    /// Transform 全轴缩放
    /// </summary>
    private static Tween CreateTransformScale(SequenceAnimation anim, Transform transform, bool reverse)
    {
        ResolveVector3(anim, reverse, transform.localScale, t => (t as Transform).localScale, (Vector3)anim.ToValue, out Vector3 start, out Vector3 end);
        transform.localScale = start;
        float duration = ResolveDuration(anim, Vector3.Distance(end, start));
        return transform.DOScale(end, duration);
    }

    /// <summary>
    /// Transform 单轴缩放
    /// </summary>
    private static Tween CreateTransformScaleAxis(SequenceAnimation anim, Transform transform, bool reverse, int axis)
    {
        Vector3 current = transform.localScale;
        float currentAxis = axis == 0 ? current.x : axis == 1 ? current.y : current.z;
        ResolveFloat(anim, reverse, currentAxis, GetTransformScaleAxis(axis), out float start, out float end);

        if (axis == 0) transform.localScale = transform.localScale.ChangeX(start);
        else if (axis == 1) transform.localScale = transform.localScale.ChangeY(start);
        else transform.localScale = transform.localScale.ChangeZ(start);

        float duration = ResolveDuration(anim, Mathf.Abs(end - start));
        if (axis == 0) return transform.DOScaleX(end, duration);
        if (axis == 1) return transform.DOScaleY(end, duration);
        return transform.DOScaleZ(end, duration);
    }

    /// <summary>
    /// Transform 旋转
    /// </summary>
    private static Tween CreateTransformRotate(SequenceAnimation anim, Transform transform, bool reverse, bool local)
    {
        Vector3 current = local ? transform.localEulerAngles : transform.eulerAngles;
        ResolveVector3(anim, reverse, current, GetTransformRotation(local), (Vector3)anim.ToValue, out Vector3 start, out Vector3 end);

        if (local) transform.localEulerAngles = start;
        else transform.eulerAngles = start;

        float duration = ResolveDuration(anim, GetAngleDistance(start, end));
        return local
            ? transform.DOLocalRotate(end, duration, RotateMode.Fast)
            : transform.DORotate(end, duration, RotateMode.Fast);
    }

    /// <summary>
    /// RectTransform 锚点移动
    /// </summary>
    private static Tween CreateRectAnchorPos(SequenceAnimation anim, Component target, bool reverse)
    {
        var rectTransform = target.GetComponent<RectTransform>();
        ResolveVector2(anim, reverse, rectTransform.anchoredPosition, t => (t as RectTransform).anchoredPosition, anim.ToValue, out Vector2 start, out Vector2 end);
        rectTransform.anchoredPosition = start;
        float duration = ResolveDuration(anim, Vector2.Distance(end, start));
        return rectTransform.DOAnchorPos(end, duration, anim.Snapping);
    }

    /// <summary>
    /// RectTransform 锚点单轴移动
    /// </summary>
    private static Tween CreateRectAnchorAxis(SequenceAnimation anim, Component target, bool reverse, int axis)
    {
        var rectTransform = target.GetComponent<RectTransform>();
        Vector2 current = rectTransform.anchoredPosition;
        float currentAxis = axis == 0 ? current.x : current.y;
        ResolveFloat(anim, reverse, currentAxis, GetRectAnchorAxis(axis), out float start, out float end);

        if (axis == 0) rectTransform.SetAnchoredPositionX(start);
        else rectTransform.SetAnchoredPositionY(start);

        float duration = ResolveDuration(anim, Mathf.Abs(end - start));
        return axis == 0
            ? rectTransform.DOAnchorPosX(end, duration, anim.Snapping)
            : rectTransform.DOAnchorPosY(end, duration, anim.Snapping);
    }

    /// <summary>
    /// RectTransform 锚点 Z 轴移动
    /// </summary>
    private static Tween CreateRectAnchorPosZ(SequenceAnimation anim, Component target, bool reverse)
    {
        var rectTransform = target.GetComponent<RectTransform>();
        float currentZ = rectTransform.anchoredPosition3D.z;
        ResolveFloat(anim, reverse, currentZ, t => (t as RectTransform).anchoredPosition3D.z, out float start, out float end);

        var currentPos = rectTransform.anchoredPosition3D;
        currentPos.z = start;
        rectTransform.anchoredPosition3D = currentPos;

        float duration = ResolveDuration(anim, Mathf.Abs(end - start));
        float endZ = end;
        Vector3 startPos = rectTransform.anchoredPosition3D;
        return DOTween.To(
            () => rectTransform.anchoredPosition3D,
            pos =>
            {
                var p = pos;
                p.z = endZ;
                rectTransform.anchoredPosition3D = p;
            },
            new Vector3(startPos.x, startPos.y, endZ),
            duration).SetTarget(rectTransform);
    }

    /// <summary>
    /// RectTransform 锚点 3D 移动
    /// </summary>
    private static Tween CreateRectAnchorPos3D(SequenceAnimation anim, Component target, bool reverse)
    {
        var rectTransform = target.GetComponent<RectTransform>();
        ResolveVector3(anim, reverse, rectTransform.anchoredPosition3D, t => (t as RectTransform).anchoredPosition3D, (Vector3)anim.ToValue, out Vector3 start, out Vector3 end);
        rectTransform.anchoredPosition3D = start;
        float duration = ResolveDuration(anim, Vector3.Distance(end, start));
        return rectTransform.DOAnchorPos3D(end, duration, anim.Snapping);
    }

    /// <summary>
    /// Graphic 颜色渐变
    /// </summary>
    private static Tween CreateGraphicColor(SequenceAnimation anim, Component target, bool reverse)
    {
        var graphic = target as Graphic;
        Color current = graphic.color;
        Color end = anim.UseToTarget ? (anim.ToTarget as Graphic).color : FromColor(anim.ToValue);
        Color start = anim.UseFromValue ? FromColor(anim.FromValue) : current;
        if (reverse) (end, start) = (start, end);
        graphic.color = start;
        float duration = anim.DurationOrSpeed;
        return graphic.DOColor(end, duration);
    }

    /// <summary>
    /// Graphic 透明度渐变
    /// </summary>
    private static Tween CreateGraphicFade(SequenceAnimation anim, Component target, bool reverse)
    {
        var graphic = target as Graphic;
        ResolveFloat(anim, reverse, graphic.color.a, t => (t as Graphic).color.a, out float start, out float end);
        graphic.color = graphic.color.WithAlpha(start);
        float duration = anim.DurationOrSpeed;
        return graphic.DOFade(end, duration);
    }

    /// <summary>
    /// CanvasGroup 透明度渐变
    /// </summary>
    private static Tween CreateCanvasGroupFade(SequenceAnimation anim, Component target, bool reverse)
    {
        var canvasGroup = target as CanvasGroup;
        ResolveFloat(anim, reverse, canvasGroup.alpha, t => (t as CanvasGroup).alpha, out float start, out float end);
        canvasGroup.alpha = start;
        float duration = anim.DurationOrSpeed;
        return canvasGroup.DOFade(end, duration);
    }

    /// <summary>
    /// Slider 数值渐变
    /// </summary>
    private static Tween CreateSliderValue(SequenceAnimation anim, Component target, bool reverse)
    {
        var slider = target as Slider;
        ResolveFloat(anim, reverse, slider.value, t => (t as Slider).value, out float start, out float end);
        slider.value = start;
        float duration = anim.DurationOrSpeed;
        return slider.DOValue(end, duration, anim.Snapping);
    }

    /// <summary>
    /// RectTransform 尺寸变化
    /// </summary>
    private static Tween CreateRectSizeDelta(SequenceAnimation anim, Component target, bool reverse)
    {
        var rectTransform = target.GetComponent<RectTransform>();
        ResolveVector2(anim, reverse, rectTransform.sizeDelta, t => (t as RectTransform).sizeDelta, anim.ToValue, out Vector2 start, out Vector2 end);
        rectTransform.sizeDelta = start;
        float duration = ResolveDuration(anim, Vector2.Distance(end, start));
        return rectTransform.DOSizeDelta(end, duration, anim.Snapping);
    }

    /// <summary>
    /// Image 填充渐变
    /// </summary>
    private static Tween CreateImageFillAmount(SequenceAnimation anim, Component target, bool reverse)
    {
        var image = target as Image;
        ResolveFloat(anim, reverse, image.fillAmount, t => (t as Image).fillAmount, out float start, out float end);
        image.fillAmount = start;
        float duration = anim.DurationOrSpeed;
        return image.DOFillAmount(end, duration);
    }

    /// <summary>
    /// LayoutElement 尺寸渐变
    /// </summary>
    private static Tween CreateLayoutElementSize(SequenceAnimation anim, Component target, bool reverse, ELayoutSizeType eSizeType)
    {
        var layoutElement = target as LayoutElement;
        Vector2 current = GetLayoutSize(layoutElement, eSizeType);
        // 保持原有逻辑 Layout 类型目标值读取 FromValue
        Vector2 end = anim.UseToTarget ? GetLayoutSize(anim.ToTarget as LayoutElement, eSizeType) : anim.FromValue;
        Vector2 start = anim.UseFromValue ? anim.FromValue : current;
        if (reverse) (end, start) = (start, end);

        SetLayoutSize(layoutElement, eSizeType, start);
        float duration = ResolveDuration(anim, Vector2.Distance(end, start));
        if (eSizeType == ELayoutSizeType.Flexible) return layoutElement.DOFlexibleSize(end, duration);
        if (eSizeType == ELayoutSizeType.Min) return layoutElement.DOMinSize(end, duration);
        return layoutElement.DOPreferredSize(end, duration);
    }

    /// <summary>
    /// 解析 Vector3 起止值
    /// </summary>
    private static void ResolveVector3(SequenceAnimation anim, bool reverse, Vector3 current, System.Func<Component, Vector3> getToTarget, Vector3 toValue, out Vector3 start, out Vector3 end)
    {
        end = anim.UseToTarget ? getToTarget(anim.ToTarget) : toValue;
        start = anim.UseFromValue ? (Vector3)anim.FromValue : current;
        if (reverse) (end, start) = (start, end);
    }

    /// <summary>
    /// 解析 Vector2 起止值
    /// </summary>
    private static void ResolveVector2(SequenceAnimation anim, bool reverse, Vector2 current, System.Func<Component, Vector2> getToTarget, Vector4 toValue, out Vector2 start, out Vector2 end)
    {
        end = anim.UseToTarget ? getToTarget(anim.ToTarget) : toValue;
        start = anim.UseFromValue ? anim.FromValue : current;
        if (reverse) (end, start) = (start, end);
    }

    /// <summary>
    /// 解析 float 起止值
    /// </summary>
    private static void ResolveFloat(SequenceAnimation anim, bool reverse, float current, System.Func<Component, float> getToTarget, out float start, out float end)
    {
        end = anim.UseToTarget ? getToTarget(anim.ToTarget) : anim.ToValue.x;
        start = anim.UseFromValue ? anim.FromValue.x : current;
        if (reverse) (end, start) = (start, end);
    }

    /// <summary>
    /// 计算持续时间
    /// </summary>
    private static float ResolveDuration(SequenceAnimation anim, float distance)
    {
        return anim.SpeedBased ? distance / anim.DurationOrSpeed : anim.DurationOrSpeed;
    }

    /// <summary>
    /// 应用 Tween 通用配置
    /// </summary>
    private static Tween ApplyTweenSettings(SequenceAnimation anim, Tween tween)
    {
        if (tween == null) return null;

        tween.SetDelay(anim.Delay);
        if (anim.CustomEase) tween.SetEase(anim.EaseCurve);
        else tween.SetEase(anim.Ease);
        if (anim.Loops > 1) tween.SetLoops(anim.Loops, anim.LoopType);
        tween.OnStart(() => anim.OnPlay?.Invoke());
        tween.OnUpdate(() => anim.OnUpdate?.Invoke());
        tween.OnComplete(() => anim.OnComplete?.Invoke());
        return tween;
    }

    private static System.Func<Component, Vector3> GetTransformPosition(bool local) =>
        local
            ? (System.Func<Component, Vector3>)(t => (t as Transform).localPosition)
            : t => (t as Transform).position;

    private static System.Func<Component, Vector3> GetTransformRotation(bool local) =>
        local ? (t => (t as Transform).localEulerAngles) : (t => (t as Transform).eulerAngles);

    private static System.Func<Component, float> GetTransformAxis(bool local, int axis)
    {
        if (local)
        {
            if (axis == 0) return t => (t as Transform).localPosition.x;
            if (axis == 1) return t => (t as Transform).localPosition.y;
            return t => (t as Transform).localPosition.z;
        }

        if (axis == 0) return t => (t as Transform).position.x;
        if (axis == 1) return t => (t as Transform).position.y;
        return t => (t as Transform).position.z;
    }

    private static System.Func<Component, float> GetTransformScaleAxis(int axis)
    {
        if (axis == 0) return t => (t as Transform).localScale.x;
        if (axis == 1) return t => (t as Transform).localScale.y;
        return t => (t as Transform).localScale.z;
    }

    private static System.Func<Component, float> GetRectAnchorAxis(int axis) =>
        axis == 0
            ? (System.Func<Component, float>)(t => (t as RectTransform).anchoredPosition.x)
            : t => (t as RectTransform).anchoredPosition.y;

    private static Vector2 GetLayoutSize(LayoutElement layoutElement, ELayoutSizeType eSizeType)
    {
        if (eSizeType == ELayoutSizeType.Flexible) return layoutElement.GetFlexibleSize();
        if (eSizeType == ELayoutSizeType.Min) return layoutElement.GetMinSize();
        return layoutElement.GetPreferredSize();
    }

    private static void SetLayoutSize(LayoutElement layoutElement, ELayoutSizeType eSizeType, Vector2 size)
    {
        if (eSizeType == ELayoutSizeType.Flexible) layoutElement.SetFlexibleSize(size);
        else if (eSizeType == ELayoutSizeType.Min) layoutElement.SetMinSize(size);
        else layoutElement.SetPreferredSize(size);
    }

    private static Color FromColor(Vector4 v) => new Color(v.x, v.y, v.z, v.w);

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

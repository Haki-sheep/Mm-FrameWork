using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using CWTools.Extensions;
using Component = UnityEngine.Component;

/// <summary>
/// SequenceAnimation 起始值重置
/// </summary>
internal static class SequenceAnimationResetApplier
{
    /// <summary>
    /// 重置动作表
    /// </summary>
    private static readonly Dictionary<DOTweenType, Action<Component, Vector4>> ResetActionDict = BuildResetActions();

    /// <summary>
    /// 应用起始值
    /// </summary>
    public static void Apply(Component target, DOTweenType eType, Vector4 resetValue)
    {
        if (target == null) return;
        if (ResetActionDict.TryGetValue(eType, out Action<Component, Vector4> action))
        {
            action(target, resetValue);
        }
    }

    /// <summary>
    /// 构建重置动作表
    /// </summary>
    private static Dictionary<DOTweenType, Action<Component, Vector4>> BuildResetActions()
    {
        var dict = new Dictionary<DOTweenType, Action<Component, Vector4>>();

        dict[DOTweenType.DOMove] = (c, v) => (c as Transform).position = v;
        dict[DOTweenType.DOMoveX] = (c, v) => (c as Transform).SetPositionX(v.x);
        dict[DOTweenType.DOMoveY] = (c, v) => (c as Transform).SetPositionY(v.x);
        dict[DOTweenType.DOMoveZ] = (c, v) => (c as Transform).SetPositionZ(v.x);
        dict[DOTweenType.DOLocalMove] = (c, v) => (c as Transform).localPosition = v;
        dict[DOTweenType.DOLocalMoveX] = (c, v) => (c as Transform).SetLocalPositionX(v.x);
        dict[DOTweenType.DOLocalMoveY] = (c, v) => (c as Transform).SetLocalPositionY(v.x);
        dict[DOTweenType.DOLocalMoveZ] = (c, v) => (c as Transform).SetLocalPositionZ(v.x);
        dict[DOTweenType.DOAnchorPos] = (c, v) => (c as RectTransform).anchoredPosition = v;
        dict[DOTweenType.DOAnchorPosX] = (c, v) => (c as RectTransform).SetAnchoredPositionX(v.x);
        dict[DOTweenType.DOAnchorPosY] = (c, v) => (c as RectTransform).SetAnchoredPositionY(v.x);
        dict[DOTweenType.DOAnchorPosZ] = (c, v) => (c as RectTransform).SetAnchoredPosition3DZ(v.x);
        dict[DOTweenType.DOAnchorPos3D] = (c, v) => (c as RectTransform).anchoredPosition3D = v;
        dict[DOTweenType.DOColor] = (c, v) => (c as Graphic).color = v;
        dict[DOTweenType.DOFade] = (c, v) => (c as Graphic).SetColorAlpha(v.x);
        dict[DOTweenType.DOCanvasGroupFade] = (c, v) => (c as CanvasGroup).alpha = v.x;
        dict[DOTweenType.DOValue] = (c, v) => (c as Slider).value = v.x;
        dict[DOTweenType.DOSizeDelta] = (c, v) => (c as RectTransform).sizeDelta = v;
        dict[DOTweenType.DOFillAmount] = (c, v) => (c as Image).fillAmount = v.x;
        dict[DOTweenType.DOFlexibleSize] = (c, v) => (c as LayoutElement).SetFlexibleSize(v);
        dict[DOTweenType.DOMinSize] = (c, v) => (c as LayoutElement).SetMinSize(v);
        dict[DOTweenType.DOPreferredSize] = (c, v) => (c as LayoutElement).SetPreferredSize(v);
        dict[DOTweenType.DOScale] = (c, v) => (c as Transform).localScale = v;
        dict[DOTweenType.DOScaleX] = (c, v) => (c as Transform).SetLocalScaleX(v.x);
        dict[DOTweenType.DOScaleY] = (c, v) => (c as Transform).SetLocalScaleY(v.x);
        dict[DOTweenType.DOScaleZ] = (c, v) => (c as Transform).SetLocalScaleZ(v.z);
        dict[DOTweenType.DORotate] = (c, v) => (c as Transform).eulerAngles = v;
        dict[DOTweenType.DOLocalRotate] = (c, v) => (c as Transform).localEulerAngles = v;

        return dict;
    }
}

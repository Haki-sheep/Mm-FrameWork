/// <summary>
/// UITemple Logic层 - 用户编写
/// </summary>

using MieMieFrameWork;
using MieMieFrameWork.UI;
using UnityEngine;
using UnityEngine.UI;

internal class UITemple : UIWindowBase
{
    internal UITempleGen View { get; private set; }

    internal protected override void OnAwake()
    {
        base.OnAwake();
        View = UIContent.GetComponent<UITempleGen>();
    }

    internal protected override void OnShow()
    {
        base.OnShow();
    }

    internal protected override void OnHide()
    {
        base.OnHide();
    }

    internal protected override void OnDestroy()
    {
        base.OnDestroy();
    }

}

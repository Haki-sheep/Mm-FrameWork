/// <summary>
/// UITemple View层 - 自动生成，请勿手动修改
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UITempleGen : MonoBehaviour
{
    /// <summary>
    /// M_ImageTextMeshProUGUI
    /// </summary>
    public TextMeshProUGUI M_ImageTextMeshProUGUI;
    /// <summary>
    /// M_ImageTransform
    /// </summary>
    public Transform M_ImageTransform;
    /// <summary>
    /// UIContentTransform
    /// </summary>
    public Transform UIContentTransform;

    private void Awake()
    {
        M_ImageTextMeshProUGUI = transform.Find("M_Image").GetComponent<TextMeshProUGUI>();
        M_ImageTransform = transform.Find("M_Image").GetComponent<Transform>();
        UIContentTransform = GetComponent<Transform>();
    }
}

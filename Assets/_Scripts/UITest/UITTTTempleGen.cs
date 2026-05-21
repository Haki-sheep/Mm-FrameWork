/// <summary>
/// UITTTTemple View层 - 自动生成，请勿手动修改
/// </summary>

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public partial class UITTTTempleGen : MonoBehaviour
{
    public Image ImgM_Image { get; private set; }
    public Button BtnM_Btn { get; private set; }

    private void Awake()
    {
        ImgM_Image = GetComponent<Image>();
        BtnM_Btn = GetComponent<Button>();
    }
}

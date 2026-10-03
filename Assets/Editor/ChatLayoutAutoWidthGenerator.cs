using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 生成纯 UGUI 聊天气泡自动宽度示例
/// </summary>
public static class ChatLayoutAutoWidthGenerator
{
    private const string DemoScenePath = "Assets/测试/ChatLayoutAutoWidthDemo.unity";
    private const string BubbleSpritePath = "Assets/MieMieFrameTools/Scripts/Frame/H_UIFramework/MmUIFrameWork/StandUIPrefabs/Arts/StandArt/Stand_RoundPanel.png";
    private const string AvatarSpritePath = "Assets/MieMieFrameTools/Scripts/Frame/H_UIFramework/MmUIFrameWork/StandUIPrefabs/Arts/StandArt/Stand_Circle.png";
    private const string FontAssetPath = "Assets/MieMieFrameTools/ARequired/Front/NotoSansSC SDF.asset";

    /// <summary>
    /// 生成聊天气泡自动宽度示例场景
    /// </summary>
    public static void GenerateDemo()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
        {
            return;
        }

        Scene DemoScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ChatLayoutAutoWidthGenerator.CreateDemoScene(DemoScene);
        EditorSceneManager.SaveScene(DemoScene, DemoScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("聊天自动宽度示例已生成");
    }

    /// <summary>
    /// 创建示例场景层级
    /// </summary>
    private static void CreateDemoScene(Scene DemoScene)
    {
        GameObject CanvasObject = ChatLayoutAutoWidthGenerator.CreateCanvas();
        GameObject BackgroundObject = ChatLayoutAutoWidthGenerator.CreateImageObject("Background", CanvasObject.transform, new Color(0.075f, 0.08f, 0.095f, 1f));
        ChatLayoutAutoWidthGenerator.SetFullStretch(BackgroundObject.GetComponent<RectTransform>());

        TMP_FontAsset FontAsset = ChatLayoutAutoWidthGenerator.LoadFontAsset();
        ChatLayoutAutoWidthGenerator.CreateHeader(CanvasObject.transform, FontAsset);
        ChatLayoutAutoWidthGenerator.CreateScrollView(CanvasObject.transform, FontAsset);
        ChatLayoutAutoWidthGenerator.CreateEventSystem();

        Canvas.ForceUpdateCanvases();
        EditorSceneManager.MarkSceneDirty(DemoScene);
    }

    /// <summary>
    /// 创建画布
    /// </summary>
    private static GameObject CreateCanvas()
    {
        GameObject CanvasObject = new GameObject("ChatLayoutAutoWidthDemo", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Canvas CanvasComponent = CanvasObject.GetComponent<Canvas>();
        CanvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasComponent.pixelPerfect = false;

        CanvasScaler CanvasScalerComponent = CanvasObject.GetComponent<CanvasScaler>();
        CanvasScalerComponent.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        CanvasScalerComponent.referenceResolution = new Vector2(1000f, 1000f);
        CanvasScalerComponent.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        CanvasScalerComponent.matchWidthOrHeight = 0.5f;
        return CanvasObject;
    }

    /// <summary>
    /// 创建顶部说明
    /// </summary>
    private static void CreateHeader(Transform CanvasTransform, TMP_FontAsset FontAsset)
    {
        GameObject HeaderObject = ChatLayoutAutoWidthGenerator.CreateTextObject("Header", CanvasTransform, FontAsset);
        RectTransform HeaderRectTransform = HeaderObject.GetComponent<RectTransform>();
        HeaderRectTransform.anchorMin = new Vector2(0f, 1f);
        HeaderRectTransform.anchorMax = new Vector2(1f, 1f);
        HeaderRectTransform.pivot = new Vector2(0.5f, 1f);
        HeaderRectTransform.anchoredPosition = new Vector2(0f, -22f);
        HeaderRectTransform.sizeDelta = new Vector2(-56f, 62f);

        TextMeshProUGUI HeaderText = HeaderObject.GetComponent<TextMeshProUGUI>();
        HeaderText.text = "纯 UGUI 聊天气泡自动宽度示例\n短文本变窄  超过最大宽度后换行并自动增高";
        HeaderText.fontSize = 25f;
        HeaderText.color = new Color(0.82f, 0.87f, 0.9f, 1f);
        HeaderText.alignment = TextAlignmentOptions.Center;
        HeaderText.lineSpacing = 8f;
        HeaderText.raycastTarget = false;
    }

    /// <summary>
    /// 创建滚动区域
    /// </summary>
    private static void CreateScrollView(Transform CanvasTransform, TMP_FontAsset FontAsset)
    {
        GameObject ScrollViewObject = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        ScrollViewObject.transform.SetParent(CanvasTransform, false);
        RectTransform ScrollViewRectTransform = ScrollViewObject.GetComponent<RectTransform>();
        ScrollViewRectTransform.anchorMin = Vector2.zero;
        ScrollViewRectTransform.anchorMax = Vector2.one;
        ScrollViewRectTransform.offsetMin = new Vector2(28f, 26f);
        ScrollViewRectTransform.offsetMax = new Vector2(-28f, -112f);

        Image ScrollBackground = ScrollViewObject.AddComponent<Image>();
        ScrollBackground.color = new Color(0.105f, 0.115f, 0.13f, 1f);
        ScrollBackground.raycastTarget = false;

        GameObject ViewportObject = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        ViewportObject.transform.SetParent(ScrollViewObject.transform, false);
        RectTransform ViewportRectTransform = ViewportObject.GetComponent<RectTransform>();
        ChatLayoutAutoWidthGenerator.SetFullStretch(ViewportRectTransform);

        GameObject ContentObject = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        ContentObject.transform.SetParent(ViewportObject.transform, false);
        RectTransform ContentRectTransform = ContentObject.GetComponent<RectTransform>();
        ContentRectTransform.anchorMin = new Vector2(0f, 1f);
        ContentRectTransform.anchorMax = new Vector2(1f, 1f);
        ContentRectTransform.pivot = new Vector2(0.5f, 1f);
        ContentRectTransform.anchoredPosition = Vector2.zero;
        ContentRectTransform.sizeDelta = Vector2.zero;

        VerticalLayoutGroup ContentLayoutGroup = ContentObject.GetComponent<VerticalLayoutGroup>();
        ContentLayoutGroup.padding = new RectOffset(22, 22, 22, 22);
        ContentLayoutGroup.spacing = 16f;
        ContentLayoutGroup.childAlignment = TextAnchor.UpperCenter;
        ContentLayoutGroup.childControlWidth = true;
        ContentLayoutGroup.childControlHeight = true;
        ContentLayoutGroup.childForceExpandWidth = true;
        ContentLayoutGroup.childForceExpandHeight = false;
        ContentLayoutGroup.childScaleWidth = false;
        ContentLayoutGroup.childScaleHeight = false;

        ContentSizeFitter ContentSizeFitterComponent = ContentObject.GetComponent<ContentSizeFitter>();
        ContentSizeFitterComponent.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        ContentSizeFitterComponent.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect ScrollRectComponent = ScrollViewObject.GetComponent<ScrollRect>();
        ScrollRectComponent.viewport = ViewportRectTransform;
        ScrollRectComponent.content = ContentRectTransform;
        ScrollRectComponent.horizontal = false;
        ScrollRectComponent.vertical = true;
        ScrollRectComponent.movementType = ScrollRect.MovementType.Clamped;
        ScrollRectComponent.inertia = true;

        string[] MessageTextList =
        {
            "短文本",
            "这是一条会随着内容变长而自动变宽的聊天消息",
            "当文本继续变长时气泡会逐渐接近最大宽度",
            "超过最大宽度以后气泡不再变宽  TMP 会根据当前宽度自动换行",
            "这条消息用于验证自动增高效果  文字换行以后气泡高度会随行数增加  外层消息列表也会重新排列",
            "一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十一二三四五六七八九十"
        };

        int MessageIndex = 0;
        foreach (string MessageText in MessageTextList)
        {
            MessageIndex++;
            ChatLayoutAutoWidthGenerator.CreateMessageRow(ContentObject.transform, FontAsset, MessageIndex, MessageText);
        }
    }

    /// <summary>
    /// 创建一条聊天消息
    /// </summary>
    private static void CreateMessageRow(Transform ContentTransform, TMP_FontAsset FontAsset, int MessageIndex, string MessageText)
    {
        GameObject MessageRowObject = new GameObject("MessageRow_" + MessageIndex, typeof(RectTransform), typeof(HorizontalLayoutGroup));
        MessageRowObject.transform.SetParent(ContentTransform, false);

        LayoutElement MessageRowLayoutElement = MessageRowObject.AddComponent<LayoutElement>();
        MessageRowLayoutElement.minHeight = 0f;
        MessageRowLayoutElement.preferredHeight = -1f;
        MessageRowLayoutElement.flexibleHeight = 0f;

        HorizontalLayoutGroup MessageRowLayoutGroup = MessageRowObject.GetComponent<HorizontalLayoutGroup>();
        MessageRowLayoutGroup.padding = new RectOffset(0, 0, 0, 0);
        MessageRowLayoutGroup.spacing = 14f;
        MessageRowLayoutGroup.childAlignment = TextAnchor.MiddleRight;
        MessageRowLayoutGroup.childControlWidth = true;
        MessageRowLayoutGroup.childControlHeight = true;
        MessageRowLayoutGroup.childForceExpandWidth = false;
        MessageRowLayoutGroup.childForceExpandHeight = false;
        MessageRowLayoutGroup.childScaleWidth = false;
        MessageRowLayoutGroup.childScaleHeight = false;

        GameObject BubbleLimitObject = new GameObject("BubbleLimit_640", typeof(RectTransform), typeof(HorizontalLayoutGroup), typeof(LayoutElement));
        BubbleLimitObject.transform.SetParent(MessageRowObject.transform, false);
        LayoutElement BubbleLimitLayoutElement = BubbleLimitObject.GetComponent<LayoutElement>();
        BubbleLimitLayoutElement.minWidth = 640f;
        BubbleLimitLayoutElement.preferredWidth = 640f;
        BubbleLimitLayoutElement.flexibleWidth = 0f;

        HorizontalLayoutGroup BubbleLimitLayoutGroup = BubbleLimitObject.GetComponent<HorizontalLayoutGroup>();
        BubbleLimitLayoutGroup.childAlignment = TextAnchor.MiddleRight;
        BubbleLimitLayoutGroup.childControlWidth = true;
        BubbleLimitLayoutGroup.childControlHeight = true;
        BubbleLimitLayoutGroup.childForceExpandWidth = false;
        BubbleLimitLayoutGroup.childForceExpandHeight = false;
        BubbleLimitLayoutGroup.childScaleWidth = false;
        BubbleLimitLayoutGroup.childScaleHeight = false;

        GameObject BubbleObject = new GameObject("Bubble", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup));
        BubbleObject.transform.SetParent(BubbleLimitObject.transform, false);
        Image BubbleImage = BubbleObject.GetComponent<Image>();
        BubbleImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChatLayoutAutoWidthGenerator.BubbleSpritePath);
        BubbleImage.type = Image.Type.Sliced;
        BubbleImage.color = new Color(0.08f, 0.78f, 0.48f, 1f);
        BubbleImage.raycastTarget = false;

        VerticalLayoutGroup BubbleLayoutGroup = BubbleObject.GetComponent<VerticalLayoutGroup>();
        BubbleLayoutGroup.padding = new RectOffset(24, 24, 16, 16);
        BubbleLayoutGroup.spacing = 0f;
        BubbleLayoutGroup.childAlignment = TextAnchor.MiddleCenter;
        BubbleLayoutGroup.childControlWidth = true;
        BubbleLayoutGroup.childControlHeight = true;
        BubbleLayoutGroup.childForceExpandWidth = false;
        BubbleLayoutGroup.childForceExpandHeight = false;
        BubbleLayoutGroup.childScaleWidth = false;
        BubbleLayoutGroup.childScaleHeight = false;

        GameObject TextObject = ChatLayoutAutoWidthGenerator.CreateTextObject("TMP_Text", BubbleObject.transform, FontAsset);
        TextMeshProUGUI MessageTextComponent = TextObject.GetComponent<TextMeshProUGUI>();
        MessageTextComponent.text = MessageText;
        MessageTextComponent.fontSize = 28f;
        MessageTextComponent.color = new Color(0.035f, 0.06f, 0.055f, 1f);
        MessageTextComponent.alignment = TextAlignmentOptions.MidlineRight;
        MessageTextComponent.textWrappingMode = TextWrappingModes.Normal;
        MessageTextComponent.overflowMode = TextOverflowModes.Overflow;
        MessageTextComponent.enableAutoSizing = false;
        MessageTextComponent.raycastTarget = false;

        GameObject AvatarObject = ChatLayoutAutoWidthGenerator.CreateImageObject("Avatar", MessageRowObject.transform, new Color(0.58f, 0.62f, 0.65f, 1f));
        LayoutElement AvatarLayoutElement = AvatarObject.AddComponent<LayoutElement>();
        AvatarLayoutElement.minWidth = 72f;
        AvatarLayoutElement.preferredWidth = 72f;
        AvatarLayoutElement.minHeight = 72f;
        AvatarLayoutElement.preferredHeight = 72f;
        AvatarLayoutElement.flexibleWidth = 0f;
        AvatarLayoutElement.flexibleHeight = 0f;

        Image AvatarImage = AvatarObject.GetComponent<Image>();
        AvatarImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ChatLayoutAutoWidthGenerator.AvatarSpritePath);
        AvatarImage.preserveAspect = true;
        AvatarImage.raycastTarget = false;

        GameObject AvatarTextObject = ChatLayoutAutoWidthGenerator.CreateTextObject("AvatarMark", AvatarObject.transform, FontAsset);
        RectTransform AvatarTextRectTransform = AvatarTextObject.GetComponent<RectTransform>();
        ChatLayoutAutoWidthGenerator.SetFullStretch(AvatarTextRectTransform);
        TextMeshProUGUI AvatarText = AvatarTextObject.GetComponent<TextMeshProUGUI>();
        AvatarText.text = "猫";
        AvatarText.fontSize = 25f;
        AvatarText.color = new Color(0.1f, 0.12f, 0.13f, 1f);
        AvatarText.alignment = TextAlignmentOptions.Center;
        AvatarText.raycastTarget = false;
    }

    /// <summary>
    /// 创建图片物体
    /// </summary>
    private static GameObject CreateImageObject(string ObjectName, Transform ParentTransform, Color Color)
    {
        GameObject ImageObject = new GameObject(ObjectName, typeof(RectTransform), typeof(Image));
        ImageObject.transform.SetParent(ParentTransform, false);
        Image ImageComponent = ImageObject.GetComponent<Image>();
        ImageComponent.color = Color;
        return ImageObject;
    }

    /// <summary>
    /// 创建 TMP 文字物体
    /// </summary>
    private static GameObject CreateTextObject(string ObjectName, Transform ParentTransform, TMP_FontAsset FontAsset)
    {
        GameObject TextObject = new GameObject(ObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        TextObject.transform.SetParent(ParentTransform, false);
        TextMeshProUGUI TextComponent = TextObject.GetComponent<TextMeshProUGUI>();
        TextComponent.font = FontAsset;
        TextComponent.textWrappingMode = TextWrappingModes.Normal;
        TextComponent.overflowMode = TextOverflowModes.Overflow;
        TextComponent.margin = Vector4.zero;
        return TextObject;
    }

    /// <summary>
    /// 设置全拉伸锚点
    /// </summary>
    private static void SetFullStretch(RectTransform RectTransform)
    {
        RectTransform.anchorMin = Vector2.zero;
        RectTransform.anchorMax = Vector2.one;
        RectTransform.offsetMin = Vector2.zero;
        RectTransform.offsetMax = Vector2.zero;
        RectTransform.localScale = Vector3.one;
    }

    /// <summary>
    /// 加载中文字体
    /// </summary>
    private static TMP_FontAsset LoadFontAsset()
    {
        TMP_FontAsset FontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(ChatLayoutAutoWidthGenerator.FontAssetPath);
        if (FontAsset != null)
        {
            return FontAsset;
        }

        return TMP_Settings.defaultFontAsset;
    }

    /// <summary>
    /// 创建事件系统
    /// </summary>
    private static void CreateEventSystem()
    {
        GameObject EventSystemObject = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        EventSystemObject.transform.position = Vector3.zero;
    }
}

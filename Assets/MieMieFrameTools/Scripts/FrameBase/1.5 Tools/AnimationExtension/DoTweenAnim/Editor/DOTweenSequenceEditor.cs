#if UNITY_EDITOR
using CWTools.Extensions;
using DG.DOTweenEditor;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

[CanEditMultipleObjects]
[CustomEditor(typeof(DOTweenSequence))]
public class DOTweenSequenceEditor : UnityEditor.Editor
{
    private SerializedProperty m_Sequence;
    private ReorderableList m_SequenceList;
    private GUIContent m_PlayBtnContent;
    private GUIContent m_RewindBtnContent;
    private GUIContent m_ResetBtnContent;
    private GUILayoutOption m_btnHeight;

    private void OnEnable()
    {
        m_PlayBtnContent = EditorGUIUtility.TrIconContent("d_PlayButton@2x", "Play");
        m_RewindBtnContent = EditorGUIUtility.TrIconContent("d_preAudioAutoPlayOff@2x", "Rewind");
        m_ResetBtnContent = EditorGUIUtility.TrIconContent("d_preAudioLoopOff@2x", "Reset");
        m_btnHeight = GUILayout.Height(35);
        m_Sequence = serializedObject.FindProperty("m_Sequence");
        m_SequenceList = new ReorderableList(serializedObject, m_Sequence);
        m_SequenceList.drawElementCallback = OnDrawSequenceItem;
        m_SequenceList.elementHeightCallback = index =>
        {
            var item = m_Sequence.GetArrayElementAtIndex(index);
            return EditorGUI.GetPropertyHeight(item);
        };
        m_SequenceList.drawHeaderCallback = OnDrawSequenceHeader;
    }

    public override void OnInspectorGUI()
    {
        if (!EditorApplication.isPlaying)
        {
            EditorGUILayout.BeginHorizontal();
            {
                if (GUILayout.Button(m_PlayBtnContent, m_btnHeight))
                {
                    if (DOTweenEditorPreview.isPreviewing)
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DOTweenSequence).DOKill();
                    }
                    DOTweenEditorPreview.PrepareTweenForPreview((target as DOTweenSequence).DOPlay(), true, true, false);
                    DOTweenEditorPreview.Start(null);
                }
                if (GUILayout.Button(m_RewindBtnContent, m_btnHeight))
                { 
                    if (DOTweenEditorPreview.isPreviewing)
                    {
                        DOTweenEditorPreview.Stop(true, true);
                        (target as DOTweenSequence).DOKill();
                    }
                    (target as DOTweenSequence).DORewind();
                    DOTweenEditorPreview.PrepareTweenForPreview((target as DOTweenSequence).DOPlay(), true, true, false);
                    DOTweenEditorPreview.Start(null);
                }
                if (GUILayout.Button(m_ResetBtnContent, m_btnHeight))
                { 
                    DOTweenEditorPreview.Stop(true, true);
                    (target as DOTweenSequence).DOKill();
                }
                GUILayout.FlexibleSpace();
                EditorGUILayout.EndHorizontal();
            }
        }

        serializedObject.Update();
        m_SequenceList.DoLayoutList();
        serializedObject.ApplyModifiedProperties();
        DrawDefaultInspector();
    }

    private void OnDrawSequenceHeader(Rect rect)
    {
        EditorGUI.LabelField(rect, "动画序列");
    }

    private void OnDrawSequenceItem(Rect rect, int index, bool isActive, bool isFocused)
    {
        SerializedProperty element = m_Sequence.GetArrayElementAtIndex(index);
        EditorGUI.PropertyField(rect, element, true);
    } 
}

[CustomPropertyDrawer(typeof(SequenceAnimation))]
public class SequenceTweenMoveDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var onPlay = property.FindPropertyRelative("OnPlay");
        var onUpdate = property.FindPropertyRelative("OnUpdate");
        var onComplete = property.FindPropertyRelative("OnComplete");
        var baseHeight = EditorGUIUtility.singleLineHeight * 11;
        return baseHeight + (property.isExpanded ? (EditorGUI.GetPropertyHeight(onPlay) + EditorGUI.GetPropertyHeight(onUpdate) + EditorGUI.GetPropertyHeight(onComplete)) : 0);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);
        EditorGUI.indentLevel++;

        var addType = property.FindPropertyRelative("AddType");
        var target = property.FindPropertyRelative("Target");
        var tweenType = property.FindPropertyRelative("AnimationType");
        var toValue = property.FindPropertyRelative("ToValue");
        var useToTarget = property.FindPropertyRelative("UseToTarget");
        var toTarget = property.FindPropertyRelative("ToTarget");
        var useFromValue = property.FindPropertyRelative("UseFromValue");
        var fromValue = property.FindPropertyRelative("FromValue");
        var duration = property.FindPropertyRelative("DurationOrSpeed");
        var speedBased = property.FindPropertyRelative("SpeedBased");
        var delay = property.FindPropertyRelative("Delay");
        var customEase = property.FindPropertyRelative("CustomEase");
        var ease = property.FindPropertyRelative("Ease");
        var easeCurve = property.FindPropertyRelative("EaseCurve");
        var loops = property.FindPropertyRelative("Loops");
        var loopType = property.FindPropertyRelative("LoopType");
        var updateType = property.FindPropertyRelative("UpdateType");
        var snapping = property.FindPropertyRelative("Snapping");
        var onPlay = property.FindPropertyRelative("OnPlay");
        var onUpdate = property.FindPropertyRelative("OnUpdate");
        var onComplete = property.FindPropertyRelative("OnComplete");

        var lastRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);

        EditorGUI.PropertyField(lastRect, addType);

        EditorGUI.BeginChangeCheck();
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, target);
        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, tweenType);

        if (EditorGUI.EndChangeCheck())
        {
            var fixedComType = GetFixedComponentType(target.objectReferenceValue as UnityEngine.Component, (DOTweenType)tweenType.enumValueIndex);
            if (fixedComType != null)
            {
                target.objectReferenceValue = fixedComType;
            }
        }

        if (target.objectReferenceValue != null && null == GetFixedComponentType(target.objectReferenceValue as UnityEngine.Component, (DOTweenType)tweenType.enumValueIndex))
        {
            lastRect.y += EditorGUIUtility.singleLineHeight;
            var typeName = tweenType.enumValueIndex < tweenType.enumDisplayNames.Length ? tweenType.enumDisplayNames[tweenType.enumValueIndex] : tweenType.enumValueIndex.ToString();
            EditorGUI.HelpBox(lastRect, $"{target.objectReferenceValue.GetType().Name} 不支持 {typeName}", MessageType.Error);
        }

        const float itemWidth = 110;
        const float setBtnWidth = 30;

        lastRect.y += EditorGUIUtility.singleLineHeight;
        var horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, delay);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        snapping.boolValue = EditorGUI.ToggleLeft(horizontalRect, "整数对齐", snapping.boolValue);

        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;

        lastRect.y += EditorGUIUtility.singleLineHeight;
        var toRect = lastRect;
        toRect.width -= setBtnWidth + itemWidth;

        var dotweenTp = (DOTweenType)tweenType.enumValueIndex;
        switch (dotweenTp)
        {
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOFade:
            case DOTweenType.DOCanvasGroupFade:
            case DOTweenType.DOFillAmount:
            case DOTweenType.DOValue:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                {
        EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
        var value = fromValue.vector4Value;
        value.x = EditorGUI.FloatField(horizontalRect, "起始值", value.x);
        fromValue.vector4Value = value;
        EditorGUI.EndDisabledGroup();

        if (!useToTarget.boolValue)
        {
            value = toValue.vector4Value;
            value.x = EditorGUI.FloatField(toRect, "目标值", value.x);
            toValue.vector4Value = value;
        }
                }
                break;
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
            case DOTweenType.DOSizeDelta:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector2Field(horizontalRect, "起始值", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector2Field(toRect, "目标值", toValue.vector4Value);
                }
                break;
            case DOTweenType.DOMove:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOScale:
            case DOTweenType.DORotate:
            case DOTweenType.DOLocalRotate:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.Vector3Field(horizontalRect, "起始值", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.Vector3Field(toRect, "目标值", toValue.vector4Value);
                }
                break;
            case DOTweenType.DOColor:
                {
                    EditorGUI.BeginDisabledGroup(!useFromValue.boolValue);
                    fromValue.vector4Value = EditorGUI.ColorField(horizontalRect, "起始颜色", fromValue.vector4Value);
                    EditorGUI.EndDisabledGroup();
                    if (!useToTarget.boolValue)
                        toValue.vector4Value = EditorGUI.ColorField(toRect, "目标颜色", toValue.vector4Value);
                }
                break;
        }

        if (useToTarget.boolValue)
        {
            toTarget.objectReferenceValue = EditorGUI.ObjectField(toRect, "To", toTarget.objectReferenceValue, target.objectReferenceValue != null ? target.objectReferenceValue.GetType() : typeof(UnityEngine.Component), true);

            if (toTarget.objectReferenceValue == null)
            {
                lastRect.y += EditorGUIUtility.singleLineHeight;
                EditorGUI.HelpBox(lastRect, "目标对象不能为空！", MessageType.Error);
            }
        }

        horizontalRect.x += horizontalRect.width;
        horizontalRect.width = setBtnWidth;
        if (useFromValue.boolValue && GUI.Button(horizontalRect, "获取"))
        {
            SetValueFromTarget(dotweenTp, target, fromValue);
        }
        horizontalRect.x += setBtnWidth;
        horizontalRect.width = itemWidth;
        useFromValue.boolValue = EditorGUI.ToggleLeft(horizontalRect, "启用起始值", useFromValue.boolValue);

        toRect.x += toRect.width;
        toRect.width = setBtnWidth;
        if (!useToTarget.boolValue && GUI.Button(toRect, "获取"))
        {
            SetValueFromTarget(dotweenTp, target, toValue);
        }
        toRect.x += setBtnWidth;
        toRect.width = itemWidth;
        useToTarget.boolValue = EditorGUI.ToggleLeft(toRect, "使用目标", useToTarget.boolValue);

        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, duration);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        speedBased.boolValue = EditorGUI.ToggleLeft(horizontalRect, "速度模式", speedBased.boolValue);

        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        if (customEase.boolValue)
            EditorGUI.PropertyField(horizontalRect, easeCurve);
        else
            EditorGUI.PropertyField(horizontalRect, ease);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        customEase.boolValue = EditorGUI.ToggleLeft(horizontalRect, "自定义曲线", customEase.boolValue);

        lastRect.y += EditorGUIUtility.singleLineHeight;
        horizontalRect = lastRect;
        horizontalRect.width -= setBtnWidth + itemWidth;
        EditorGUI.PropertyField(horizontalRect, loops);
        horizontalRect.x += setBtnWidth + horizontalRect.width;
        horizontalRect.width = itemWidth;
        EditorGUI.BeginDisabledGroup(loops.intValue == 1);
        loopType.enumValueIndex = (int)(DG.Tweening.LoopType)EditorGUI.EnumPopup(horizontalRect, (DG.Tweening.LoopType)loopType.enumValueIndex);
        EditorGUI.EndDisabledGroup();

        lastRect.y += EditorGUIUtility.singleLineHeight;
        EditorGUI.PropertyField(lastRect, updateType);

        lastRect.y += EditorGUIUtility.singleLineHeight;
        property.isExpanded = EditorGUI.Foldout(lastRect, property.isExpanded, "回调事件");
        if (property.isExpanded)
        {
            lastRect.y += EditorGUIUtility.singleLineHeight;
            EditorGUI.PropertyField(lastRect, onPlay);

            lastRect.y += EditorGUI.GetPropertyHeight(onPlay);
            EditorGUI.PropertyField(lastRect, onUpdate);

            lastRect.y += EditorGUI.GetPropertyHeight(onUpdate);
            EditorGUI.PropertyField(lastRect, onComplete);
        }

        EditorGUI.indentLevel--;
        EditorGUI.EndProperty();
    }

    private void SetValueFromTarget(DOTweenType tweenType, SerializedProperty target, SerializedProperty value)
    {
        if (target.objectReferenceValue == null) return;
        var targetCom = target.objectReferenceValue;
        switch (tweenType)
        {
            case DOTweenType.DOMove:
                value.vector4Value = (targetCom as UnityEngine.Transform).position;
                break;
            case DOTweenType.DOMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).position.x;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).position.y;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).position.z;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOLocalMove:
                value.vector4Value = (targetCom as UnityEngine.Transform).localPosition;
                break;
            case DOTweenType.DOLocalMoveX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).localPosition.x;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOLocalMoveY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).localPosition.y;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOLocalMoveZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).localPosition.z;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOAnchorPos:
                value.vector4Value = (targetCom as UnityEngine.RectTransform).anchoredPosition;
                break;
            case DOTweenType.DOAnchorPosX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.RectTransform).anchoredPosition.x;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOAnchorPosY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.RectTransform).anchoredPosition.y;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOAnchorPosZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.RectTransform).anchoredPosition3D.z;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOAnchorPos3D:
                value.vector4Value = (targetCom as UnityEngine.RectTransform).anchoredPosition3D;
                break;
            case DOTweenType.DOColor:
                value.vector4Value = (targetCom as UnityEngine.UI.Graphic).color;
                break;
            case DOTweenType.DOFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Graphic).color.a;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOCanvasGroupFade:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.CanvasGroup).alpha;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOValue:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Slider).value;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOSizeDelta:
                value.vector4Value = (targetCom as UnityEngine.RectTransform).sizeDelta;
                break;
            case DOTweenType.DOFillAmount:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.UI.Image).fillAmount;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOFlexibleSize:
                value.vector4Value = (targetCom as UnityEngine.UI.LayoutElement).GetFlexibleSize();
                break;
            case DOTweenType.DOMinSize:
                value.vector4Value = (targetCom as UnityEngine.UI.LayoutElement).GetMinSize();
                break;
            case DOTweenType.DOPreferredSize:
                value.vector4Value = (targetCom as UnityEngine.UI.LayoutElement).GetPreferredSize();
                break;
            case DOTweenType.DOScale:
                value.vector4Value = (targetCom as UnityEngine.Transform).localScale;
                break;
            case DOTweenType.DOScaleX:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).localScale.x;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOScaleY:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).localScale.y;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DOScaleZ:
                {
                    var tmpValue = value.vector4Value;
                    tmpValue.x = (targetCom as UnityEngine.Transform).localScale.z;
                    value.vector4Value = tmpValue;
                }
                break;
            case DOTweenType.DORotate:
                value.vector4Value = (targetCom as UnityEngine.Transform).eulerAngles;
                break;
            case DOTweenType.DOLocalRotate:
                value.vector4Value = (targetCom as UnityEngine.Transform).localEulerAngles;
                break;
        }
    }

    private static UnityEngine.Component GetFixedComponentType(UnityEngine.Component com, DOTweenType tweenType)
    {
        if (com == null) return null;
        switch (tweenType)
        {
            case DOTweenType.DOMove:
            case DOTweenType.DOMoveX:
            case DOTweenType.DOMoveY:
            case DOTweenType.DOMoveZ:
            case DOTweenType.DOLocalMove:
            case DOTweenType.DOLocalMoveX:
            case DOTweenType.DOLocalMoveY:
            case DOTweenType.DOLocalMoveZ:
            case DOTweenType.DOScale:
            case DOTweenType.DOScaleX:
            case DOTweenType.DOScaleY:
            case DOTweenType.DOScaleZ:
                return com.gameObject.GetComponent<UnityEngine.Transform>();
            case DOTweenType.DOAnchorPos:
            case DOTweenType.DOAnchorPosX:
            case DOTweenType.DOAnchorPosY:
            case DOTweenType.DOAnchorPosZ:
            case DOTweenType.DOAnchorPos3D:
            case DOTweenType.DOSizeDelta:
                return com.gameObject.GetComponent<UnityEngine.RectTransform>();
            case DOTweenType.DOColor:
            case DOTweenType.DOFade:
                return com.gameObject.GetComponent<UnityEngine.UI.Graphic>();
            case DOTweenType.DOCanvasGroupFade:
                return com.gameObject.GetComponent<UnityEngine.CanvasGroup>();
            case DOTweenType.DOFillAmount:
                return com.gameObject.GetComponent<UnityEngine.UI.Image>();
            case DOTweenType.DOFlexibleSize:
            case DOTweenType.DOMinSize:
            case DOTweenType.DOPreferredSize:
                return com.gameObject.GetComponent<UnityEngine.UI.LayoutElement>();
            case DOTweenType.DOValue:
                return com.gameObject.GetComponent<UnityEngine.UI.Slider>();
        }
        return null;
    }
}
#endif

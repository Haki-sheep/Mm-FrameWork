using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;
using System.Text;

namespace MieMieFrameWork.UI
{
    public class UIWindowBase: UIDataBase
    {

        #region 遮罩管理
        public void SetMask(bool isShow,float minAlpha=0,float maxAlpha=0.5f){
            if(this.UIMask==null) {Debug.Log("没有找到遮罩");return;};
            
            if(isShow)//显示遮罩
            {
                this.UIMask.raycastTarget =true;
                this.UIMask.maskable =true;
                this.UIMask.color = new Color(0,0,0,maxAlpha);
                //动态调整位置
                this.UIMask.transform.SetAsLastSibling();
            }
            else
            {
                this.UIMask.raycastTarget =false;
                this.UIMask.maskable =false;
                this.UIMask.color = new Color(0,0,0,minAlpha);
                //动态调整位置
                this.UIMask.transform.SetAsFirstSibling();
            }
        }
        #endregion


        #region 绑定UI 实现生命周期
        
        public override void BindGameObject(GameObject uiPrefab,Camera uiCamera)
        {
            this.UIGameObject = uiPrefab;

            this.UIContent = UIGameObject.transform.Find("UIContent");

            this.UIMask = UIGameObject.transform.Find("UIMask").GetComponent<Image>();

            this.UICanvas = UIGameObject.GetComponent<Canvas>();
            
            this.UICanvasGroup = UIGameObject.GetComponent<CanvasGroup>();

            UICanvas.worldCamera = uiCamera;
        }

        internal protected override void OnAwake() 
        {
            // GetAllUIBehaviour();
        }

        internal protected override void OnShow()
        {
            if(ApplyAniamtion)
                GlobalAnimationShow();
            else{
                this.UICanvasGroup.alpha = 1;
            }
            this.UICanvasGroup.blocksRaycasts = true;
            this.UICanvasGroup.interactable = true;
        }
        
        internal protected override void OnHide()
        {
             if(ApplyAniamtion) 
                GlobalAnimationHide();
            else{
                //不同动画效果则直接将透明度设置为0
                this.UICanvasGroup.alpha = 0;
            }
            this.UICanvasGroup.blocksRaycasts = false;
            this.UICanvasGroup.interactable = false;

        }

        internal protected override void OnDestroy()
        {
            //清除所有事件 清空列表
            // uiDic.Clear();
            GameObject.Destroy(this.UIGameObject);
        }

        #endregion


        #region 事件管理

        // protected readonly Dictionary<string,UIBehaviour> uiDic = new ();

        // /// <summary>
        // /// 获取UI组件
        // /// </summary>
        // protected T GetUIComp<T>(string uiName) where T : UIBehaviour
        // {
        //     if(uiDic.TryGetValue(uiName, out UIBehaviour comp) && comp is T typedComp)
        //     {
        //         return typedComp;
        //     }
        //     return default;
        // }


        // /// <summary>
        // /// 获取所有过滤后的UI组件并存储到字典中
        // /// </summary>
        // protected void GetAllUIBehaviour()
        // {
        //     if (UIContent == null) {Debug.LogError("没有找到UIContent");return;};
            
        //     uiDic.Clear();
            
        //     // 获取所有UIBehaviour组件
        //     UIBehaviour[] allComponents = UIContent.GetComponentsInChildren<UIBehaviour>(true);
            
        //     foreach (UIBehaviour comp in allComponents)
        //     {
        //         string compName = comp.gameObject.name;
                
        //         // 只收集带前缀或交互组件
        //         if (!ShouldCollectComponent(compName, comp))
        //         {
        //             continue;
        //         }
                
        //         // 如果对象名已存在，则跳过
        //         if (!uiDic.ContainsKey(compName))
        //         {
        //             uiDic.Add(compName, comp);
        //         }
        //     }
        // }

        // /// <summary>
        // /// 检查组件是否应该被收集（带前缀或是交互组件）
        // /// </summary>
        // protected virtual bool ShouldCollectComponent(string componentName, Component component = null)
        // {
        //     var frameSetting = MieMieFrameWork.ModuleHub.Instance?.FrameSetting;
        //     if (frameSetting == null) return false;

        //     // 检查是否以任何配置的前缀开头
        //     foreach (var pair in frameSetting.PrefixToComponentTypeMap)
        //     {
        //         if (componentName.StartsWith(pair.Prefix))
        //             return true;
        //     }

        //     // 如果开启了自动收集交互组件且提供了组件引用，检查是否是交互组件
        //     if (component != null && frameSetting.AutoCollectInteractiveComponents)
        //     {
        //         return component is Button || 
        //                component is Toggle || 
        //                component is InputField || 
        //                component is TMP_InputField ||
        //                component is Dropdown ||
        //                component is TMP_Dropdown;
        //     }

        //     return false;
        // }

        #endregion
    }
}


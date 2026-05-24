namespace MieMieFrameWork.UI
{
    using UnityEngine;
    using DG.Tweening;
    using UnityEngine.UI;

    /// <summary>
    /// 所有UI的基类
    /// </summary>
    public abstract class UIDataBase{
        
        #region 属性
        public GameObject UIGameObject { get; protected set; }//绑定
        public bool UIIsShow=>UICanvasGroup.alpha > 0;//查询当前UI是否显示
        public Canvas UICanvas{get; protected set;}//用于设置基础信息 和 绑定相机
        public Transform UIContent{get; protected set;}//用于存放具体的UI内容
        public CanvasGroup UICanvasGroup{get; protected set;}//用于显示和隐藏
        public Image UIMask{get;protected set;}//遮罩
        public bool ApplyAniamtion{get;set;} = false;//是否启用动画
        #endregion


        #region 生命周期
        public abstract void BindGameObject(GameObject uiPrefab,Camera uiCamera);
        internal protected abstract void OnAwake();
        internal protected abstract void OnShow();
        internal protected abstract void OnHide();
        internal protected abstract void OnDestroy();
        #endregion

        #region 全局动画效果
        protected virtual void GlobalAnimationShow()
        {
            this.UIContent.localScale = Vector3.one * 0.8f;
            this.UIContent.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack).OnComplete(()=>{
                this.UICanvasGroup.DOFade(1, 0.15f);
            });
        }

        protected virtual void GlobalAnimationHide()
        {
            this.UIContent.DOScale(Vector3.one * 0.8f, 0.2f).SetEase(Ease.InBack).OnComplete(()=>{
                this.UICanvasGroup.DOFade(0, 0.15f);
            });
        }
        #endregion

    } 
}
   



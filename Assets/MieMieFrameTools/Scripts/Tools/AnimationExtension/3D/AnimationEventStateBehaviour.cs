

namespace MieMieFrameWork.MMAnimation
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;
    [Serializable]
    public class AnimationEventInfo
    {
        public string eventName;
        public bool triggerOnce;
        [Range(0f, 1f)] public float triggerTime;

        public E_AniamtionParamType paramType = E_AniamtionParamType.None;
        public int intValue;
        public float floatValue;
        public string stringValue;
        public UnityEngine.Object objectValue;
        public bool isTrigger = false;

        /// <summary>
        /// 是否等待 Animator 过渡结束后再触发 适合音效特效
        /// </summary>
        public bool waitTransitionEnd = true;
    }

    public class AnimationEventStateBehaviour : StateMachineBehaviour
    {
        [SerializeField] private List<AnimationEventInfo> animationEventInfoList = new();
        private AnimationReceiver reciver;
        private float animationStartTime; 
        private float previewFrameTime;
        private bool isFirstFrame = true; 

        public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            animationStartTime = stateInfo.normalizedTime; // 记录进入时的归一化时间
            previewFrameTime = animationStartTime;
            isFirstFrame = true; // 重置第一帧标记
            reciver ??= animator.GetComponent<AnimationReceiver>();

            // 重置所有事件的触发状态
            foreach (var item in animationEventInfoList)
            {
                item.isTrigger = false;
            }
        }

        public override void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            float currentTime = stateInfo.normalizedTime;

            // 首帧将预览时间锚定在进入点 避免单帧跨度过大时漏掉早期触发点
            if (isFirstFrame)
            {
                isFirstFrame = false;
                previewFrameTime = animationStartTime;
            }

            // 计算相对于动画开始时间的归一化时间
            float normalizedCurrentTime = (currentTime - animationStartTime) % 1f;
            if (normalizedCurrentTime < 0) normalizedCurrentTime += 1f;

            float normalizedPreviewTime = (previewFrameTime - animationStartTime) % 1f;
            if (normalizedPreviewTime < 0) normalizedPreviewTime += 1f;

            bool inTransition = animator.IsInTransition(layerIndex);

            foreach (var item in animationEventInfoList)
            {
                //是否已经循环 - 使用相对时间
                bool looped = normalizedCurrentTime < normalizedPreviewTime;

                //如果是循环触发模式且动画循环了，重置触发标记
                if (looped && !item.triggerOnce)
                {
                    item.isTrigger = false;
                }

                if (item.isTrigger)
                    continue;

                // 等待过渡结束模式 融合期间不触发 过渡结束后补发
                if (item.waitTransitionEnd)
                {
                    if (inTransition)
                        continue;

                    if (normalizedCurrentTime >= item.triggerTime)
                    {
                        item.isTrigger = true;
                        TriggerEvent(item, normalizedCurrentTime, normalizedPreviewTime);
                    }

                    continue;
                }

                //触发点检测 - 使用相对时间
                bool onTriggerPoint = normalizedPreviewTime <= item.triggerTime && normalizedCurrentTime >= item.triggerTime;

                // 检测触发点
                if (onTriggerPoint)
                {
                    item.isTrigger = true;
                    TriggerEvent(item, normalizedCurrentTime, normalizedPreviewTime);
                }
            }
            previewFrameTime = currentTime;
        }

        /// <summary>
        /// 触发事件并输出调试信息
        /// </summary>
        private void TriggerEvent(AnimationEventInfo item, float normalizedCurrentTime, float normalizedPreviewTime)
        {
            switch (item.paramType)
            {
                case E_AniamtionParamType.None:
                    reciver.OnAnimationEventTriggered(item.eventName);
                    break;
                case E_AniamtionParamType.Int:
                    reciver.OnIntAnimationEventTriggered(item.eventName, item.intValue);
                    break;
                case E_AniamtionParamType.Float:
                    reciver.OnFloatAnimationEventTriggered(item.eventName, item.floatValue);
                    break;
                case E_AniamtionParamType.String:
                    reciver.OnStringAnimationEventTriggered(item.eventName, item.stringValue);
                    break;
                case E_AniamtionParamType.Object:
                    reciver.OnObjectAnimationEventTriggered(item.eventName, item.objectValue);
                    break;
            }

            Debug.Log($"AnimationEvent:{item.eventName} + " +
                 $"TriggerNomalizaTime:{item.triggerTime} + " +
                 $"CurrentRelativeTime:{normalizedCurrentTime} + " +
                 $"Offset:{normalizedCurrentTime - normalizedPreviewTime} + " +
                 $"WaitTransitionEnd:{item.waitTransitionEnd}");
        }
    }
}

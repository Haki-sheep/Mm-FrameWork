

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
    }

    public class AnimationEventStateBehaviour : StateMachineBehaviour
    {
        [SerializeField] private List<AnimationEventInfo> animationEventInfoList = new();
        private AnimationReceiver reciver;
        private float animationStartTime; // 新增：记录动画开始时间
        private float previewFrameTime;
        private bool isFirstFrame = true; // 新增：标记是否第一帧

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

            // 计算相对于动画开始时间的归一化时间
            float normalizedCurrentTime = (currentTime - animationStartTime) % 1f;
            if (normalizedCurrentTime < 0) normalizedCurrentTime += 1f;

            float normalizedPreviewTime = (previewFrameTime - animationStartTime) % 1f;
            if (normalizedPreviewTime < 0) normalizedPreviewTime += 1f;

            // 第一帧跳过，避免立即触发
            if (isFirstFrame)
            {
                isFirstFrame = false;
                previewFrameTime = currentTime;
                return;
            }

            foreach (var item in animationEventInfoList)
            {
                //触发点检测 - 使用相对时间
                bool onTriggerPoint = normalizedPreviewTime <= item.triggerTime && normalizedCurrentTime >= item.triggerTime;
                //是否已经循环 - 使用相对时间
                bool looped = normalizedCurrentTime < normalizedPreviewTime;

                //如果是循环触发模式且动画循环了，重置触发标记
                if (looped && !item.triggerOnce)
                {
                    item.isTrigger = false;
                }

                // 检测触发点
                if (onTriggerPoint && !item.isTrigger)
                {
                    item.isTrigger = true;
                    TriggerEvent(item);

                    Debug.Log($"AnimationEvent:{item.eventName} + " +
                         $"TriggerNomalizaTime:{item.triggerTime} + " +
                         $"CurrentRelativeTime:{normalizedCurrentTime} + " +
                         $"Offset:{normalizedCurrentTime - normalizedPreviewTime}");
                }
            }
            previewFrameTime = currentTime;
        }

        // TriggerEvent 方法保持不变
        private void TriggerEvent(AnimationEventInfo item)
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
        }
    }
}

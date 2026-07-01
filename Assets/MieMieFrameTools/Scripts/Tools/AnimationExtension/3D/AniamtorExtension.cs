
namespace MieMieFrameWork.MMAnimation
{
    using UnityEngine;
    using System.Collections.Generic;


    /// <summary>
    /// 用来存放动画的string 转 hash值
    /// </summary>
    public static class AniamtorEncapsulation
    {
        private static readonly Dictionary<string, int> animationHashDict = new();

        public static int GetHashFromDict(this Animator animator, string animationName)
        {
            if (animationHashDict.TryGetValue(animationName, out int hash))
            {
                return hash;
            }
            else
            {
                hash = Animator.StringToHash(animationName);
                animationHashDict[animationName] = hash;
                return hash;
            }
        }
        public static void ClearHashDict(this Animator animator)
        {
            animationHashDict.Clear();
        }


        /// <summary>
        /// 对比Tag
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="tagName"></param>
        /// <param name="layerIndex"></param>
        /// <returns></returns>
        public static bool IsAnimationAtTag(this Animator animator, string tagName, int layerIndex = 0)
        {
            return animator.GetCurrentAnimatorStateInfo(layerIndex).IsTag(tagName);
        }


        #region Current
        /// <summary>
        /// 获取当前动画的信息 并返回其长度
        /// </summary>
        public static AnimatorStateInfo? GetAmCurrentStateInfo(this Animator animator, string name, out float amLength, int layerIndex = 0)
        {
            return GetAmNextStateInfo(animator, GetHashFromDict(animator, name), out amLength, layerIndex);
        }

        public static AnimatorStateInfo? GetAmCurrentStateInfo(this Animator animator, int hash, out float amLength, int layerIndex = 0)
        {
            var currentInfo = animator.GetCurrentAnimatorStateInfo(0);
            amLength = currentInfo.length;
            if (hash == currentInfo.shortNameHash)
            {
                return currentInfo;
            }
            return null;
        }

        
        /// <summary>
        /// 得到当前播放动画的进度 并且返回命中信息
        /// </summary>
        /// <returns></returns>
        public static bool GetAmCurrentStateNormalizedTime(this Animator animator, string name, out float normalizedTime, int layerIndex = 0)
        {
            AnimatorStateInfo? currentInfo = GetAmCurrentStateInfo(animator, GetHashFromDict(animator, name), out float amLength, layerIndex);
            normalizedTime = currentInfo?.normalizedTime ?? 0;
            if (currentInfo is null)
                return false;
                
            return true;
        }
        #endregion

        #region Next
        /// <summary>
        /// 获取下一个动画的信息 并返回其长度
        /// </summary>
        public static AnimatorStateInfo? GetAmNextStateInfo(this Animator animator, string name, out float amLength, int layerIndex = 0)
        {
            return GetAmNextStateInfo(animator, GetHashFromDict(animator, name), out amLength, layerIndex);
        }

        public static AnimatorStateInfo? GetAmNextStateInfo(this Animator animator, int hash, out float amLength, int layerIndex = 0)
        {
            var nextInfo = animator.GetNextAnimatorStateInfo(0);
            amLength = nextInfo.length;
            if (hash == nextInfo.shortNameHash)
            {
                return nextInfo;
            }
            return null;
        }

        /// <summary>
        /// 得到下一个播放动画的进度 并且返回命中信息
        /// </summary>
        /// <returns></returns>
        public static bool GetAmNextStateNormalizedTime(this Animator animator, string name, out float normalizedTime, int layerIndex = 0)
        {
            AnimatorStateInfo? nextInfo = GetAmNextStateInfo(animator, GetHashFromDict(animator, name), out float amLength, layerIndex);
            normalizedTime = nextInfo?.normalizedTime ?? 0;
            if (nextInfo is null)
                return false;
                
            return true;
        }

        #endregion

    }
}

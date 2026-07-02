using UnityEngine;

namespace MieMieFrameWork.Interaction
{
    /// <summary>
    /// 单次交互查询上下文
    /// </summary>
    public struct InteractionContext
    {
        /// <summary>
        /// 交互发起者 Transform 通常为射线起点
        /// </summary>
        public Transform InteractorTransform;

        /// <summary>
        /// 射线命中点
        /// </summary>
        public Vector3 HitPoint;

        /// <summary>
        /// 与命中点的距离
        /// </summary>
        public float Distance;

        public InteractionContext(Transform interactorTransform, Vector3 hitPoint, float distance)
        {
            InteractorTransform = interactorTransform;
            HitPoint = hitPoint;
            Distance = distance;
        }
    }
}

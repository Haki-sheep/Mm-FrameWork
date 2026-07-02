using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;

namespace MieMieFrameWork.Interaction
{
    /// <summary>
    /// 交互球形投射检测 SphereCastNonAlloc 无堆分配
    /// </summary>
    [System.Serializable]
    public class InteractionDetector
    {
        [SerializeField, LabelText("射线起点")]
        private Transform rayOrigin;

        [SerializeField, LabelText("最大距离"), MinValue(0.1f)]
        private float maxDistance = 3f;

        [SerializeField, LabelText("投射半径"), MinValue(0.01f)]
        private float sphereCastRadius = 0.2f;

        [SerializeField, LabelText("检测 Layer")]
        private LayerMask interactLayer = ~0;

        [SerializeField, LabelText("Trigger 检测")]
        private QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Collide;

        [SerializeField, LabelText("调试投射")]
        private bool debugDraw;

        /// <summary>
        /// SphereCast 命中缓冲
        /// </summary>
        private readonly RaycastHit[] castHitBuffer = new RaycastHit[1];

        /// <summary>
        /// Collider 实例 ID 到 IInteractable 缓存
        /// </summary>
        private readonly Dictionary<int, IInteractable> interactableCacheDict = new();

        /// <summary>
        /// 射线起点 Transform
        /// </summary>
        public Transform RayOrigin => rayOrigin;

        /// <summary>
        /// 设置射线起点
        /// </summary>
        public void SetRayOrigin(Transform origin)
        {
            rayOrigin = origin;
        }

        /// <summary>
        /// 解析默认射线起点
        /// </summary>
        public void EnsureRayOrigin()
        {
            if (rayOrigin != null)
                return;

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
                rayOrigin = mainCamera.transform;
        }

        /// <summary>
        /// 尝试检测当前聚焦的可交互物
        /// </summary>
        public bool TryDetect(out RaycastHit hit, out IInteractable target)
        {
            target = null;
            hit = default;

            EnsureRayOrigin();
            if (rayOrigin == null)
                return false;

            Vector3 origin = rayOrigin.position;
            Vector3 direction = rayOrigin.forward;

            // 球形范围投射 比单射线更易命中交互 Trigger
            int hitCount = PhysicRayCast.SphereCastNonAlloc(
                origin,
                sphereCastRadius,
                direction,
                castHitBuffer,
                maxDistance,
                interactLayer,
                queryTriggerInteraction,
                debugDraw);

            if (hitCount <= 0)
                return false;

            hit = castHitBuffer[0];
            target = ResolveInteractable(hit.collider);
            return target != null;
        }

        /// <summary>
        /// 从 Collider 解析 IInteractable 带缓存
        /// </summary>
        private IInteractable ResolveInteractable(Collider collider)
        {
            if (collider == null)
                return null;

            int instanceId = collider.GetInstanceID();
            if (interactableCacheDict.TryGetValue(instanceId, out IInteractable cached))
                return cached;

            var interactable = collider.GetComponentInParent<IInteractable>();
            interactableCacheDict[instanceId] = interactable;
            return interactable;
        }

        /// <summary>
        /// 清空 Collider 缓存
        /// </summary>
        public void ClearCache()
        {
            interactableCacheDict.Clear();
        }
    }
}

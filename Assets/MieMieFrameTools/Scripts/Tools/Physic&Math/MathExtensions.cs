namespace MieMieFrameWork
{
    using UnityEngine;
    public static class MathExtensions
    {
        /// <summary>
        /// 平滑看向目标方向（以Y轴为中心）
        /// </summary>
        /// <param name="transform">目标Transform</param>
        /// <param name="target">目标位置</param>
        /// <param name="smoothTime">平滑时间（建议大于10）</param>
        public static void LerpLookAt(this Transform transform, Vector3 target, float smoothTime = 10f)
        {
            Vector3 direction = (target - transform.position).normalized;
            direction.y = 0f; // 只在Y轴上旋转

            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, GetFrameRateIndependentLerp(smoothTime));
            } 
        }

        /// <summary>
        /// 平滑看向目标Transform（以Y轴为中心）
        /// </summary>
        /// <param name="transform">目标Transform</param>
        /// <param name="target">目标Transform</param>
        /// <param name="smoothTime">平滑时间（建议大于10）</param>
        public static void LerpLookAt(this Transform transform, Transform target, float smoothTime = 10f)
        {
            if (target != null)
            {
                transform.LerpLookAt(target.position, smoothTime);
            }
        }

        /// <summary>
        /// 获取不受帧率影响的插值系数
        /// 
        /// 这个方法使用指数衰减来生成平滑的插值系数，
        /// 无论帧率如何变化，都能保持一致的动画速度。
        /// 
        /// 数学原理：使用 1 - e^(-smoothTime * deltaTime) 公式
        /// 当deltaTime变化时，动画的视觉效果保持一致
        /// </summary>
        /// <param name="smoothTime">平滑时间（值越大越平滑，建议范围：1-20）</param>
        /// <returns>插值系数 (0-1)</returns>
        public static float GetFrameRateIndependentLerp(float smoothTime = 10f)
        {
            return 1f - Mathf.Exp(-smoothTime * Time.deltaTime);
        }

        /// <summary>
        /// 平滑移动到目标位置
        /// </summary>
        /// <param name="transform">目标Transform</param>
        /// <param name="targetPosition">目标位置</param>
        /// <param name="smoothTime">平滑时间</param>
        public static void LerpMoveTo(this Transform transform, Vector3 targetPosition, float smoothTime = 10f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, GetFrameRateIndependentLerp(smoothTime));
        }

        /// <summary>
        /// 检查两个向量是否近似相等
        /// </summary>
        /// <param name="vector">向量1</param>
        /// <param name="other">向量2</param>
        /// <param name="threshold">阈值</param>
        /// <returns>是否近似相等</returns>
        public static bool Approximately(this Vector3 vector, Vector3 other, float threshold = 0.01f)
        {
            return Vector3.Distance(vector, other) < threshold;

        }
    }
}
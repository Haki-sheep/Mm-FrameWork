namespace MieMieFrameWork.Effects
{
    using UnityEngine;
    using UnityEngine.SceneManagement;

    /// <summary>
    /// 播放姿态与所有者选项 实例始终放在框架全局节点下
    /// </summary>
    public readonly struct EffectSpawnOptions
    {
        public Vector3 Position { get; }
        public Quaternion Rotation { get; }
        public Transform Owner { get; }
        public bool FollowOwner { get; }
        public bool Persistent { get; }
        public bool UseUnscaledTime { get; }
        public float Scale { get; }
        public Scene Scene { get; }

        /// <summary>
        /// 无所有者时使用世界姿态 有所有者时使用局部偏移
        /// </summary>
        public EffectSpawnOptions(Vector3 position, Quaternion rotation, Transform owner = null,
            bool followOwner = false, bool persistent = false, bool useUnscaledTime = false,
            float scale = 1f, Scene scene = default)
        {
            Position = position;
            Rotation = rotation;
            Owner = owner;
            FollowOwner = followOwner;
            Persistent = persistent;
            UseUnscaledTime = useUnscaledTime;
            Scale = scale;
            Scene = scene;
        }
    }
}

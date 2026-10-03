namespace MieMieFrameWork.Effects
{
    using System;
    using System.Collections.Generic;
    using Cysharp.Threading.Tasks;
    using MieMieFrameWork.Asset;
    using MieMieFrameWork.Pool;
    using UnityEngine;
    using YooAsset;

    /// <summary>
    /// 单地址资源与独占实例池 所有实例实际销毁后释放原生句柄
    /// </summary>
    internal sealed class EffectResourceEntry
    {
        /// <summary> 已初始化的实例组件 </summary>
        private readonly Dictionary<GameObject, EffectInstance> instanceDict = new();

        /// <summary> 已安排销毁但 Unity 尚未实际销毁的实例 </summary>
        private readonly List<GameObject> destroyingList = new();

        /// <summary> 支持多个调用方同时等待的释放完成信号 </summary>
        private readonly UniTaskCompletionSource releaseCompletion = new();

        public EffectDefinition Definition { get; }
        public AssetHandle Asset { get; private set; }
        public PoolHandle Pool { get; private set; }
        public GameObject Prefab { get; private set; }
        public int Requests { get; set; }
        public int PlaybackRequests { get; set; }
        public bool Retired { get; private set; }
        public bool Released { get; private set; }
        public UniTask ReleaseTask => releaseCompletion.Task;

        /// <summary>
        /// 单类特效创建一份原生加载持有 多个请求共同等待
        /// </summary>
        public EffectResourceEntry(EffectDefinition definition)
        {
            Definition = definition;
            Asset = YooAssetMgr.LoadAssetAsync<GameObject>(definition.Location);
        }

        /// <summary>
        /// 加载完成后注册独占池 拒绝与其他模块共用同一预制体池
        /// </summary>
        public void CreatePool(PoolManager poolManager)
        {
            if (Pool != null)
                return;
            if (Asset.Status != EOperationStatus.Succeeded)
                throw new InvalidOperationException($"[EffectManager] 加载失败 {Definition.Location} {Asset.Error}");
            Prefab = Asset.AssetObject as GameObject;
            if (Prefab == null || Prefab.GetComponent<EffectInstance>() == null)
                throw new InvalidOperationException($"[EffectManager] {Definition.Location} 根节点必须包含 EffectInstance");
            if (poolManager.TryGetPool(Prefab, out var ExistingPool))
                throw new InvalidOperationException($"[EffectManager] {Definition.Location} 已被其他实例池持有");
            Pool = poolManager.GetPool(Prefab, Definition.MaxInactive, Definition.MaxConcurrent,
                OnGet, OnRelease, OnDestroy);
        }

        /// <summary>
        /// 从唯一取出回调初始化组件 缓存成功后不重复初始化
        /// </summary>
        private void OnGet(GameObject instance)
        {
            if (instanceDict.ContainsKey(instance))
                return;
            var Component = instance.GetComponent<EffectInstance>();
            Component.InitComponents();
            if (Component.HasLoop != Definition.Looping)
                throw new InvalidOperationException($"[EffectManager] {Definition.Location} 循环声明与粒子配置不一致");
            instanceDict.Add(instance, Component);
        }

        /// <summary>
        /// 归还前清空本轮粒子 半配置失败实例不重复访问组件
        /// </summary>
        private void OnRelease(GameObject instance)
        {
            if (instanceDict.TryGetValue(instance, out var Component))
                Component.RefreshStopped();
        }

        /// <summary>
        /// 销毁前解除组件持有 并记录 Unity 延迟销毁责任
        /// </summary>
        private void OnDestroy(GameObject instance)
        {
            instanceDict.Remove(instance);
            CollectDestroyed();
            if (!destroyingList.Contains(instance))
                destroyingList.Add(instance);
        }

        /// <summary>
        /// 获取已经初始化的实例组件
        /// </summary>
        public EffectInstance GetInstance(GameObject instance) => instanceDict[instance];

        /// <summary>
        /// 剔除已经实际销毁的实例 保留尚未完成的销毁责任
        /// </summary>
        public void CollectDestroyed()
        {
            for (int Index = destroyingList.Count - 1; Index >= 0; Index--)
                if (destroyingList[Index] == null)
                    destroyingList.RemoveAt(Index);
        }

        /// <summary>
        /// 注销独占池并等待实际销毁 归还失败仍安排资源清理
        /// </summary>
        public void Retire(PoolManager poolManager)
        {
            if (Retired)
                return;
            Retired = true;
            foreach (var Instance in instanceDict.Keys)
                if (!destroyingList.Contains(Instance))
                    destroyingList.Add(Instance);
            try
            {
                if (Pool != null && !Pool.IsDisposed)
                    poolManager.ClearGameObject(Prefab);
            }
            finally
            {
                ReleaseAfterDestroyAsync().Forget();
            }
        }

        /// <summary>
        /// 不依赖管理器继续 Tick 待池实例实际销毁后释放资源
        /// </summary>
        private async UniTask ReleaseAfterDestroyAsync()
        {
            try
            {
                CollectDestroyed();
                while (Pool != null && !Pool.IsDisposed || destroyingList.Count > 0)
                {
                    await UniTask.NextFrame();
                    CollectDestroyed();
                }
                Asset.Release();
                Asset = null;
                Prefab = null;
                instanceDict.Clear();
                Released = true;
                releaseCompletion.TrySetResult();
            }
            catch (Exception Failure)
            {
                releaseCompletion.TrySetException(Failure);
                throw;
            }
        }
    }
}

using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 一个资源键对应的图集内容与共享 Sprite
    /// </summary>
    internal sealed class DynamicAtlasEntry
    {
        /// <summary>
        /// 资源键
        /// </summary>
        internal string Key;
        /// <summary>
        /// 所属页
        /// </summary>
        internal DynamicAtlasPage Page;
        /// <summary>
        /// 含边缘扩展的矩形
        /// </summary>
        internal RectInt Area;
        /// <summary>
        /// 共享 Sprite
        /// </summary>
        internal Sprite Sprite;
        /// <summary>
        /// 活跃使用句柄数量
        /// </summary>
        internal int ReferenceCount;
        /// <summary>
        /// 尚未取得句柄的加载等待者
        /// </summary>
        internal DynamicAtlasRequest Request;
    }

    /// <summary>
    /// 合并同键加载 每个等待者独立取消
    /// </summary>
    internal sealed class DynamicAtlasRequest
    {
        /// <summary>
        /// 可供多个等待者读取的完成源
        /// </summary>
        internal readonly UniTaskCompletionSource<DynamicAtlasEntry> Completion = new UniTaskCompletionSource<DynamicAtlasEntry>();
        /// <summary>
        /// 全部等待者取消或服务销毁时取消源加载
        /// </summary>
        internal CancellationTokenSource Cancellation;
        /// <summary>
        /// 同键请求必须使用同一来源加载器
        /// </summary>
        internal IDynamicAtlasSourceLoader Loader;
        /// <summary>
        /// 源释放与缓存发布事务已经结束
        /// </summary>
        internal bool IsCompleted;
        /// <summary>
        /// 当前等待者数量
        /// </summary>
        internal int WaiterCount;
        /// <summary>
        /// 已写入的结果
        /// </summary>
        internal DynamicAtlasEntry Entry;
    }
}

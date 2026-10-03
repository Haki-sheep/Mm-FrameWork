using System;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 实例化图集组 合并加载 管理使用引用与整页回收
    /// </summary>
    public sealed class DynamicAtlasService : IDisposable
    {
        /// <summary>
        /// 配置快照 单页宽度
        /// </summary>
        private readonly int pageWidth;
        /// <summary>
        /// 配置快照 单页高度
        /// </summary>
        private readonly int pageHeight;
        /// <summary>
        /// 配置快照 边缘扩展
        /// </summary>
        private readonly int padding;
        /// <summary>
        /// 配置快照 最大页数
        /// </summary>
        private readonly int maxPageCount;
        /// <summary>
        /// 配置快照 纹理逻辑常驻预算
        /// </summary>
        private readonly long memoryBudgetBytes;
        /// <summary>
        /// 配置快照 纹理色彩空间
        /// </summary>
        private readonly bool srgb;
        /// <summary>
        /// 配置快照 Sprite 轴心
        /// </summary>
        private readonly Vector2 pivot;
        /// <summary>
        /// 配置快照 Sprite 每单位像素
        /// </summary>
        private readonly float pixelsPerUnit;
        /// <summary>
        /// GPU 写入器
        /// </summary>
        private readonly DynamicAtlasGpuWriter writer;
        /// <summary>
        /// 常驻图集页
        /// </summary>
        private readonly List<DynamicAtlasPage> pageList = new List<DynamicAtlasPage>();
        /// <summary>
        /// 同键共享条目
        /// </summary>
        private readonly Dictionary<string, DynamicAtlasEntry> entryDict = new Dictionary<string, DynamicAtlasEntry>(StringComparer.Ordinal);
        /// <summary>
        /// 同键合并中的加载请求
        /// </summary>
        private readonly Dictionary<string, DynamicAtlasRequest> requestDict = new Dictionary<string, DynamicAtlasRequest>(StringComparer.Ordinal);
        /// <summary>
        /// 服务销毁取消源
        /// </summary>
        private readonly CancellationTokenSource lifetimeCancellation = new CancellationTokenSource();
        /// <summary>
        /// 当前实例内不复用的页编号
        /// </summary>
        private int nextPageId;

        public bool IsDisposed { get; private set; }
        public int PageCount => pageList.Count;
        public int PendingCount => requestDict.Count;
        public long ResidentBytes => pageList.Count * PageBytes;
        public long MemoryBudgetBytes => memoryBudgetBytes;
        private long PageBytes => (long)pageWidth * pageHeight * 4;

        #region 生命周期

        /// <summary>
        /// 主线程校验配置并建立独立服务 配置修改仅影响下次创建
        /// </summary>
        public DynamicAtlasService(DynamicAtlasConfig config)
        {
            AssertMainThread();
            if (config == null)
                throw new ArgumentNullException(nameof(config));
            pageWidth = config.PageWidth;
            pageHeight = config.PageHeight;
            padding = config.Padding;
            maxPageCount = config.MaxPageCount;
            memoryBudgetBytes = config.MemoryBudgetBytes;
            srgb = config.Srgb;
            pivot = config.Pivot;
            pixelsPerUnit = config.PixelsPerUnit;
            if (pageWidth <= 0 || pageHeight <= 0 || pageWidth > SystemInfo.maxTextureSize || pageHeight > SystemInfo.maxTextureSize
                || padding < 1 || (long)padding * 2 >= pageWidth || (long)padding * 2 >= pageHeight || maxPageCount < 1
                || memoryBudgetBytes < PageBytes || !float.IsFinite(pixelsPerUnit) || pixelsPerUnit <= 0
                || !float.IsFinite(pivot.x) || !float.IsFinite(pivot.y))
                throw new ArgumentException($"动态图集配置无效 页 {pageWidth}x{pageHeight} Padding {padding} 最大页数 {maxPageCount} 预算 {memoryBudgetBytes}", nameof(config));
            writer = new DynamicAtlasGpuWriter(config.PaddingShader, srgb);
        }

        /// <summary>
        /// 取消未完成请求 失效所有句柄 并释放生成的 Unity 资源
        /// </summary>
        public void Dispose()
        {
            AssertMainThread();
            if (IsDisposed)
                return;
            IsDisposed = true;
            var RequestList = new List<DynamicAtlasRequest>(requestDict.Values);
            requestDict.Clear();
            try
            {
                lifetimeCancellation.Cancel();
            }
            finally
            {
                foreach (var Request in RequestList)
                    Request.Completion.TrySetCanceled(lifetimeCancellation.Token);
                entryDict.Clear();
                foreach (var Page in pageList)
                    Page.Dispose();
                pageList.Clear();
                writer.Dispose();
                lifetimeCancellation.Dispose();
            }
        }

        #endregion

        #region 获取与释放

        /// <summary>
        /// 借用源纹理同步合图 返回一个使用句柄 不销毁输入纹理
        /// </summary>
        public DynamicAtlasHandle Acquire(string key, Texture2D source)
        {
            CheckAccessible();
            ValidateKey(key);
            if (entryDict.TryGetValue(key, out var Existing))
                return Retain(Existing);
            if (requestDict.ContainsKey(key))
                throw new InvalidOperationException($"动态图集资源 {key} 正在异步加载 请使用同一异步入口");
            var Entry = CreateEntry(key, source);
            entryDict.Add(key, Entry);
            return Retain(Entry);
        }

        /// <summary>
        /// 同键只加载一次 独立取消当前等待者 成功后返回独立使用句柄
        /// </summary>
        public async UniTask<DynamicAtlasHandle> LoadAsync(string key, IDynamicAtlasSourceLoader loader, CancellationToken cancellationToken = default)
        {
            CheckAccessible();
            ValidateKey(key);
            cancellationToken.ThrowIfCancellationRequested();
            if (entryDict.TryGetValue(key, out var Existing))
                return Retain(Existing);
            if (loader == null)
                throw new ArgumentNullException(nameof(loader));
            bool IsNew = !requestDict.TryGetValue(key, out var Request);
            if (IsNew)
            {
                Request = new DynamicAtlasRequest
                {
                    Cancellation = CancellationTokenSource.CreateLinkedTokenSource(lifetimeCancellation.Token),
                    Loader = loader
                };
                requestDict.Add(key, Request);
            }
            else if (!ReferenceEquals(Request.Loader, loader))
            {
                throw new InvalidOperationException($"动态图集同键并发请求必须使用同一加载器实例 键 {key}");
            }
            Request.WaiterCount++;
            if (IsNew)
                LoadEntryAsync(key, loader, Request).Forget();
            try
            {
                var Entry = await Request.Completion.Task.AttachExternalCancellation(cancellationToken);
                await UniTask.SwitchToMainThread();
                cancellationToken.ThrowIfCancellationRequested();
                CheckAccessible();
                return Retain(Entry);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                Request.WaiterCount--;
                if (Request.WaiterCount == 0 && Request.IsCompleted && Request.Entry != null)
                    Request.Entry.Request = null;
                if (Request.WaiterCount == 0 && !Request.IsCompleted && requestDict.TryGetValue(key, out var Current) && Current == Request)
                {
                    requestDict.Remove(key);
                    Request.Cancellation.Cancel();
                }
            }
        }

        /// <summary>
        /// 归还活跃使用引用 服务销毁后允许遗留句柄正常退出
        /// </summary>
        internal void Release(DynamicAtlasEntry entry)
        {
            AssertMainThread();
            if (!IsDisposed)
                entry.ReferenceCount--;
        }

        /// <summary>
        /// 增加一次消费者引用并建立句柄
        /// </summary>
        private DynamicAtlasHandle Retain(DynamicAtlasEntry entry)
        {
            entry.ReferenceCount++;
            return new DynamicAtlasHandle(this, entry);
        }

        #endregion

        #region 加载与合图

        /// <summary>
        /// 加载源资源 回主线程写入 释放源所有权 并广播原始错误
        /// </summary>
        private async UniTask LoadEntryAsync(string key, IDynamicAtlasSourceLoader loader, DynamicAtlasRequest request)
        {
            DynamicAtlasSource Source = null;
            Exception Failure = null;
            try
            {
                Source = await loader.LoadAsync(key, request.Cancellation.Token);
            }
            catch (Exception Error)
            {
                Failure = Error;
            }
            await UniTask.SwitchToMainThread();
            try
            {
                using (Source)
                {
                    if (Failure != null)
                        ExceptionDispatchInfo.Capture(Failure).Throw();
                    request.Cancellation.Token.ThrowIfCancellationRequested();
                    CheckAccessible();
                    if (Source == null)
                        throw new InvalidOperationException($"动态图集加载器返回空源资源 键 {key}");
                    request.Entry = CreateEntry(key, Source.Texture);
                    request.Entry.Request = request;
                }
                request.Cancellation.Token.ThrowIfCancellationRequested();
                CheckAccessible();
                // 源释放成功后才发布缓存 防止回调重入取得未完成条目
                entryDict.Add(key, request.Entry);
                request.IsCompleted = true;
                request.Completion.TrySetResult(request.Entry);
            }
            catch (OperationCanceledException Error)
            {
                RollbackEntry(request);
                request.IsCompleted = true;
                request.Completion.TrySetCanceled(Error.CancellationToken);
            }
            catch (Exception Error)
            {
                RollbackEntry(request);
                request.IsCompleted = true;
                request.Completion.TrySetException(Error);
            }
            finally
            {
                request.IsCompleted = true;
                if (requestDict.TryGetValue(key, out var Current) && Current == request)
                    requestDict.Remove(key);
                if (request.WaiterCount == 0 && request.Entry != null)
                    request.Entry.Request = null;
                request.Cancellation.Dispose();
            }
        }

        /// <summary>
        /// 撤销未完成条目 不误删同键替代结果 服务销毁后不重复销毁资源
        /// </summary>
        private void RollbackEntry(DynamicAtlasRequest request)
        {
            var Entry = request.Entry;
            if (Entry == null)
                return;
            if (!IsDisposed)
            {
                if (entryDict.TryGetValue(Entry.Key, out var Current) && Current == Entry)
                    entryDict.Remove(Entry.Key);
                Entry.Page.entryList.Remove(Entry);
                DynamicAtlasPage.DestroyObject(Entry.Sprite);
            }
            request.Entry = null;
        }

        /// <summary>
        /// 分配空间并生成 FullRect Sprite 不读取源纹理 CPU 副本
        /// </summary>
        private DynamicAtlasEntry CreateEntry(string key, Texture2D source)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source), $"动态图集源纹理为空 键 {key}");
            if (source.width > pageWidth - padding * 2 || source.height > pageHeight - padding * 2)
                throw new ArgumentException($"动态图集源图片超出页尺寸 键 {key} 图片 {source.width}x{source.height} 页 {pageWidth}x{pageHeight} Padding {padding}");
            int Width = source.width + padding * 2;
            int Height = source.height + padding * 2;
            var Page = AllocatePage(key, Width, Height, out var Area);
            writer.Write(source, Page.Texture, Area, padding);
            var Sprite = UnityEngine.Sprite.Create(Page.Texture, new Rect(Area.x + padding, Area.y + padding, source.width, source.height),
                pivot, pixelsPerUnit, 0, SpriteMeshType.FullRect);
            Sprite.name = key;
            Sprite.hideFlags = HideFlags.HideAndDontSave;
            var Entry = new DynamicAtlasEntry { Key = key, Page = Page, Area = Area, Sprite = Sprite };
            Page.entryList.Add(Entry);
            return Entry;
        }

        /// <summary>
        /// 优先复用页内剩余空间 达预算时仅回收整页零引用缓存
        /// </summary>
        private DynamicAtlasPage AllocatePage(string key, int width, int height, out RectInt area)
        {
            foreach (var Page in pageList)
            {
                if (Page.TryAllocate(width, height, out area))
                    return Page;
            }
            for (int Index = pageList.Count - 1; Index >= 0 && (pageList.Count >= maxPageCount || ResidentBytes + PageBytes > memoryBudgetBytes); Index--)
            {
                if (pageList[Index].CanRecycle())
                    RemovePage(Index);
            }
            if (pageList.Count >= maxPageCount || ResidentBytes + PageBytes > memoryBudgetBytes)
                throw new InvalidOperationException($"动态图集容量已满且无可回收页 键 {key} 请求 {width}x{height} 页数 {pageList.Count} 常驻 {ResidentBytes} 预算 {memoryBudgetBytes}");
            var NewPage = new DynamicAtlasPage(++nextPageId, pageWidth, pageHeight, srgb);
            pageList.Add(NewPage);
            if (!NewPage.TryAllocate(width, height, out area))
                throw new InvalidOperationException($"动态图集分配器拒绝合法尺寸 键 {key} 请求 {width}x{height}");
            return NewPage;
        }

        #endregion

        #region 回收与诊断

        /// <summary>
        /// 显式回收全部零引用页 返回回收页数 不移动活跃内容
        /// </summary>
        public int TrimUnusedPages()
        {
            CheckAccessible();
            int Removed = 0;
            for (int Index = pageList.Count - 1; Index >= 0; Index--)
            {
                if (!pageList[Index].CanRecycle())
                    continue;
                RemovePage(Index);
                Removed++;
            }
            return Removed;
        }

        /// <summary>
        /// 为调试窗口生成快照 不在业务逐帧调用
        /// </summary>
        public IReadOnlyList<DynamicAtlasPageInfo> GetPageInfoList()
        {
            CheckAccessible();
            var InfoList = new List<DynamicAtlasPageInfo>(pageList.Count);
            foreach (var Page in pageList)
                InfoList.Add(new DynamicAtlasPageInfo(Page));
            return InfoList.AsReadOnly();
        }

        /// <summary>
        /// 删除页缓存索引并销毁页持有的 Unity 资源
        /// </summary>
        private void RemovePage(int index)
        {
            var Page = pageList[index];
            foreach (var Entry in Page.entryList)
                entryDict.Remove(Entry.Key);
            pageList.RemoveAt(index);
            Page.Dispose();
        }

        /// <summary>
        /// 拒绝空键避免跨来源误共享
        /// </summary>
        private static void ValidateKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("动态图集资源键不能为空", nameof(key));
        }

        /// <summary>
        /// 校验 Unity 主线程与服务生命周期
        /// </summary>
        private void CheckAccessible()
        {
            AssertMainThread();
            if (IsDisposed)
                throw new ObjectDisposedException(nameof(DynamicAtlasService));
        }

        /// <summary>
        /// 禁止在后台线程操作 Unity 资源与共享索引
        /// </summary>
        private static void AssertMainThread()
        {
            if (!PlayerLoopHelper.IsMainThread)
                throw new InvalidOperationException("动态图集只能在 Unity 主线程访问");
        }

        #endregion
    }
}

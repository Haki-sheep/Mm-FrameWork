using System;
using System.Collections.Generic;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// 正常分配使用 Skyline 发布失败后按存活矩形恢复可用空间
    /// </summary>
    internal sealed class DynamicAtlasAllocator
    {
        /// <summary>
        /// 页宽
        /// </summary>
        private readonly int width;
        /// <summary>
        /// 页高
        /// </summary>
        private readonly int height;
        /// <summary>
        /// 从左到右排列的轮廓节点
        /// </summary>
        private readonly List<RectInt> nodeList = new List<RectInt>();
        /// <summary> 发布回滚后由仍存活的条目重建的可用空间 </summary>
        private List<RectInt> freeAreaList;

        /// <summary>
        /// 输入页尺寸 建立初始轮廓
        /// </summary>
        public DynamicAtlasAllocator(int width, int height)
        {
            this.width = width;
            this.height = height;
            nodeList.Add(new RectInt(0, 0, width, 0));
        }

        /// <summary>
        /// 输入含边缘扩展的尺寸 成功时输出不重叠矩形
        /// </summary>
        public bool TryAllocate(int requestedWidth, int requestedHeight, out RectInt area)
        {
            if (freeAreaList != null)
                return TryAllocateRecovered(requestedWidth, requestedHeight, out area);
            int BestIndex = -1;
            int BestY = int.MaxValue;
            int BestWidth = int.MaxValue;
            area = default;
            for (int Index = 0; Index < nodeList.Count; Index++)
            {
                if (!TryFit(Index, requestedWidth, requestedHeight, out int Y))
                    continue;
                if (Y < BestY || Y == BestY && nodeList[Index].width < BestWidth)
                {
                    BestIndex = Index;
                    BestY = Y;
                    BestWidth = nodeList[Index].width;
                }
            }
            if (BestIndex < 0)
                return false;

            int X = nodeList[BestIndex].x;
            area = new RectInt(X, BestY, requestedWidth, requestedHeight);
            nodeList.Insert(BestIndex, new RectInt(X, BestY + requestedHeight, requestedWidth, 0));
            for (int Index = BestIndex + 1; Index < nodeList.Count; Index++)
            {
                var Previous = nodeList[Index - 1];
                var Current = nodeList[Index];
                int Overlap = Previous.xMax - Current.x;
                if (Overlap <= 0)
                    break;
                Current.x += Overlap;
                Current.width -= Overlap;
                if (Current.width > 0)
                {
                    nodeList[Index] = Current;
                    break;
                }
                nodeList.RemoveAt(Index);
                Index--;
            }
            for (int Index = 0; Index < nodeList.Count - 1;)
            {
                var Current = nodeList[Index];
                var Next = nodeList[Index + 1];
                if (Current.y != Next.y)
                {
                    Index++;
                    continue;
                }
                Current.width += Next.width;
                nodeList[Index] = Current;
                nodeList.RemoveAt(Index + 1);
            }
            return true;
        }

        /// <summary>
        /// 源释放允许重入 以当前仍占用的矩形重建空位 保留后来发布的条目
        /// </summary>
        internal void Recover(IEnumerable<RectInt> occupiedAreas)
        {
            freeAreaList = new List<RectInt> { new RectInt(0, 0, width, height) };
            foreach (var Area in occupiedAreas)
                ExcludeArea(Area);
        }

        private bool TryAllocateRecovered(int requestedWidth, int requestedHeight, out RectInt area)
        {
            area = default;
            if (requestedWidth <= 0 || requestedHeight <= 0)
                return false;
            foreach (var Free in freeAreaList)
            {
                if (Free.width >= requestedWidth && Free.height >= requestedHeight)
                {
                    area = new RectInt(Free.x, Free.y, requestedWidth, requestedHeight);
                    break;
                }
            }
            if (area.width == 0)
                return false;
            ExcludeArea(area);
            return true;
        }

        private void ExcludeArea(RectInt area)
        {
            // 保留每个交集四边的完整空矩形 允许候选空位重叠
            for (int Index = freeAreaList.Count - 1; Index >= 0; Index--)
            {
                var Free = freeAreaList[Index];
                if (!Free.Overlaps(area))
                    continue;
                freeAreaList.RemoveAt(Index);
                if (Free.x < area.x)
                    freeAreaList.Add(new RectInt(Free.x, Free.y, area.x - Free.x, Free.height));
                if (Free.xMax > area.xMax)
                    freeAreaList.Add(new RectInt(area.xMax, Free.y, Free.xMax - area.xMax, Free.height));
                if (Free.y < area.y)
                    freeAreaList.Add(new RectInt(Free.x, Free.y, Free.width, area.y - Free.y));
                if (Free.yMax > area.yMax)
                    freeAreaList.Add(new RectInt(Free.x, area.yMax, Free.width, Free.yMax - area.yMax));
            }
            for (int Index = freeAreaList.Count - 1; Index >= 0; Index--)
            {
                var Free = freeAreaList[Index];
                for (int OtherIndex = 0; OtherIndex < freeAreaList.Count; OtherIndex++)
                {
                    var Other = freeAreaList[OtherIndex];
                    if (Index != OtherIndex && Free.x >= Other.x && Free.y >= Other.y &&
                        Free.xMax <= Other.xMax && Free.yMax <= Other.yMax)
                    {
                        freeAreaList.RemoveAt(Index);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// 检查轮廓剩余宽度与最高点 不足时拒绝分配
        /// </summary>
        private bool TryFit(int index, int requestedWidth, int requestedHeight, out int y)
        {
            y = nodeList[index].y;
            if (requestedWidth <= 0 || requestedHeight <= 0 || requestedWidth > width - nodeList[index].x)
                return false;
            int Remaining = requestedWidth;
            for (int Index = index; Index < nodeList.Count && Remaining > 0; Index++)
            {
                y = Math.Max(y, nodeList[Index].y);
                if (requestedHeight > height - y)
                    return false;
                Remaining -= nodeList[Index].width;
            }
            return Remaining <= 0;
        }
    }
}

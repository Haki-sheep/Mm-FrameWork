using System;
using System.Collections.Generic;
using UnityEngine;

namespace MieMieFrameWork.Asset.DynamicAtlas
{
    /// <summary>
    /// Skyline 只追加分配 不旋转 不回收单个矩形
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

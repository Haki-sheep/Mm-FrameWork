namespace MieMieFrameWork
{
    using System;
    using System.Collections.Generic;
    using UnityEngine;

    public partial class AudioManager
    {
        /// <summary>
        /// 调用方取得的背景音乐片段与独立资源持有记录
        /// </summary>
        private readonly Dictionary<AudioClip, List<AudioClipLease>> bgClipLeaseDict = new();

        /// <summary>
        /// 调用者片段接口尚未完成的加载持有
        /// </summary>
        private readonly HashSet<AudioClipLease> pendingClipLeaseHashList = new();

        /// <summary>
        /// 登记调用方取得的片段 每次加载保留一份独立资源持有
        /// </summary>
        private void TrackBgClip(AudioClip clip, AudioClipLease lease)
        {
            if (!bgClipLeaseDict.TryGetValue(clip, out var LeaseList))
            {
                LeaseList = new List<AudioClipLease>();
                bgClipLeaseDict.Add(clip, LeaseList);
            }
            LeaseList.Add(lease);
        }

        /// <summary>
        /// 释放由 LoadBgClipAsync 取得的一份资源持有 不能释放外部片段
        /// </summary>
        public void ReleaseBgClip(AudioClip clip)
        {
            RequireActive();
            if (!bgClipLeaseDict.TryGetValue(clip, out var LeaseList))
                throw new InvalidOperationException($"音频片段不属于 LoadBgClipAsync 的未释放结果 {clip}");

            int Index = LeaseList.Count - 1;
            LeaseList[Index].Dispose();
            LeaseList.RemoveAt(Index);
            if (LeaseList.Count == 0)
                bgClipLeaseDict.Remove(clip);
        }

        /// <summary>
        /// 管理器退出时回收调用方尚未释放的片段 不遗留会话资源句柄
        /// </summary>
        private void ReleaseBorrowedAudioClips(List<Exception> failureList)
        {
            foreach (var Lease in pendingClipLeaseHashList)
                DisposeAudioResource(Lease, failureList);
            pendingClipLeaseHashList.Clear();
            foreach (var LeaseList in bgClipLeaseDict.Values)
            {
                foreach (var Lease in LeaseList)
                    DisposeAudioResource(Lease, failureList);
            }
            bgClipLeaseDict.Clear();
        }
    }
}

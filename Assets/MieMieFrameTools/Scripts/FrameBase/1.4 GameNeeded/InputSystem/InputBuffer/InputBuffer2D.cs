using System;
using UnityEngine;
using Sirenix.OdinInspector;

namespace MieMieFrameWork.M_InputSystem
{
    [Serializable]
    public enum E_InputType2D
    {
        Jump, //跳跃预处理缓冲
        Attack,//土狼时间
        Dash,
        Skill,
    }

    /// <summary>
    /// 2D输入缓冲管理器
    /// </summary>
    public class Mm_InputBuffer2D : MonoBehaviour, IManagerBase
    {
        [Header("缓冲设置"), SerializeField, LabelText("默认缓冲时间"), Range(0.1f, 1f)]
        private float defaultBufferTime = 0.2f;

        //Buff消耗缓冲区
        private readonly BuffSlot2D[] buffSlot2DArray =new BuffSlot2D[Enum.GetValues(typeof(E_InputType2D)).Length];
        //辅助初始化
        private readonly E_InputType2D[] inputEnumTypeArray =(E_InputType2D[])Enum.GetValues(typeof(E_InputType2D));

        public void Init()
        {
            ModuleHub.Instance?.GetManager<MonoManager>()?.AddUpdateListener(UpdateBuffer2D);
            InitializeBuffers();
        }

        /// <summary>
        /// 借助inputEnumTypeArray创建输入缓冲槽数组 存入对应枚举数量的对象
        /// </summary>
        private void InitializeBuffers()
        {
            for (int i = 0; i < inputEnumTypeArray.Length; i++)
            {
                buffSlot2DArray[i] = new BuffSlot2D();
            }
        }

        
        /// <summary>
        /// 创建一次输入缓冲
        /// </summary>
        /// <param name="e_InputType"></param>
        /// <param name="myDuration"></param>
        public void CreatOneBuffer(E_InputType2D e_InputType, float myDuration = -1)
        {
            float defaultTime = myDuration == -1 ? defaultBufferTime : myDuration;
            buffSlot2DArray[(int)e_InputType].CreatOneBufferSlot2D(defaultTime);
        }

        /// <summary>
        /// 更新所有的Buff缓冲槽
        /// </summary>
        private void UpdateBuffer2D()
        {
            //更新所有BuffSlot
            foreach (var slot in buffSlot2DArray)
            {
                slot.UpdateBuff2D(Time.deltaTime);
            }
        }
        
        /// <summary>
        /// 消耗一次Buffer
        /// </summary>
        /// <param name="e_InputType"></param>
        /// <returns></returns>
        public bool ConsumeOneBuffer2D(E_InputType2D e_InputType)
        {
            int index = (int)e_InputType;
            if (index >= 0 && index < buffSlot2DArray.Length)
            {
                return buffSlot2DArray[index].ConsumeBuffer2D();
            }
            return false;
        }

        /// <summary>
        /// 检查某类型缓冲是否存在
        /// </summary>
        /// <param name="e_InputType"></param>
        /// <returns></returns>
        public bool CheckHasBuffered(E_InputType2D e_InputType)
        {
            int index = (int)e_InputType;
            if (index >= 0 && index < buffSlot2DArray.Length)
            {
                return buffSlot2DArray[index].IsActive;
            }
            return false;
        }

        /// <summary>
        /// 获取剩余时间
        /// </summary>
        /// <param name="e_InputType"></param>
        /// <returns></returns>
        public float GetRemainTime(E_InputType e_InputType)
        {
            int index = (int)e_InputType;
            if (index >= 0 && index < buffSlot2DArray.Length)
            {
                return buffSlot2DArray[index].RemainingTime;
            }
            return 0;
        }

        
        /// <summary>
        /// 清除指定Buffer
        /// </summary>
        /// <param name="e_InputType"></param>
        public void ClearBuffer(E_InputType2D e_InputType)
        {
            if (CheckHasBuffered(e_InputType))
            {
                buffSlot2DArray[(int)e_InputType].ClearBuff2D();
            }
        }

        /// <summary>
        /// 清除所有Buffer
        /// </summary>
        public void ClearAllBuffer()
        {
            foreach (var slot in buffSlot2DArray)
            {
                slot.ClearBuff2D();
            }
        }

        public void Dispose()
        {
            ModuleHub.Instance?.GetManager<MonoManager>()?.RemoveUpdateListener(UpdateBuffer2D);
        }
    }

    /// <summary>
    /// 输入缓冲槽 原理就是通过时间戳和Bool来判断是否消耗缓冲
    /// </summary>
    public class BuffSlot2D
    {
        //时间参数:缓冲时间,剩余时间
        private float bufferDurration;
        private float remaingTime;
        //状态参数:是否缓冲中
        private bool isBuffered;

        //属性
        public bool IsBuffered => isBuffered;
        public bool IsActive => isBuffered && remaingTime > 0;
        public float RemainingTime => remaingTime;
        public float Progress => IsBuffered ? 1 - remaingTime / bufferDurration : 0;

        #region 方法

        /// <summary>
        /// 初始化缓冲槽
        /// </summary>
        /// <param name="bufferDurration"></param>
        public void CreatOneBufferSlot2D(float bufferDurration)
        {
            this.bufferDurration = bufferDurration;
            remaingTime = bufferDurration;
            isBuffered = true;
        }

        public void UpdateBuff2D(float deltaTime)
        {
            //如果在激活状态
            if (IsActive)
            {
                remaingTime -= deltaTime;
                if (remaingTime <= 0)
                {
                    ClearBuff2D();
                }
            }

        }

        /// <summary>
        /// 主动消耗Buff : 如果正在消耗中则直接清空Buff 返回T
        /// </summary>
        /// <returns></returns>
        public bool ConsumeBuffer2D()
        {
            if (IsActive)
            {
                ClearBuff2D();
                return true;
            }
            return false;
        }


        public void ClearBuff2D()
        {
            isBuffered = false;
            remaingTime = 0;
        }
        
        #endregion
    }
}
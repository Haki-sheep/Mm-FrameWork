using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Events;

namespace MieMieFrameWork.M_InputSystem
{
    [Serializable]
    public enum E_InputType
    {
        Jump,
        Attack,
        Dash,
        Skill,
    }

    /// <summary>
    /// 优化的输入缓冲管理器 - 更简洁、性能更好
    /// </summary>      
    [ManagerAttribute(8)]
    public class InputBuffer : MonoBehaviour, IManagerBase
    {

        [Header("缓冲设置")]
        [SerializeField, LabelText("默认缓冲时间"), Range(0.1f, 1f)] private float defaultBufferTime = 0.2f;
        [SerializeField, LabelText("队列大小"), Range(1, 10)] private int maxQueueSize = 3;
        
        //Enum.GetValues是返回该枚举类型的数组 之后获取其长度
        private readonly BufferSlot[] bufferSlots = new BufferSlot[Enum.GetValues(typeof(E_InputType)).Length];
        private readonly Queue<BufferSlot>[] queueBuffers = new Queue<BufferSlot>[Enum.GetValues(typeof(E_InputType)).Length];
        
        // 缓存枚举值避免重复计算
        private static readonly E_InputType[] inputTypes = Enum.GetValues(typeof(E_InputType)) as E_InputType[];
        private static readonly int inputTypeCount = inputTypes.Length;

        public void Init()
        {
            ModuleHub.Instance.GetManager<MonoManager>().AddUpdateListener(UpdateBuffers);
            InitializeBuffers();
        }

        void OnDestroy()
        {
            ModuleHub.Instance.GetManager<MonoManager>().RemoveUpdateListener(UpdateBuffers);
            ClearAllBuffers();
        }

        #region 初始化

        private void InitializeBuffers()
        {
            // 初始化单一缓冲
            for (int i = 0; i < inputTypeCount; i++)
            {
                bufferSlots[i] = new BufferSlot(defaultBufferTime);
            }

            // 初始化队列缓冲
            for (int i = 0; i < inputTypeCount; i++)
            {
                queueBuffers[i] = new Queue<BufferSlot>();
            }
        }

        #endregion

        #region 单一输入缓冲

        /// <summary>
        /// 添加输入到缓冲
        /// </summary>
        public void BufferInput(E_InputType inputType)
        {
            int index = (int)inputType;
            if (index >= 0 && index < inputTypeCount)
            {
                bufferSlots[index].Activate();
            }
        }

        /// <summary>
        /// 检查并消耗缓冲输入
        /// </summary>
        public bool ConsumeBuffer(E_InputType inputType, UnityAction callback = null)
        {
            int index = (int)inputType;
            if (index >= 0 && index < inputTypeCount && bufferSlots[index].IsActive)
            {
                bufferSlots[index].Consume();
                callback?.Invoke();
                return true;
            }
            return false;
        }

        /// <summary>
        /// 检查缓冲是否存在（不消耗）
        /// </summary>
        public bool HasBuffer(E_InputType inputType)
        {
            int index = (int)inputType;
            return index >= 0 && index < inputTypeCount && bufferSlots[index].IsActive;
        }

        #endregion

        #region 队列输入缓冲

        /// <summary>
        /// 添加输入到队列缓冲
        /// </summary>
        public void BufferQueueInput(E_InputType inputType, float customBufferTime = -1f)
        {
            int index = (int)inputType;
            if (index >= 0 && index < inputTypeCount)
            {
                var queue = queueBuffers[index];
                
                // 限制队列大小
                if (queue.Count >= maxQueueSize)
                {
                    queue.Dequeue();
                }

                float bufferTime = customBufferTime > 0 ? customBufferTime : defaultBufferTime;
                queue.Enqueue(new BufferSlot(bufferTime));
            }
        }

        /// <summary>
        /// 检查并消耗队列缓冲
        /// </summary>
        public bool ConsumeQueueBuffer(E_InputType inputType, UnityAction callback = null)
        {
            int index = (int)inputType;
            if (index >= 0 && index < inputTypeCount)
            {
                var queue = queueBuffers[index];
                if (queue.Count > 0)
                {
                    var slot = queue.Dequeue();
                    if (slot.IsActive)
                    {
                        callback?.Invoke();
                        return true;
                    }
                }
            }
            return false;
        }

        #endregion

        #region 更新逻辑

        private void UpdateBuffers()
        {
            float deltaTime = Time.deltaTime;
            
            // 更新单一缓冲
            for (int i = 0; i < inputTypeCount; i++)
            {
                bufferSlots[i].Update(deltaTime);
            }

            // 更新队列缓冲
            for (int i = 0; i < inputTypeCount; i++)
            {
                UpdateQueueBuffer(queueBuffers[i], deltaTime);
            }
        }

        private void UpdateQueueBuffer(Queue<BufferSlot> queue, float deltaTime)
        {
            while (queue.Count > 0)
            {
                var slot = queue.Peek();
                slot.Update(deltaTime);
                
                if (!slot.IsActive)
                {
                    queue.Dequeue();
                }
                else
                {
                    break; // 队列按时间顺序排列，第一个没过期，后面的也不会过期
                }
            }
        }

        #endregion

        #region 打断机制

        /// <summary>
        /// 打断指定输入缓冲
        /// </summary>
        public void Interrupt(E_InputType inputType)
        {
            int index = (int)inputType;
            if (index >= 0 && index < inputTypeCount)
            {
                bufferSlots[index].Interrupt();
            }
        }

        /// <summary>
        /// 打断所有输入缓冲
        /// </summary>
        public void InterruptAll()
        {
            for (int i = 0; i < inputTypeCount; i++)
            {
                bufferSlots[i].Interrupt();
                queueBuffers[i].Clear();
            }
        }

        #endregion

        #region 清理

        private void ClearAllBuffers()
        {
            for (int i = 0; i < inputTypeCount; i++)
            {
                bufferSlots[i].Interrupt();
                queueBuffers[i].Clear();
            }
        }

        #endregion

    }

    /// <summary>
    /// 优化的缓冲槽 - 更简洁的实现
    /// </summary>
    [Serializable]
    public class BufferSlot
    {
        [SerializeField] private float bufferTime;
        private float remainingTime;
        private bool isActive;

        public bool IsActive => isActive && remainingTime > 0f;
        public float RemainingTime => remainingTime;
        public float Progress => 1f - (remainingTime / bufferTime);

        public BufferSlot(float bufferTime = 0.2f)
        {
            this.bufferTime = bufferTime;
            this.remainingTime = 0f;
            this.isActive = false;
        }

        public void Activate()
        {
            isActive = true;
            remainingTime = bufferTime;
        }

        public void Consume()
        {
            isActive = false;
            remainingTime = 0f;
        }

        public void Interrupt()
        {
            isActive = false;
            remainingTime = 0f;
        }

        public void Update(float deltaTime)
        {
            if (isActive && remainingTime > 0f)
            {
                remainingTime -= deltaTime;
                if (remainingTime <= 0f)
                {
                    isActive = false;
                    remainingTime = 0f;
                }
            }
        }
    }
}
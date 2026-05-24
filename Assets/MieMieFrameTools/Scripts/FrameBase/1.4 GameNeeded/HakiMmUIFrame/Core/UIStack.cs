using UnityEngine;
using System.Collections.Generic;
using System;

namespace MieMieFrameWork.UI
{
    /// <summary>
    /// UI堆栈系统
    /// </summary>
    public class UIStack 
    {
        private Stack<UIDataBase> stack = new();

        /// <summary>
        /// 入栈
        /// </summary>
        /// <param name="ui">UI实例</param>
        public void PushUI(UIDataBase ui)
        {
            if (ui != null)
            {
                stack.Push(ui);
            }
        }

        /// <summary>
        /// 出栈
        /// </summary>
        /// <returns>UI实例</returns>
        public UIDataBase PopUI()
        {
            if (stack.Count > 0)
            {
                return stack.Pop();
            }
            return null;
        }

        /// <summary>
        /// 移除指定类型的UI
        /// </summary>
        /// <typeparam name="T">UI类型</typeparam>
        /// <returns>是否成功移除</returns>
        public bool RemoveUI<T>() where T : UIDataBase
        {
            if (stack.Count == 0) return false;

            // 临时存储，找到目标后重建堆栈
            var temp = new Stack<UIDataBase>();
            bool found = false;

            while (stack.Count > 0)
            {
                var ui = stack.Pop();
                if (ui is T && !found)
                    found = true; // 找到目标，不放回临时栈
                else
                    temp.Push(ui);
            }

            // 重建原堆栈
            while (temp.Count > 0)
                stack.Push(temp.Pop());

            return found;
        }

        /// <summary>
        /// 获取栈顶UI
        /// </summary>
        public UIDataBase GetTopUI()
        {
            if (stack.Count > 0)
            {
                return stack.Peek();
            }
            return null;
        }

        /// <summary>
        /// 清空堆栈
        /// </summary>
        public void ClearStack()
        {
            stack.Clear();
        }


    }


}
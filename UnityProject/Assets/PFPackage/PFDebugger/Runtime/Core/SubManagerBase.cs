using System;

namespace PFDebugger
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class SubManagerAttribute : Attribute
    {
        /// <summary>初始化优先级，数值越小越先 Init（越后 DeInit）</summary>
        public int Priority { get; }

        public SubManagerAttribute(int priority = 0)
        {
            Priority = priority;
        }
    }
    
    public abstract class SubManagerBase
    {
        /// <summary> 初始化 </summary>
        public virtual void Init() { }

        /// <summary> 所有 Manager Init 完成后调用 </summary>
        public virtual void PostInit() { }

        /// <summary> 每帧调用。</summary>
        public virtual void Tick(float elapseSeconds, float realElapseSeconds) { }

        /// <summary> 按优先级逆序调用。 </summary>
        public virtual void DeInit() { }
    }
}
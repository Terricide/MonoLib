#region Assembly mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089
// C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.7.2\mscorlib.dll
// Decompiled with ICSharpCode.Decompiler 5.0.2.5153
#endregion

using System.Threading;

namespace System
{
    public class EventArgs<T> : EventArgs
    {
        public EventArgs(T val)
        {
            this.Value = val;
        }
        public T Value { get; set; }
    }

    public class Progress<T> : IProgress<T>
    {
        private readonly SynchronizationContext m_synchronizationContext;

        private readonly Action<T> m_handler;

        private readonly SendOrPostCallback m_invokeHandlers;

        public event EventHandler<EventArgs<T>> ProgressChanged;

        public Progress()
        {
            m_synchronizationContext = SynchronizationContext.Current;
            if (m_synchronizationContext == null)
            {
                m_synchronizationContext = new SynchronizationContext();
            }
            m_invokeHandlers = InvokeHandlers;
        }

        public Progress(Action<T> handler)
            : this()
        {
            if (handler == null)
            {
                throw new ArgumentNullException("handler");
            }
            m_handler = handler;
        }

        protected virtual void OnReport(T value)
        {
            var handler = m_handler;
            var progressChanged = this.ProgressChanged;
            if (handler != null || progressChanged != null)
            {
                m_synchronizationContext.Post(m_invokeHandlers, value);
            }
        }

        void IProgress<T>.Report(T value)
        {
            OnReport(value);
        }

        private void InvokeHandlers(object state)
        {
            T val = (T)state;
            var handler = m_handler;
            var progressChanged = this.ProgressChanged;
            handler?.Invoke(val);
            progressChanged?.Invoke(this, new EventArgs<T>(val));
        }
    }
}

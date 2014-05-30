using System;

namespace MemBroker
{
    internal abstract class WeakActionBase
    {
        private WeakReference reference;

        public Func<object, bool> Filter { get; protected set; }
        
        protected WeakActionBase(object target)
        {
            reference = new WeakReference(target);
        }

        public bool IsAlive
        {
            get
            {
                var weakReference = reference;
                return weakReference != null && weakReference.IsAlive;
            }
        }

        public object Target
        {
            get
            {
                var weakReference = reference;
                return weakReference != null ? weakReference.Target : null;
            }
        }

        public void MarkForDeletion()
        {
            reference = null;
        }

        public abstract void Execute(object parameter);
    }
}
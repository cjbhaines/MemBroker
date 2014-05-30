using System;

namespace MemBroker
{
    internal abstract class WeakActionBase
    {
        protected WeakReference reference;

        public Func<object, bool> Filter { get; protected set; }
        
        protected WeakActionBase(object target)
        {
            reference = new WeakReference(target);
        }

        public bool IsAlive
        {
            get { return reference != null && reference.IsAlive; }
        }

        public object Target
        {
            get { return reference != null ? reference.Target : null; }
        }

        public void MarkForDeletion()
        {
            reference = null;
        }

        public abstract void Execute(object parameter);
    }
}
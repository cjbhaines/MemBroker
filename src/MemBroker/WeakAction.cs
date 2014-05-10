using System;

namespace MemBroker
{
    internal class WeakAction<T> : WeakActionBase
    {
        private readonly Action<T> action;
        public WeakAction(object target, Action<T> action, Func<T, bool> filter)
            : base(target)
        {
            this.action = action;
            Filter = x => filter((T) x);
        }

        public Action<T> Action
        {
            get { return action; }
        }

        public override void Execute(object obj)
        {
            var message = (T)obj;
            if (action != null && IsAlive)
            {
                action(message);
            }
        }
    }
}
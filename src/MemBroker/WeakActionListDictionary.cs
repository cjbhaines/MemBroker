using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MemBroker
{
    /// <summary>
    ///  The class is thread-safe insofar as it shouldn't throw exceptions if you're using it in a multi-threaded 
    ///  environment, but its behaviour under various race conditions, or pertaining to dirty reads is variable
    ///  (e.g. a listener cannot guarantee to receive no more messages after an Unregister call, although it can
    ///  be sure it will not receive any messages that were sent after its Unregister call completes).
    /// </summary>
    internal class WeakActionListDictionary
    {
        private readonly ConcurrentDictionary<Type, ISet<WeakActionBase>> dictionary = new ConcurrentDictionary<Type, ISet<WeakActionBase>>();

        public void Add<TMessage>(WeakAction<TMessage> action)
        {
            dictionary.AddOrUpdate(typeof(TMessage), t => new HashSet<WeakActionBase> { action }, (t, l) => { l.UnionWith(new[] { action }); return l; });
        }

        public IEnumerable<WeakActionBase> GetValue(Type typeToFind)
        {
            ISet<WeakActionBase> innerBag;
            if (dictionary.TryGetValue(typeToFind, out innerBag))
            {
                return innerBag.Select(wa => wa);
            }
            return new List<WeakActionBase>();
        }

        public IEnumerable<WeakAction<TMessage>> GetValue<TMessage>()
        {
            ISet<WeakActionBase> innerBag;
            if (dictionary.TryGetValue(typeof(TMessage), out innerBag))
            {
                return innerBag.Select(wa => (WeakAction<TMessage>)wa);
            }
            return new List<WeakAction<TMessage>>();
        }

        public void RemoveRecipient(object recipient)
        {
            foreach (var innerBag in dictionary.Values)
            {
                lock (innerBag)
                {
                    var weakActionsToRemove = innerBag.Where(wa => wa != null && wa.Target == recipient).ToArray();
                    foreach (var action in weakActionsToRemove)
                    {
                        innerBag.Remove(action);
                    }
                }
            }
        }

        public void Cleanup()
        {
            var keysToRemove = new List<Type>();
            foreach (var pair in dictionary.ToList())
            {
                lock (pair.Value)
                {
                    var deadSubscribers = pair.Value.Where(item => item == null || !item.IsAlive).ToList();
                    pair.Value.ExceptWith(deadSubscribers);
                    if (!pair.Value.Any())
                    {
                        keysToRemove.Add(pair.Key);
                    }
                }
            }
            ISet<WeakActionBase> removedValue;
            keysToRemove.ForEach(key => dictionary.TryRemove(key, out removedValue));
        }

        public void Clear()
        {
            dictionary.Clear();
        }
    }
}
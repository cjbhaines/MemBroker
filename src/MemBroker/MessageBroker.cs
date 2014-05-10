using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace MemBroker
{
    public class MessageBroker : IMessageBroker
    {
        private static readonly Dictionary<Type, IEnumerable<Type>> superTypes = new Dictionary<Type, IEnumerable<Type>>();
        private static readonly object superTypesLock = new object();

        private readonly WeakActionListDictionary weakActionsDictionary = new WeakActionListDictionary();

        /// <summary>
        ///  The class is thread-safe insofar as it shouldn't throw exceptions if you're using it in a multi-threaded 
        ///  environment, but its behaviour under various race conditions, or pertaining to dirty reads is variable
        ///  (e.g. a listener cannot guarantee to receive no more messages after an Unregister call, although it can
        ///  be sure it will not receive any messages that were sent after its Unregister call completes).
        /// </summary>
        private class WeakActionListDictionary
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
                        pair.Value.ExceptWith(pair.Value.Where(item => item == null || !item.IsAlive));
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

        public void Register<TMessage>(object recipient, Action<TMessage> action)
        {
            Register(recipient, action, message => true);
        }

        public void Register<TMessage>(object recipient, Action<TMessage> action, Func<TMessage, bool> filter)
        {
            var weakAction = new WeakAction<TMessage>(recipient, action, filter);
            weakActionsDictionary.Add(weakAction);
        }

        public void Send<TMessage>(IEnumerable<TMessage> messages)
        {
            messages.ToList().ForEach(Send);
        }

        public void Unregister(object recipient)
        {
            weakActionsDictionary.RemoveRecipient(recipient);
        }

        public void Unregister<TMessage>(object recipient, Action<TMessage> action)
        {
            weakActionsDictionary.GetValue<TMessage>().ToList().ForEach(weakAction =>
            {
                if (weakAction != null && weakAction.Target == recipient && weakAction.Action == action)
                {
                    weakAction.MarkForDeletion();
                }
            });
        }

        public void Send<TMessage>(TMessage message)
        {
            Type messageType = message.GetType();
            var types = GetSuperTypes(messageType);

            foreach (var typeToTest in types)
            {
                var weakActions = weakActionsDictionary.GetValue(typeToTest);
                weakActions.ToList().ForEach(weakAction =>
                {
                    if (weakAction != null && weakAction.IsAlive && weakAction.Target != null && weakAction.Filter(message))
                    {
                        weakAction.Execute(message);
                    }
                });
            }
        }

        private IEnumerable<Type> GetSuperTypes(Type typeToTest)
        {
            IEnumerable<Type> types;
            if (superTypes.TryGetValue(typeToTest, out types))
            {
                return types;
            }

            lock (superTypesLock)
            {
                if (superTypes.TryGetValue(typeToTest, out types))
                {
                    return types;
                }

                var superTypesFound = FindSuperTypes(typeToTest);
                superTypes.Add(typeToTest, superTypesFound);
                return superTypesFound;
            }
        }

        private List<Type> FindSuperTypes(Type typeToTest)
        {
            var interfaces = typeToTest.GetInterfaces();
            var baseClass = typeToTest.BaseType;
            var types = new List<Type>();

            if (baseClass != null)
            {
                types.AddRange(FindSuperTypes(baseClass));
            }

            foreach (var interfaceType in interfaces)
            {
                if (!types.Contains(interfaceType))
                {
                    types.Add(interfaceType);
                }
            }
            types.Add(typeToTest);
            return types;
        }

        public void Cleanup()
        {
            weakActionsDictionary.Cleanup();
        }

        public void Clear()
        {
            weakActionsDictionary.Clear();
        }
    }
}
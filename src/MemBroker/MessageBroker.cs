using System;
using System.Collections.Generic;
using System.Linq;

namespace MemBroker
{
    public class MessageBroker : IMessageBroker
    {
        private static readonly Dictionary<Type, IEnumerable<Type>> superTypes = new Dictionary<Type, IEnumerable<Type>>();
        private static readonly object superTypesLock = new object();
        
        private readonly WeakActionListDictionary weakActionsDictionary = new WeakActionListDictionary();
        private readonly object cleanupLock = new object();
        private readonly TimeSpan? cleanupInterval;
        private DateTime lastCleanupPerformed = DateTime.MinValue;

        public MessageBroker(TimeSpan? autoCleanupInterval = null)
        {
            cleanupInterval = autoCleanupInterval;
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

            CleanupIfRequired();
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
                // Check again just in case more than one thread made it through to waiting on the lock
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

        // TODO: How to test this?? I don't want to use reflection
        private void CleanupIfRequired()
        {
            if (cleanupInterval.HasValue && DateTime.UtcNow - lastCleanupPerformed > cleanupInterval.Value)
            {
                lock (cleanupLock)
                {
                    // Check again just in case more than one thread made it through to waiting on the lock
                    if (DateTime.UtcNow - lastCleanupPerformed > cleanupInterval.Value)
                    {
                        Cleanup();
                        lastCleanupPerformed = DateTime.UtcNow;
                    }
                }
            }
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
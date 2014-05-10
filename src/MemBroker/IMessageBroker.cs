using System;
using System.Collections.Generic;

namespace MemBroker
{
    public interface IMessageBroker
    {
        /// <summary>
        /// Register to a given message type
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to register to</typeparam>
        /// <param name="recipient">The object to which the message will be delivered. This reference keeps the registration alive</param>
        /// <param name="action">The callback to invoke upon receiving a message</param>
        void Register<TMessage>(object recipient, Action<TMessage> action);

        /// <summary>
        /// Register to a given message type with an additional filter
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to register to</typeparam>
        /// <param name="recipient">The object to which the message will be delivered. This reference keeps the registration alive</param>
        /// <param name="action">The callback to invoke upon receiving a message</param>
        /// <param name="filter">A Func to filter messages</param>
        void Register<TMessage>(object recipient, Action<TMessage> action, Func<TMessage, bool> filter);
        
        /// <summary>
        /// Send a message to all subscribers
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to send</typeparam>
        /// <param name="message">The message</param>
        void Send<TMessage>(TMessage message);

        /// <summary>
        /// Send a collection of messages to all subscribers
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to send</typeparam>
        /// <param name="messages">The collection of messages</param>
        void Send<TMessage>(IEnumerable<TMessage> messages);

        /// <summary>
        /// Unregister an entire recipient instance
        /// </summary>
        /// <param name="recipient">The recipient instance to unregister</param>
        void Unregister(object recipient);

        /// <summary>
        /// Unregister a specific registration
        /// </summary>
        /// <typeparam name="TMessage">The message type to uregister</typeparam>
        /// <param name="recipient">The recipient instance to unregister</param>
        /// <param name="action">The callback reference</param>
        void Unregister<TMessage>(object recipient, Action<TMessage> action);
        
        /// <summary>
        /// Remove any registrations for recipients that have been garbage collected
        /// </summary>
        void Cleanup();

        /// <summary>
        /// Remove all registrations
        /// </summary>
        void Clear();
    }
}
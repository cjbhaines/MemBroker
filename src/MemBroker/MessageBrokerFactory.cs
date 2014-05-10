namespace MemBroker
{
    public class MessageBrokerFactory : IMessageBrokerFactory
    {
        public IMessageBroker CreateMessageBroker()
        {
            return new MessageBroker();
        }
    }
}
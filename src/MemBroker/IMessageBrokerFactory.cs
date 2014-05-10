namespace MemBroker
{
    public interface IMessageBrokerFactory
    {
        IMessageBroker CreateMessageBroker();
    }
}
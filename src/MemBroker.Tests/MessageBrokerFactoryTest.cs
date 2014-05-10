using NUnit.Framework;

namespace MemBroker.Tests
{
    [TestFixture]
    public class MessageBrokerFactoryTest
    {
        [Test]
        public void Create()
        {
            // Arrange
            IMessageBrokerFactory messageBrokerFactory = new MessageBrokerFactory();

            // Act
            IMessageBroker messageBroker = messageBrokerFactory.CreateMessageBroker();

            // Assert
            Assert.That(messageBroker, Is.Not.Null);
            Assert.That(messageBroker, Is.TypeOf(typeof(MessageBroker)));
        }
    }
}

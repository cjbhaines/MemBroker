using System;
using System.Collections.Generic;
using MemBroker.Tests.Messages;
using NUnit.Framework;

namespace MemBroker.Tests
{
    [TestFixture]
    public class MessageBrokerTest
    {
        [Test]
        public void GivenIHaveRegisteredForAMessageType_WhenISendAMessage_ThenTheMessageIsReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true));

            // Act
            messageBroker.Send(new TestMessage());

            // Assert
            Assert.That(messageReceived, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageType_WhenIClearAllRegistrations_ThenNoMoreMessagesAreReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true));

            messageBroker.Clear();

            // Act
            messageBroker.Send(new TestMessage());

            // Assert
            Assert.That(messageReceived, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageType_WhenISendMultipleMessages_ThenTheMessagesAreReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            int messageCount = 0;
            messageBroker.Register(this, new Action<TestMessage>(message => messageCount++));

            IEnumerable<TestMessage> testMessages = new List<TestMessage>
            {
                new TestMessage(),
                new TestMessage(),
                new TestMessage(),
            };

            // Act
            messageBroker.Send(testMessages);

            // Assert
            Assert.That(messageCount, Is.EqualTo(3));
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageType_WhenIUnregisterForThatMessageType_ThenNoMoreMessagesAreReceived()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();

            bool actionExecuted = false;
            Action<TestMessage> testMessageAction = x => actionExecuted = true;
            messageBroker.Register(this, testMessageAction);

            var testMessage = new TestMessage();
            messageBroker.Send(testMessage);

            Assert.That(actionExecuted, Is.True);
            actionExecuted = false;

            // Act
            messageBroker.Unregister(this, testMessageAction);

            // Assert
            messageBroker.Send(testMessage);
            Assert.That(actionExecuted, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageType_WhenIUnregisterAnEntireSubscriber_ThenNoMoreMessagesAreReceived()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();
            bool actionExecuted = false;
            messageBroker.Register(this, new Action<TestMessage>(message => actionExecuted = true));

            var testMessages = new List<TestMessage>
            {
                new TestMessage(),
                new TestMessage(),
                new TestMessage(),
            };

            // Act
            messageBroker.Unregister(this);
            messageBroker.Send(testMessages);

            // Assert
            Assert.That(actionExecuted, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageBaseType_WhenISendADerivedTypeOfThatMessageType_ThenTheMessageIsReceived()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();
            bool wasCalled = false;
            messageBroker.Register(this, new Action<TestMessage>(messenger => wasCalled = true));

            // Act
            messageBroker.Send(new TestMessageSubclass());

            // Assert
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageInterfaceType_WhenISendAnImplementationOfThatInterface_ThenTheMessageIsReceived()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();
            bool wasCalled = false;
            messageBroker.Register(this, new Action<ITestInterface>(messenger => wasCalled = true));

            // Act
            messageBroker.Send(new TestInterfaceImplementation());

            // Assert
            Assert.That(wasCalled, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageSubType_WhenISendTheBaseType_ThenTheMessageIsNotReceived()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();
            bool wasCalled = false;
            messageBroker.Register(this, new Action<TestMessageSubclass>(messenger => wasCalled = true));

            // Act
            messageBroker.Send(new TestMessage());

            // Assert
            Assert.That(wasCalled, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageBaseTypeAndTheActualType_WhenISendTheDerivedTypeOfThatMessageType_ThenTheMessageIsReceivedTwice()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();
            bool subClassWasCalled = false;
            bool baseClassWasCalled = false;

            messageBroker.Register(this, new Action<TestMessageSubclass>(messenger => subClassWasCalled = true));
            messageBroker.Register(this, new Action<TestMessage>(messenger => baseClassWasCalled = true));

            // Act
            messageBroker.Send(new TestMessageSubclass());

            // Assert
            Assert.That(subClassWasCalled, Is.True);
            Assert.That(baseClassWasCalled, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageTypeWithAFalseFiler_WhenISendAMessage_ThenTheMessageIsNotReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true), x => false);

            // Act
            messageBroker.Send(new TestMessage());

            // Assert
            Assert.That(messageReceived, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageTypeWithATrueFiler_WhenISendAMessage_ThenTheMessageIsReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true), x => true);

            // Act
            messageBroker.Send(new TestMessage());

            // Assert
            Assert.That(messageReceived, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageTypeWithASpecificFiler_WhenISendAMessageThatMeetsTheFilterCriteria_ThenTheMessageIsReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true), x => x.Id == 1);

            // Act
            messageBroker.Send(new TestMessage { Id = 1 });

            // Assert
            Assert.That(messageReceived, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageBaseTypeWithASpecificFiler_WhenISendASubTypeMessageThatMeetsTheFilterCriteria_ThenTheMessageIsReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true), x => x.Id == 1);

            // Act
            messageBroker.Send(new TestMessageSubclass { Id = 1 });

            // Assert
            Assert.That(messageReceived, Is.True);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageTypeWithASpecificFiler_WhenISendAMessageThatDoesNotMeetTheFilterCriteria_ThenTheMessageIsNotReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true), x => x.Id == 1);

            // Act
            messageBroker.Send(new TestMessage { Id = 2 });

            // Assert
            Assert.That(messageReceived, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageBaseTypeWithASpecificFiler_WhenISendASubTypeMessageThatDoesNotMeetTheFilterCriteria_ThenTheMessageIsNotReceived()
        {
            // Arrange
            var messageBroker = new MessageBroker();
            bool messageReceived = false;
            messageBroker.Register(this, new Action<TestMessage>(message => messageReceived = true), x => x.Id == 1);

            // Act
            messageBroker.Send(new TestMessageSubclass { Id = 2 });

            // Assert
            Assert.That(messageReceived, Is.False);
        }

        [Test]
        public void GivenIHaveRegisteredForAMessageTypeAndIDestoryTheSubscriber_WhenISendAMessage_ThenTheMessageIsNotReceived()
        {
            // Arrange
            IMessageBroker messageBroker = new MessageBroker();
            bool actionExecuted = false;

            var subscriberObject = new object();
            messageBroker.Register(subscriberObject, new Action<TestMessage>(message => actionExecuted = true));

            subscriberObject = null;
            GC.Collect();

            // Act
            messageBroker.Send(new TestMessage());

            // Assert
            Assert.That(actionExecuted, Is.False);
        }

        [Test]
        public void GivenIHaveAnAutoCleanupMessageBroker_WhenISendMultipleMessages_ThenTheCleanupDoesNotThrow()
        {
            // Arrange
            var messageBroker = new MessageBroker(TimeSpan.FromMinutes(1));
            messageBroker.Register(this, new Action<TestMessage>(message => {}));

            // Act
            Assert.DoesNotThrow(() =>
            {
                messageBroker.Send(new TestMessage());
                messageBroker.Send(new TestMessage());
                messageBroker.Send(new TestMessage());
            });
        }
    }
}
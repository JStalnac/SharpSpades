using Moq;
using SharpSpades.Api.Events;
using SharpSpades.Api.Plugins;
using SharpSpades.Events;
using System.Threading.Tasks.Dataflow;
using Xunit;

namespace SharpSpades.Tests
{
    public class EventTests
    {
        private readonly EventManager eventManager;
        private readonly IPlugin plugin;

        public EventTests()
        {
            eventManager = new EventManager(TestHelpers.CreateLogger<EventManager>());
            plugin = Mock.Of<IPlugin>();
        }

        [Fact]
        public async Task Test_Register_EventAdded()
        {
            eventManager.Register<TestEvent>();

            bool eventFired = false;
            eventManager.Subscribe<TestEvent>(plugin, _ =>
            {
                eventFired = true;
                return Task.CompletedTask;
            });
            await eventManager.FireAsync(new TestEvent());
            Assert.True(eventFired);
        }

        [Fact]
        public void Test_Subscribe_EventDoesntExist()
        {
            eventManager.Subscribe<TestEvent>(plugin, _ => Task.CompletedTask);
        }

        [Fact]
        public async Task Test_Fire_CorrectOrder()
        {
            var block = new BufferBlock<int>();
            eventManager.Register<TestEvent>();

            eventManager.Subscribe<TestEvent>(plugin, _ =>
            {
                block.Post(1);
                return Task.CompletedTask;
            }, Priority.Normal);

            eventManager.Subscribe<TestEvent>(plugin, _ =>
            {
                block.Post(3);
                return Task.CompletedTask;
            }, Priority.Lower);

            eventManager.Subscribe<TestEvent>(plugin, _ =>
            {
                block.Post(2);
                return Task.CompletedTask;
            }, Priority.Higher);

            var ct = new CancellationToken(false);            
            await eventManager.FireAsync(new TestEvent());

            Assert.Equal(2, block.Receive());
            Assert.Equal(1, block.Receive());
            Assert.Equal(3, block.Receive());
        }

        [Fact]
        public async Task Test_Listener_Remove()
        {
            eventManager.Register<TestEvent>();
            bool called = false;

            eventManager.Subscribe<TestEvent>(plugin, _ =>
            {
                called = true;
                return Task.CompletedTask;
            });

            eventManager.RemovePlugin(plugin);

            await eventManager.FireAsync(new TestEvent());

            Assert.False(called);
        }

        [Fact]
        public async Task Test_Event_Remove()
        {
            eventManager.Register<TestEvent>();
            bool called = false;
            
            eventManager.Subscribe<TestEvent>(plugin, _ =>
            {
                called = true;
                return Task.CompletedTask;
            });

            // Fine
            eventManager.RemovePlugin(null);

            // This may start failing if the behaviour is changed
            await Assert.ThrowsAsync<InvalidOperationException>(async () =>
                await eventManager.FireAsync(new TestEvent()));
            
            Assert.False(called);
        }
    }

    class TestEvent : Event
    {

    }
}
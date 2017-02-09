using System;
using CQRSlite.Config;
using CQRSlite.Tests.Substitutes;
using Xunit;

namespace CQRSlite.Tests.Bus
{
    public class When_registering_handlers
    {
        private readonly BusRegistrar _register;
        private readonly TestServiceProvider _provider;

        public When_registering_handlers()
        {
            _provider = new TestServiceProvider();
            _register = new BusRegistrar(_provider, new TestHandleRegistrar());
            if (TestHandleRegistrar.HandlerList.Count == 0)
                _register.Register(GetType());
        }

        [Fact]
        public void Should_register_all_handlers()
        {
            Assert.Equal(3, TestHandleRegistrar.HandlerList.Count);
        }

        [Fact]
        public void Should_be_able_to_run_all_handlers()
        {
            foreach (var item in TestHandleRegistrar.HandlerList)
            {
                var @event = Activator.CreateInstance(item.Type);
                item.Handler(@event);
            }
            foreach (var handler in _provider.Handlers)
            {
                Assert.Equal(1, handler.TimesRun);
            }
        }
    }
}

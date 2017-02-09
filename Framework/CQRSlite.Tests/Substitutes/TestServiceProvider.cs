using System;
using System.Collections.Generic;

namespace CQRSlite.Tests.Substitutes
{
    public class TestServiceProvider : IServiceProvider
    {
        public readonly List<dynamic> Handlers = new List<dynamic>();
        public T GetService<T>()
        {
            return (T)GetService(typeof(T));
        }

        public object GetService(Type type)
        {
            if (type == typeof(TestAggregateDidSomethingHandler))
            {
                var handler = new TestAggregateDidSomethingHandler();
                Handlers.Add(handler);
                return handler;
            }
            if (type == typeof(TestAggregateDoSomethingHandler) || type == typeof(TestAggregateDoSomethingElseHandler))
            {
                var handler = new TestAggregateDoSomethingHandler();
                Handlers.Add(handler);
                return handler;
            }

            throw new ArgumentException($"I don't know how to resolve {type}");
        }
    }
}
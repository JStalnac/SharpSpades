using System;
using System.Collections.Concurrent;

namespace SharpSpades.Utils
{
    public sealed class ObjectPool<T> where T : class, new()
    {
        public static ObjectPool<T> Shared => shared ??= new ObjectPool<T>(() => new T());
        private static ObjectPool<T> shared;

        private readonly ConcurrentBag<T> pool = new();
        private readonly Func<T> initializer;
        
        public ObjectPool(Func<T> initializer)
        {
            this.initializer = initializer;
        }

        public T Get()
        {
            if (pool.TryTake(out T o))
                return o;
            return initializer();
        }

        public void Return(T obj)
        {
            pool.Add(obj);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SmithersDS4.Reading
{
    public class ObjectPool<T>
        where T : new()
    {
        List<T> _items = new List<T>();
        Queue<T> _pool = new Queue<T>();

        public ObjectPool(int capacity)
        {
            for (int i = 0; i < capacity; ++i)
            {
                T item = CreateItem();
                _items.Add(item);
                _pool.Enqueue(item);
            }
        }

        public int FreeSpace { get { return _pool.Count; } }

        public virtual T CreateItem()
        {
            return new T();
        }

        public IEnumerable<T> AllItems()
        {
            return _items;
        }

        public T ObjectFromPool()
        {
            lock (_pool)
            {
                return _pool.Dequeue();
            }
       }

        public void ReturnObjectToPool(T item)
        {
            lock (_pool)
            {
                _pool.Enqueue(item);
            }
        }
    }

    public class Handle<T> : IDisposable
        where T : new()
    {
        ObjectPool<T> _pool;
        int _refCount = 1;
        object _lock = new object();
        T _item;

        public T Item { get { return _item; } }

        public Handle(T item, ObjectPool<T> pool)
        {
            _item = item;
            _pool = pool;
        }

        public Handle<T> Clone()
        {
            lock (_lock)
            {
                _refCount += 1;
            }
            return this;
        }

        public void Dispose()
        {
            bool shouldReturnToPool;
            lock (_lock)
            {
                _refCount -= 1;
                shouldReturnToPool = _refCount == 0;
            }
            if (shouldReturnToPool)
            {
                _pool.ReturnObjectToPool(_item);
            }
        }
    }
}

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Library.Memory
{
    public class PacketMemoryPool<T> where T : new()
    {    
        private readonly ConcurrentBag<T> _pool;        

        public PacketMemoryPool()
        {
            _pool = new ConcurrentBag<T>();
        }

        public T Rent()
        {
            if (_pool.TryTake(out T buffer))
            {
                return buffer;
            }

            // 풀에 사용 가능한 버퍼가 없으면 새로운 버퍼를 생성합니다.
            return new T();
        }

        public void Return(T buffer)
        {            
            _pool.Add(buffer);
        }
    }
}

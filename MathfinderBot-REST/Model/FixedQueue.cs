using System.Collections.Concurrent;

namespace MathfinderBot
{
    public class FixedQueue<T> 
    {
        readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();
        public int Capacity { get; private set; }

        public FixedQueue(int capacity) =>
            Capacity = capacity;

       
        public new void Enqueue(T obj)
        {
            queue.Enqueue(obj);
            while (queue.Count > Capacity)
            {
                T overflow;
                queue.TryDequeue(out overflow);
            }
        }   
    }
}

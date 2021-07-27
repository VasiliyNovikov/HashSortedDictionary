using System.Collections.Generic;

namespace HashSortedDictionary.Benchmarks
{
    public class SortedBenchmarkingQueue<T> : IBenchmarkingQueue<T>
    {
        private readonly SortedDictionary<T, bool> _implementation = new();
        
        public void Enqueue(T key) => _implementation[key] = false;

        public T Dequeue()
        {
            T key;
            using (var keysEnumerator = _implementation.Keys.GetEnumerator())
            {
                keysEnumerator.MoveNext();
                key = keysEnumerator.Current;
            }
            _implementation.Remove(key!);
            return key;
        }
    }
}
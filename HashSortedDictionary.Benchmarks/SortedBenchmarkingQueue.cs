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
            using (var enumerator = _implementation.GetEnumerator())
            {
                enumerator.MoveNext();
                key = enumerator.Current.Key;
            }
            _implementation.Remove(key!);
            return key;
        }
    }
}
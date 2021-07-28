using System;

namespace HashSortedDictionary.Benchmarks
{
    public class HashSortedBenchmarkingQueue<T> : IBenchmarkingQueue<T>
    {
        private readonly HashSortedDictionary<T, bool> _implementation;

        public HashSortedBenchmarkingQueue(Func<T, int> sortedHash, byte bucketSizeBits) => _implementation = new(sortedHash, bucketSizeBits);

        public void Enqueue(T key) => _implementation.TryAdd(key, false);

        public T Dequeue()
        {
            _implementation.TryGetFirst(out var key, out _);
            _implementation.TryRemove(key, out _);
            return key;
        }
    }
}
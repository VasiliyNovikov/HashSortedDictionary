using System;
using BenchmarkDotNet.Attributes;

namespace HashSortedDictionary.Benchmarks
{
    [MemoryDiagnoser]
    public class QueueBenchmarks
    {
        //public static readonly IEnumerable<int> BucketSizes = new[] {4, 16, 64, 256}; 
        
        [Params(1000, 100000)]
        public int KeyRange { get; set; }
        
        [Params(100, 10000)]
        public int Size { get; set; }

        [Benchmark]
        [Arguments(BenchmarkingQueueType.Sorted, 0)]
        [Arguments(BenchmarkingQueueType.HashSorted, 4)]
        [Arguments(BenchmarkingQueueType.HashSorted, 32)]
        [Arguments(BenchmarkingQueueType.HashSorted, 256)]
        public void Enqueue_Dequeue_Benchmark(BenchmarkingQueueType type, int bucketSize)
        {
            IBenchmarkingQueue<int> queue = type switch
            {
                BenchmarkingQueueType.Sorted => new SortedBenchmarkingQueue<int>(),
                BenchmarkingQueueType.HashSorted => new HashSortedBenchmarkingQueue<int>(k => k, bucketSize),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var rnd = new Random(42);
            for (var i = 0; i < Size; ++i) 
                queue.Enqueue(rnd.Next(KeyRange));

            for (var i = 0; i < Size * 100; ++i)
            {
                queue.Dequeue();
                queue.Enqueue(rnd.Next(KeyRange));
            }
        }
    }
}
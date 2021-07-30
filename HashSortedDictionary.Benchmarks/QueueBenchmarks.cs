using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;

namespace HashSortedDictionary.Benchmarks
{
    [MemoryDiagnoser]
    public class QueueBenchmarks
    {
        public static IEnumerable<object[]> Queues() => new[]
        {
            new object[] {BenchmarkingQueueType.Sorted, null},
            new object[] {BenchmarkingQueueType.HashSorted, 2},
            new object[] {BenchmarkingQueueType.HashSorted, 4},
            new object[] {BenchmarkingQueueType.HashSorted, 6},
            new object[] {BenchmarkingQueueType.HashSorted, 8}
        };

        [Params(1000, 100000)]
        public int KeyRange { get; set; }

        [Params(100, 10000)]
        public int Size { get; set; }

        [Benchmark]
        [ArgumentsSource(nameof(Queues))]
        public void Enqueue_Dequeue_Benchmark(BenchmarkingQueueType type, int? bucketSizeBits)
        {
            IBenchmarkingQueue<int> queue = type switch
            {
                BenchmarkingQueueType.Sorted => new SortedBenchmarkingQueue<int>(),
                BenchmarkingQueueType.HashSorted => new HashSortedBenchmarkingQueue<int>(k => (uint)k, (byte)bucketSizeBits!.Value),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var rnd = new Random(42);
            for (var i = 0; i < Size; ++i) 
                queue.Enqueue(rnd.Next(KeyRange));

            for (var i = 0; i < Size * 20; ++i)
            {
                queue.Dequeue();
                queue.Enqueue(rnd.Next(KeyRange));
            }
        }
    }
}
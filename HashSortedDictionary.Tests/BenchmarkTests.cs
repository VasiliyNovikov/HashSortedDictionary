using HashSortedDictionary.Benchmarks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HashSortedDictionary.Tests
{
    [TestClass]
    public class BenchmarkTests
    {
        [TestMethod]
        public void Test_Benchmark()
        {
            var bench = new QueueBenchmarks {Size = 100, KeyRange = 1000};
            bench.Enqueue_Dequeue_Benchmark(BenchmarkingQueueType.HashSorted, 2);
        }
    }
}
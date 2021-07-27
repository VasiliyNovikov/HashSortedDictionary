using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HashSortedDictionary.Tests
{
    [TestClass]
    public class HashSortedDictionaryTests
    {
        private const int TestItemCount = 10_000;

        protected static IEnumerable<object[]> Test_Bucket_Sizes() => new[]
        {
            new object[] {2},
            new object[] {3},
            new object[] {4},
            new object[] {7},
            new object[] {10},
            new object[] {11},
            new object[] {16},
            new object[] {32},
            new object[] {128},
            new object[] {1000},
            new object[] {1001}
        };

        [TestMethod]
        [DynamicData(nameof(Test_Bucket_Sizes), DynamicDataSourceType.Method)]
        public void HashSortedDictionary_Add_Remove_Test(int bucketSize)
        {
            var dict = new HashSortedDictionary<int, int>(k => k, bucketSize);
            Assert.AreEqual(0, dict.Count);
            var rnd = new Random(24);
            var testKeys = Enumerable.Range(0, TestItemCount * 10)
                                     .Select(_ => rnd.Next(100_000))
                                     .Distinct()
                                     .Take(TestItemCount)
                                     .ToList();
            for (var i = 0; i < testKeys.Count; ++i)
            {
                var key = testKeys[i];
                Assert.IsFalse(dict.TryGet(key, out var value));
                Assert.IsTrue(dict.TryAdd(key, key + 42));
                Assert.IsFalse(dict.TryAdd(key, -1));
                Assert.IsTrue(dict.TryGet(key, out value));
                Assert.AreEqual(key + 42, value);
                Assert.AreEqual(i + 1, dict.Count);
            }

            for (var i = 0; i < testKeys.Count; ++i)
            {
                var key = testKeys[i];
                Assert.IsTrue(dict.TryRemove(key, out var value));
                Assert.AreEqual(key + 42, value);
                Assert.IsFalse(dict.TryGet(key, out value));
                Assert.AreEqual(testKeys.Count - i - 1, dict.Count);
            }
        }

        [TestMethod]
        [DynamicData(nameof(Test_Bucket_Sizes), DynamicDataSourceType.Method)]
        public void HashSortedDictionary_GetFirst_Test(int bucketSize)
        {
            var dict = new HashSortedDictionary<int, int>(k => k, bucketSize);
            Assert.AreEqual(0, dict.Count);
            var rnd = new Random(24);
            var testKeys = Enumerable.Range(0, TestItemCount * 10)
                .Select(_ => rnd.Next(100_000))
                .Distinct()
                .Take(TestItemCount)
                .ToList();

            foreach (var key in testKeys) 
                Assert.IsTrue(dict.TryAdd(key, key + 42));

            foreach (var expectedKey in testKeys.OrderBy(k => k))
            {
                Assert.IsTrue(dict.TryGetFirst(out var key, out var value));
                Assert.AreEqual(expectedKey, key);
                Assert.AreEqual(key + 42, value);
                Assert.IsTrue(dict.TryRemove(key, out value));
            }

            Assert.AreEqual(0, dict.Count);
        }
    }
}
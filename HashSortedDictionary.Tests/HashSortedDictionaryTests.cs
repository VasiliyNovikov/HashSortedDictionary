using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace HashSortedDictionary.Tests
{
    [TestClass]
    public class HashSortedDictionaryTests
    {
        private const int TestKeyRange = 100_000_000;
        private const int TestItemCount = 100_000;

        private static uint TestHash(int key) => (uint)key / 10;

        protected static IEnumerable<object[]> Test_Bucket_Sizes() => new byte[] {1, 2, 3, 4, 5, 7, 8, 10, 11, 12, 13, 14, 15, 16, 17}.Select(bsb => new object[] {bsb});

        [TestMethod]
        [DynamicData(nameof(Test_Bucket_Sizes), DynamicDataSourceType.Method)]
        public void HashSortedDictionary_Add_Remove_Test(byte bucketSizeBits)
        {
            var dict = new HashSortedDictionary<int, int>(TestHash, bucketSizeBits);
            Assert.AreEqual(0, dict.Count);
            var rnd = new Random(24);
            var testKeys = Enumerable.Range(0, TestItemCount * 10)
                                     .Select(_ => rnd.Next(TestKeyRange))
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
        public void HashSortedDictionary_GetFirst_Test(byte bucketSizeBits)
        {
            var dict = new HashSortedDictionary<int, int>(TestHash, bucketSizeBits);
            Assert.AreEqual(0, dict.Count);
            var rnd = new Random(24);
            var testKeys = Enumerable.Range(0, TestItemCount * 10)
                                     .Select(_ => rnd.Next(TestKeyRange))
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
#nullable enable
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HashSortedDictionary
{
    public class HashSortedDictionary<TKey, TValue>
    {
        private static readonly IComparer<TKey> KeyComparer = Comparer<TKey>.Default;
        
        private readonly Func<TKey, int> _sortedHash;
        private readonly int _bucketSize;
        private IHierarchicalBucket? _root;
        private int _firstHash;
        private int _capacity;

        public int Count { get; private set; }

        public HashSortedDictionary(Func<TKey, int> sortedHash, int bucketSize)
        {
            _sortedHash = sortedHash ?? throw new ArgumentNullException(nameof(sortedHash));
            _bucketSize = bucketSize;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            var hash = _sortedHash(key);
            if (_root == null)
            {
                _root = new LeafBucket(_bucketSize);
                _capacity = _bucketSize;
                _firstHash = hash;
            }
            else
            {
                while (hash >= _firstHash + _capacity)
                {
                    _capacity *= _bucketSize;
                    _root = new BranchBucket(_bucketSize, _root.Level + 1, _capacity, _root);
                }

                while (hash < _firstHash)
                {
                    _firstHash += _capacity;
                    _capacity *= _bucketSize;
                    _firstHash -= _capacity;
                    _root = new BranchBucket(_bucketSize, _root.Level + 1, _capacity, null, _root);
                }
            }
 
            if (!_root.TryAdd(hash - _firstHash, key, value))
                return false;
            ++Count;
            return true;

        }

        public bool TryRemove(TKey key, [NotNullWhen(true)]out TValue? value)
        {
            if (_root != null)
            {
                var index = _sortedHash(key) - _firstHash;
                if (index >= 0 && index < _capacity && _root.TryRemove(index, key, out value))
                {
                    if (_root.BucketCount == 0)
                    {
                        _root = null;
                        _capacity = 0;
                    }
                    else
                        while (_root!.BucketCount == 1 && _root.Level > 0)
                        {
                            _root.TryGetFirstBucket(out _root, out var entryIndex);
                            _capacity /= _bucketSize;
                            _firstHash += _capacity * entryIndex;
                        }
                    --Count;
                    return true;
                }
            }
            value = default;
            return false;
        }

        public bool TryGet(TKey key, [NotNullWhen(true)]out TValue? value)
        {
            if (_root != null)
            {
                var index = _sortedHash(key) - _firstHash;
                if (index >= 0 && index < _capacity)
                    return _root.TryGet(index, key, out value);
            }

            value = default;
            return false;
        }

        public bool TryGetFirst([NotNullWhen(true)]out TKey? key, [NotNullWhen(true)]out TValue? value)
        {
            if (_root != null)
                return _root.TryGetFirst(out key, out value);
            key = default;
            value = default;
            return false;
        }

        private interface IHierarchicalBucket
        {
            int Level { get; }
            int BucketCount { get; }

            bool TryAdd(int index, TKey key, TValue value);
            bool TryGet(int index, TKey key, [NotNullWhen(true)] out TValue? value);
            bool TryRemove(int index, TKey key, [NotNullWhen(true)] out TValue? value);
            bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out int bucketIndex);
            bool TryGetFirst([NotNullWhen(true)] out TKey? key, [NotNullWhen(true)] out TValue? value);
        }

        private class Bucket<TEntry> where TEntry : class
        {
            protected readonly int BucketSize;
            protected readonly TEntry?[] Entries;

            protected Bucket(int bucketSize)
            {
                BucketSize = bucketSize;
                Entries = new TEntry?[bucketSize];
            }
        }

        private class LeafBucket : Bucket<List<KeyValuePair<TKey, TValue>>>, IHierarchicalBucket
        {
            public int Level => 0;
            public int BucketCount { get; private set; }
            
            public LeafBucket(int bucketSize) : base(bucketSize)
            {
            }

            public bool TryAdd(int index, TKey key, TValue value)
            {
                var entry = Entries[index];
                if (entry == null)
                    Entries[index] = entry = new List<KeyValuePair<TKey, TValue>>();
                else
                    foreach (var keyValuePair in entry)
                        if (KeyComparer.Compare(key, keyValuePair.Key) == 0)
                            return false;
                entry.Add(new(key, value));
                ++BucketCount;
                return true;
            }

            public bool TryGet(int index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index];
                if (entry != null)
                    foreach (var keyValuePair in entry)
                        if (KeyComparer.Compare(key, keyValuePair.Key) == 0)
                        {
                            value = keyValuePair.Value!;
                            return true;
                        }
                value = default;
                return false;
            }

            public bool TryRemove(int index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index];
                if (entry != null)
                    for (var i = 0; i < entry.Count; i++)
                    {
                        var keyValuePair = entry[i];
                        if (KeyComparer.Compare(key, keyValuePair.Key) == 0)
                        {
                            value = keyValuePair.Value!;
                            entry.RemoveAt(i);
                            if (entry.Count == 0)
                                Entries[index] = null;
                            --BucketCount;
                            return true;
                        }
                    }
                value = default;
                return false;
            }

            public bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out int bucketIndex)
            {
                bucket = null;
                bucketIndex = -1;
                return false;
            }

            public bool TryGetFirst([NotNullWhen(true)] out TKey? key, [NotNullWhen(true)] out TValue? value)
            {
                if (BucketCount != 0)
                {
                    foreach (var entry in Entries)
                        if (entry != null)
                        {
                            var pair = entry[0];
                            key = pair.Key!;
                            value = pair.Value!;
                            return true;
                        }
                }

                key = default;
                value = default;
                return false;
            }
        }

        private class BranchBucket : Bucket<IHierarchicalBucket>, IHierarchicalBucket
        {
            private readonly int _entryCapacity;
            public int Level { get; }
            public int BucketCount { get; private set; }

            public BranchBucket(int bucketSize, int level, int capacity, IHierarchicalBucket? firstChild = null, IHierarchicalBucket? lastChild = null)
                : base(bucketSize)
            {
                Level = level;
                _entryCapacity = capacity / BucketSize;
                Entries[0] = firstChild;
                Entries[^1] = lastChild;
                BucketCount = (firstChild == null ? 0 : 1) + (lastChild == null ? 0 : 1);
            }

            public bool TryAdd(int index, TKey key, TValue value)
            {
                var entryIndex = index / _entryCapacity;
                var entry = Entries[entryIndex];
                if (entry == null)
                {
                    entry = Level == 0 ? new LeafBucket(BucketSize) : new BranchBucket(BucketSize, Level - 1, _entryCapacity);
                    Entries[entryIndex] = entry;
                    ++BucketCount;
                }

                return entry.TryAdd(index % _entryCapacity, key, value);
            }

            public bool TryGet(int index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index / _entryCapacity];
                if (entry != null)
                    return entry.TryGet(index % _entryCapacity, key, out value);
                value = default;
                return false;
            }

            public bool TryRemove(int index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entryIndex = index / _entryCapacity;
                var entry = Entries[entryIndex];
                if (entry != null && entry.TryRemove(index % _entryCapacity, key, out value))
                {
                    if (entry.BucketCount == 0)
                    {
                        --BucketCount;
                        Entries[entryIndex] = null;
                    }
                    return true;
                }
                value = default;
                return false;
            }

            public bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out int bucketIndex)
            {
                if (BucketCount != 0)
                    for (var i = 0; i < Entries.Length; ++i)
                    {
                        var entry = Entries[i];
                        if (entry != null)
                        {
                            bucket = entry;
                            bucketIndex = i;
                            return true;
                        }
                    }

                bucket = null;
                bucketIndex = -1;
                return false;
            }

            public bool TryGetFirst([NotNullWhen(true)] out TKey? key, [NotNullWhen(true)] out TValue? value)
            {
                if (TryGetFirstBucket(out var entry, out _))
                    return entry.TryGetFirst(out key, out value);
                key = default;
                value = default;
                return false;
            }
        }
    }
}
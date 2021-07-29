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
        private readonly byte _bucketSizeBits;
        private IHierarchicalBucket? _root;
        private long _firstHash;
        private long _capacity;
        private byte _capacityBits;

        public int Count { get; private set; }

        public HashSortedDictionary(Func<TKey, int> sortedHash, byte bucketSizeBits)
        {
            _sortedHash = sortedHash ?? throw new ArgumentNullException(nameof(sortedHash));
            _bucketSizeBits = bucketSizeBits;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            var hash = _sortedHash(key);
            if (_root == null)
            {
                _root = new LeafBucket(_bucketSizeBits);
                _capacityBits = _bucketSizeBits;
                _capacity = 1L << _capacityBits;
                _firstHash = (hash >> _capacityBits) << _capacityBits;
            }
            else
            {
                while (hash >= _firstHash + _capacity || hash < _firstHash)
                {
                    _capacityBits += _bucketSizeBits;
                    _capacity = 1L << _capacityBits;
                    var prevFirstHash = _firstHash;
                    _firstHash = (prevFirstHash >> _capacityBits) << _capacityBits;
                    _root = new BranchBucket(_bucketSizeBits, _root.Level + 1, _capacityBits, _root, prevFirstHash - _firstHash);
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
                        _capacityBits = 0;
                        _capacity = 0;
                    }
                    else
                        while (_root!.BucketCount == 1 && _root.Level > 0)
                        {
                            _root.TryGetFirstBucket(out _root, out var entryIndex);
                            _capacityBits -= _bucketSizeBits;
                            _capacity = 1L << _capacityBits;
                            _firstHash += (long)entryIndex << _capacityBits;
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

            bool TryAdd(long index, TKey key, TValue value);
            bool TryGet(long index, TKey key, [NotNullWhen(true)] out TValue? value);
            bool TryRemove(long index, TKey key, [NotNullWhen(true)] out TValue? value);
            bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out int bucketIndex);
            bool TryGetFirst([NotNullWhen(true)] out TKey? key, [NotNullWhen(true)] out TValue? value);
        }

        private class Bucket<TEntry> where TEntry : class
        {
            protected readonly TEntry?[] Entries;
            protected readonly byte BucketSizeBits;

            protected Bucket(byte bucketSizeBits)
            {
                BucketSizeBits = bucketSizeBits;
                Entries = new TEntry?[1 << bucketSizeBits];
            }
        }

        private class LeafEntry
        {
            public TKey Key { get; }
            public TValue Value { get; }
            public LeafEntry? Next { get; set; }

            public LeafEntry(TKey key, TValue value, LeafEntry? next)
            {
                Key = key;
                Value = value;
                Next = next;
            }
        }

        private class LeafBucket : Bucket<LeafEntry>, IHierarchicalBucket
        {
            public int Level => 0;
            public int BucketCount { get; private set; }
            
            public LeafBucket(byte bucketSizeBits) : base(bucketSizeBits)
            {
            }

            public bool TryAdd(long index, TKey key, TValue value)
            {
                var firstEntry = Entries[index];
                var entry = firstEntry;
                while (entry != null)
                {
                    if (KeyComparer.Compare(key, entry.Key) == 0)
                        return false;
                    entry = entry.Next;
                }
                Entries[index] = new LeafEntry(key, value, firstEntry);
                ++BucketCount;
                return true;
            }

            public bool TryGet(long index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index];
                while (entry != null)
                {
                    if (KeyComparer.Compare(key, entry.Key) == 0)
                    {
                        value = entry.Value!;
                        return true;
                    }
                    entry = entry.Next;
                }
                value = default;
                return false;
            }

            public bool TryRemove(long index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index];
                LeafEntry? prevEntry = null;
                while (entry != null)
                {
                    if (KeyComparer.Compare(key, entry.Key) == 0)
                    {
                        value = entry.Value!;
                        if (prevEntry == null) 
                            Entries[index] = entry.Next;
                        else
                            prevEntry.Next = entry.Next;
                        --BucketCount;
                        return true;
                    }
                    prevEntry = entry;
                    entry = entry.Next;
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
                    foreach (var firstEntry in Entries)
                    {
                        if (firstEntry == null)
                            continue;

                        var entry = firstEntry;
                        key = entry.Key!;
                        value = entry.Value!;
                        entry = entry.Next;
                        while (entry != null)
                        {
                            if (KeyComparer.Compare(entry.Key, key) < 0)
                            {
                                key = entry.Key!;
                                value = entry.Value!;
                            }
                            entry = entry.Next;
                        }
                        return true;
                    }
                key = default;
                value = default;
                return false;
            }
        }

        private class BranchBucket : Bucket<IHierarchicalBucket>, IHierarchicalBucket
        {
            private readonly byte _entryCapacityBits;
            private readonly long _entryIndexMask;
            
            public int Level { get; }
            public int BucketCount { get; private set; }

            public BranchBucket(byte bucketSizeBits, int level, byte capacityBits, IHierarchicalBucket? child = null, long childIndex = 0)
                : base(bucketSizeBits)
            {
                Level = level;
                _entryCapacityBits = (byte)(capacityBits - BucketSizeBits);
                _entryIndexMask = (1L << _entryCapacityBits) - 1L;
                if (child == null)
                    return;
                Entries[childIndex >> _entryCapacityBits] = child;
                BucketCount = 1;
            }

            public bool TryAdd(long index, TKey key, TValue value)
            {
                var entryIndex = index >> _entryCapacityBits;
                var entry = Entries[entryIndex];
                if (entry == null)
                {
                    entry = Level == 1 ? new LeafBucket(BucketSizeBits) : new BranchBucket(BucketSizeBits, Level - 1, _entryCapacityBits);
                    Entries[entryIndex] = entry;
                    ++BucketCount;
                }

                return entry.TryAdd(index & _entryIndexMask, key, value);
            }

            public bool TryGet(long index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index >> _entryCapacityBits];
                if (entry != null)
                    return entry.TryGet(index & _entryIndexMask, key, out value);
                value = default;
                return false;
            }

            public bool TryRemove(long index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entryIndex = index >> _entryCapacityBits;
                var entry = Entries[entryIndex];
                if (entry != null && entry.TryRemove(index & _entryIndexMask, key, out value))
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
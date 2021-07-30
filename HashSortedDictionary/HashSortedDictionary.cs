using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace HashSortedDictionary
{
    public class HashSortedDictionary<TKey, TValue>
    {
        private static readonly IComparer<TKey> KeyComparer = Comparer<TKey>.Default;

        private readonly Func<TKey, uint> _sortedHash;
        private readonly byte _bucketSizeBits;
        private Constants.BucketLevelInformation? _rootLevel;
        private IHierarchicalBucket? _root;
        private uint _firstHash;

        public int Count { get; private set; }

        public HashSortedDictionary(Func<TKey, uint> sortedHash, byte bucketSizeBits)
        {
            if (bucketSizeBits is 0 or > Constants.MaxBucketSizeBits)
                throw new ArgumentOutOfRangeException(nameof(bucketSizeBits));
            _sortedHash = sortedHash ?? throw new ArgumentNullException(nameof(sortedHash));
            _bucketSizeBits = bucketSizeBits;
        }

        public bool TryAdd(TKey key, TValue value)
        {
            var hash = _sortedHash(key);
            if (_root == null)
            {
                _rootLevel = Constants.Levels[_bucketSizeBits];
                _root = new LeafBucket(_rootLevel);
                _firstHash = hash & _rootLevel.HashAlignmentMask;
            }
            else
            {
                while (hash > _firstHash + _rootLevel!.ActualMaxIndex || hash < _firstHash)
                {
                    _rootLevel = _rootLevel.Next!;
                    var prevFirstHash = _firstHash;
                    _firstHash = prevFirstHash & _rootLevel.HashAlignmentMask;
                    _root = new BranchBucket(_rootLevel, _root, prevFirstHash - _firstHash);
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
                var hash = _sortedHash(key);
                if (hash >= _firstHash)
                {
                    var index = hash - _firstHash;
                    if (index <= _rootLevel!.ActualMaxIndex && _root.TryRemove(index, key, out value))
                    {
                        if (_root.BucketCount == 0)
                        {
                            _rootLevel = null;
                            _root = null;
                        }
                        else
                            while (_root!.BucketCount == 1 && _rootLevel!.Level > 0)
                            {
                                _rootLevel = _rootLevel.Prev!;
                                _root.TryGetFirstBucket(out _root, out var entryIndex);
                                _firstHash += entryIndex << _rootLevel.ActualCapacityBits;
                            }
                        --Count;
                        return true;
                    }
                }
            }
            value = default;
            return false;
        }

        public bool TryGet(TKey key, [NotNullWhen(true)]out TValue? value)
        {
            if (_root != null)
            {
                var hash = _sortedHash(key);
                if (hash >= _firstHash)
                {
                    var index = _sortedHash(key) - _firstHash;
                    if (index <= _rootLevel!.ActualMaxIndex)
                        return _root.TryGet(index, key, out value);
                }
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
            int BucketCount { get; }

            bool TryAdd(uint index, TKey key, TValue value);
            bool TryGet(uint index, TKey key, [NotNullWhen(true)] out TValue? value);
            bool TryRemove(uint index, TKey key, [NotNullWhen(true)] out TValue? value);
            bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out uint bucketIndex);
            bool TryGetFirst([NotNullWhen(true)] out TKey? key, [NotNullWhen(true)] out TValue? value);
        }

        private class Bucket<TEntry> where TEntry : class
        {
            protected readonly Constants.BucketLevelInformation Level;
            protected readonly TEntry?[] Entries;

            public int BucketCount { get; protected set; }

            protected Bucket(Constants.BucketLevelInformation level)
            {
                Level = level;
                Entries = new TEntry?[Level.ActualBucketSize];
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
            public LeafBucket(Constants.BucketLevelInformation level) : base(level)
            {
            }

            public bool TryAdd(uint index, TKey key, TValue value)
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

            public bool TryGet(uint index, TKey key, [NotNullWhen(true)] out TValue? value)
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

            public bool TryRemove(uint index, TKey key, [NotNullWhen(true)] out TValue? value)
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

            public bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out uint bucketIndex)
            {
                bucket = null;
                bucketIndex = 0;
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
            public BranchBucket(Constants.BucketLevelInformation level, IHierarchicalBucket? child = null, uint childIndex = 0)
                : base(level)
            {
                if (child == null)
                    return;
                Entries[childIndex >> Level.EntryCapacityBits] = child;
                BucketCount = 1;
            }

            public bool TryAdd(uint index, TKey key, TValue value)
            {
                var entryIndex = index >> Level.EntryCapacityBits;
                var entry = Entries[entryIndex];
                if (entry == null)
                {
                    var childLevel = Level.Prev!;
                    entry = Level.Level == 1 ? new LeafBucket(childLevel) : new BranchBucket(childLevel);
                    Entries[entryIndex] = entry;
                    ++BucketCount;
                }

                return entry.TryAdd(index & Level.EntryIndexMask, key, value);
            }

            public bool TryGet(uint index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entry = Entries[index >> Level.EntryCapacityBits];
                if (entry != null)
                    return entry.TryGet(index & Level.EntryIndexMask, key, out value);
                value = default;
                return false;
            }

            public bool TryRemove(uint index, TKey key, [NotNullWhen(true)] out TValue? value)
            {
                var entryIndex = index >> Level.EntryCapacityBits;
                var entry = Entries[entryIndex];
                if (entry != null && entry.TryRemove(index & Level.EntryIndexMask, key, out value))
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

            public bool TryGetFirstBucket([NotNullWhen(true)] out IHierarchicalBucket? bucket, out uint bucketIndex)
            {
                if (BucketCount != 0)
                    for (var i = 0U; i < Entries.Length; ++i)
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
                bucketIndex = 0;
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
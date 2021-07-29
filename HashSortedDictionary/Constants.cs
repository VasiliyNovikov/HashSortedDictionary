namespace HashSortedDictionary
{
    internal static class Constants
    {
        public const byte MaxBucketSizeBits = 26;
        private const byte MaxCapacityBits = 31;

        public static readonly BucketLevelInformation[] Levels = GenerateLevels();

        private static BucketLevelInformation[] GenerateLevels()
        {
            var levels = new BucketLevelInformation[MaxBucketSizeBits + 1];
            for (byte bucketSizeBits = 1; bucketSizeBits <= MaxBucketSizeBits; ++bucketSizeBits)
                levels[bucketSizeBits] = new BucketLevelInformation(0, bucketSizeBits);
            return levels;
        }

        public class BucketLevelInformation
        {
            public byte Level { get; }
            public byte BaseBucketSizeBits { get; }
            public byte ActualBucketSizeBits { get; }
            public byte ActualCapacityBits { get; }
            public byte EntryCapacityBits { get; }
            public uint ActualBucketSize { get; }
            public uint ActualCapacity { get; }
            public uint EntryIndexMask { get; }
            public uint HashAlignmentMask { get; }
            
            public BucketLevelInformation? Prev { get; }
            public BucketLevelInformation? Next { get; }

            public BucketLevelInformation(byte level, byte baseBucketSizeBits, BucketLevelInformation? prev = null)
            {
                Level = level;
                BaseBucketSizeBits = baseBucketSizeBits;
                ActualBucketSizeBits = baseBucketSizeBits;
                ActualCapacityBits = (byte)(baseBucketSizeBits * (level + 1));
                if (ActualCapacityBits > MaxCapacityBits)
                {
                    ActualBucketSizeBits -= (byte)(ActualCapacityBits - MaxCapacityBits); 
                    ActualCapacityBits = MaxCapacityBits;
                }
                EntryCapacityBits = (byte)(ActualCapacityBits - ActualBucketSizeBits);

                ActualBucketSize = 1U << ActualBucketSizeBits;
                ActualCapacity = 1U << ActualCapacityBits;

                EntryIndexMask = (1U << EntryCapacityBits) - 1;
                HashAlignmentMask = ~(ActualCapacity - 1);

                Prev = prev;
                var levelCount = (MaxCapacityBits + baseBucketSizeBits - 1) / baseBucketSizeBits;
                var nextLevel = (byte)(level + 1);
                if (nextLevel < levelCount) 
                    Next = new BucketLevelInformation(nextLevel, baseBucketSizeBits, this);
            }
        }
    }
}
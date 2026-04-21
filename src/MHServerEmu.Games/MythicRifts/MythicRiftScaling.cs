namespace MHServerEmu.Games.MythicRifts
{
    public static class MythicRiftScaling
    {
        private const double MythicRiftLevelToD3EquivalentFactor = 0.40d;
        private const float D3SoloGreaterRiftHealthModifier = 0.625f;
        private static readonly float[] D3GreaterRiftHealthModifiersByBucket =
        {
            0.625f,
            1.25f,
            1.875f,
            2.5f
        };

        public static int GetEffectivePlayerCount(int requestedPlayerCount)
        {
            if (requestedPlayerCount <= 1)
                return 1;

            if (requestedPlayerCount >= 4)
                return 4;

            return requestedPlayerCount;
        }

        public static float GetEquivalentD3RiftLevel(int riftLevel)
        {
            int normalizedLevel = Math.Max(riftLevel, 1);
            return (float)(1d + ((normalizedLevel - 1d) * MythicRiftLevelToD3EquivalentFactor));
        }

        public static float GetGroupHealthMultiplier(int requestedPlayerCount)
        {
            int effectivePlayerCount = GetEffectivePlayerCount(requestedPlayerCount);
            float d3GroupModifier = D3GreaterRiftHealthModifiersByBucket[effectivePlayerCount - 1];
            return d3GroupModifier / D3SoloGreaterRiftHealthModifier;
        }

        public static float GetHealthMultiplier(int riftLevel)
        {
            double tiersAboveBase = Math.Max(GetEquivalentD3RiftLevel(riftLevel) - 1d, 0d);
            return (float)Math.Pow(1.17d, tiersAboveBase);
        }

        public static float GetDamageMultiplier(int riftLevel)
        {
            double tiersAboveBase = Math.Max(GetEquivalentD3RiftLevel(riftLevel) - 1d, 0d);
            double earlyLevels = Math.Min(tiersAboveBase, 25d);
            double midLevels = Math.Min(Math.Max(tiersAboveBase - 25d, 0d), 45d);
            double lateLevels = Math.Max(tiersAboveBase - 70d, 0d);

            double multiplier = Math.Pow(1.13d, earlyLevels)
                * Math.Pow(1.07d, midLevels)
                * Math.Pow(1.02d, lateLevels);

            return (float)multiplier;
        }

        public static MythicRiftDifficultySnapshot BuildSnapshot(int riftLevel, int requestedPlayerCount)
        {
            int effectivePlayerCount = GetEffectivePlayerCount(requestedPlayerCount);
            float equivalentD3RiftLevel = GetEquivalentD3RiftLevel(riftLevel);
            float groupHealthMultiplier = GetGroupHealthMultiplier(requestedPlayerCount);
            float soloHealthMultiplier = GetHealthMultiplier(riftLevel);
            float finalHealthMultiplier = soloHealthMultiplier * groupHealthMultiplier;
            float finalDamageMultiplier = GetDamageMultiplier(riftLevel);

            return new MythicRiftDifficultySnapshot(
                riftLevel,
                effectivePlayerCount,
                equivalentD3RiftLevel,
                groupHealthMultiplier,
                finalHealthMultiplier,
                finalDamageMultiplier);
        }
    }
}

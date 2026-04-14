namespace MHServerEmu.Games.MythicRifts
{
    public static class MythicRiftScaling
    {
        public static int GetEffectivePlayerCount(int requestedPlayerCount)
        {
            if (requestedPlayerCount <= 1)
                return 1;

            if (requestedPlayerCount >= 4)
                return 4;

            return requestedPlayerCount;
        }

        public static float GetHealthMultiplier(int riftLevel)
        {
            int normalizedLevel = Math.Max(riftLevel, 1);
            return (float)Math.Pow(1.17d, normalizedLevel - 1);
        }

        public static float GetDamageMultiplier(int riftLevel)
        {
            int normalizedLevel = Math.Max(riftLevel, 1);
            int earlyLevels = Math.Min(normalizedLevel - 1, 25);
            int midLevels = Math.Max(Math.Min(normalizedLevel - 26, 45), 0);
            int lateLevels = Math.Max(normalizedLevel - 71, 0);

            double multiplier = Math.Pow(1.13d, earlyLevels)
                * Math.Pow(1.07d, midLevels)
                * Math.Pow(1.02d, lateLevels);

            return (float)multiplier;
        }

        public static MythicRiftDifficultySnapshot BuildSnapshot(int riftLevel, int requestedPlayerCount)
        {
            int effectivePlayerCount = GetEffectivePlayerCount(requestedPlayerCount);

            return new MythicRiftDifficultySnapshot(
                riftLevel,
                effectivePlayerCount,
                GetHealthMultiplier(riftLevel),
                GetDamageMultiplier(riftLevel));
        }
    }
}

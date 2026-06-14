namespace MHServerEmu.Games.MythicRifts
{
    public static class MythicRiftProgression
    {
        public static int ResolveNextUnlockedLevel(int currentUnlockedLevel, int completedLevel)
        {
            int normalizedCurrentLevel = Math.Max(currentUnlockedLevel, 1);
            int completedRunNextLevel = Math.Max(completedLevel + 1, 1);
            return Math.Max(normalizedCurrentLevel, Math.Min(normalizedCurrentLevel + 1, completedRunNextLevel));
        }
    }
}

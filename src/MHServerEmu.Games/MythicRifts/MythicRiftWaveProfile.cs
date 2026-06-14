namespace MHServerEmu.Games.MythicRifts
{
    public readonly record struct MythicRiftWaveProfile(
        int Wave,
        int BossCount,
        float PerBossHealthMultiplier)
    {
        public float TotalBossHealthMultiplier => BossCount * PerBossHealthMultiplier;
    }
}

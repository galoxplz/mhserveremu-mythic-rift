namespace MHServerEmu.Games.MythicRifts
{
    public readonly struct MythicRiftDifficultySnapshot
    {
        public int RiftLevel { get; }
        public int EffectivePlayerCount { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }

        public MythicRiftDifficultySnapshot(int riftLevel, int effectivePlayerCount, float healthMultiplier, float damageMultiplier)
        {
            RiftLevel = riftLevel;
            EffectivePlayerCount = effectivePlayerCount;
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }
    }
}

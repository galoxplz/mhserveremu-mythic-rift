namespace MHServerEmu.Games.MythicRifts
{
    public readonly struct MythicRiftDifficultySnapshot
    {
        public int RiftLevel { get; }
        public int EffectivePlayerCount { get; }
        public float EquivalentD3RiftLevel { get; }
        public float GroupHealthMultiplier { get; }
        public float HealthMultiplier { get; }
        public float DamageMultiplier { get; }

        public MythicRiftDifficultySnapshot(
            int riftLevel,
            int effectivePlayerCount,
            float equivalentD3RiftLevel,
            float groupHealthMultiplier,
            float healthMultiplier,
            float damageMultiplier)
        {
            RiftLevel = riftLevel;
            EffectivePlayerCount = effectivePlayerCount;
            EquivalentD3RiftLevel = equivalentD3RiftLevel;
            GroupHealthMultiplier = groupHealthMultiplier;
            HealthMultiplier = healthMultiplier;
            DamageMultiplier = damageMultiplier;
        }
    }
}

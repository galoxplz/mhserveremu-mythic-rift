namespace MHServerEmu.Games.MythicRifts
{
    public static class MythicRiftBossWaveSelector
    {
        public static IReadOnlyList<MythicRiftContentEntry> BuildDistinctRoster(
            MythicRiftContentEntry primaryBoss,
            IEnumerable<MythicRiftContentEntry> eligibleBosses,
            int requestedCount,
            Func<int, int> nextIndex)
        {
            List<MythicRiftContentEntry> roster = new();
            int targetCount = Math.Max(requestedCount, 1);

            AddIfDistinct(roster, primaryBoss);
            if (eligibleBosses == null || nextIndex == null)
                return roster;

            List<MythicRiftContentEntry> remaining = eligibleBosses
                .Where(candidate => candidate?.HasValidBossSource == true)
                .Where(candidate => IsDistinct(roster, candidate))
                .ToList();

            while (roster.Count < targetCount && remaining.Count > 0)
            {
                int index = Math.Clamp(nextIndex(remaining.Count), 0, remaining.Count - 1);
                MythicRiftContentEntry selected = remaining[index];
                remaining.RemoveAt(index);
                AddIfDistinct(roster, selected);
                remaining.RemoveAll(candidate => IsDistinct(roster, candidate) == false);
            }

            return roster;
        }

        private static void AddIfDistinct(List<MythicRiftContentEntry> roster, MythicRiftContentEntry candidate)
        {
            if (candidate?.HasValidBossSource == true && IsDistinct(roster, candidate))
                roster.Add(candidate);
        }

        private static bool IsDistinct(IEnumerable<MythicRiftContentEntry> roster, MythicRiftContentEntry candidate)
        {
            if (candidate == null)
                return false;

            return roster.All(existing =>
                string.Equals(existing.Id, candidate.Id, StringComparison.OrdinalIgnoreCase) == false &&
                existing.BossProtoRef != candidate.BossProtoRef);
        }
    }
}

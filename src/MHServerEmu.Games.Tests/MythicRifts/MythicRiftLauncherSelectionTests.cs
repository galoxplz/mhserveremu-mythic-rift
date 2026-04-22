using System.Linq;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftLauncherSelectionTests
    {
        [Fact]
        public void ConsumableEntryPoint_UsesCurrentChosenBeaconPrototype()
        {
            MythicRiftEntryService entryService = new(null);

            MythicRiftEntryPointDefinition entryPoint = entryService.GetEntryPoint(MythicRiftEntryService.PortalToRandomDungeonEntryPointId);

            Assert.NotNull(entryPoint);
            Assert.Equal(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName, entryPoint.CandidateItemPrototypeName);
            Assert.Equal(MythicRiftLauncherService.PreferredCosmicRiftBeaconPrototypeName, entryPoint.CandidateItemPrototypeName);
            Assert.True(entryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName));
            Assert.True(entryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.LegacyCosmicRiftBeaconPrototypeName));
        }

        [Fact]
        public void LauncherCandidates_PreferMaxAffixWhileKeepingLegacyDangerRoomCompatibility()
        {
            MythicRiftEntryService entryService = new(null);

            MythicRiftLauncherItemCandidate chosenCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.CosmicRiftBeaconPrototypeName);
            MythicRiftLauncherItemCandidate legacyCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.LegacyCosmicRiftBeaconPrototypeName);

            Assert.Equal("chosen", chosenCandidate.Recommendation);
            Assert.True(chosenCandidate.IsLikelyUnusedOrLowRisk);
            Assert.Equal("compatibility", legacyCandidate.Recommendation);
            Assert.False(legacyCandidate.IsLikelyUnusedOrLowRisk);
        }
    }
}

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

            MythicRiftEntryPointDefinition entryPoint = entryService.GetEntryPoint(MythicRiftEntryService.ConsumablePortalEntryPointId);

            Assert.NotNull(entryPoint);
            Assert.Equal(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName, entryPoint.CandidateItemPrototypeName);
            Assert.Equal(MythicRiftLauncherService.PreferredCosmicRiftBeaconPrototypeName, entryPoint.CandidateItemPrototypeName);
            Assert.True(entryPoint.AcceptsLauncherItemPrototypeName(MythicRiftLauncherService.CosmicRiftBeaconPrototypeName));
            Assert.Single(entryPoint.AcceptedCandidateItemPrototypeNames);
        }

        [Fact]
        public void LauncherCandidates_PreferMaxAffixWithoutLegacyDangerRoomCompatibility()
        {
            MythicRiftEntryService entryService = new(null);

            MythicRiftLauncherItemCandidate chosenCandidate = entryService.LauncherItemCandidates.Single(candidate =>
                candidate.PrototypeName == MythicRiftLauncherService.CosmicRiftBeaconPrototypeName);

            Assert.Equal("chosen", chosenCandidate.Recommendation);
            Assert.True(chosenCandidate.IsLikelyUnusedOrLowRisk);
            Assert.DoesNotContain(entryService.LauncherItemCandidates, candidate =>
                candidate.Recommendation == "compatibility");
        }
    }
}

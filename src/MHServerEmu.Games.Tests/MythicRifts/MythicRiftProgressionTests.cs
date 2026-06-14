using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftProgressionTests
    {
        [Theory]
        [InlineData(1, 50, 2)]
        [InlineData(10, 50, 11)]
        [InlineData(25, 25, 26)]
        [InlineData(30, 20, 30)]
        public void ResolveNextUnlockedLevel_AdvancesAtMostOnePersonalLevel(int currentLevel, int completedLevel, int expectedLevel)
        {
            Assert.Equal(expectedLevel, MythicRiftProgression.ResolveNextUnlockedLevel(currentLevel, completedLevel));
        }
    }
}

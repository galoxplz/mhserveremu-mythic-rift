using MHServerEmu.Games.GameData;
using MHServerEmu.Games.MythicRifts;

namespace MHServerEmu.Games.Tests.MythicRifts
{
    public class MythicRiftUiOwnershipTests
    {
        private static readonly PrototypeId RiftContextRef = (PrototypeId)100UL;
        private static readonly PrototypeId LevelWidgetRef = (PrototypeId)200UL;
        private static readonly PrototypeId QuotaWidgetRef = (PrototypeId)300UL;
        private static readonly PrototypeId TimerWidgetRef = (PrototypeId)400UL;

        [Theory]
        [InlineData(200UL)]
        [InlineData(300UL)]
        [InlineData(400UL)]
        public void IsRiftOwnedWidget_AllowsOnlyRiftWidgetsWithRiftContext(ulong widgetRef)
        {
            Assert.True(MythicRiftUiOwnership.IsRiftOwnedWidget(
                (PrototypeId)widgetRef,
                RiftContextRef,
                RiftContextRef,
                LevelWidgetRef,
                QuotaWidgetRef,
                TimerWidgetRef));
        }

        [Fact]
        public void IsRiftOwnedWidget_RejectsNativeWidget()
        {
            Assert.False(MythicRiftUiOwnership.IsRiftOwnedWidget(
                (PrototypeId)500UL,
                RiftContextRef,
                RiftContextRef,
                LevelWidgetRef,
                QuotaWidgetRef,
                TimerWidgetRef));
        }

        [Fact]
        public void IsRiftOwnedWidget_RejectsRiftWidgetWithNativeContext()
        {
            Assert.False(MythicRiftUiOwnership.IsRiftOwnedWidget(
                LevelWidgetRef,
                (PrototypeId)101UL,
                RiftContextRef,
                LevelWidgetRef,
                QuotaWidgetRef,
                TimerWidgetRef));
        }
    }
}

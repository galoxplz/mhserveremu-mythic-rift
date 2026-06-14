using MHServerEmu.Games.GameData;

namespace MHServerEmu.Games.MythicRifts
{
    public static class MythicRiftUiOwnership
    {
        public static bool IsRiftOwnedWidget(
            PrototypeId widgetRef,
            PrototypeId contextRef,
            PrototypeId riftContextRef,
            PrototypeId levelWidgetRef,
            PrototypeId quotaWidgetRef,
            PrototypeId timerWidgetRef)
        {
            if (riftContextRef == PrototypeId.Invalid || contextRef != riftContextRef)
                return false;

            return widgetRef == levelWidgetRef ||
                   widgetRef == quotaWidgetRef ||
                   widgetRef == timerWidgetRef;
        }
    }
}

using System.Collections;
using System.Reflection;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.UI;

namespace MHServerEmu.Games.MythicRifts
{
    internal static class MythicRiftUiController
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly FieldInfo WidgetDictionaryField = typeof(UIDataProvider).GetField("_dataDict", BindingFlags.Instance | BindingFlags.NonPublic);
        private static bool _reflectionWarningLogged;

        public static int RemoveNativeWidgets(
            UIDataProvider uiDataProvider,
            PrototypeId riftContextRef,
            PrototypeId levelWidgetRef,
            PrototypeId quotaWidgetRef,
            PrototypeId timerWidgetRef)
        {
            if (uiDataProvider == null || riftContextRef == PrototypeId.Invalid)
                return 0;

            if (WidgetDictionaryField?.GetValue(uiDataProvider) is not IDictionary widgetDictionary)
            {
                if (_reflectionWarningLogged == false)
                {
                    _reflectionWarningLogged = true;
                    Logger.Warn("Mythic Rift UI controller could not inspect UIDataProvider widgets; native HUD cleanup is unavailable.");
                }

                return 0;
            }

            List<(PrototypeId WidgetRef, PrototypeId ContextRef)> widgetsToDelete = new();
            foreach (DictionaryEntry entry in widgetDictionary)
            {
                if (entry.Key is not ValueTuple<PrototypeId, PrototypeId> key)
                    continue;

                if (MythicRiftUiOwnership.IsRiftOwnedWidget(
                    key.Item1,
                    key.Item2,
                    riftContextRef,
                    levelWidgetRef,
                    quotaWidgetRef,
                    timerWidgetRef))
                {
                    continue;
                }

                widgetsToDelete.Add((key.Item1, key.Item2));
            }

            foreach ((PrototypeId widgetRef, PrototypeId contextRef) in widgetsToDelete)
                uiDataProvider.DeleteWidget(widgetRef, contextRef);

            return widgetsToDelete.Count;
        }
    }
}

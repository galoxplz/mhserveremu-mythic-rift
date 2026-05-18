using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Properties;

namespace MHServerEmu.Games.GameData
{
    public static class ServerSideStringOverrideManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly string StringOverridesDirectory = Path.Combine(FileHelper.DataDirectory, "Game", "StringOverrides");

        private static readonly object LoadLock = new();
        private static bool _loaded;
        private static Dictionary<PrototypeId, Dictionary<PropertyEnum, LocaleStringId>> _worldEntityPropertyOverrides = new();

        public static bool TryGetWorldEntityPropertyOverrides(PrototypeId prototypeRef, out IReadOnlyDictionary<PropertyEnum, LocaleStringId> overrides)
        {
            EnsureLoaded();

            if (_worldEntityPropertyOverrides.TryGetValue(prototypeRef, out Dictionary<PropertyEnum, LocaleStringId> propertyOverrides))
            {
                overrides = propertyOverrides;
                return true;
            }

            overrides = null;
            return false;
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            lock (LoadLock)
            {
                if (_loaded)
                    return;

                _worldEntityPropertyOverrides = LoadWorldEntityPropertyOverrides();
                _loaded = true;
            }
        }

        private static Dictionary<PrototypeId, Dictionary<PropertyEnum, LocaleStringId>> LoadWorldEntityPropertyOverrides()
        {
            Dictionary<PrototypeId, Dictionary<PropertyEnum, LocaleStringId>> overrides = new();
            if (Directory.Exists(StringOverridesDirectory) == false)
                return overrides;

            int count = 0;
            foreach (string filePath in FileHelper.GetFilesWithPrefix(StringOverridesDirectory, "ServerSideStringPropertyOverrides", "json"))
            {
                string fileName = Path.GetFileName(filePath);
                ServerSideStringPropertyOverrideEntry[] entries = FileHelper.DeserializeJson<ServerSideStringPropertyOverrideEntry[]>(filePath);
                if (entries == null)
                {
                    Logger.Warn($"LoadWorldEntityPropertyOverrides(): Failed to parse {fileName}, skipping.");
                    continue;
                }

                foreach (ServerSideStringPropertyOverrideEntry entry in entries)
                {
                    if (entry.Enabled == false)
                        continue;

                    PrototypeId prototypeRef = ResolvePrototypeRef(entry);
                    if (prototypeRef == PrototypeId.Invalid || GameDatabase.GetPrototype<Prototype>(prototypeRef) == null)
                    {
                        Logger.Warn($"LoadWorldEntityPropertyOverrides(): Invalid prototype '{entry.Prototype}' in {fileName}, skipping.");
                        continue;
                    }

                    if (Enum.TryParse(entry.Property, ignoreCase: true, out PropertyEnum property) == false)
                    {
                        Logger.Warn($"LoadWorldEntityPropertyOverrides(): Invalid property '{entry.Property}' in {fileName}, skipping.");
                        continue;
                    }

                    if (entry.LocaleStringId == 0)
                    {
                        Logger.Warn($"LoadWorldEntityPropertyOverrides(): Missing LocaleStringId for prototype '{entry.Prototype}' property '{entry.Property}' in {fileName}, skipping.");
                        continue;
                    }

                    if (overrides.TryGetValue(prototypeRef, out Dictionary<PropertyEnum, LocaleStringId> propertyOverrides) == false)
                    {
                        propertyOverrides = new();
                        overrides[prototypeRef] = propertyOverrides;
                    }

                    propertyOverrides[property] = (LocaleStringId)entry.LocaleStringId;
                    count++;
                }
            }

            Logger.Info($"Loaded {count} server-side string property override(s).");
            return overrides;
        }

        private static PrototypeId ResolvePrototypeRef(ServerSideStringPropertyOverrideEntry entry)
        {
            if (entry.PrototypeId != 0)
                return (PrototypeId)entry.PrototypeId;

            if (string.IsNullOrWhiteSpace(entry.Prototype))
                return PrototypeId.Invalid;

            return GameDatabase.GetPrototypeRefByName(entry.Prototype);
        }

        private sealed class ServerSideStringPropertyOverrideEntry
        {
            public bool Enabled { get; set; } = true;
            public string Prototype { get; set; }
            public ulong PrototypeId { get; set; }
            public string Property { get; set; }
            public ulong LocaleStringId { get; set; }
        }
    }
}

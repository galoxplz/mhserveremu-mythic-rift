using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;

namespace MHServerEmu.Games.MythicRifts
{
    /// <summary>
    /// Resolves future GRIFT launcher items into validated Mythic Rift entry requests.
    /// This intentionally stops short of wiring itself into every item use path until the final
    /// launcher item and patcher strategy are locked down.
    /// </summary>
    public sealed class MythicRiftLauncherService
    {
        public const string CosmicRiftBeaconDisplayName = "Cosmic Rift Beacon";
        public const string CosmicRiftBeaconPrototypeName = "PortalToRandomDungeon";
        public static readonly TimeSpan DefaultLauncherTimeLimit = TimeSpan.FromMinutes(10);
        private readonly Dictionary<string, string> _candidateToEntryPointId = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<ulong, MythicRiftLauncherIntent> _pendingIntentsByPlayerDbId = new();

        public Game Game { get; }
        public MythicRiftEntryService EntryService => Game.MythicRiftEntryService;
        public IReadOnlyCollection<MythicRiftLauncherIntent> PendingIntents => _pendingIntentsByPlayerDbId.Values;

        public MythicRiftLauncherService(Game game)
        {
            Game = game;
            RegisterDefaultMappings();
        }

        public bool CanHandleItem(Item item)
        {
            if (item == null)
                return false;

            return TryResolveCandidateEntryPointId(item.PrototypeDataRef, out _);
        }

        public MythicRiftLauncherItemCandidate ResolveCandidate(Item item)
        {
            if (item == null)
                return null;

            return EntryService.LauncherItemCandidates.FirstOrDefault(candidate =>
                PrototypeNameMatches(item.PrototypeDataRef, candidate.PrototypeName));
        }

        public MythicRiftLauncherItemCandidate ResolveChosenCandidate()
        {
            return EntryService.LauncherItemCandidates.FirstOrDefault(candidate =>
                string.Equals(candidate.PrototypeName, CosmicRiftBeaconPrototypeName, StringComparison.OrdinalIgnoreCase));
        }

        public MythicRiftLauncherIntent RegisterLauncherItemUse(Player player, Item item)
        {
            if (player == null || item == null)
                return null;

            string itemPrototypeName = item.PrototypeDataRef.GetNameFormatted();
            if (string.IsNullOrWhiteSpace(itemPrototypeName) || TryResolveCandidateEntryPointId(item.PrototypeDataRef, out string entryPointId) == false)
                return null;

            MythicRiftLauncherIntent intent = new()
            {
                PlayerDbId = player.DatabaseUniqueId,
                ItemPrototypeName = itemPrototypeName,
                EntryPointId = entryPointId,
                PortalTargetRegionProtoRef = item.ItemPrototype?.GetPortalTarget() ?? PrototypeId.Invalid,
                CreatedAt = Game.CurrentTime
            };

            _pendingIntentsByPlayerDbId[player.DatabaseUniqueId] = intent;
            return intent;
        }

        public MythicRiftLauncherIntent GetPendingIntent(ulong playerDbId)
        {
            if (playerDbId == 0)
                return null;

            return _pendingIntentsByPlayerDbId.TryGetValue(playerDbId, out MythicRiftLauncherIntent intent)
                ? intent
                : null;
        }

        public MythicRiftLauncherUseResult ConsumePendingIntent(Player player, int riftLevel, TimeSpan timeLimit)
        {
            if (player == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Player not found."
                };
            }

            MythicRiftLauncherIntent intent = GetPendingIntent(player.DatabaseUniqueId);
            if (intent == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "No pending Mythic Rift launcher intent for this player."
                };
            }

            riftLevel = NormalizeRiftLevel(player, riftLevel);
            timeLimit = NormalizeTimeLimit(timeLimit);

            MythicRiftLauncherUseResult result = TryRequestRunFromPrototypeName(player, intent.ItemPrototypeName, riftLevel, timeLimit);
            if (result.Success)
                _pendingIntentsByPlayerDbId.Remove(player.DatabaseUniqueId);

            return result;
        }

        public MythicRiftLauncherUseResult ConsumePendingIntentAuto(Player player, TimeSpan? timeLimit = null)
        {
            if (player == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Player not found."
                };
            }

            int riftLevel = NormalizeRiftLevel(player, 0);
            TimeSpan resolvedTimeLimit = NormalizeTimeLimit(timeLimit.GetValueOrDefault());
            return ConsumePendingIntent(player, riftLevel, resolvedTimeLimit);
        }

        public MythicRiftLauncherUseResult TryRequestRunFromItem(Player player, Item item, int riftLevel, TimeSpan timeLimit)
        {
            if (player == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Player not found."
                };
            }

            if (item == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Launcher item not found."
                };
            }

            riftLevel = NormalizeRiftLevel(player, riftLevel);
            timeLimit = NormalizeTimeLimit(timeLimit);

            string itemPrototypeName = item.PrototypeDataRef.GetName();
            if (string.IsNullOrWhiteSpace(itemPrototypeName) || TryResolveCandidateEntryPointId(item.PrototypeDataRef, out string entryPointId) == false)
            {
                return new MythicRiftLauncherUseResult
                {
                    ItemPrototypeName = item.PrototypeDataRef.GetNameFormatted(),
                    ErrorMessage = $"Item is not registered as a Mythic Rift launcher candidate: {item.PrototypeDataRef.GetNameFormatted() ?? "unknown"}"
                };
            }

            MythicRiftLauncherItemCandidate candidate = ResolveCandidate(item);
            itemPrototypeName = candidate?.PrototypeName ?? item.PrototypeDataRef.GetNameFormatted();
            PrototypeId portalTargetRegionProtoRef = item.ItemPrototype?.GetPortalTarget() ?? PrototypeId.Invalid;

            MythicRiftEntryResult entryResult = EntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                EntryPointId = entryPointId,
                LauncherItemPrototypeName = itemPrototypeName,
                RiftLevel = riftLevel,
                TimeLimit = timeLimit
            });

            return new MythicRiftLauncherUseResult
            {
                EntryResult = entryResult,
                Candidate = candidate,
                ItemPrototypeName = itemPrototypeName,
                PortalTargetRegionProtoRef = portalTargetRegionProtoRef,
                ResolvedRiftLevel = riftLevel,
                ResolvedTimeLimit = timeLimit,
                ErrorMessage = entryResult.Success ? string.Empty : entryResult.ErrorMessage
            };
        }

        public MythicRiftLauncherUseResult TryRequestRunFromPrototypeName(Player player, string itemPrototypeName, int riftLevel, TimeSpan timeLimit)
        {
            if (player == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Player not found."
                };
            }

            if (string.IsNullOrWhiteSpace(itemPrototypeName))
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Launcher item prototype name not found."
                };
            }

            riftLevel = NormalizeRiftLevel(player, riftLevel);
            timeLimit = NormalizeTimeLimit(timeLimit);

            if (_candidateToEntryPointId.TryGetValue(itemPrototypeName, out string entryPointId) == false)
            {
                return new MythicRiftLauncherUseResult
                {
                    ItemPrototypeName = itemPrototypeName,
                    ErrorMessage = $"Item is not registered as a Mythic Rift launcher candidate: {itemPrototypeName}"
                };
            }

            ItemPrototype itemProto = null;
            foreach (PrototypeId candidateProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ItemPrototype>())
            {
                ItemPrototype candidateProto = candidateProtoRef.As<ItemPrototype>();
                if (candidateProto == null)
                    continue;

                if (PrototypeNameMatches(candidateProto.DataRef, itemPrototypeName))
                {
                    itemProto = candidateProto;
                    break;
                }
            }

            if (itemProto == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ItemPrototypeName = itemPrototypeName,
                    ErrorMessage = $"Item prototype not found in game data: {itemPrototypeName}"
                };
            }

            MythicRiftLauncherItemCandidate candidate = EntryService.LauncherItemCandidates.FirstOrDefault(entry =>
                string.Equals(entry.PrototypeName, itemPrototypeName, StringComparison.OrdinalIgnoreCase));

            MythicRiftEntryResult entryResult = EntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                EntryPointId = entryPointId,
                LauncherItemPrototypeName = itemPrototypeName,
                RiftLevel = riftLevel,
                TimeLimit = timeLimit
            });

            return new MythicRiftLauncherUseResult
            {
                EntryResult = entryResult,
                Candidate = candidate,
                ItemPrototypeName = itemPrototypeName,
                PortalTargetRegionProtoRef = itemProto.GetPortalTarget(),
                ResolvedRiftLevel = riftLevel,
                ResolvedTimeLimit = timeLimit,
                ErrorMessage = entryResult.Success ? string.Empty : entryResult.ErrorMessage
            };
        }

        public bool TryGrantChosenLauncher(Player player, int count, out PrototypeId itemProtoRef, out string errorMessage)
        {
            itemProtoRef = PrototypeId.Invalid;
            errorMessage = string.Empty;

            if (player == null)
            {
                errorMessage = "Player not found.";
                return false;
            }

            if (count <= 0)
            {
                errorMessage = "Invalid beacon count.";
                return false;
            }

            itemProtoRef = ResolveItemPrototypeRef(CosmicRiftBeaconPrototypeName);
            if (itemProtoRef == PrototypeId.Invalid)
            {
                errorMessage = $"Chosen launcher prototype not found in game data: {CosmicRiftBeaconPrototypeName}";
                return false;
            }

            for (int i = 0; i < count; i++)
            {
                if (Game.LootManager.GiveItem(itemProtoRef, LootContext.CashShop, player) == false)
                {
                    errorMessage = $"Failed to grant {CosmicRiftBeaconDisplayName} to the player.";
                    return false;
                }
            }

            return true;
        }

        private int NormalizeRiftLevel(Player player, int requestedRiftLevel)
        {
            if (requestedRiftLevel > 0)
                return requestedRiftLevel;

            if (player == null)
                return 1;

            return Math.Max(Game.MythicRiftManager.GetHighestUnlockedRiftLevel(player.DatabaseUniqueId), 1);
        }

        private static TimeSpan NormalizeTimeLimit(TimeSpan timeLimit)
        {
            return timeLimit <= TimeSpan.Zero
                ? DefaultLauncherTimeLimit
                : timeLimit;
        }

        private static PrototypeId ResolveItemPrototypeRef(string itemPrototypeName)
        {
            if (string.IsNullOrWhiteSpace(itemPrototypeName))
                return PrototypeId.Invalid;

            foreach (PrototypeId candidateProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ItemPrototype>())
            {
                ItemPrototype candidateProto = candidateProtoRef.As<ItemPrototype>();
                if (candidateProto == null)
                    continue;

                if (PrototypeNameMatches(candidateProto.DataRef, itemPrototypeName))
                    return candidateProto.DataRef;
            }

            return PrototypeId.Invalid;
        }

        private bool TryResolveCandidateEntryPointId(PrototypeId prototypeRef, out string entryPointId)
        {
            entryPointId = null;
            if (prototypeRef == PrototypeId.Invalid)
                return false;

            foreach ((string candidatePrototypeName, string candidateEntryPointId) in _candidateToEntryPointId)
            {
                if (PrototypeNameMatches(prototypeRef, candidatePrototypeName) == false)
                    continue;

                entryPointId = candidateEntryPointId;
                return true;
            }

            return false;
        }

        private static bool PrototypeNameMatches(PrototypeId prototypeRef, string expectedName)
        {
            if (prototypeRef == PrototypeId.Invalid || string.IsNullOrWhiteSpace(expectedName))
                return false;

            string rawName = prototypeRef.GetName();
            if (string.Equals(rawName, expectedName, StringComparison.OrdinalIgnoreCase))
                return true;

            string formattedName = prototypeRef.GetNameFormatted();
            return string.Equals(formattedName, expectedName, StringComparison.OrdinalIgnoreCase);
        }

        private void RegisterDefaultMappings()
        {
            RegisterCandidateMapping("PortalToRandomDungeon", MythicRiftEntryService.PortalToRandomDungeonEntryPointId);
            RegisterCandidateMapping("PortalToCowLevelOneTimeUse", MythicRiftEntryService.PortalToRandomDungeonEntryPointId);
            RegisterCandidateMapping("PortalToCowLevel", MythicRiftEntryService.PortalToRandomDungeonEntryPointId);
        }

        private void RegisterCandidateMapping(string prototypeName, string entryPointId)
        {
            if (string.IsNullOrWhiteSpace(prototypeName) || string.IsNullOrWhiteSpace(entryPointId))
                return;

            _candidateToEntryPointId[prototypeName] = entryPointId;
        }
    }
}

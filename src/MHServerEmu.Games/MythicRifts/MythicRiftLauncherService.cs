using Gazillion;
using MHServerEmu.Core.Memory;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Inventories;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Regions;

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
        private readonly Dictionary<ulong, MythicRiftArmedLauncherState> _armedLaunchesByPlayerDbId = new();
        private readonly Dictionary<ulong, MythicRiftLauncherUseResult> _lastArmedLaunchResultsByPlayerDbId = new();
        private readonly Dictionary<ulong, Dictionary<ulong, int>> _trackedBeaconChargesByPlayerDbId = new();

        public Game Game { get; }
        public MythicRiftEntryService EntryService => Game.MythicRiftEntryService;
        public IReadOnlyCollection<MythicRiftLauncherIntent> PendingIntents => _pendingIntentsByPlayerDbId.Values;
        public IReadOnlyCollection<MythicRiftArmedLauncherState> ArmedLaunches => _armedLaunchesByPlayerDbId.Values;

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

        public MythicRiftArmedLauncherState ArmChosenLauncher(Player player, int requestedRiftLevel, TimeSpan timeLimit, string fixedContentId = null)
        {
            if (player == null)
                return null;

            MythicRiftArmedLauncherState armedState = new()
            {
                PlayerDbId = player.DatabaseUniqueId,
                ArmedAt = Game.CurrentTime,
                RequestedRiftLevel = requestedRiftLevel,
                TimeLimit = NormalizeTimeLimit(timeLimit),
                FixedContentId = string.IsNullOrWhiteSpace(fixedContentId) ? null : fixedContentId
            };

            _armedLaunchesByPlayerDbId[player.DatabaseUniqueId] = armedState;
            return armedState;
        }

        public bool DisarmChosenLauncher(ulong playerDbId)
        {
            if (playerDbId == 0)
                return false;

            return _armedLaunchesByPlayerDbId.Remove(playerDbId);
        }

        public MythicRiftArmedLauncherState GetArmedLauncherState(ulong playerDbId)
        {
            if (playerDbId == 0)
                return null;

            return _armedLaunchesByPlayerDbId.TryGetValue(playerDbId, out MythicRiftArmedLauncherState armedState)
                ? armedState
                : null;
        }

        public MythicRiftLauncherUseResult GetLastArmedLaunchResult(ulong playerDbId)
        {
            if (playerDbId == 0)
                return null;

            return _lastArmedLaunchResultsByPlayerDbId.TryGetValue(playerDbId, out MythicRiftLauncherUseResult result)
                ? result
                : null;
        }

        public int GetTrackedBeaconChargeCount(Player player, Item item)
        {
            if (player == null || item == null)
                return 0;

            if (_trackedBeaconChargesByPlayerDbId.TryGetValue(player.DatabaseUniqueId, out Dictionary<ulong, int> chargesByItemId) == false)
                return 0;

            return chargesByItemId.TryGetValue(item.Id, out int chargeCount)
                ? Math.Max(chargeCount, 0)
                : 0;
        }

        public int GetTotalTrackedBeaconCharges(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 0;

            if (_trackedBeaconChargesByPlayerDbId.TryGetValue(playerDbId, out Dictionary<ulong, int> chargesByItemId) == false)
                return 0;

            return chargesByItemId.Values.Sum(value => Math.Max(value, 0));
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
                ItemEntityId = item.Id,
                PortalTargetRegionProtoRef = portalTargetRegionProtoRef,
                ResolvedRiftLevel = riftLevel,
                ResolvedTimeLimit = timeLimit,
                ErrorMessage = entryResult.Success ? string.Empty : entryResult.ErrorMessage
            };
        }

        public MythicRiftLauncherUseResult TryHandleArmedLauncherUse(Player player, Item item)
        {
            if (player == null || item == null)
                return null;

            MythicRiftArmedLauncherState armedState = GetArmedLauncherState(player.DatabaseUniqueId);
            if (armedState == null || armedState.IsValid == false)
                return null;

            if (CanHandleItem(item) == false)
                return null;

            int resolvedRiftLevel = NormalizeRiftLevel(player, armedState.RequestedRiftLevel);
            TimeSpan resolvedTimeLimit = NormalizeTimeLimit(armedState.TimeLimit);

            MythicRiftLauncherUseResult result = string.IsNullOrWhiteSpace(armedState.FixedContentId)
                ? TryRequestRunFromItem(player, item, resolvedRiftLevel, resolvedTimeLimit)
                : TryRequestRunFromArmedFixedContent(player, item, armedState.FixedContentId, resolvedRiftLevel, resolvedTimeLimit);
            if (result != null)
            {
                result.ConsumedArmedLaunchMode = true;
                TryTeleportToRunEntry(player, result);
                _lastArmedLaunchResultsByPlayerDbId[player.DatabaseUniqueId] = result;
                NotifyLauncherUse(player, result);
            }

            if (IsCommittedLauncherUse(result))
                _armedLaunchesByPlayerDbId.Remove(player.DatabaseUniqueId);

            return result;
        }

        public MythicRiftLauncherUseResult TryHandleTrackedBeaconUse(Player player, Item item)
        {
            if (player == null || item == null)
                return null;

            if (CanHandleItem(item) == false)
                return null;

            int availableCharges = GetTrackedBeaconChargeCount(player, item);
            bool usingGenericTrackedChargeFallback = false;
            if (availableCharges <= 0)
            {
                // The generic fallback exists only to tolerate live-server stack/entity-id differences for the
                // chosen Cosmic Rift beacon base. Do not let it hijack other portal candidates or future items.
                if (PrototypeNameMatches(item.PrototypeDataRef, CosmicRiftBeaconPrototypeName) == false)
                    return null;

                availableCharges = GetTotalTrackedBeaconCharges(player.DatabaseUniqueId);
                if (availableCharges <= 0)
                    return null;

                usingGenericTrackedChargeFallback = true;
            }

            MythicRiftArmedLauncherState armedState = GetArmedLauncherState(player.DatabaseUniqueId);
            int resolvedRiftLevel = NormalizeRiftLevel(player, armedState?.RequestedRiftLevel ?? 0);
            TimeSpan resolvedTimeLimit = NormalizeTimeLimit(armedState?.TimeLimit ?? DefaultLauncherTimeLimit);

            MythicRiftLauncherUseResult result = string.IsNullOrWhiteSpace(armedState?.FixedContentId)
                ? TryRequestRunFromItem(player, item, resolvedRiftLevel, resolvedTimeLimit)
                : TryRequestRunFromArmedFixedContent(player, item, armedState.FixedContentId, resolvedRiftLevel, resolvedTimeLimit);

            if (result == null)
                return null;

            result.InterceptedItemUse = true;
            result.UsedTrackedBeaconInstance = true;
            result.ConsumedArmedLaunchMode = armedState != null;

            if (result.Success)
                TryTeleportToRunEntry(player, result);

            _lastArmedLaunchResultsByPlayerDbId[player.DatabaseUniqueId] = result;

            if (IsCommittedLauncherUse(result))
            {
                if (usingGenericTrackedChargeFallback)
                    ConsumeAnyTrackedBeaconCharge(player.DatabaseUniqueId);
                else
                    ConsumeTrackedBeaconCharge(player.DatabaseUniqueId, item.Id);

                if (armedState != null)
                    _armedLaunchesByPlayerDbId.Remove(player.DatabaseUniqueId);
            }

            NotifyLauncherUse(player, result);

            return result;
        }

        private MythicRiftLauncherUseResult TryRequestRunFromArmedFixedContent(Player player, Item item, string contentId, int riftLevel, TimeSpan timeLimit)
        {
            if (player == null || item == null)
            {
                return new MythicRiftLauncherUseResult
                {
                    ErrorMessage = "Player or launcher item not found."
                };
            }

            MythicRiftLauncherItemCandidate candidate = ResolveCandidate(item);
            string itemPrototypeName = candidate?.PrototypeName ?? item.PrototypeDataRef.GetNameFormatted();
            PrototypeId portalTargetRegionProtoRef = item.ItemPrototype?.GetPortalTarget() ?? PrototypeId.Invalid;

            MythicRiftEntryResult entryResult = EntryService.RequestRun(player, new MythicRiftEntryRequest
            {
                EntryPointId = MythicRiftEntryService.DefaultEntryPointId,
                LauncherItemPrototypeName = itemPrototypeName,
                RiftLevel = riftLevel,
                ContentId = contentId,
                TimeLimit = timeLimit
            });

            return new MythicRiftLauncherUseResult
            {
                EntryResult = entryResult,
                Candidate = candidate,
                ItemPrototypeName = itemPrototypeName,
                ItemEntityId = item.Id,
                PortalTargetRegionProtoRef = portalTargetRegionProtoRef,
                ResolvedRiftLevel = riftLevel,
                ResolvedTimeLimit = timeLimit,
                ErrorMessage = entryResult.Success ? string.Empty : entryResult.ErrorMessage
            };
        }

        private void TryTeleportToRunEntry(Player player, MythicRiftLauncherUseResult result)
        {
            if (player == null || result?.Success != true)
                return;

            MythicRiftRunState runState = result.EntryResult?.RunState;
            PrototypeId startTargetRef = ResolveRunStartTarget(result.EntryResult.RunState);
            result.TeleportTargetProtoRef = startTargetRef;
            result.TeleportAttempted = startTargetRef != PrototypeId.Invalid;

            if (startTargetRef == PrototypeId.Invalid)
            {
                result.TeleportErrorMessage = "No valid region start target was found for the selected Rift content.";
                AbortUnboundLaunchRun(runState, result.TeleportErrorMessage);
                return;
            }

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Debug);

            bool teleportSucceeded = teleporter.TeleportToTarget(startTargetRef);
            result.TeleportSucceeded = teleportSucceeded;

            if (teleportSucceeded == false)
            {
                result.TeleportErrorMessage = $"Teleport to Rift start target failed: {startTargetRef.GetNameFormatted()}";
                AbortUnboundLaunchRun(runState, result.TeleportErrorMessage);
            }
        }

        private static PrototypeId ResolveRunStartTarget(MythicRiftRunState runState)
        {
            return runState?.Config.StartTargetProtoRef ?? PrototypeId.Invalid;
        }

        private static bool IsCommittedLauncherUse(MythicRiftLauncherUseResult result)
        {
            return result?.Success == true && string.IsNullOrWhiteSpace(result.TeleportErrorMessage);
        }

        private void NotifyLauncherUse(Player player, MythicRiftLauncherUseResult result)
        {
            if (player == null || result == null)
                return;

            if (result.Success == false)
            {
                string failureMessage = string.IsNullOrWhiteSpace(result.ErrorMessage)
                    ? "[Cosmic Rift] Beacon use failed."
                    : $"[Cosmic Rift] Beacon use failed: {result.ErrorMessage}";
                Game.ChatManager.SendChatFromCustomSystem(player, failureMessage, showSender: false);
                return;
            }

            MythicRiftRunConfig config = result.EntryResult?.RunState?.Config;
            if (config == null)
            {
                Game.ChatManager.SendChatFromCustomSystem(player, "[Cosmic Rift] Beacon accepted, but no run details were resolved.", showSender: false);
                return;
            }

            string teleportSummary = result.TeleportAttempted == false
                ? "teleport=not-attempted"
                : result.TeleportSucceeded
                    ? "teleport=ok"
                    : $"teleport=failed ({result.TeleportErrorMessage ?? "unknown error"})";

            string bossName = ResolveBossDisplayName(config);
            string message = $"[Cosmic Rift] Beacon accepted. runId={config.RunId} | map={config.Content.DisplayName} | boss={bossName} | level={config.RiftLevel} | timer={config.TimeLimit.TotalMinutes:0} min | {teleportSummary}";
            Game.ChatManager.SendChatFromCustomSystem(player, message, showSender: false);
        }

        private static string ResolveBossDisplayName(MythicRiftRunConfig config)
        {
            if (config?.BossContent?.DisplayName == null)
                return config?.BossProtoRef.GetNameFormatted() ?? "Unknown Boss";

            string bossName = config.BossContent.DisplayName;
            const string terminalSuffix = " Terminal";
            if (bossName.EndsWith(terminalSuffix, StringComparison.OrdinalIgnoreCase))
                return bossName[..^terminalSuffix.Length];

            return bossName;
        }

        private void AbortUnboundLaunchRun(MythicRiftRunState runState, string reason)
        {
            if (runState == null || runState.RegionId != 0)
                return;

            string abortReason = string.IsNullOrWhiteSpace(reason)
                ? "Rift launch failed before entering the selected terminal."
                : $"Rift launch failed before entering the selected terminal: {reason}";

            Game.MythicRiftManager.AbortRunWithReason(runState.Config.RunId, Game.CurrentTime, abortReason);
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
                Dictionary<ulong, int> beforeSnapshot = SnapshotChosenLauncherStacks(player);
                if (Game.LootManager.GiveItem(itemProtoRef, LootContext.CashShop, player) == false)
                {
                    errorMessage = $"Failed to grant {CosmicRiftBeaconDisplayName} to the player.";
                    return false;
                }

                Dictionary<ulong, int> afterSnapshot = SnapshotChosenLauncherStacks(player);
                RegisterTrackedBeaconGrantDelta(player.DatabaseUniqueId, beforeSnapshot, afterSnapshot);
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

        private void AddTrackedBeaconCharges(ulong playerDbId, ulong itemEntityId, int additionalCharges)
        {
            if (playerDbId == 0 || itemEntityId == Entity.InvalidId || additionalCharges <= 0)
                return;

            if (_trackedBeaconChargesByPlayerDbId.TryGetValue(playerDbId, out Dictionary<ulong, int> chargesByItemId) == false)
            {
                chargesByItemId = new();
                _trackedBeaconChargesByPlayerDbId[playerDbId] = chargesByItemId;
            }

            chargesByItemId[itemEntityId] = chargesByItemId.GetValueOrDefault(itemEntityId) + additionalCharges;
        }

        private void ConsumeTrackedBeaconCharge(ulong playerDbId, ulong itemEntityId)
        {
            if (playerDbId == 0 || itemEntityId == Entity.InvalidId)
                return;

            if (_trackedBeaconChargesByPlayerDbId.TryGetValue(playerDbId, out Dictionary<ulong, int> chargesByItemId) == false)
                return;

            if (chargesByItemId.TryGetValue(itemEntityId, out int chargeCount) == false)
                return;

            chargeCount--;
            if (chargeCount > 0)
            {
                chargesByItemId[itemEntityId] = chargeCount;
                return;
            }

            chargesByItemId.Remove(itemEntityId);
            if (chargesByItemId.Count == 0)
                _trackedBeaconChargesByPlayerDbId.Remove(playerDbId);
        }

        private void ConsumeAnyTrackedBeaconCharge(ulong playerDbId)
        {
            if (playerDbId == 0)
                return;

            if (_trackedBeaconChargesByPlayerDbId.TryGetValue(playerDbId, out Dictionary<ulong, int> chargesByItemId) == false ||
                chargesByItemId.Count == 0)
                return;

            ulong itemEntityId = chargesByItemId.Keys.First();
            ConsumeTrackedBeaconCharge(playerDbId, itemEntityId);
        }

        private Dictionary<ulong, int> SnapshotChosenLauncherStacks(Player player)
        {
            Dictionary<ulong, int> snapshot = new();
            if (player == null)
                return snapshot;

            CaptureInventorySnapshot(player.GetInventory(InventoryConvenienceLabel.General), snapshot);
            CaptureInventorySnapshot(player.GetInventory(InventoryConvenienceLabel.DeliveryBox), snapshot);
            return snapshot;
        }

        private void CaptureInventorySnapshot(Inventory inventory, Dictionary<ulong, int> snapshot)
        {
            if (inventory == null)
                return;

            foreach (var entry in inventory)
            {
                Item item = Game.EntityManager.GetEntity<Item>(entry.Id);
                if (item == null || PrototypeNameMatches(item.PrototypeDataRef, CosmicRiftBeaconPrototypeName) == false)
                    continue;

                snapshot[item.Id] = item.CurrentStackSize;
            }
        }

        private void RegisterTrackedBeaconGrantDelta(ulong playerDbId, Dictionary<ulong, int> beforeSnapshot, Dictionary<ulong, int> afterSnapshot)
        {
            if (playerDbId == 0 || afterSnapshot == null || afterSnapshot.Count == 0)
                return;

            foreach ((ulong itemEntityId, int afterCount) in afterSnapshot)
            {
                int beforeCount = beforeSnapshot?.GetValueOrDefault(itemEntityId) ?? 0;
                int delta = afterCount - beforeCount;
                if (delta > 0)
                    AddTrackedBeaconCharges(playerDbId, itemEntityId, delta);
            }
        }
    }
}

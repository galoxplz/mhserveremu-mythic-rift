using MHServerEmu.Games.Entities;

namespace MHServerEmu.Games.MythicRifts
{
    /// <summary>
    /// Headless server-side entry layer for Mythic Rift requests.
    /// This intentionally does not assume any concrete in-game object or client UI yet.
    /// </summary>
    public sealed class MythicRiftEntryService
    {
        public const string DefaultEntryPointId = "default";
        public const string PortalToRandomDungeonEntryPointId = "portal-to-random-dungeon";
        private readonly Dictionary<string, MythicRiftEntryPointDefinition> _entryPoints = new(StringComparer.OrdinalIgnoreCase);
        private readonly List<MythicRiftLauncherItemCandidate> _launcherItemCandidates = new();

        public Game Game { get; }
        public MythicRiftManager Manager => Game.MythicRiftManager;
        public IReadOnlyCollection<MythicRiftEntryPointDefinition> EntryPoints => _entryPoints.Values;
        public IReadOnlyList<MythicRiftLauncherItemCandidate> LauncherItemCandidates => _launcherItemCandidates;

        public MythicRiftEntryService(Game game)
        {
            Game = game;
            RegisterDefaultEntryPoints();
            RegisterDefaultLauncherItemCandidates();
        }

        public MythicRiftEntryResult RequestRun(Player player, MythicRiftEntryRequest request)
        {
            if (player == null)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = "Player not found."
                };
            }

            if (request == null)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = "Rift entry request not found."
                };
            }

            if (request.RiftLevel <= 0)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = "Invalid Rift level."
                };
            }

            string entryPointId = string.IsNullOrWhiteSpace(request.EntryPointId)
                ? DefaultEntryPointId
                : request.EntryPointId;

            MythicRiftEntryPointDefinition entryPoint = GetEntryPoint(entryPointId);
            if (entryPoint == null)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = $"Unknown Mythic Rift entry point: {entryPointId}"
                };
            }

            if (request.HasFixedContent && entryPoint.AllowsFixedContentSelection == false)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = $"Entry point {entryPoint.DisplayName} does not allow fixed content selection."
                };
            }

            if (request.HasFixedContent == false && entryPoint.AllowsRandomContent == false)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = $"Entry point {entryPoint.DisplayName} does not allow random content selection."
                };
            }

            if (request.HasLauncherItemPrototypeName && entryPoint.AcceptsLauncherItemPrototypeName(request.LauncherItemPrototypeName) == false)
            {
                return new MythicRiftEntryResult
                {
                    ErrorMessage = $"Entry point {entryPoint.DisplayName} expects launcher item {DescribeAcceptedLauncherItems(entryPoint)}."
                };
            }

            TimeSpan timeLimit = request.TimeLimit <= TimeSpan.Zero
                ? TimeSpan.FromMinutes(10)
                : request.TimeLimit;

            int killQuota = request.KillQuotaOverride.GetValueOrDefault();

            MythicRiftRunState runState = request.HasFixedContent
                ? Manager.RequestFixedRun(player, request.ContentId, request.RiftLevel, killQuota, timeLimit, out string errorMessage)
                : Manager.RequestRun(player, request.RiftLevel, killQuota, timeLimit, out errorMessage);

            MythicRiftPortalLaunchPlan launchPlan = BuildLaunchPlan(entryPoint, request);

            return new MythicRiftEntryResult
            {
                RunState = runState,
                LaunchPlan = launchPlan,
                ErrorMessage = runState == null ? errorMessage : string.Empty
            };
        }

        public MythicRiftEntryPointDefinition GetEntryPoint(string entryPointId)
        {
            if (string.IsNullOrWhiteSpace(entryPointId))
                return null;

            return _entryPoints.TryGetValue(entryPointId, out MythicRiftEntryPointDefinition entryPoint)
                ? entryPoint
                : null;
        }

        public bool EntryPointAcceptsLauncherItem(string entryPointId, string launcherItemPrototypeName)
        {
            MythicRiftEntryPointDefinition entryPoint = GetEntryPoint(entryPointId);
            return entryPoint?.AcceptsLauncherItemPrototypeName(launcherItemPrototypeName) == true;
        }

        public MythicRiftPortalLaunchPlan BuildLaunchPlan(string entryPointId, MythicRiftEntryRequest request = null)
        {
            MythicRiftEntryPointDefinition entryPoint = GetEntryPoint(entryPointId);
            return BuildLaunchPlan(entryPoint, request);
        }

        private void RegisterEntryPoint(MythicRiftEntryPointDefinition entryPoint)
        {
            if (entryPoint == null || entryPoint.IsValid == false)
                return;

            _entryPoints[entryPoint.Id] = entryPoint;
        }

        private void RegisterLauncherItemCandidate(MythicRiftLauncherItemCandidate candidate)
        {
            if (candidate == null || candidate.IsValid == false)
                return;

            _launcherItemCandidates.Add(candidate);
        }

        private void RegisterDefaultEntryPoints()
        {
            RegisterEntryPoint(new MythicRiftEntryPointDefinition
            {
                Id = DefaultEntryPointId,
                DisplayName = "Default Mythic Rift Launcher",
                AllowsRandomContent = true,
                AllowsFixedContentSelection = true,
                IsPatcherFriendly = false,
                LaunchModel = "headless-server",
                Notes = "Headless server-side default entry point used for local development and future launcher integration."
            });

            RegisterEntryPoint(new MythicRiftEntryPointDefinition
            {
                Id = "capital-hub",
                DisplayName = "Capital Hub Launcher",
                AllowsRandomContent = true,
                AllowsFixedContentSelection = true,
                IsPatcherFriendly = true,
                LaunchModel = "hub-interactable",
                Notes = "Planned future player-facing launcher for a hub area such as Avengers Tower."
            });

            RegisterEntryPoint(new MythicRiftEntryPointDefinition
            {
                Id = PortalToRandomDungeonEntryPointId,
                DisplayName = "Cosmic Rift Consumable Launcher",
                AllowsRandomContent = true,
                AllowsFixedContentSelection = false,
                IsPatcherFriendly = true,
                LaunchModel = "consumable-portal",
                CandidateItemPrototypeName = MythicRiftLauncherService.CosmicRiftBeaconPrototypeName,
                AcceptedCandidateItemPrototypeNames = new[]
                {
                    MythicRiftLauncherService.CosmicRiftBeaconPrototypeName,
                    MythicRiftLauncherService.LegacyCosmicRiftBeaconPrototypeName
                },
                CandidateTransitionPrototypeName = "CowLevelTransition",
                Notes = "Official current direction for the feature: Cosmic Rift uses PortalToRandomMaxAffixDungeon as its preferred technical launcher base, keeps PortalToRandomDungeon as a compatibility fallback, and still follows a private direct portal flow inspired by Bovineheim/Cow Level."
            });
        }

        private void RegisterDefaultLauncherItemCandidates()
        {
            RegisterLauncherItemCandidate(new MythicRiftLauncherItemCandidate
            {
                PrototypeName = MythicRiftLauncherService.CosmicRiftBeaconPrototypeName,
                DisplayName = "Cosmic Rift Beacon Base",
                SourceFamily = "DangerRoom / RandomDungeon",
                IsLikelyUnusedOrLowRisk = true,
                IsShopLinked = false,
                SupportsRandomThemeIdentity = true,
                PatcherFriendly = true,
                Recommendation = "chosen",
                Notes = "Official chosen launcher base for the project. It matches the random-dungeon identity, appears unreferenced in normal gameplay, and should be the safest long-term item base once TAHITI patches its DesignState to Live."
            });

            RegisterLauncherItemCandidate(new MythicRiftLauncherItemCandidate
            {
                PrototypeName = MythicRiftLauncherService.LegacyCosmicRiftBeaconPrototypeName,
                DisplayName = "Legacy Danger Room Portal Base",
                SourceFamily = "DangerRoom / RandomDungeon",
                IsLikelyUnusedOrLowRisk = false,
                IsShopLinked = false,
                SupportsRandomThemeIdentity = true,
                PatcherFriendly = true,
                Recommendation = "compatibility",
                Notes = "Kept as a compatibility fallback because earlier prototype work and some Danger Room testing already used it. Mythic Rift still recognizes it, but the preferred long-term launcher base is PortalToRandomMaxAffixDungeon so normal Danger Room behavior stays easier to isolate."
            });

            RegisterLauncherItemCandidate(new MythicRiftLauncherItemCandidate
            {
                PrototypeName = "PortalToCowLevelOneTimeUse",
                DisplayName = "Portal To Cow Level One Time Use",
                SourceFamily = "Cow Level / FortuneCard",
                IsLikelyUnusedOrLowRisk = false,
                IsShopLinked = false,
                SupportsRandomThemeIdentity = false,
                PatcherFriendly = true,
                Recommendation = "fallback",
                Notes = "Technically attractive because it is already a one-time-use portal consumable, but the Cow Level theme is less clean for a permanent GRIFT identity."
            });

            RegisterLauncherItemCandidate(new MythicRiftLauncherItemCandidate
            {
                PrototypeName = "PortalToCowLevel",
                DisplayName = "Portal To Cow Level",
                SourceFamily = "Cow Level / FortuneCard",
                IsLikelyUnusedOrLowRisk = false,
                IsShopLinked = false,
                SupportsRandomThemeIdentity = false,
                PatcherFriendly = true,
                Recommendation = "fallback",
                Notes = "Usable as a technical template, but thematically too specific unless cloned or repurposed."
            });

            RegisterLauncherItemCandidate(new MythicRiftLauncherItemCandidate
            {
                PrototypeName = "PortalToBovineheim",
                DisplayName = "Portal To Bovineheim",
                SourceFamily = "Bovineheim / GShop",
                IsLikelyUnusedOrLowRisk = false,
                IsShopLinked = true,
                SupportsRandomThemeIdentity = false,
                PatcherFriendly = true,
                Recommendation = "avoid-direct",
                Notes = "Good reference for portal behavior, but less attractive as a direct GRIFT launcher because it is already tied to Bovineheim and shop flavor."
            });

            RegisterLauncherItemCandidate(new MythicRiftLauncherItemCandidate
            {
                PrototypeName = "DevOnlyCowKingReward",
                DisplayName = "Dev Only Cow King Reward",
                SourceFamily = "Test / DevOnly",
                IsLikelyUnusedOrLowRisk = true,
                IsShopLinked = false,
                SupportsRandomThemeIdentity = false,
                PatcherFriendly = true,
                Recommendation = "research-only",
                Notes = "Interesting because it looks safely non-player-facing today, but it is reward-oriented rather than a clean portal launcher, so it is better as a research lead than as the final item."
            });
        }

        private static MythicRiftPortalLaunchPlan BuildLaunchPlan(MythicRiftEntryPointDefinition entryPoint, MythicRiftEntryRequest request)
        {
            if (entryPoint == null)
                return null;

            string launcherItemPrototypeName = request?.HasLauncherItemPrototypeName == true
                ? request.LauncherItemPrototypeName
                : entryPoint.CandidateItemPrototypeName;

            return new MythicRiftPortalLaunchPlan
            {
                EntryPointId = entryPoint.Id,
                LaunchModel = entryPoint.LaunchModel,
                LauncherItemPrototypeName = launcherItemPrototypeName,
                TransitionPrototypeName = entryPoint.CandidateTransitionPrototypeName,
                ConsumesLauncherItem = string.Equals(entryPoint.LaunchModel, "consumable-portal", StringComparison.OrdinalIgnoreCase),
                CreatesPrivatePortal = string.Equals(entryPoint.LaunchModel, "consumable-portal", StringComparison.OrdinalIgnoreCase),
                RandomContentOnly = entryPoint.AllowsRandomContent && entryPoint.AllowsFixedContentSelection == false,
                IsPatcherFriendly = entryPoint.IsPatcherFriendly,
                Notes = entryPoint.Notes
            };
        }

        private static string DescribeAcceptedLauncherItems(MythicRiftEntryPointDefinition entryPoint)
        {
            if (entryPoint == null)
                return "n/a";

            IReadOnlyList<string> acceptedCandidateItemPrototypeNames = entryPoint.AcceptedCandidateItemPrototypeNames;
            if (acceptedCandidateItemPrototypeNames != null && acceptedCandidateItemPrototypeNames.Count > 0)
                return string.Join(" or ", acceptedCandidateItemPrototypeNames);

            return string.IsNullOrWhiteSpace(entryPoint.CandidateItemPrototypeName)
                ? "n/a"
                : entryPoint.CandidateItemPrototypeName;
        }
    }
}

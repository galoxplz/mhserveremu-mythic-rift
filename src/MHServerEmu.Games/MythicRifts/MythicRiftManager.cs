using Gazillion;
using MHServerEmu.Core.Helpers;
using MHServerEmu.Core.Logging;
using MHServerEmu.Core.Memory;
using MHServerEmu.Core.VectorMath;
using MHServerEmu.Games.Entities;
using MHServerEmu.Games.Entities.Avatars;
using MHServerEmu.Games.Entities.Items;
using MHServerEmu.Games.Events;
using MHServerEmu.Games.GameData;
using MHServerEmu.Games.GameData.Prototypes;
using MHServerEmu.Games.Loot;
using MHServerEmu.Games.Missions;
using MHServerEmu.Games.Navi;
using MHServerEmu.Games.Properties;
using MHServerEmu.Games.Regions;
using MHServerEmu.Games.Social.Parties;
using MHServerEmu.Games.UI;
using MHServerEmu.Games.UI.Widgets;

namespace MHServerEmu.Games.MythicRifts
{
    public sealed class MythicRiftManager
    {
        private static readonly Logger Logger = LogManager.CreateLogger();
        private static readonly bool SuspendNativeTerminalMissionsDuringRifts = true;
        private static readonly bool SuspendNativeRegionEventMissionsDuringRifts = true;
        private static readonly int[] TimeWarningThresholdSeconds = { 540, 480, 420, 360, 300, 240, 180, 120, 60, 30 };
        private static readonly int[] KillProgressMilestonePercents = { 25, 50, 75 };
        private static readonly TimeSpan ParticipantDisconnectAbortGracePeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan PendingRunBindGracePeriod = TimeSpan.FromMinutes(2);
        private static readonly TimeSpan CompletedRunRetention = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan NativeBossSuppressionScanInterval = TimeSpan.FromSeconds(2);
        private static readonly TimeSpan RiftObjectiveWidgetRefreshInterval = TimeSpan.FromMilliseconds(500);
        private static readonly TimeSpan FailedRunEvacuationDelay = TimeSpan.FromMilliseconds(250);
        private static readonly TimeSpan FailedRunEvacuationRetryDelay = TimeSpan.FromSeconds(1);
        private static readonly TimeSpan PlayerDeathTimePenalty = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan CustomRiftPopulationSpawnInterval = TimeSpan.FromSeconds(4);
        private static readonly TimeSpan CheckpointBossSpawnRetryInterval = TimeSpan.FromSeconds(1);
        private const int CustomRiftPopulationBaseTargetAlive = 18;
        private const int CustomRiftPopulationTargetAlivePerExtraPlayer = 4;
        private const int CustomRiftPopulationBaseMaxAlive = 30;
        private const int CustomRiftPopulationMaxAlivePerExtraPlayer = 6;
        private const int CustomRiftPopulationBaseSpawnBatch = 8;
        private const int CustomRiftPopulationSpawnBatchPerExtraPlayer = 2;
        private const float CustomRiftPopulationSpawnMinDistance = 450f;
        private const float CustomRiftPopulationSpawnMaxDistance = 1500f;
        private const float CustomRiftPopulationFallbackSpawnDistance = 700f;
        private const int CheckpointRiftLevelInterval = 5;
        private const int CheckpointBossHealthTierInterval = 10;
        private const float CheckpointBossBaseHealthMultiplier = 2.0f;
        private const float CheckpointBossHealthMultiplierPerTier = 0.15f;
        private const float CheckpointBossMaxHealthMultiplier = 4.0f;
        private const float CheckpointBossSpawnDistance = 320f;
        private const float CheckpointBossSpawnSearchDistance = 180f;
        private const int RiftPopulationRespawnDelayMS = 20000;
        private const int ChampionKillCountCredit = 3;
        private const int EliteKillCountCredit = 5;
        private const int MiniBossKillCountCredit = 8;
        private const int RecentRandomMapHistoryLimit = 4;
        private const ulong RiftEntryBannerLocaleStringBase = 18000000000000000000UL;
        private const int RiftEntryBannerLocalizedLevelLimit = 10000;
        private const int RiftEntryBannerTimeToLiveMS = 5000;
        private const ulong RiftClearedBannerLocaleStringBase = 18000000000000030000UL;
        private const int RiftClearedBannerLocaleStringCount = 20;
        private const int RiftClearedBannerTimeToLiveMS = 2000;
        private const string RiftDangerRoomLevelWidgetPrototypeName = "UI/MetaGame/MissionName.prototype";
        private const string RiftDangerRoomQuotaWidgetPrototypeName = "UI/MetaGame/DangerRoom/DangerRoomCounterBarBASE.prototype";
        private const string RiftDangerRoomTimerWidgetPrototypeName = "UI/MetaGame/DangerRoom/DangerRoomTimer.prototype";
        private const ulong RiftDangerRoomLevelLocaleStringBase = 18000000000000010000UL;
        private const int RiftDangerRoomLevelLocalizedLevelLimit = 10000;
        private static readonly PrototypeId RiftDangerRoomLevelWidgetPrototypeRef = (PrototypeId)7164846210465729875UL;
        private static readonly PrototypeId RiftDangerRoomQuotaWidgetPrototypeRef = (PrototypeId)1488507445230442250UL;
        private static readonly PrototypeId RiftDangerRoomTimerWidgetPrototypeRef = (PrototypeId)15369535438503023451UL;
        private const string RiftExitPortalPrototypeName = "Entity/Transitions/ReturnToLastBaseDR.prototype";
        private const float SpecialRandomMapChance = 0.05f;
        private static readonly HashSet<string> RandomCheckpointContentExclusions = new(StringComparer.OrdinalIgnoreCase);
        private static readonly string[] CustomRiftPopulationMobPrototypeNames =
        {
            "Entity/Characters/Mobs/EndGameRandoms01/ThugEG06.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/MaggiaGoonEG06.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/MaggiaBruiserEG06.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/HydraGunnerEG13.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/HydraPowerBrawlerEG13.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/HydraPlasmaCasterEG13.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/HandNinjaEG11.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/HandAssassinEG11.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/PurifierAcolyteEG10.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/PurifierGrenadierEG10.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/DoombotInfernoEG12.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/ServoGuardRangedEG12.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/BroodSoldierEG08.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/BroodFlyerEG08.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/RaptorEG04.prototype",
            "Entity/Characters/Mobs/EndGameRandoms01/MoloidEG01.prototype"
        };
        private static readonly MythicRiftContentDefinition[] DefaultContentDefinitions =
        {
            new(
                "shocker",
                "Shocker Terminal",
                45,
                "Regions/EndGame/Terminals/Green/ShockerSubway/AltRegions/DailyGShockerSubwayRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G01ShockerSubwayDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD01GShocker.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AbandonedSubway/ShockerTerminalLoot.prototype"),
            new(
                "doctor-octopus",
                "Doctor Octopus Terminal",
                50,
                "Regions/EndGame/Terminals/Green/KingpinsWarehouse/AltRegions/DailyGKPWarehouseRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G02DoctorOctopusDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD02GDoctorOctopus.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/KingpinsWarehouse/DrOctopusTerminalLoot.prototype"),
            new(
                "taskmaster",
                "Taskmaster Terminal",
                50,
                "Regions/EndGame/Terminals/Green/Taskmaster/AltRegions/DailyGTaskmasterRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G03TaskmasterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD03GTaskmaster.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TaskmasterInstitute/TaskmasterTerminalLoot.prototype"),
            new(
                "hood",
                "Hood Terminal",
                55,
                "Regions/EndGame/Terminals/Green/HoodsShip/AltRegions/DailyGHoodsShipRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G04HoodDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD04GHood.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HoodsHideout/HoodTerminalLoot.prototype"),
            new(
                "magneto",
                "Magneto Terminal",
                60,
                "Regions/EndGame/Terminals/Green/MagnetoBunker/AltRegions/DailyGStrykerBunkerRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G05MagnetoDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD05GMagneto.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/StrykerCommandBunker/MagnetoTerminalLoot.prototype",
                RandomMapEligible: false,
                RandomBossEligible: false),
            new(
                "sinister",
                "Mister Sinister Terminal",
                60,
                "Regions/EndGame/Terminals/Green/SinistersLab/AltRegions/DailyGSinisterLabRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G06MisterSinisterDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD06GMrSinister.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/SinisterLab/MisterSinisterTerminalLoot.prototype"),
            new(
                "modok",
                "MODOK Terminal",
                60,
                "Regions/EndGame/Terminals/Green/AIMFacility/AltRegions/DailyGAIMFacilityRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G07MODOKDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD07GMODOK.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AIMWeaponFacility/ModokTerminalLoot.prototype",
                RandomMapEligible: true,
                RandomBossEligible: false),
            new(
                "mandarin",
                "Mandarin Terminal",
                65,
                "Regions/EndGame/Terminals/Green/HYDRAIsland/AltRegions/DailyGHYDRAIslandRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G08MandarinDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD08GMandarin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/HydraIsland/MandarinTerminalLoot.prototype"),
            new(
                "kingpin",
                "Kingpin Terminal",
                65,
                "Regions/EndGame/Terminals/Green/FiskTower/AltRegions/DailyGFiskTowerRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G10FiskTowerDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD10GKingpin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/FiskTower/KingpinLTerminalLoot.prototype"),
            new(
                "ultron",
                "Ultron Terminal",
                70,
                "Regions/EndGame/Terminals/Green/TimesSquare/AltRegions/DailyGTimesSquareRegionL60.prototype",
                "Missions/Prototypes/PVEEndgame/Dailies/Green/G14UltronDailyEndgame.prototype",
                "Entity/Characters/Bosses/PVEDailies/Green/EGD14GUltronTerminal.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TimesSquare/UltronTerminalLoot.prototype",
                RandomMapEligible: false,
                RandomBossEligible: false),
            new(
                "boss-pyro",
                "Pyro",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGD05GPyro.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/StrykerCommandBunker/MagnetoTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-aim-doctor-octopus",
                "A.I.M. Doctor Octopus",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGD07GSBDoctorOctopus.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AIMWeaponFacility/ModokTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-wizard",
                "Wizard",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGD07GSBWizard.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AIMWeaponFacility/ModokTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-bullseye",
                "Bullseye",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGD10GSBBullseye.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/FiskTower/KingpinLTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-elektra",
                "Elektra",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGD10GSBElektra.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/FiskTower/KingpinLTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-black-cat",
                "Black Cat",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGDGSBBlackCat.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/KingpinsWarehouse/DrOctopusTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-blob",
                "Blob",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGDGSBBlob.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/StrykerCommandBunker/MagnetoTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-green-goblin",
                "Green Goblin",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGDGSBGreenGoblin.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/KingpinsWarehouse/DrOctopusTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-rhino",
                "Rhino",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGDGSBRhino.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/AbandonedSubway/ShockerTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "boss-venom",
                "Venom",
                1,
                null,
                null,
                "Entity/Characters/Bosses/PVEDailies/Green/EGDGSBVenom.prototype",
                "Loot/Tables/Mob/Bosses/EndgameDailies/Terminals/TaskmasterInstitute/TaskmasterTerminalLoot.prototype",
                RandomMapEligible: false),
            new(
                "bronx-zoo",
                "Bronx Zoo",
                80,
                "Regions/EndGame/OneShotMissions/NonChapterBound/BronxZoo/AltRegions/BronxZooRegionL60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "wakanda-jungle",
                "Wakanda Jungle",
                65,
                "Regions/EndGame/OneShotMissions/NonChapterBound/WakandaPart1/AltRegions/WakandaP1RegionL60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "hydra-island-one-shot",
                "HYDRA Island One-Shot",
                65,
                "Regions/EndGame/OneShotMissions/NonChapterBound/HydraIslandPartDeux/AltRegions/HYDRAIslandPartDeuxRegionL60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false),
            new(
                "daily-bugle",
                "Daily Bugle Operation",
                55,
                "Regions/Operations/Events/DailyBugle/OpDailyBugleRegionL11To60.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                UseCustomPopulation: true),
            new(
                "dr-strange-times-square",
                "Doctor Strange Times Square",
                45,
                "Regions/EndGame/StaticScenarios/DrStrangeEvent/Cosmic/DrStrangeTimesSquareRegionCosmic.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                IsSpecialRandomMap: true,
                UseCustomPopulation: true),
            new(
                "civil-war-airport-cap",
                "Civil War Airport - Captain America",
                60,
                "Regions/EndGame/StaticScenarios/CivilWar/Airport/AltRegions/AirportCap/CivilWarAirportCaptainAmericaRegion60Cosmic.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                UseCustomPopulation: true,
                MaxPlayerCount: 1),
            new(
                "civil-war-airport-iron-man",
                "Civil War Airport - Iron Man",
                60,
                "Regions/EndGame/StaticScenarios/CivilWar/Airport/AltRegions/AirportIronMan/CivilWarAirportIronManRegion60Cosmic.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                UseCustomPopulation: true,
                MaxPlayerCount: 1),
            new(
                "civil-war-bazaar",
                "Civil War Bazaar",
                60,
                "Regions/EndGame/StaticScenarios/CivilWar/Bazaar/AltRegions/CivilWarBazaarRegion60Cosmic.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                UseCustomPopulation: true,
                MaxPlayerCount: 1),
            new(
                "march-to-axis",
                "March to Axis",
                75,
                "Regions/RAIDS/AxisRaid/AxisRaidRegionGreen.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                IsSpecialRandomMap: true,
                UseCustomPopulation: true,
                MinRandomRiftLevel: 15),
            new(
                "muspelheim-raid",
                "Muspelheim Raid",
                75,
                "Regions/RAIDS/MuspelheimRaid/SurturRaidRegionGreen.prototype",
                null,
                null,
                null,
                RandomBossEligible: false,
                IsSpecialRandomMap: true,
                UseCustomPopulation: true,
                MinRandomRiftLevel: 15),
            new(
                "cosmic-doop-sector",
                "Cosmic Doop Sector",
                100,
                "Regions/EndGame/Special/CosmicDoopSectorSpace/CosmicDoopSectorSpaceRegion.prototype",
                "Missions/Prototypes/BonusMissions/OMDoopZone.prototype",
                "Entity/Characters/Mobs/DoopAllChapters/CosmicDoop/CosmicDoopOverlord.prototype",
                "Loot/Tables/Mob/NormalMobs/CosmicDoopOverlordTable.prototype",
                RandomBossEligible: false,
                IsSpecialRandomMap: true,
                UseOwnBossSourceWhenSelected: true,
                MinRandomRiftLevel: 25),
            new(
                "sabretooth-showdown",
                "Sabretooth Showdown",
                45,
                "Regions/StoryRevamp/CH07SavageLand/CH0705SabretoothShowdownRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "supervillain-rec-center",
                "Supervillain Rec Center",
                45,
                "Regions/StoryRevamp/CH05MutantTown/CH0503SupervillainRecCenterRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "sc-kill-house",
                "Stryker Kill House",
                50,
                "Regions/StoryRevamp/CH06FortStryker/TreasureRooms/TRKillHouse/SCKillHouseRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "sc-missile-silo",
                "Stryker Missile Silo",
                50,
                "Regions/StoryRevamp/CH06FortStryker/TreasureRooms/TRMissileSilo/SCMissileSiloRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "sc-mineshaft",
                "Stryker Mineshaft",
                45,
                "Regions/StoryRevamp/CH06FortStryker/TreasureRooms/TRMineshaft/SCMineshaftRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "sc-dino-graveyard",
                "Dino Graveyard",
                55,
                "Regions/StoryRevamp/CH07SavageLand/TreasureRooms/TRDinoGraveyard/SCDinoGraveyardRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "sc-fire-swamp",
                "Fire Swamp",
                55,
                "Regions/StoryRevamp/CH07SavageLand/TreasureRooms/TRFireSwamp/SCFireSwampRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "tr-asgard-estate",
                "Asgard Estate",
                45,
                "Regions/StoryRevamp/CH09Asgard/TreasureRooms/TRAsgardEstate/TREstateRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "tr-norway-tomb",
                "Norway Tomb",
                45,
                "Regions/StoryRevamp/CH09Asgard/TreasureRooms/TRNorwayTomb/TRTombRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
            new(
                "tr-sacred-dojo",
                "Sacred Dojo",
                40,
                "Regions/StoryRevamp/CH03Madripoor/TreasureRooms/BambooForest/SacredDojo/TRSacredDojoRegion.prototype",
                null,
                null,
                null,
                RandomMapEligible: false,
                RandomBossEligible: false,
                BossOnlyCheckpointEligible: true),
        };

        private readonly List<MythicRiftContentEntry> _contentPool = new();
        private readonly Dictionary<ulong, MythicRiftRunState> _activeRuns = new();
        private readonly Dictionary<ulong, Event<EntityDeadGameEvent>.Action> _regionEntityDeadActions = new();
        private readonly Dictionary<ulong, int> _highestUnlockedRiftLevelByPlayer = new();
        private readonly Dictionary<ulong, int> _preferredLaunchRiftLevelByPlayer = new();
        private readonly Dictionary<ulong, string> _lastCompletedMapContentIdByPlayer = new();
        private readonly Dictionary<ulong, List<string>> _recentRandomMapContentIdsByPlayer = new();
        private readonly Dictionary<ulong, TimeSpan> _nextNativeBossSuppressionScanAt = new();
        private readonly Dictionary<ulong, TimeSpan> _nextRiftObjectiveWidgetRefreshAt = new();
        private readonly Dictionary<ulong, TimeSpan> _pendingFailedRunEvacuationsAt = new();
        private readonly Dictionary<ulong, TimeSpan> _nextCheckpointBossSpawnRetryAt = new();
        private readonly Dictionary<ulong, HashSet<Mission>> _serverSuspendedNativeObjectiveMissionsByRun = new();
        private readonly Dictionary<string, IReadOnlyList<PrototypeId>> _rewardItemPoolsByDirectory = new(StringComparer.OrdinalIgnoreCase);
        private static PrototypeId _cachedRiftDangerRoomLevelWidgetPrototypeRef = PrototypeId.Invalid;
        private static PrototypeId _cachedRiftDangerRoomQuotaWidgetPrototypeRef = PrototypeId.Invalid;
        private static PrototypeId _cachedRiftDangerRoomTimerWidgetPrototypeRef = PrototypeId.Invalid;
        private static PrototypeId[] _cachedCustomRiftPopulationMobPrototypeRefs;
        private MythicRiftRewardTuning _rewardTuning = MythicRiftRewardTuning.CreateDefault();
        private string _rewardTuningLastLoadMessage = "Using built-in default Cosmic Rift reward tuning.";
        private ulong _nextRunId = 1;

        public Game Game { get; }

        public MythicRiftManager(Game game)
        {
            Game = game;
            RegisterDefaultContent();
            TryReloadRewardTuning(out _rewardTuningLastLoadMessage);
            Logger.Info($"Mythic Rift reward tuning: {_rewardTuningLastLoadMessage}");
        }

        public IReadOnlyList<MythicRiftContentEntry> ContentPool => _contentPool;
        public IReadOnlyList<MythicRiftContentEntry> RandomEligibleContentPool => RandomMapEligibleContentPool;
        public IReadOnlyList<MythicRiftContentEntry> RandomMapEligibleContentPool => _contentPool.Where(entry => entry.RandomMapEligible).ToList();
        public IReadOnlyList<MythicRiftContentEntry> RandomBossEligibleContentPool => _contentPool.Where(entry => entry.RandomBossEligible && entry.HasValidBossSource).ToList();
        public IReadOnlyCollection<MythicRiftRunState> ActiveRuns => _activeRuns.Values;
        public MythicRiftRewardTuning RewardTuning => _rewardTuning;
        public string RewardTuningLastLoadMessage => _rewardTuningLastLoadMessage;
        public MythicRiftDifficultySnapshot GetDifficultySnapshot(
            int riftLevel,
            int requestedPlayerCount,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            return MythicRiftScaling.BuildSnapshot(
                riftLevel,
                requestedPlayerCount,
                mode == MythicRiftMode.Endless);
        }

        public int GetHighestUnlockedRiftLevel(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 1;

            Player onlinePlayer = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (onlinePlayer != null)
            {
                int persistentLevel = onlinePlayer.MythicRiftHighestUnlockedLevel;
                if (_highestUnlockedRiftLevelByPlayer.TryGetValue(playerDbId, out int cachedUnlockedLevel))
                    return Math.Max(Math.Max(cachedUnlockedLevel, persistentLevel), 1);

                return Math.Max(persistentLevel, 1);
            }

            return _highestUnlockedRiftLevelByPlayer.TryGetValue(playerDbId, out int unlockedLevel)
                ? Math.Max(unlockedLevel, 1)
                : 1;
        }

        public bool CanAccessRiftLevel(ulong playerDbId, int riftLevel)
        {
            if (riftLevel <= 0)
                return false;

            return riftLevel <= GetHighestUnlockedRiftLevel(playerDbId);
        }

        public int GetPreferredLaunchRiftLevel(ulong playerDbId)
        {
            int highestUnlockedLevel = GetHighestUnlockedRiftLevel(playerDbId);
            if (playerDbId == 0)
                return highestUnlockedLevel;

            if (_preferredLaunchRiftLevelByPlayer.TryGetValue(playerDbId, out int preferredLevel) == false || preferredLevel <= 0)
                return highestUnlockedLevel;

            return Math.Min(Math.Max(preferredLevel, 1), highestUnlockedLevel);
        }

        public bool TrySetPreferredLaunchRiftLevel(ulong playerDbId, int riftLevel, out int appliedLevel, out string errorMessage)
        {
            appliedLevel = GetPreferredLaunchRiftLevel(playerDbId);
            errorMessage = string.Empty;

            if (playerDbId == 0)
            {
                errorMessage = "Player not found.";
                return false;
            }

            if (riftLevel <= 0)
            {
                errorMessage = "Rift level must be greater than zero.";
                return false;
            }

            int highestUnlockedLevel = GetHighestUnlockedRiftLevel(playerDbId);
            if (riftLevel > highestUnlockedLevel)
            {
                errorMessage = $"Requested Rift level {riftLevel} is locked. Highest unlocked level: {highestUnlockedLevel}.";
                return false;
            }

            _preferredLaunchRiftLevelByPlayer[playerDbId] = riftLevel;
            appliedLevel = riftLevel;
            return true;
        }

        public int UseHighestUnlockedLaunchRiftLevel(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 1;

            _preferredLaunchRiftLevelByPlayer.Remove(playerDbId);
            return GetHighestUnlockedRiftLevel(playerDbId);
        }

        public bool ConsumePreferredLaunchRiftLevel(ulong playerDbId)
        {
            if (playerDbId == 0)
                return false;

            return _preferredLaunchRiftLevelByPlayer.Remove(playerDbId);
        }

        public int SetHighestUnlockedRiftLevel(ulong playerDbId, int unlockedLevel, bool allowDecrease = false)
        {
            if (playerDbId == 0)
                return 1;

            int normalizedLevel = Math.Max(unlockedLevel, 1);
            if (allowDecrease == false)
                normalizedLevel = Math.Max(normalizedLevel, GetHighestUnlockedRiftLevel(playerDbId));

            _highestUnlockedRiftLevelByPlayer[playerDbId] = normalizedLevel;
            SyncOnlinePlayerRiftLevel(playerDbId, normalizedLevel);
            return normalizedLevel;
        }

        public int ResetRiftProgress(ulong playerDbId)
        {
            if (playerDbId == 0)
                return 1;

            _preferredLaunchRiftLevelByPlayer.Remove(playerDbId);
            return SetHighestUnlockedRiftLevel(playerDbId, 1, allowDecrease: true);
        }

        public int GrantNextRiftLevel(ulong playerDbId, int completedLevel)
        {
            if (playerDbId == 0)
                return 1;

            int currentUnlockedLevel = GetHighestUnlockedRiftLevel(playerDbId);
            int nextUnlockedLevel = MythicRiftProgression.ResolveNextUnlockedLevel(currentUnlockedLevel, completedLevel);
            if (nextUnlockedLevel <= currentUnlockedLevel)
                return currentUnlockedLevel;

            _highestUnlockedRiftLevelByPlayer[playerDbId] = nextUnlockedLevel;
            SyncOnlinePlayerRiftLevel(playerDbId, nextUnlockedLevel);
            return nextUnlockedLevel;
        }

        public MythicRiftRunConfig CreateDebugRunConfig(
            string contentId,
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            MythicRiftContentEntry content = GetContent(contentId);
            if (content == null)
                return null;

            MythicRiftContentEntry bossContent = SelectBossContentForFixedMap(content);

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit, mode);
        }

        public MythicRiftRunConfig CreateDebugRunConfig(
            string contentId,
            string bossContentId,
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            MythicRiftContentEntry content = GetContent(contentId);
            MythicRiftContentEntry bossContent = GetContent(bossContentId);
            if (content == null || bossContent == null)
                return null;

            if (bossContent.HasValidBossSource == false)
                return null;

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit, mode);
        }

        public MythicRiftRunConfig CreateRandomDebugRunConfig(
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            IReadOnlyCollection<string> excludedMapContentIds = null,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            MythicRiftContentEntry content = SelectRandomMapContent(riftLevel, requestedPlayerCount, excludedMapContentIds);
            MythicRiftContentEntry bossContent = SelectBossContentForRandomMap(content);
            if (content == null || bossContent == null)
                return null;

            return CreateRunConfig(content, bossContent, riftLevel, requestedPlayerCount, killQuota, timeLimit, mode);
        }

        public MythicRiftRunState CreateRunState(MythicRiftRunConfig config)
        {
            if (config == null || config.IsValid == false)
                return null;

            return new MythicRiftRunState(config);
        }

        public MythicRiftRunState CreateDebugRun(
            string contentId,
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            MythicRiftRunConfig config = CreateDebugRunConfig(contentId, riftLevel, requestedPlayerCount, killQuota, timeLimit, mode);
            return RegisterRun(config);
        }

        public MythicRiftRunState CreateDebugRun(
            string contentId,
            string bossContentId,
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            MythicRiftRunConfig config = CreateDebugRunConfig(contentId, bossContentId, riftLevel, requestedPlayerCount, killQuota, timeLimit, mode);
            return RegisterRun(config);
        }

        public MythicRiftRunState CreateRandomDebugRun(
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            IReadOnlyCollection<string> excludedMapContentIds = null,
            MythicRiftMode mode = MythicRiftMode.Standard)
        {
            MythicRiftRunConfig config = CreateRandomDebugRunConfig(riftLevel, requestedPlayerCount, killQuota, timeLimit, excludedMapContentIds, mode);
            return RegisterRun(config);
        }

        public MythicRiftRunState RequestRun(Player player, int riftLevel, int killQuota, TimeSpan timeLimit, out string errorMessage)
        {
            return RequestRun(player, riftLevel, killQuota, timeLimit, MythicRiftMode.Standard, out errorMessage);
        }

        public MythicRiftRunState RequestRun(
            Player player,
            int riftLevel,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode,
            out string errorMessage)
        {
            return RequestRunInternal(player, null, riftLevel, killQuota, timeLimit, mode, useRandomContent: true, out errorMessage);
        }

        public MythicRiftRunState RequestFixedRun(Player player, string contentId, int riftLevel, int killQuota, TimeSpan timeLimit, out string errorMessage)
        {
            return RequestFixedRun(player, contentId, riftLevel, killQuota, timeLimit, MythicRiftMode.Standard, out errorMessage);
        }

        public MythicRiftRunState RequestFixedRun(
            Player player,
            string contentId,
            int riftLevel,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode,
            out string errorMessage)
        {
            return RequestRunInternal(player, contentId, riftLevel, killQuota, timeLimit, mode, useRandomContent: false, out errorMessage);
        }

        public MythicRiftRunState GetRun(ulong runId)
        {
            if (runId == 0)
                return null;

            return _activeRuns.TryGetValue(runId, out MythicRiftRunState runState) ? runState : null;
        }

        public MythicRiftRunState GetInProgressRunForPlayer(ulong playerDbId)
        {
            if (playerDbId == 0)
                return null;

            return _activeRuns.Values.FirstOrDefault(runState =>
                runState.IsInProgress &&
                runState.ParticipantPlayerDbIds.Contains(playerDbId));
        }

        public bool CanLaunchFromCompletedRiftRegion(Player player)
        {
            if (player == null || player.DatabaseUniqueId == 0)
                return false;

            Region region = player.GetRegion();
            if (region == null)
                return false;

            return _activeRuns.Values.Any(runState =>
                runState.Status == MythicRiftRunStatus.Success &&
                runState.RegionId == region.Id &&
                runState.HasParticipantLeftEarly(player.DatabaseUniqueId) == false &&
                runState.ParticipantPlayerDbIds.Contains(player.DatabaseUniqueId));
        }

        public bool RemoveRun(ulong runId)
        {
            if (runId == 0)
                return false;

            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            ulong regionId = runState.RegionId;
            SendStopRiftTimer(runState);
            TryRestoreRegionDifficultyScaling(runState);
            RestoreSuspendedNativeObjectiveMissions(runState);
            RequestRunRegionShutdownWhenVacant(runState);
            bool removed = _activeRuns.Remove(runId);

            if (removed && regionId != 0)
                CleanupRegionListener(regionId);

            if (removed)
            {
                _nextNativeBossSuppressionScanAt.Remove(runId);
                _nextRiftObjectiveWidgetRefreshAt.Remove(runId);
                _nextCheckpointBossSpawnRetryAt.Remove(runId);
                _serverSuspendedNativeObjectiveMissionsByRun.Remove(runId);
                _pendingFailedRunEvacuationsAt.Remove(runId);
            }

            return removed;
        }

        public bool StartRun(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            runState.Start(currentTime);
            if (runState.Status == MythicRiftRunStatus.Active)
            {
                SendStartRiftTimer(runState);
                SuppressNativeTerminalBosses(runState, currentTime, force: true);
                SuppressNativeCheckpointPopulation(runState);
                RefreshRiftObjectiveWidgets(runState, currentTime, force: true);
                NotifyRunStarted(runState);
                TryStartBossOnlyCheckpoint(runState, currentTime);
            }

            return runState.Status == MythicRiftRunStatus.Active;
        }

        public bool AddKills(ulong runId, int killCount)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || killCount <= 0)
                return false;

            runState.AddKills(killCount);
            RefreshRiftHudWidgets(runState, Game.CurrentTime);
            return true;
        }

        public bool MarkRunSuccess(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return CompleteRunSuccess(runState, currentTime);
        }

        public bool MarkRunFailed(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return CompleteRunFailure(runState, currentTime, "Time expired. No completion rewards.", returnParticipantsToHub: true);
        }

        public bool MarkRunAborted(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return AbortRun(runState, currentTime, "The Rift was abandoned. A new Beacon is required to start another run.");
        }

        public int ReturnRunParticipantsToDangerRoomHub(MythicRiftRunState runState, Player fallbackPlayer = null, bool includePlayersAlreadyOutsideRunRegion = true)
        {
            if (runState == null || TryResolveDangerRoomHubStartTarget(out PrototypeId dangerRoomHubStartTarget) == false)
                return 0;

            HashSet<ulong> playerDbIds = new(runState.ParticipantPlayerDbIds);
            if (runState.RegionId != 0)
            {
                Region region = Game.RegionManager.GetRegion(runState.RegionId);
                if (region != null)
                {
                    foreach (Player regionPlayer in new PlayerIterator(region))
                    {
                        if (regionPlayer?.DatabaseUniqueId != 0)
                            playerDbIds.Add(regionPlayer.DatabaseUniqueId);
                    }
                }
            }

            if (playerDbIds.Count == 0 && fallbackPlayer?.DatabaseUniqueId != 0)
                playerDbIds.Add(fallbackPlayer.DatabaseUniqueId);

            int teleportedPlayerCount = 0;
            foreach (ulong playerDbId in playerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player?.CurrentAvatar?.IsInWorld != true)
                    continue;

                if (includePlayersAlreadyOutsideRunRegion == false && IsPlayerInRunRegion(player, runState) == false)
                    continue;

                if (EnsurePlayerAvatarAliveForRiftExit(player, runState, "return-to-hub") == false)
                    continue;

                using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
                teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Resurrect);
                teleporter.DifficultyTierRef = GameDatabase.GlobalsPrototype.DifficultyTierDefault;
                if (teleporter.TeleportToTarget(dangerRoomHubStartTarget))
                    teleportedPlayerCount++;
            }

            return teleportedPlayerCount;
        }

        private static bool EnsurePlayerAvatarAliveForRiftExit(Player player, MythicRiftRunState runState, string context)
        {
            Avatar avatar = player?.CurrentAvatar;
            if (avatar == null || avatar.IsDead == false)
                return true;

            bool resurrected = avatar.Resurrect();
            if (avatar.IsDead == false)
            {
                Logger.Info($"Mythic Rift run {runState?.Config?.RunId ?? 0} cleared dead avatar state before {context} for playerDbId=0x{player.DatabaseUniqueId:X}. resurrectResult={resurrected}");
                return true;
            }

            Logger.Warn($"Mythic Rift run {runState?.Config?.RunId ?? 0} failed to resurrect dead avatar before {context} for playerDbId=0x{player.DatabaseUniqueId:X}.");
            return false;
        }

        public bool AbortRunWithReason(ulong runId, TimeSpan currentTime, string reason)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null)
                return false;

            return AbortRun(runState, currentTime, reason);
        }

        public bool GrantRewardsToPlayer(ulong runId, Player player)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || player == null)
                return false;

            if (runState.Status is not (MythicRiftRunStatus.Success or MythicRiftRunStatus.Failed))
                return false;

            if (runState.IsRewardEligible(player.DatabaseUniqueId) == false)
                return false;

            if (runState.HasRewardForPlayer(player.DatabaseUniqueId))
                return false;

            MythicRiftRewardOutcome rewardOutcome = runState.RewardOutcome ?? ResolveRewardOutcome(runState);
            if (rewardOutcome == null || rewardOutcome.HasAnyLoot == false)
                return false;

            Avatar avatar = player.CurrentAvatar;
            if (avatar == null)
                return false;

            PropertyId rarityPropertyId = new(PropertyEnum.LootBonusRarityPct);
            PropertyId specialPropertyId = new(PropertyEnum.LootBonusSpecialPct);

            float originalRarity = avatar.Properties[PropertyEnum.LootBonusRarityPct];
            float originalSpecial = avatar.Properties[PropertyEnum.LootBonusSpecialPct];
            bool hadRarityProperty = avatar.Properties.HasProperty(PropertyEnum.LootBonusRarityPct);
            bool hadSpecialProperty = avatar.Properties.HasProperty(PropertyEnum.LootBonusSpecialPct);

            try
            {
                if (rewardOutcome.BonusRarityPct > 0f)
                    avatar.Properties.AdjustProperty(rewardOutcome.BonusRarityPct, rarityPropertyId);

                if (rewardOutcome.BonusSpecialPct > 0f)
                    avatar.Properties.AdjustProperty(rewardOutcome.BonusSpecialPct, specialPropertyId);

                using LootInputSettings inputSettings = MHServerEmu.Core.Memory.ObjectPoolManager.Instance.Get<LootInputSettings>();
                inputSettings.Initialize(LootContext.Drop, player, avatar);

                int groundRecipientId = 1;
                if (rewardOutcome.HasBossLootTable)
                    GrantRewardLootTable(rewardOutcome.BossLootTableProtoRef, inputSettings, rewardOutcome.BossLootDelivery, ref groundRecipientId);

                foreach (MythicRiftRewardExtraLootTable extraLootTable in rewardOutcome.ExtraLootTables)
                {
                    for (int i = 0; i < extraLootTable.Rolls; i++)
                        GrantRewardLootTable(extraLootTable.LootTableProtoRef, inputSettings, extraLootTable.Delivery, ref groundRecipientId);
                }

                foreach (MythicRiftRewardGuaranteedItem guaranteedItem in rewardOutcome.GuaranteedItems)
                {
                    for (int i = 0; i < guaranteedItem.Quantity; i++)
                        GrantRewardItem(guaranteedItem.ItemProtoRef, player, avatar, guaranteedItem.Delivery, guaranteedItem.ItemLevel);
                }

                runState.MarkRewardGrantedToPlayer(player.DatabaseUniqueId);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} granted rewards to player {player}. profile={rewardOutcome.RewardProfileName ?? "default"} bossLootSource={rewardOutcome.BossLootTableSourceId ?? "native-boss"} bossDelivery={rewardOutcome.BossLootDelivery ?? "inventory"} extraTables={rewardOutcome.ExtraLootTables.Count} guaranteedItems={rewardOutcome.GuaranteedItems.Count}");
                return true;
            }
            finally
            {
                if (hadRarityProperty)
                    avatar.Properties[PropertyEnum.LootBonusRarityPct] = originalRarity;
                else
                    avatar.Properties.RemoveProperty(rarityPropertyId);

                if (hadSpecialProperty)
                    avatar.Properties[PropertyEnum.LootBonusSpecialPct] = originalSpecial;
                else
                    avatar.Properties.RemoveProperty(specialPropertyId);
            }
        }

        private void GrantRewardLootTable(PrototypeId lootTableProtoRef, LootInputSettings inputSettings, string delivery, ref int groundRecipientId)
        {
            if (lootTableProtoRef == PrototypeId.Invalid || inputSettings == null)
                return;

            if (MythicRiftRewardTuning.IsGroundDelivery(delivery))
            {
                Game.LootManager.SpawnLootFromTable(lootTableProtoRef, inputSettings, groundRecipientId++);
                return;
            }

            Game.LootManager.GiveLootFromTable(lootTableProtoRef, inputSettings);
        }

        private void GrantRewardItem(PrototypeId itemProtoRef, Player player, Avatar avatar, string delivery, int itemLevel = 1)
        {
            if (itemProtoRef == PrototypeId.Invalid || player == null)
                return;

            if (itemLevel > 1)
            {
                ItemSpec itemSpec = Game.LootManager.CreateItemSpec(itemProtoRef, LootContext.Drop, player, itemLevel);
                if (itemSpec == null)
                {
                    Logger.Warn($"Mythic Rift failed to create reward item {itemProtoRef.GetNameFormatted()} at level {itemLevel}.");
                    return;
                }

                using LootResultSummary lootResultSummary = ObjectPoolManager.Instance.Get<LootResultSummary>();
                lootResultSummary.Add(new LootResult(itemSpec));

                if (MythicRiftRewardTuning.IsGroundDelivery(delivery))
                {
                    using LootInputSettings inputSettings = ObjectPoolManager.Instance.Get<LootInputSettings>();
                    inputSettings.Initialize(LootContext.Drop, player, avatar, itemLevel);
                    Game.LootManager.SpawnLootFromSummary(lootResultSummary, inputSettings);
                }
                else
                {
                    Game.LootManager.GiveLootFromSummary(lootResultSummary, player, PrototypeId.Invalid);
                }

                return;
            }

            if (MythicRiftRewardTuning.IsGroundDelivery(delivery))
            {
                Game.LootManager.SpawnItem(itemProtoRef, LootContext.Drop, player, avatar);
                return;
            }

            Game.LootManager.GiveItem(itemProtoRef, LootContext.Drop, player);
        }

        public int GrantRewardsToRunPlayers(ulong runId)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || runState.Status is not (MythicRiftRunStatus.Success or MythicRiftRunStatus.Failed))
                return 0;

            HashSet<ulong> recipientDbIds = new(runState.RewardEligiblePlayerDbIds);

            int grantedCount = 0;
            foreach (ulong playerDbId in recipientDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player == null)
                    continue;

                if (GrantRewardsToPlayer(runId, player))
                    grantedCount++;
            }

            return grantedCount;
        }

        public bool TryReloadRewardTuning(out string message)
        {
            string configPath = MythicRiftRewardTuning.ConfigPath;
            MythicRiftRewardTuning previousTuning = _rewardTuning ?? MythicRiftRewardTuning.CreateDefault();

            if (File.Exists(configPath) == false)
            {
                _rewardTuning = MythicRiftRewardTuning.CreateDefault();
                _rewardItemPoolsByDirectory.Clear();
                message = $"Reward tuning file not found at {FileHelper.GetRelativePath(configPath)}; using built-in defaults.";
                _rewardTuningLastLoadMessage = message;
                return true;
            }

            MythicRiftRewardTuning loadedTuning = FileHelper.DeserializeJson<MythicRiftRewardTuning>(configPath, MythicRiftRewardTuning.JsonOptions);
            if (loadedTuning == null)
            {
                _rewardTuning = previousTuning;
                message = $"Failed to load reward tuning from {FileHelper.GetRelativePath(configPath)}; keeping previous profile '{previousTuning.ProfileName}'.";
                _rewardTuningLastLoadMessage = message;
                return false;
            }

            loadedTuning.Normalize();
            _rewardTuning = loadedTuning.Enabled ? loadedTuning : MythicRiftRewardTuning.CreateDefault();
            _rewardItemPoolsByDirectory.Clear();
            message = loadedTuning.Enabled
                ? $"Loaded reward tuning profile '{_rewardTuning.ProfileName}' from {FileHelper.GetRelativePath(configPath)}. primaryOverrides={_rewardTuning.PrimaryLootTableOverrides.Count} extraLootTables={_rewardTuning.ExtraLootTables.Count} rewardRecipes={_rewardTuning.RewardRecipes.Count} randomItemPools={_rewardTuning.RandomItemPools.Count} guaranteedItems={_rewardTuning.GuaranteedItems.Count}"
                : $"Reward tuning file loaded but disabled; using built-in defaults. path={FileHelper.GetRelativePath(configPath)}";
            _rewardTuningLastLoadMessage = message;
            return true;
        }

        public List<string> BuildRewardTuningDiagnostics()
        {
            MythicRiftRewardTuning tuning = _rewardTuning ?? MythicRiftRewardTuning.CreateDefault();
            List<string> lines = new()
            {
                $"rewardTuningPath={FileHelper.GetRelativePath(MythicRiftRewardTuning.ConfigPath)}",
                $"lastLoad={_rewardTuningLastLoadMessage}",
                $"profile={tuning.ProfileName} | enabled={tuning.Enabled}",
                $"bossLootOnSuccess={tuning.GrantBossLootOnSuccess} | bossLootOnFailure={tuning.GrantBossLootOnFailure}",
                $"suppressNativeRiftBossLoot={tuning.SuppressNativeRiftBossLoot}",
                $"defaultDelivery={tuning.DefaultDelivery} | primaryLootTableOverrides={tuning.PrimaryLootTableOverrides.Count}",
                $"timedSuccessBonusRIF={tuning.TimedSuccessBonusRarityPct:P0} | timedSuccessBonusSIF={tuning.TimedSuccessBonusSpecialPct:P0}",
                $"checkpointBonusRIF={tuning.CheckpointSuccessBonusRarityPct:P0} | checkpointBonusSIF={tuning.CheckpointSuccessBonusSpecialPct:P0}",
                $"failureBonusRIF={tuning.FailureBonusRarityPct:P0} | failureBonusSIF={tuning.FailureBonusSpecialPct:P0}",
                $"extraLootTables={tuning.ExtraLootTables.Count} | rewardRecipes={tuning.RewardRecipes.Count} | randomItemPools={tuning.RandomItemPools.Count} | guaranteedItems={tuning.GuaranteedItems.Count} | lootTableAliases={tuning.LootTableAliases.Count}"
            };

            foreach (MythicRiftPrimaryLootTableTuning entry in tuning.PrimaryLootTableOverrides.Take(20))
            {
                string maxLevelText = entry.MaxRiftLevel > 0 ? entry.MaxRiftLevel.ToString() : "none";
                lines.Add(
                    $"primaryLoot id={entry.Id} | enabled={entry.Enabled} | delivery={entry.Delivery} | min={entry.MinRiftLevel} | max={maxLevelText} | checkpointOnly={entry.CheckpointOnly} | classicOnly={entry.ClassicOnly} | lootTable={entry.LootTablePrototype}");
            }

            if (tuning.PrimaryLootTableOverrides.Count > 20)
                lines.Add($"... {tuning.PrimaryLootTableOverrides.Count - 20} more primary loot table override entries omitted.");

            foreach (MythicRiftExtraLootTableTuning entry in tuning.ExtraLootTables.Take(20))
            {
                string maxLevelText = entry.MaxRiftLevel > 0 ? entry.MaxRiftLevel.ToString() : "none";
                lines.Add(
                    $"extraLoot id={entry.Id} | enabled={entry.Enabled} | delivery={entry.Delivery} | chance={entry.ChancePercent:0.##}% | rolls={entry.Rolls} | min={entry.MinRiftLevel} | max={maxLevelText} | successOnly={entry.SuccessOnly} | checkpointOnly={entry.CheckpointOnly} | classicOnly={entry.ClassicOnly} | lootTable={entry.LootTablePrototype}");
            }

            if (tuning.ExtraLootTables.Count > 20)
                lines.Add($"... {tuning.ExtraLootTables.Count - 20} more extra loot table entries omitted.");

            foreach (MythicRiftRewardRecipeTuning recipe in tuning.RewardRecipes.Take(20))
            {
                string maxLevelText = recipe.MaxRiftLevel > 0 ? recipe.MaxRiftLevel.ToString() : "none";
                lines.Add(
                    $"rewardRecipe id={recipe.Id} | enabled={recipe.Enabled} | min={recipe.MinRiftLevel} | max={maxLevelText} | successOnly={recipe.SuccessOnly} | checkpointOnly={recipe.CheckpointOnly} | classicOnly={recipe.ClassicOnly} | tables={recipe.Tables.Count} | bosses={string.Join(",", recipe.BossSourceIds)}");
            }

            if (tuning.RewardRecipes.Count > 20)
                lines.Add($"... {tuning.RewardRecipes.Count - 20} more reward recipe entries omitted.");

            foreach (MythicRiftRandomItemPoolTuning itemPool in tuning.RandomItemPools.Take(20))
            {
                string maxLevelText = itemPool.MaxRiftLevel > 0 ? itemPool.MaxRiftLevel.ToString() : "none";
                int candidateCount = ResolveRewardItemPool(itemPool.PrototypeDirectoryPrefix).Count;
                lines.Add(
                    $"randomItemPool id={itemPool.Id} | enabled={itemPool.Enabled} | candidates={candidateCount} | itemLevel={itemPool.ItemLevel} | delivery={itemPool.Delivery} | chance={itemPool.ChancePercent:0.##}% | rolls={itemPool.Rolls} | min={itemPool.MinRiftLevel} | max={maxLevelText} | checkpointOnly={itemPool.CheckpointOnly} | directory={itemPool.PrototypeDirectoryPrefix}");
            }

            if (tuning.RandomItemPools.Count > 20)
                lines.Add($"... {tuning.RandomItemPools.Count - 20} more random item pool entries omitted.");

            foreach (MythicRiftGuaranteedItemTuning item in tuning.GuaranteedItems.Take(20))
            {
                string maxLevelText = item.MaxRiftLevel > 0 ? item.MaxRiftLevel.ToString() : "none";
                string maxWaveText = item.MaxWave > 0 ? item.MaxWave.ToString() : "none";
                lines.Add(
                    $"guaranteedItem id={item.Id} | enabled={item.Enabled} | runtimeId={item.ItemPrototypeRuntimeId} | quantity={item.Quantity} | delivery={item.Delivery} | minLevel={item.MinRiftLevel} | maxLevel={maxLevelText} | minWave={item.MinWave} | maxWave={maxWaveText} | checkpointOnly={item.CheckpointOnly}");
            }

            if (tuning.GuaranteedItems.Count > 20)
                lines.Add($"... {tuning.GuaranteedItems.Count - 20} more guaranteed item entries omitted.");

            return lines;
        }

        public bool EvaluateRunTimer(ulong runId, TimeSpan currentTime)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || runState.HasExpired(currentTime) == false)
                return false;

            return CompleteRunFailure(runState, currentTime, "Time expired. No completion rewards.", returnParticipantsToHub: true);
        }

        public bool TryHandleRiftDeathRelease(Avatar avatar, DeathReleaseRequestType requestType)
        {
            if (avatar == null || requestType != DeathReleaseRequestType.Checkpoint)
                return false;

            Player player = avatar.GetOwnerOfType<Player>();
            if (player == null)
                return false;

            MythicRiftRunState runState = GetInProgressRunForPlayer(player.DatabaseUniqueId);
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return false;

            Region region = avatar.Region;
            if (region == null || region.Id != runState.RegionId)
                return false;

            RegionConnectionTargetPrototype startTargetProto = runState.Config.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();
            if (startTargetProto == null)
                return false;

            Vector3 position = Vector3.Zero;
            Orientation orientation = Orientation.Zero;
            PrototypeId cellRef = GameDatabase.GetDataRefByAsset(startTargetProto.Cell);
            if (region.FindTargetLocation(ref position, ref orientation, startTargetProto.Area, cellRef, startTargetProto.Entity) == false)
                return false;

            position = RegionLocation.ProjectToFloor(region, position);

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Resurrect);
            teleporter.DifficultyTierRef = region.DifficultyTierRef;

            bool teleported = teleporter.TeleportToRegionLocation(region.Id, position);
            if (teleported)
                Logger.Info($"Mythic Rift run {runState.Config.RunId} handled death release inside Rift region for playerDbId=0x{player.DatabaseUniqueId:X} target={runState.Config.StartTargetProtoRef.GetNameFormatted()}.");

            return teleported;
        }

        public bool AttachRunToRegion(ulong runId, Region region)
        {
            MythicRiftRunState runState = GetRun(runId);
            if (runState == null || region == null)
                return false;

            runState.AttachRegion(region.Id);
            RegisterRegionPlayersAsParticipants(runState, region);
            ApplyRunDifficultyToRegion(runState, region);
            EnsureRegionListener(region);
            if (runState.Config.Content.BossOnlyCheckpointEligible == false)
                EnableRiftPopulationRespawns(runState, region);

            return true;
        }

        public void Update(TimeSpan currentTime)
        {
            List<ulong> runsToRemove = null;

            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                TryProcessPendingFailedRunEvacuation(runState, currentTime);
                RegisterBoundRegionPlayersAsParticipants(runState);
                UpdateParticipantPresence(runState, currentTime);
                TryAutoBindAndStartPendingRun(runState, currentTime);
                TryApplyRunDifficultyToBoundRegion(runState);
                MaintainCustomRiftPopulation(runState, currentTime);
                TrySpawnPendingMilestoneEncounters(runState);
                SuppressNativeCheckpointPopulation(runState);
                TryStartBossOnlyCheckpoint(runState, currentTime);
                TryMaintainBossWave(runState, currentTime);
                SuppressNativeTerminalBosses(runState, currentTime);
                RefreshRiftObjectiveWidgets(runState, currentTime);

                if (runState.HasExpired(currentTime))
                {
                    CompleteRunFailure(runState, currentTime, "Time expired. No completion rewards.", returnParticipantsToHub: true);
                }
                else
                {
                    TryNotifyRunTimeWarnings(runState, currentTime);
                }

                if (TryAbortRunForDisconnectedParticipants(runState, currentTime))
                    continue;

                if (TryHandleParticipantExit(runState, currentTime))
                {
                    runsToRemove ??= new();
                    runsToRemove.Add(runState.Config.RunId);
                    continue;
                }

                if (TryAbortStalePendingRun(runState, currentTime))
                    continue;

                if (ShouldRemoveCompletedRunBecauseRegionIsEmpty(runState))
                {
                    runsToRemove ??= new();
                    runsToRemove.Add(runState.Config.RunId);
                    continue;
                }

                if (ShouldAutoRemoveRun(runState, currentTime) == false)
                    continue;

                runsToRemove ??= new();
                runsToRemove.Add(runState.Config.RunId);
            }

            if (runsToRemove == null)
                return;

            foreach (ulong runId in runsToRemove)
            {
                if (RemoveRun(runId))
                    Logger.Info($"Mythic Rift run {runId} was removed automatically after retention cleanup.");
            }
        }

        private void QueueFailedRunEvacuation(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState?.Config == null)
                return;

            _pendingFailedRunEvacuationsAt[runState.Config.RunId] = currentTime + FailedRunEvacuationDelay;
        }

        private void TryProcessPendingFailedRunEvacuation(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState?.Config == null ||
                _pendingFailedRunEvacuationsAt.TryGetValue(runState.Config.RunId, out TimeSpan evacuationAt) == false ||
                currentTime < evacuationAt)
            {
                return;
            }

            int returnedPlayerCount = ReturnRunParticipantsToDangerRoomHub(runState, includePlayersAlreadyOutsideRunRegion: false);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} returned {returnedPlayerCount} online participant(s) to the Danger Room hub after timer failure.");

            if (HasAnyPlayerInRunRegion(runState))
            {
                _pendingFailedRunEvacuationsAt[runState.Config.RunId] = currentTime + FailedRunEvacuationRetryDelay;
                return;
            }

            _pendingFailedRunEvacuationsAt.Remove(runState.Config.RunId);
            RequestRunRegionShutdownWhenVacant(runState);
        }

        public MythicRiftContentEntry GetContent(string contentId)
        {
            if (string.IsNullOrWhiteSpace(contentId))
                return null;

            return _contentPool.FirstOrDefault(entry => entry.Id.Equals(contentId, StringComparison.OrdinalIgnoreCase));
        }

        public void RegisterContent(MythicRiftContentEntry content)
        {
            if (content == null || content.IsValid == false)
            {
                Logger.Warn("RegisterContent(): invalid mythic rift content entry");
                return;
            }

            if (GetContent(content.Id) != null)
            {
                Logger.Warn($"RegisterContent(): duplicate mythic rift content id={content.Id}");
                return;
            }

            _contentPool.Add(content);
        }

        private MythicRiftRunState RegisterRun(MythicRiftRunConfig config)
        {
            MythicRiftRunState runState = CreateRunState(config);
            if (runState == null)
                return null;

            runState.SetRegisteredAt(Game.CurrentTime);
            _activeRuns[runState.Config.RunId] = runState;
            return runState;
        }

        private MythicRiftRunState RequestRunInternal(
            Player player,
            string contentId,
            int riftLevel,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode,
            bool useRandomContent,
            out string errorMessage)
        {
            errorMessage = string.Empty;

            if (player == null)
            {
                errorMessage = "Player not found.";
                return null;
            }

            if (riftLevel <= 0)
            {
                errorMessage = "Invalid Rift level.";
                return null;
            }

            Party party = player.GetParty();
            if (party != null && party.NumMembers > 1 && player.IsPartyLeader() == false)
            {
                Logger.Info($"Mythic Rift request rejected because requester is not party leader. playerDbId=0x{player.DatabaseUniqueId:X} partyId=0x{party.PartyId:X} leaderDbId=0x{party.LeaderId:X}");
                errorMessage = "Only the party leader can request a group Mythic Rift run.";
                return null;
            }

            if (CanAccessRiftLevel(player.DatabaseUniqueId, riftLevel) == false)
            {
                int unlockedLevel = GetHighestUnlockedRiftLevel(player.DatabaseUniqueId);
                errorMessage = $"Requested Rift level {riftLevel} is locked. Highest unlocked level: {unlockedLevel}.";
                return null;
            }

            HashSet<ulong> launchRoster = BuildEligibleLaunchRoster(player, party);
            int requestedPlayerCount = Math.Clamp(launchRoster.Count, 1, 5);
            if (TryFindInProgressRunConflict(launchRoster, out MythicRiftRunState conflictingRun))
            {
                errorMessage = $"A Mythic Rift run is already in progress for this player or party (runId={conflictingRun.Config.RunId}, status={conflictingRun.Status}).";
                return null;
            }

            HashSet<string> excludedMapContentIds = useRandomContent
                ? BuildRandomMapExclusions(player, party)
                : null;

            MythicRiftRunState runState = useRandomContent
                ? CreateRandomDebugRun(riftLevel, requestedPlayerCount, killQuota, timeLimit, excludedMapContentIds, mode)
                : CreateDebugRun(contentId, riftLevel, requestedPlayerCount, killQuota, timeLimit, mode);

            if (runState == null)
            {
                errorMessage = useRandomContent
                    ? "Failed to create Mythic Rift run."
                    : $"Failed to create Mythic Rift run for content id: {contentId}";
                return null;
            }

            RegisterInitialParticipants(runState, launchRoster);
            if (useRandomContent)
                TrackRecentlySelectedMapContent(runState);

            int excludedPartyMembers = Math.Max((party?.NumMembers ?? 1) - launchRoster.Count, 0);
            if (excludedPartyMembers > 0)
            {
                Game.ChatManager.SendChatFromCustomSystem(
                    player,
                    $"[Cosmic Rift] {excludedPartyMembers} party member(s) were not included because they were offline or not in your current region.",
                    showSender: false);
            }

            Logger.Info($"Mythic Rift run {runState.Config.RunId} requested by playerDbId=0x{player.DatabaseUniqueId:X} at level {riftLevel}. mode={mode} partyId=0x{party?.PartyId ?? 0UL:X} partyLeaderDbId=0x{party?.LeaderId ?? 0UL:X} partyMembers={party?.NumMembers ?? 1} launchRoster={launchRoster.Count} excludedPartyMembers={excludedPartyMembers}");
            return runState;
        }

        private MythicRiftContentEntry SelectRandomMapContent(int riftLevel, int requestedPlayerCount, IReadOnlyCollection<string> excludedContentIds = null)
        {
            bool isCheckpointLevel = IsCheckpointRiftLevel(riftLevel);
            List<MythicRiftContentEntry> eligibleContent = isCheckpointLevel
                ? _contentPool
                    .Where(entry => entry.BossOnlyCheckpointEligible &&
                                    entry.CanAppearAtRandomRiftLevel(riftLevel) &&
                                    entry.SupportsPlayerCount(requestedPlayerCount) &&
                                    RandomCheckpointContentExclusions.Contains(entry.Id) == false)
                    .ToList()
                : _contentPool
                    .Where(entry => entry.RandomMapEligible &&
                                    entry.BossOnlyCheckpointEligible == false &&
                                    entry.CanAppearAtRandomRiftLevel(riftLevel) &&
                                    entry.SupportsPlayerCount(requestedPlayerCount))
                    .ToList();

            if (eligibleContent.Count == 0)
                return null;

            if (excludedContentIds != null && excludedContentIds.Count > 0 && eligibleContent.Count > excludedContentIds.Count)
            {
                List<MythicRiftContentEntry> filteredContent = eligibleContent
                    .Where(entry => excludedContentIds.Any(excludedId => string.Equals(excludedId, entry.Id, StringComparison.OrdinalIgnoreCase)) == false)
                    .ToList();

                if (filteredContent.Count > 0)
                    eligibleContent = filteredContent;
            }

            List<MythicRiftContentEntry> specialContent = eligibleContent.Where(entry => entry.IsSpecialRandomMap).ToList();
            List<MythicRiftContentEntry> standardContent = eligibleContent.Where(entry => entry.IsSpecialRandomMap == false).ToList();
            if (specialContent.Count > 0 && standardContent.Count > 0 && Game.Random.NextFloat() < SpecialRandomMapChance)
                return PickRandomContent(specialContent);

            if (standardContent.Count > 0)
                return PickRandomContent(standardContent);

            return PickRandomContent(eligibleContent);
        }

        private MythicRiftContentEntry SelectBossContentForRandomMap(MythicRiftContentEntry mapContent)
        {
            if (mapContent?.UseOwnBossSourceWhenSelected == true && mapContent.HasValidBossSource)
                return mapContent;

            return SelectRandomBossContent(mapContent);
        }

        private static bool IsCheckpointRiftLevel(int riftLevel)
        {
            return riftLevel > 0 && riftLevel % CheckpointRiftLevelInterval == 0;
        }

        private MythicRiftContentEntry SelectBossContentForFixedMap(MythicRiftContentEntry mapContent)
        {
            if (mapContent == null)
                return null;

            if (mapContent.HasValidBossSource && (mapContent.RandomBossEligible || mapContent.UseOwnBossSourceWhenSelected))
                return mapContent;

            return SelectRandomBossContent(mapContent);
        }

        private MythicRiftContentEntry SelectRandomBossContent(MythicRiftContentEntry mapContent)
        {
            List<MythicRiftContentEntry> eligibleContent = _contentPool.Where(entry => entry.RandomBossEligible && entry.HasValidBossSource).ToList();
            if (eligibleContent.Count == 0)
                return null;

            if (mapContent != null && eligibleContent.Count > 1)
            {
                List<MythicRiftContentEntry> alternateBossContent = eligibleContent
                    .Where(entry => string.Equals(entry.Id, mapContent.Id, StringComparison.OrdinalIgnoreCase) == false)
                    .ToList();

                if (alternateBossContent.Count > 0)
                    eligibleContent = alternateBossContent;
            }

            return PickRandomContent(eligibleContent);
        }

        private MythicRiftContentEntry PickRandomContent(IReadOnlyList<MythicRiftContentEntry> eligibleContent)
        {
            if (eligibleContent == null || eligibleContent.Count == 0)
                return null;

            int index = Game.Random.Next(0, eligibleContent.Count);
            return eligibleContent[index];
        }

        private void EnsureRegionListener(Region region)
        {
            if (region == null || _regionEntityDeadActions.ContainsKey(region.Id))
                return;

            Event<EntityDeadGameEvent>.Action action = (in EntityDeadGameEvent evt) => OnRegionEntityDead(region.Id, evt);
            region.EntityDeadEvent.AddActionBack(action);
            _regionEntityDeadActions[region.Id] = action;
        }

        private void CleanupRegionListener(ulong regionId)
        {
            if (regionId == 0)
                return;

            bool regionStillUsed = _activeRuns.Values.Any(run => run.RegionId == regionId);
            if (regionStillUsed)
                return;

            if (_regionEntityDeadActions.TryGetValue(regionId, out Event<EntityDeadGameEvent>.Action action) == false)
                return;

            Region region = Game.RegionManager.GetRegion(regionId);
            region?.EntityDeadEvent.RemoveAction(action);
            _regionEntityDeadActions.Remove(regionId);
        }

        private static void EnableRiftPopulationRespawns(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return;

            int enabledCount = 0;
            foreach (Area area in region.IterateAreas())
            {
                var spawnEvent = area?.PopulationArea?.SpawnEvent;
                if (spawnEvent == null)
                    continue;

                spawnEvent.RespawnObject = true;
                spawnEvent.RespawnDelayMS = RiftPopulationRespawnDelayMS;
                enabledCount++;
            }

            if (enabledCount > 0)
                Logger.Info($"Mythic Rift run {runState.Config.RunId} enabled Rift-only population respawns for {enabledCount} area(s), delay={RiftPopulationRespawnDelayMS}ms.");
        }

        private void ApplyRunDifficultyToRegion(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null || runState.RegionDifficultyScalingApplied)
                return;

            if (runState.AdmissionTrackingEnabled && runState.AdmissionFinalized == false)
                return;

            float currentPlayerToMobDamageMultiplier = region.Properties[PropertyEnum.DamageRegionPlayerToMob];
            float currentMobToPlayerDamageMultiplier = region.Properties[PropertyEnum.DamageRegionMobToPlayer];

            runState.CaptureRegionDifficultyScaling(currentPlayerToMobDamageMultiplier, currentMobToPlayerDamageMultiplier);

            float effectiveHealthMultiplier = Math.Max(runState.Difficulty.HealthMultiplier, 0.01f);
            float effectiveDamageMultiplier = Math.Max(runState.Difficulty.DamageMultiplier, 0.01f);

            region.Properties[PropertyEnum.DamageRegionPlayerToMob] = currentPlayerToMobDamageMultiplier / effectiveHealthMultiplier;
            region.Properties[PropertyEnum.DamageRegionMobToPlayer] = currentMobToPlayerDamageMultiplier * effectiveDamageMultiplier;

            Logger.Info(
                $"Mythic Rift run {runState.Config.RunId} applied region difficulty scaling: " +
                $"playerToMob {currentPlayerToMobDamageMultiplier:F4}->{region.Properties[PropertyEnum.DamageRegionPlayerToMob]:F4}, " +
                $"mobToPlayer {currentMobToPlayerDamageMultiplier:F4}->{region.Properties[PropertyEnum.DamageRegionMobToPlayer]:F4}, " +
                $"hpMultiplier={runState.Difficulty.HealthMultiplier:F4}, damageMultiplier={runState.Difficulty.DamageMultiplier:F4}, admittedPlayers={runState.AdmittedPlayerCount}.");
        }

        private void TryApplyRunDifficultyToBoundRegion(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0 || runState.RegionDifficultyScalingApplied)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region != null)
                ApplyRunDifficultyToRegion(runState, region);
        }

        private void TryRestoreRegionDifficultyScaling(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionDifficultyScalingApplied == false || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region != null)
            {
                region.Properties[PropertyEnum.DamageRegionPlayerToMob] = runState.RegionPlayerToMobDamageMultiplierBeforeScaling;
                region.Properties[PropertyEnum.DamageRegionMobToPlayer] = runState.RegionMobToPlayerDamageMultiplierBeforeScaling;

                Logger.Info(
                    $"Mythic Rift run {runState.Config.RunId} restored region difficulty scaling: " +
                    $"playerToMob={runState.RegionPlayerToMobDamageMultiplierBeforeScaling:F4}, " +
                    $"mobToPlayer={runState.RegionMobToPlayerDamageMultiplierBeforeScaling:F4}.");
            }

            runState.ClearRegionDifficultyScaling();
        }

        private int SuppressNativeTerminalBosses(MythicRiftRunState runState, TimeSpan currentTime, bool force = false)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return 0;

            if (force == false &&
                _nextNativeBossSuppressionScanAt.TryGetValue(runState.Config.RunId, out TimeSpan nextScanAt) &&
                currentTime < nextScanAt)
            {
                return 0;
            }

            _nextNativeBossSuppressionScanAt[runState.Config.RunId] = currentTime + NativeBossSuppressionScanInterval;

            PrototypeId nativeBossProtoRef = runState.Config.Content?.BossProtoRef ?? PrototypeId.Invalid;
            if (nativeBossProtoRef == PrototypeId.Invalid)
                return 0;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return 0;

            List<Agent> nativeBossesToDestroy = null;
            foreach (Entity entity in region.Entities)
            {
                if (entity is not Agent agent)
                    continue;

                if (agent.IsDestroyed || agent.IsDead || agent.IsInWorld == false)
                    continue;

                if (runState.IsTrackedBoss(agent.Id))
                    continue;

                if (runState.CustomPopulationEntityIds.Contains(agent.Id))
                    continue;

                if (agent.IsAPrototype(nativeBossProtoRef) == false)
                    continue;

                nativeBossesToDestroy ??= new();
                nativeBossesToDestroy.Add(agent);
            }

            if (nativeBossesToDestroy == null)
                return 0;

            int destroyedCount = 0;
            foreach (Agent nativeBoss in nativeBossesToDestroy)
            {
                Logger.Info(
                    $"Mythic Rift run {runState.Config.RunId} suppressed native terminal boss {nativeBoss.PrototypeName} " +
                    $"so Rift boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} remains quota-gated.");
                nativeBoss.Destroy();
                destroyedCount++;
            }

            return destroyedCount;
        }

        private int SuppressNativeCheckpointPopulation(MythicRiftRunState runState)
        {
            if (runState?.Config?.Content?.BossOnlyCheckpointEligible != true)
                return 0;

            if (runState.Status != MythicRiftRunStatus.Active ||
                runState.RegionId == 0 ||
                runState.BossSpawnCount >= runState.Config.RequiredBossKillCount)
                return 0;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return 0;

            List<Agent> nativeAgentsToDestroy = null;
            foreach (Entity entity in region.Entities)
            {
                if (entity is not Agent agent)
                    continue;

                if (agent is Avatar || agent.IsTeamUpAgent)
                    continue;

                if (agent.IsDestroyed || agent.IsDead || agent.IsInWorld == false)
                    continue;

                if (runState.IsTrackedBoss(agent.Id))
                    continue;

                if (agent.IsHostileToPlayers() == false)
                    continue;

                nativeAgentsToDestroy ??= new();
                nativeAgentsToDestroy.Add(agent);
            }

            if (nativeAgentsToDestroy == null)
                return 0;

            int destroyedCount = 0;
            foreach (Agent nativeAgent in nativeAgentsToDestroy)
            {
                Logger.Info($"Mythic Rift checkpoint run {runState.Config.RunId} suppressed native checkpoint entity {nativeAgent.PrototypeName} so the room behaves as a clean Rift boss arena.");
                nativeAgent.Destroy();
                destroyedCount++;
            }

            return destroyedCount;
        }

        public bool TryRefreshNativeObjectiveWidgetOverride(MissionObjective objective)
        {
            Mission mission = objective?.Mission;
            MythicRiftRunState runState = FindActiveRunForNativeObjectiveMission(mission, null);
            if (runState == null)
                return false;

            Region region = mission.Region;
            if (region == null)
                return false;

            RefreshRiftObjectiveWidgetsForMission(region, mission, runState, Game.CurrentTime);
            return true;
        }

        public bool TrySendNativeMissionUpdateOverride(Mission mission, Player player, MissionUpdateFlags missionFlags, MissionObjectiveUpdateFlags objectiveFlags)
        {
            if (missionFlags == MissionUpdateFlags.None && objectiveFlags == MissionObjectiveUpdateFlags.None)
                return false;

            MythicRiftRunState runState = FindActiveRunForNativeObjectiveMission(mission, player);
            if (runState == null)
                return false;

            SendNativeMissionTrackerSuppression(player, mission, runState);
            return true;
        }

        public bool TrySendNativeObjectiveUpdateOverride(MissionObjective objective, Player player, MissionObjectiveUpdateFlags objectiveFlags)
        {
            if (objectiveFlags == MissionObjectiveUpdateFlags.None)
                return false;

            Mission mission = objective?.Mission;
            MythicRiftRunState runState = FindActiveRunForNativeObjectiveMission(mission, player);
            if (runState == null)
                return false;

            SendNativeObjectiveTrackerSuppression(player, mission, objective, runState);
            return true;
        }

        private MythicRiftRunState FindActiveRunForNativeObjectiveMission(Mission mission, Player player)
        {
            if (mission == null || mission.PrototypeDataRef == PrototypeId.Invalid)
                return null;

            Region region = mission.Region ?? player?.GetRegion();
            if (region == null)
                return null;

            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                if (runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                    continue;

                if (runState.RegionId != region.Id && IsMatchingRunRegion(region, runState) == false)
                    continue;

                if (player != null && IsPlayerInRunRegion(player, runState) == false)
                    continue;

                if (IsNativeObjectiveMissionForRun(mission, runState))
                    return runState;
            }

            return null;
        }

        private static bool IsNativeObjectiveMissionForRun(Mission mission, MythicRiftRunState runState)
        {
            if (mission == null || runState?.Config == null)
                return false;

            bool isNativeTerminalMission = mission.PrototypeDataRef == runState.Config.MissionProtoRef;
            bool shouldControlTerminalMission = isNativeTerminalMission && SuspendNativeTerminalMissionsDuringRifts;
            bool shouldControlRegionEventMission = mission.IsRegionEventMission && SuspendNativeRegionEventMissionsDuringRifts;
            bool shouldControlRegionMission = mission.MissionManager?.IsRegionMissionManager() == true;

            return shouldControlTerminalMission || shouldControlRegionEventMission || shouldControlRegionMission;
        }

        private void RefreshRiftObjectiveWidgets(MythicRiftRunState runState, TimeSpan currentTime, bool force = false)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return;

            if (force == false &&
                _nextRiftObjectiveWidgetRefreshAt.TryGetValue(runState.Config.RunId, out TimeSpan nextRefreshAt) &&
                currentTime < nextRefreshAt)
            {
                return;
            }

            _nextRiftObjectiveWidgetRefreshAt[runState.Config.RunId] = currentTime + RiftObjectiveWidgetRefreshInterval;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            RemoveNativeRegionWidgets(region.UIDataProvider, runState);
            RefreshDangerRoomRiftWidgets(region.UIDataProvider, runState, currentTime);

            using var missionHandle = HashSetPool<Mission>.Instance.Get(out HashSet<Mission> missions);
            AddNativeTerminalMission(missions, region.MissionManager, runState.Config.MissionProtoRef);
            AddActiveMissions(missions, region.MissionManager);

            foreach (Player player in new PlayerIterator(region))
                AddNativeTerminalMission(missions, player?.MissionManager, runState.Config.MissionProtoRef);

            foreach (Mission mission in missions)
            {
                TrySuspendNativeObjectiveMissionForRun(runState, mission);
                SuppressNativeMissionTrackerForRunPlayers(mission, runState);
                RefreshRiftObjectiveWidgetsForMission(region, mission, runState, currentTime);
            }

            RemoveNativeRegionWidgets(region.UIDataProvider, runState);
        }

        private void RefreshRiftHudWidgets(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            RefreshDangerRoomRiftWidgets(region.UIDataProvider, runState, currentTime);
        }

        private void ClearRiftObjectiveWidgets(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            ClearDangerRoomRiftWidgets(region.UIDataProvider, runState);

            using var missionHandle = HashSetPool<Mission>.Instance.Get(out HashSet<Mission> missions);
            AddNativeTerminalMission(missions, region.MissionManager, runState.Config.MissionProtoRef);
            AddActiveMissions(missions, region.MissionManager);

            foreach (Player player in new PlayerIterator(region))
                AddNativeTerminalMission(missions, player?.MissionManager, runState.Config.MissionProtoRef);

            foreach (Mission mission in missions)
                ClearRiftObjectiveWidgetsForMission(region, mission);
        }

        public void ForceRefreshRiftUiForDiagnostics(MythicRiftRunState runState, Region currentRegion, List<string> lines)
        {
            if (lines == null)
                return;

            if (runState == null)
            {
                lines.Add("riftUi=skipped | reason=no active Rift run for player");
                return;
            }

            Region boundRegion = runState.RegionId != 0
                ? Game.RegionManager.GetRegion(runState.RegionId)
                : null;

            lines.Add(
                $"riftUi.runId={runState.Config.RunId} | status={runState.Status} | boundRegion={(boundRegion?.PrototypeDataRef.GetNameFormatted() ?? "none")} | boundRegionId=0x{runState.RegionId:X} | currentMatchesBound={boundRegion != null && currentRegion?.Id == boundRegion.Id}");

            if (runState.Status != MythicRiftRunStatus.Active)
            {
                lines.Add("riftUi=skipped | reason=Rift is not active");
                return;
            }

            Region refreshRegion = boundRegion ?? currentRegion;
            if (refreshRegion?.UIDataProvider == null)
            {
                lines.Add("riftUi=skipped | reason=Rift region or UIDataProvider not found");
                return;
            }

            RefreshDangerRoomRiftWidgets(refreshRegion.UIDataProvider, runState, Game.CurrentTime);
            AppendDangerRoomRiftWidgetDiagnostics(lines, refreshRegion.UIDataProvider, runState);
        }

        private static void RefreshDangerRoomRiftWidgets(UIDataProvider uiDataProvider, MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (uiDataProvider == null || runState?.Config == null)
                return;

            PrototypeId contextRef = GetRiftWidgetContextRef(runState);
            if (contextRef == PrototypeId.Invalid)
                return;

            RefreshDangerRoomRiftLevelWidget(uiDataProvider, runState, contextRef);

            if (runState.Config.Content.BossOnlyCheckpointEligible)
            {
                uiDataProvider.DeleteWidget(GetRiftDangerRoomQuotaWidgetPrototypeRef(), contextRef);
            }
            else
            {
                int requiredCount = Math.Max(runState.Config.KillQuota, 1);
                int currentCount = Math.Clamp(runState.CurrentKillCount, 0, requiredCount);

                UIWidgetGenericFraction quotaWidget = GetRiftGenericFractionWidget(
                    uiDataProvider,
                    GetRiftDangerRoomQuotaWidgetPrototypeRef(),
                    contextRef);

                if (quotaWidget != null)
                {
                    quotaWidget.SetAreaContext(contextRef);
                    quotaWidget.SetCount(currentCount, requiredCount);
                }
            }

            TimeSpan remaining = runState.GetTimeRemaining(currentTime);
            if (remaining <= TimeSpan.Zero)
                return;

            UIWidgetGenericFraction timerWidget = GetRiftGenericFractionWidget(
                uiDataProvider,
                GetRiftDangerRoomTimerWidgetPrototypeRef(),
                contextRef);

            if (timerWidget != null)
            {
                timerWidget.SetAreaContext(contextRef);
                timerWidget.SetTimeRemaining((long)remaining.TotalMilliseconds);
            }
        }

        private static void RefreshDangerRoomRiftLevelWidget(UIDataProvider uiDataProvider, MythicRiftRunState runState, PrototypeId contextRef)
        {
            LocaleStringId levelText = GetDangerRoomRiftLevelLocaleStringId(runState.Config.RiftLevel);
            if (levelText == LocaleStringId.Invalid)
                return;

            UIWidgetMissionText levelWidget = GetRiftMissionTextWidget(
                uiDataProvider,
                GetRiftDangerRoomLevelWidgetPrototypeRef(),
                contextRef);

            if (levelWidget == null)
                return;

            levelWidget.SetAreaContext(contextRef);
            levelWidget.SetText(levelText, LocaleStringId.Blank);
        }

        private static void ClearDangerRoomRiftWidgets(UIDataProvider uiDataProvider, MythicRiftRunState runState)
        {
            if (uiDataProvider == null || runState?.Config == null)
                return;

            PrototypeId contextRef = GetRiftWidgetContextRef(runState);
            if (contextRef == PrototypeId.Invalid)
                return;

            uiDataProvider.DeleteWidget(GetRiftDangerRoomLevelWidgetPrototypeRef(), contextRef);
            uiDataProvider.DeleteWidget(GetRiftDangerRoomQuotaWidgetPrototypeRef(), contextRef);
            uiDataProvider.DeleteWidget(GetRiftDangerRoomTimerWidgetPrototypeRef(), contextRef);
        }

        private static PrototypeId GetRiftWidgetContextRef(MythicRiftRunState runState)
        {
            if (runState?.Config == null)
                return PrototypeId.Invalid;

            // Reusing the same region context for repeated runs of the same map can leave the
            // client-side Danger Room HUD in a stale state. The boss prototype is a real,
            // client-known ref and varies more often while still being stable for the run.
            if (runState.Config.BossProtoRef != PrototypeId.Invalid)
                return runState.Config.BossProtoRef;

            return runState.Config.RegionProtoRef;
        }

        private static PrototypeId GetRiftDangerRoomLevelWidgetPrototypeRef()
        {
            if (_cachedRiftDangerRoomLevelWidgetPrototypeRef == PrototypeId.Invalid)
                _cachedRiftDangerRoomLevelWidgetPrototypeRef = ResolveRiftWidgetPrototypeRef(RiftDangerRoomLevelWidgetPrototypeName, RiftDangerRoomLevelWidgetPrototypeRef);

            return _cachedRiftDangerRoomLevelWidgetPrototypeRef;
        }

        private static PrototypeId GetRiftDangerRoomQuotaWidgetPrototypeRef()
        {
            if (_cachedRiftDangerRoomQuotaWidgetPrototypeRef == PrototypeId.Invalid)
                _cachedRiftDangerRoomQuotaWidgetPrototypeRef = ResolveRiftWidgetPrototypeRef(RiftDangerRoomQuotaWidgetPrototypeName, RiftDangerRoomQuotaWidgetPrototypeRef);

            return _cachedRiftDangerRoomQuotaWidgetPrototypeRef;
        }

        private static PrototypeId GetRiftDangerRoomTimerWidgetPrototypeRef()
        {
            if (_cachedRiftDangerRoomTimerWidgetPrototypeRef == PrototypeId.Invalid)
                _cachedRiftDangerRoomTimerWidgetPrototypeRef = ResolveRiftWidgetPrototypeRef(RiftDangerRoomTimerWidgetPrototypeName, RiftDangerRoomTimerWidgetPrototypeRef);

            return _cachedRiftDangerRoomTimerWidgetPrototypeRef;
        }

        private static PrototypeId ResolveRiftWidgetPrototypeRef(string prototypeName, PrototypeId fallbackRef)
        {
            PrototypeId resolvedRef = string.IsNullOrWhiteSpace(prototypeName)
                ? PrototypeId.Invalid
                : GameDatabase.GetPrototypeRefByName(prototypeName);

            if (resolvedRef != PrototypeId.Invalid && GameDatabase.GetPrototype<MetaGameDataPrototype>(resolvedRef) != null)
                return resolvedRef;

            return fallbackRef;
        }

        private static void AppendDangerRoomRiftWidgetDiagnostics(List<string> lines, UIDataProvider uiDataProvider, MythicRiftRunState runState)
        {
            if (lines == null)
                return;

            PrototypeId contextRef = GetRiftWidgetContextRef(runState);
            lines.Add($"riftUi.context={contextRef.GetNameFormatted()}");
            AppendRiftWidgetPrototypeDiagnostic(lines, "level", RiftDangerRoomLevelWidgetPrototypeName, GetRiftDangerRoomLevelWidgetPrototypeRef(), typeof(UIWidgetMissionTextPrototype));
            AppendRiftWidgetPrototypeDiagnostic(lines, "quota", RiftDangerRoomQuotaWidgetPrototypeName, GetRiftDangerRoomQuotaWidgetPrototypeRef(), typeof(UIWidgetGenericFractionPrototype));
            AppendRiftWidgetPrototypeDiagnostic(lines, "timer", RiftDangerRoomTimerWidgetPrototypeName, GetRiftDangerRoomTimerWidgetPrototypeRef(), typeof(UIWidgetGenericFractionPrototype));

            string uiDump = uiDataProvider?.ToString() ?? string.Empty;
            lines.Add(string.IsNullOrWhiteSpace(uiDump)
                ? "riftUi.widgetsAfterRefresh=empty"
                : "riftUi.widgetsAfterRefresh=present");
        }

        private static void AppendRiftWidgetPrototypeDiagnostic(List<string> lines, string label, string prototypeName, PrototypeId widgetRef, Type expectedPrototypeType)
        {
            MetaGameDataPrototype widgetProto = widgetRef != PrototypeId.Invalid
                ? GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef)
                : null;

            lines.Add(
                $"riftUi.widget.{label}=name:{prototypeName} | ref:{widgetRef.GetNameFormatted()} | type:{widgetProto?.GetType().Name ?? "missing"} | expected:{expectedPrototypeType.Name} | valid:{widgetProto != null && expectedPrototypeType.IsInstanceOfType(widgetProto)}");
        }

        private static UIWidgetGenericFraction GetRiftGenericFractionWidget(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef)
        {
            if (uiDataProvider == null || widgetRef == PrototypeId.Invalid)
                return null;

            if (GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef) is not UIWidgetGenericFractionPrototype)
                return null;

            return uiDataProvider.GetWidget<UIWidgetGenericFraction>(widgetRef, contextRef);
        }

        private static UIWidgetMissionText GetRiftMissionTextWidget(UIDataProvider uiDataProvider, PrototypeId widgetRef, PrototypeId contextRef)
        {
            if (uiDataProvider == null || widgetRef == PrototypeId.Invalid)
                return null;

            if (GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef) is not UIWidgetMissionTextPrototype)
                return null;

            return uiDataProvider.GetWidget<UIWidgetMissionText>(widgetRef, contextRef);
        }

        private static LocaleStringId GetRiftEntryBannerLocaleStringId(int riftLevel)
        {
            if (riftLevel <= 0 || riftLevel > RiftEntryBannerLocalizedLevelLimit)
                return LocaleStringId.Invalid;

            return (LocaleStringId)(RiftEntryBannerLocaleStringBase + (ulong)riftLevel);
        }

        private static LocaleStringId GetDangerRoomRiftLevelLocaleStringId(int riftLevel)
        {
            if (riftLevel <= 0 || riftLevel > RiftDangerRoomLevelLocalizedLevelLimit)
                return LocaleStringId.Invalid;

            return (LocaleStringId)(RiftDangerRoomLevelLocaleStringBase + (ulong)riftLevel);
        }

        private static void AddNativeTerminalMission(HashSet<Mission> missions, MissionManager missionManager, PrototypeId missionRef)
        {
            if (missions == null || missionManager == null || missionRef == PrototypeId.Invalid)
                return;

            Mission mission = missionManager.FindMissionByDataRef(missionRef);
            if (mission != null)
                missions.Add(mission);
        }

        private static void AddActiveMissions(HashSet<Mission> missions, MissionManager missionManager)
        {
            if (missions == null || missionManager == null)
                return;

            foreach (PrototypeId missionRef in missionManager.ActiveMissions)
            {
                Mission mission = missionManager.FindMissionByDataRef(missionRef);
                if (mission != null)
                    missions.Add(mission);
            }
        }

        private static int RemoveNativeRegionWidgets(UIDataProvider uiDataProvider, MythicRiftRunState runState)
        {
            if (uiDataProvider == null || runState?.Config == null)
                return 0;

            return MythicRiftUiController.RemoveNativeWidgets(
                uiDataProvider,
                GetRiftWidgetContextRef(runState),
                GetRiftDangerRoomLevelWidgetPrototypeRef(),
                GetRiftDangerRoomQuotaWidgetPrototypeRef(),
                GetRiftDangerRoomTimerWidgetPrototypeRef());
        }

        private void TrySuspendNativeObjectiveMissionForRun(MythicRiftRunState runState, Mission mission)
        {
            if (runState?.Config == null || mission == null)
                return;

            bool isNativeTerminalMission = mission.PrototypeDataRef == runState.Config.MissionProtoRef;
            bool shouldSuspendTerminalMission = isNativeTerminalMission && SuspendNativeTerminalMissionsDuringRifts;
            bool shouldSuspendRegionEventMission = mission.IsRegionEventMission && SuspendNativeRegionEventMissionsDuringRifts;

            if (shouldSuspendTerminalMission == false && shouldSuspendRegionEventMission == false)
                return;

            if (mission.PrototypeDataRef == PrototypeId.Invalid || mission.IsSuspended)
                return;

            if (mission.SetSuspendedState(true) == false)
                return;

            if (_serverSuspendedNativeObjectiveMissionsByRun.TryGetValue(runState.Config.RunId, out HashSet<Mission> suspendedMissions) == false)
            {
                suspendedMissions = new();
                _serverSuspendedNativeObjectiveMissionsByRun[runState.Config.RunId] = suspendedMissions;
            }

            suspendedMissions.Add(mission);
            string missionKind = isNativeTerminalMission ? "native terminal" : "region event";
            Logger.Info($"Mythic Rift run {runState.Config.RunId} suspended {missionKind} mission {mission.PrototypeName} while the Rift is active.");
        }

        private void RestoreSuspendedNativeObjectiveMissions(MythicRiftRunState runState)
        {
            if (runState?.Config == null)
                return;

            if (_serverSuspendedNativeObjectiveMissionsByRun.TryGetValue(runState.Config.RunId, out HashSet<Mission> suspendedMissions) == false)
                return;

            int restoredCount = 0;
            foreach (Mission mission in suspendedMissions)
            {
                if (mission?.IsSuspended != true)
                    continue;

                if (mission.SetSuspendedState(false))
                    restoredCount++;
            }

            if (restoredCount > 0)
                Logger.Info($"Mythic Rift run {runState.Config.RunId} restored {restoredCount} suspended native objective mission(s).");
        }

        private void RefreshRiftObjectiveWidgetsForMission(Region region, Mission mission, MythicRiftRunState runState, TimeSpan currentTime)
        {
            UIDataProvider uiDataProvider = region?.UIDataProvider;
            if (uiDataProvider == null || mission == null || runState?.Config == null)
                return;

            PrototypeId missionRef = mission.PrototypeDataRef;
            if (missionRef == PrototypeId.Invalid)
                return;

            bool removeMissionNameWidget = false;
            foreach (MissionObjective objective in mission.Objectives)
            {
                MissionObjectivePrototype objectiveProto = objective?.Prototype;
                if (objectiveProto == null)
                    continue;

                RefreshRiftObjectiveWidget(uiDataProvider, missionRef, objectiveProto.MetaGameWidget, runState, currentTime, ref removeMissionNameWidget);
                SuppressNativeObjectiveWidget(uiDataProvider, missionRef, objectiveProto.MetaGameWidgetFail, ref removeMissionNameWidget);
            }

            if (removeMissionNameWidget)
                SuppressMissionNameWidget(uiDataProvider, missionRef);
        }

        private void SuppressNativeMissionTrackerForRunPlayers(Mission mission, MythicRiftRunState runState)
        {
            if (mission == null || runState == null)
                return;

            foreach (Player player in GetRunPlayers(runState))
            {
                if (IsPlayerInRunRegion(player, runState) == false)
                    continue;

                SendNativeMissionTrackerSuppression(player, mission, runState);
            }
        }

        private static void SendNativeMissionTrackerSuppression(Player player, Mission mission, MythicRiftRunState runState)
        {
            if (player == null || mission == null || mission.PrototypeDataRef == PrototypeId.Invalid)
                return;

            try
            {
                NetMessageMissionUpdate missionMessage = NetMessageMissionUpdate.CreateBuilder()
                    .SetMissionPrototypeId((ulong)mission.PrototypeDataRef)
                    .SetMissionState((uint)MissionState.Inactive)
                    .SetSuppressNotification(true)
                    .SetSuspendedState(true)
                    .Build();

                player.SendMessage(missionMessage);

                foreach (MissionObjective objective in mission.Objectives)
                    SendNativeObjectiveTrackerSuppression(player, mission, objective, runState);
            }
            catch (Exception e)
            {
                Logger.Warn($"SendNativeMissionTrackerSuppression(): failed for mission {mission.PrototypeName}: {e.Message}");
            }
        }

        private static void SendNativeObjectiveTrackerSuppression(Player player, Mission mission, MissionObjective objective, MythicRiftRunState runState)
        {
            if (player == null || mission == null || objective == null || mission.PrototypeDataRef == PrototypeId.Invalid)
                return;

            uint requiredCount = (uint)Math.Max(runState?.Config?.KillQuota ?? 1, 1);
            uint currentCount = (uint)Math.Clamp(runState?.CurrentKillCount ?? 0, 0, (int)requiredCount);
            bool isGenericCounter = UsesGenericFractionWidget(objective);

            // The client can keep one native bounty counter visible. For that one, keep a generic
            // objective alive but replace its numbers with the Rift quota. Everything else is hidden.
            MissionObjectiveState objectiveState = isGenericCounter ? MissionObjectiveState.Active : MissionObjectiveState.Invalid;

            NetMessageMissionObjectiveUpdate objectiveMessage = NetMessageMissionObjectiveUpdate.CreateBuilder()
                .SetMissionPrototypeId((ulong)mission.PrototypeDataRef)
                .SetObjectiveIndex(objective.PrototypeIndex)
                .SetObjectiveState((uint)objectiveState)
                .SetCurrentCount(currentCount)
                .SetRequiredCount(requiredCount)
                .SetFailCurrentCount(0)
                .SetFailRequiredCount(0)
                .SetSuppressNotification(true)
                .SetSuspendedState(isGenericCounter == false)
                .Build();

            player.SendMessage(objectiveMessage);
        }

        private static bool UsesGenericFractionWidget(MissionObjective objective)
        {
            PrototypeId widgetRef = objective?.Prototype?.MetaGameWidget ?? PrototypeId.Invalid;
            if (widgetRef == PrototypeId.Invalid)
                return false;

            return GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef) is UIWidgetGenericFractionPrototype;
        }

        private static void ClearRiftObjectiveWidgetsForMission(Region region, Mission mission)
        {
            UIDataProvider uiDataProvider = region?.UIDataProvider;
            if (uiDataProvider == null || mission == null)
                return;

            PrototypeId missionRef = mission.PrototypeDataRef;
            if (missionRef == PrototypeId.Invalid)
                return;

            foreach (MissionObjective objective in mission.Objectives)
            {
                MissionObjectivePrototype objectiveProto = objective?.Prototype;
                if (objectiveProto == null)
                    continue;

                if (objectiveProto.MetaGameWidget != PrototypeId.Invalid)
                    uiDataProvider.DeleteWidget(objectiveProto.MetaGameWidget, missionRef);

                if (objectiveProto.MetaGameWidgetFail != PrototypeId.Invalid)
                    uiDataProvider.DeleteWidget(objectiveProto.MetaGameWidgetFail, missionRef);
            }

            SuppressMissionNameWidget(uiDataProvider, missionRef);
        }

        private void RefreshRiftObjectiveWidget(UIDataProvider uiDataProvider, PrototypeId missionRef, PrototypeId widgetRef, MythicRiftRunState runState, TimeSpan currentTime, ref bool removeMissionNameWidget)
        {
            if (widgetRef == PrototypeId.Invalid)
                return;

            MetaGameDataPrototype metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef);
            if (metaDataProto == null)
                return;

            removeMissionNameWidget |= metaDataProto.DisplayMissionName;

            if (metaDataProto is UIWidgetGenericFractionPrototype)
            {
                UIWidgetGenericFraction fractionWidget = uiDataProvider.GetWidget<UIWidgetGenericFraction>(widgetRef, missionRef);
                if (fractionWidget == null)
                    return;

                int requiredCount = Math.Max(runState.Config.KillQuota, 1);
                int currentCount = Math.Clamp(runState.CurrentKillCount, 0, requiredCount);
                fractionWidget.SetCount(currentCount, requiredCount);

                TimeSpan remaining = runState.GetTimeRemaining(currentTime);
                if (remaining > TimeSpan.Zero)
                    fractionWidget.SetTimeRemaining((long)remaining.TotalMilliseconds);

                fractionWidget.SetAreaContext(missionRef);
                return;
            }

            // Keep native terminal logic alive, but hide terminal-specific HUD text such as "Defeat Kingpin".
            uiDataProvider.DeleteWidget(widgetRef, missionRef);
        }

        private static void SuppressNativeObjectiveWidget(UIDataProvider uiDataProvider, PrototypeId missionRef, PrototypeId widgetRef, ref bool removeMissionNameWidget)
        {
            if (uiDataProvider == null || widgetRef == PrototypeId.Invalid)
                return;

            MetaGameDataPrototype metaDataProto = GameDatabase.GetPrototype<MetaGameDataPrototype>(widgetRef);
            if (metaDataProto != null)
                removeMissionNameWidget |= metaDataProto.DisplayMissionName;

            uiDataProvider.DeleteWidget(widgetRef, missionRef);
        }

        private static void SuppressMissionNameWidget(UIDataProvider uiDataProvider, PrototypeId missionRef)
        {
            PrototypeId missionNameWidgetRef = GameDatabase.UIGlobalsPrototype?.MetaGameWidgetMissionName ?? PrototypeId.Invalid;
            if (uiDataProvider == null || missionNameWidgetRef == PrototypeId.Invalid)
                return;

            uiDataProvider.DeleteWidget(missionNameWidgetRef, missionRef);
        }

        private void OnRegionEntityDead(ulong regionId, in EntityDeadGameEvent evt)
        {
            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                if (runState.RegionId != regionId || runState.Status != MythicRiftRunStatus.Active)
                    continue;

                RegisterParticipantsFromEvent(runState, evt);

                if (TryApplyPlayerDeathPenalty(runState, evt))
                    continue;

                if (IsExpectedBossKill(runState, evt))
                {
                    runState.MarkBossDefeated(evt.Defender.Id);

                    if (runState.BossUnlocked && runState.BossKillCount >= runState.Config.RequiredBossKillCount)
                    {
                        CaptureSuccessfulCompletionEligibility(runState);
                        CompleteRunSuccess(runState, Game.CurrentTime);
                        Logger.Info($"Mythic Rift run {runState.Config.RunId} completed after defeating {runState.BossKillCount}/{runState.Config.RequiredBossKillCount} Rift bosses.");
                    }
                    else
                    {
                        int bossesRemaining = Math.Max(runState.Config.RequiredBossKillCount - runState.BossKillCount, 0);
                        NotifyRunPlayers(runState, $"[Cosmic Rift] Rift boss defeated. {bossesRemaining} boss(es) remaining.");
                    }

                    continue;
                }

                if (runState.Config.Content.BossOnlyCheckpointEligible)
                    continue;

                bool shouldCountKill = ShouldCountKill(evt);
                if (runState.BossUnlocked)
                {
                    if (runState.BossSpawnCount < runState.Config.RequiredBossKillCount && shouldCountKill)
                    {
                        if (TrySpawnConfiguredBoss(runState))
                        {
                            CaptureBossUnlockEligibility(runState);
                            NotifyBossUnlocked(runState);
                        }
                        else
                        {
                            Logger.Warn($"Mythic Rift run {runState.Config.RunId} failed to retry spawn for boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} after quota unlock.");
                        }
                    }

                    continue;
                }

                if (shouldCountKill == false)
                    continue;

                int previousKillCount = runState.CurrentKillCount;
                int killCredit = GetKillCountCredit(evt);
                runState.AddKills(killCredit);
                RefreshRiftHudWidgets(runState, Game.CurrentTime);
                TrySpawnPendingMilestoneEncounters(runState, allowAfterBossUnlock: true);

                if (runState.BossUnlocked && previousKillCount < runState.Config.KillQuota)
                {
                    if (TrySpawnConfiguredBoss(runState))
                    {
                        CaptureBossUnlockEligibility(runState);
                        NotifyBossUnlocked(runState);
                    }
                    else
                    {
                        Logger.Warn($"Mythic Rift run {runState.Config.RunId} unlocked boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} but the initial spawn attempt failed.");
                    }
                    Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked its boss after reaching {runState.CurrentKillCount}/{runState.Config.KillQuota} kills.");
                }
                else
                {
                    TryNotifyKillProgress(runState);
                }
            }
        }

        private bool TryApplyPlayerDeathPenalty(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active)
                return false;

            if (evt.Defender is not Avatar deadAvatar)
                return false;

            Player deadPlayer = deadAvatar.GetOwnerOfType<Player>();
            if (deadPlayer == null || IsPlayerInRunRegion(deadPlayer, runState) == false)
                return false;

            if (runState.IsParticipant(deadPlayer.DatabaseUniqueId) == false)
                return false;

            runState.ApplyTimePenalty(PlayerDeathTimePenalty);
            SendStartRiftTimer(runState);
            RefreshRiftHudWidgets(runState, Game.CurrentTime);

            string remaining = FormatDuration(runState.GetTimeRemaining(Game.CurrentTime));
            NotifyRunPlayers(runState, $"[Cosmic Rift] {deadPlayer.GetName()} died. Death penalty: -{(int)PlayerDeathTimePenalty.TotalSeconds} sec. Time remaining: {remaining}.");
            Logger.Info($"Mythic Rift run {runState.Config.RunId} applied a {PlayerDeathTimePenalty.TotalSeconds:0}s death penalty to playerDbId=0x{deadPlayer.DatabaseUniqueId:X}.");

            if (runState.HasExpired(Game.CurrentTime))
                CompleteRunFailure(runState, Game.CurrentTime, "Time expired after a death penalty. No completion rewards.", returnParticipantsToHub: true);

            return true;
        }

        private static bool IsExpectedBossKill(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null || evt.Defender == null)
                return false;

            if (runState.BossUnlocked == false)
                return false;

            if (runState.IsTrackedBoss(evt.Defender.Id))
                return true;

            if (runState.BossSpawnCount > 0)
                return false;

            PrototypeId expectedBossRef = runState.Config.BossProtoRef;
            if (expectedBossRef == PrototypeId.Invalid)
                return false;

            return evt.Defender.IsAPrototype(expectedBossRef);
        }

        private static void RegisterParticipantsFromEvent(MythicRiftRunState runState, in EntityDeadGameEvent evt)
        {
            if (runState == null)
                return;

            if (evt.Killer != null)
                runState.MarkParticipantSeenInRunRegion(evt.Killer.DatabaseUniqueId);

            Player attackerOwner = evt.Attacker?.GetOwnerOfType<Player>();
            if (attackerOwner != null)
                runState.MarkParticipantSeenInRunRegion(attackerOwner.DatabaseUniqueId);

            if (evt.Defender?.TagPlayers == null)
                return;

            foreach (Player taggedPlayer in evt.Defender.TagPlayers.GetPlayers())
                runState.MarkParticipantSeenInRunRegion(taggedPlayer.DatabaseUniqueId);
        }

        private void RegisterRegionPlayersAsParticipants(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return;

            foreach (Player player in new PlayerIterator(region))
            {
                bool newlyRegistered = false;
                if (runState.IsParticipant(player.DatabaseUniqueId) == false)
                {
                    if (runState.AdmissionFinalized)
                        continue;

                    newlyRegistered = runState.RegisterParticipant(player.DatabaseUniqueId);
                }

                runState.MarkParticipantSeenInRunRegion(player.DatabaseUniqueId);

                if (runState.Status == MythicRiftRunStatus.Active)
                    TrySendRiftEntryBanner(runState, player);

                if (newlyRegistered && runState.Status == MythicRiftRunStatus.Active)
                {
                    SendStartRiftTimer(runState, player);
                    Game.ChatManager.SendChatFromCustomSystem(
                        player,
                        BuildJoinMessage(runState, Game.CurrentTime),
                        showSender: false);
                }
            }
        }

        private void TrySendRiftEntryBanner(MythicRiftRunState runState, Player player)
        {
            if (runState == null || player == null || runState.Status != MythicRiftRunStatus.Active)
                return;

            if (runState.MarkRiftEntryBannerSent(player.DatabaseUniqueId) == false)
                return;

            LocaleStringId bannerText = GetRiftEntryBannerLocaleStringId(runState.Config.RiftLevel);
            if (bannerText == LocaleStringId.Invalid)
            {
                Logger.Warn($"Mythic Rift run {runState.Config.RunId} skipped entry banner because level {runState.Config.RiftLevel} is outside the localized banner range 1-{RiftEntryBannerLocalizedLevelLimit}.");
                return;
            }

            player.SendBannerMessage(
                bannerText,
                TextStylePrototype.BannerMessageLarge,
                RiftEntryBannerTimeToLiveMS,
                BannerMessageStyle.FlyIn,
                doNotQueue: true,
                showImmediately: true);
        }

        private HashSet<ulong> BuildEligibleLaunchRoster(Player requester, Party party)
        {
            HashSet<ulong> launchRoster = new();
            if (requester?.DatabaseUniqueId == 0)
                return launchRoster;

            launchRoster.Add(requester.DatabaseUniqueId);
            if (party == null)
                return launchRoster;

            Region requesterRegion = requester.GetRegion();
            if (requesterRegion == null)
                return launchRoster;

            foreach (var kvp in party)
            {
                ulong memberDbId = kvp.Value.PlayerDbId;
                if (memberDbId == 0 || memberDbId == requester.DatabaseUniqueId)
                    continue;

                Player member = Game.EntityManager.GetEntityByDbGuid<Player>(memberDbId);
                if (member?.GetRegion()?.Id == requesterRegion.Id)
                    launchRoster.Add(memberDbId);
            }

            return launchRoster;
        }

        private static void RegisterInitialParticipants(MythicRiftRunState runState, IEnumerable<ulong> launchRoster)
        {
            if (runState == null || launchRoster == null)
                return;

            runState.EnableAdmissionTracking();
            foreach (ulong playerDbId in launchRoster)
                runState.RegisterParticipant(playerDbId);
        }

        private MythicRiftRewardOutcome ResolveRewardOutcome(MythicRiftRunState runState)
        {
            if (runState == null)
                return null;

            bool timedSuccess = runState.Status == MythicRiftRunStatus.Success;
            bool checkpointSuccess = timedSuccess && runState.Config.Content.BossOnlyCheckpointEligible;
            MythicRiftRewardTuning tuning = _rewardTuning ?? MythicRiftRewardTuning.CreateDefault();
            bool grantBossLoot = timedSuccess ? tuning.GrantBossLootOnSuccess : tuning.GrantBossLootOnFailure;
            string bossLootTableSourceId = grantBossLoot ? "native-boss" : "disabled";
            string bossLootDelivery = MythicRiftRewardTuning.NormalizeDelivery(tuning.DefaultDelivery);
            PrototypeId bossLootTableProtoRef = grantBossLoot
                ? ResolvePrimaryRewardLootTable(runState, tuning, checkpointSuccess, out bossLootTableSourceId, out bossLootDelivery)
                : PrototypeId.Invalid;

            List<MythicRiftRewardExtraLootTable> extraLootTables = ResolveExtraRewardLootTables(runState, tuning, timedSuccess, checkpointSuccess);
            if (grantBossLoot)
                extraLootTables.InsertRange(0, ResolveAdditionalBossWaveLootTables(runState, tuning.DefaultDelivery));

            List<MythicRiftRewardGuaranteedItem> guaranteedItems = ResolveGuaranteedRewardItems(runState, tuning, timedSuccess, checkpointSuccess);
            guaranteedItems.AddRange(ResolveRandomItemPoolRewards(runState, tuning, timedSuccess, checkpointSuccess));

            MythicRiftRewardOutcome rewardOutcome = new()
            {
                BossLootTableProtoRef = bossLootTableProtoRef,
                BossLootTableSourceId = bossLootTableSourceId,
                BossLootDelivery = bossLootDelivery,
                RewardProfileName = tuning.ProfileName,
                TimedSuccessBonusApplied = timedSuccess,
                BonusRarityPct = timedSuccess
                    ? tuning.TimedSuccessBonusRarityPct + (checkpointSuccess ? tuning.CheckpointSuccessBonusRarityPct : 0f)
                    : tuning.FailureBonusRarityPct,
                BonusSpecialPct = timedSuccess
                    ? tuning.TimedSuccessBonusSpecialPct + (checkpointSuccess ? tuning.CheckpointSuccessBonusSpecialPct : 0f)
                    : tuning.FailureBonusSpecialPct,
                ExtraLootTables = extraLootTables,
                GuaranteedItems = guaranteedItems
            };

            runState.SetRewardOutcome(rewardOutcome);
            return rewardOutcome;
        }

        private PrototypeId ResolvePrimaryRewardLootTable(MythicRiftRunState runState, MythicRiftRewardTuning tuning, bool checkpointSuccess, out string sourceId, out string delivery)
        {
            sourceId = "native-boss";
            delivery = MythicRiftRewardTuning.NormalizeDelivery(tuning?.DefaultDelivery);
            if (runState?.Config == null)
                return PrototypeId.Invalid;

            if (tuning?.PrimaryLootTableOverrides != null)
            {
                foreach (MythicRiftPrimaryLootTableTuning entry in tuning.PrimaryLootTableOverrides)
                {
                    if (entry == null || entry.AppliesTo(runState, checkpointSuccess) == false)
                        continue;

                    string lootTablePrototype = tuning.ResolveLootTableReference(entry.LootTablePrototype);
                    PrototypeId overrideLootTableProtoRef = ResolvePrototype(lootTablePrototype);
                    if (overrideLootTableProtoRef == PrototypeId.Invalid || overrideLootTableProtoRef.As<LootTablePrototype>() == null)
                    {
                        Logger.Warn($"Mythic Rift reward tuning skipped invalid primary loot table override id={entry.Id} lootTable={lootTablePrototype}");
                        continue;
                    }

                    sourceId = entry.Id;
                    delivery = MythicRiftRewardTuning.NormalizeDelivery(entry.Delivery);
                    return overrideLootTableProtoRef;
                }
            }

            return runState.Config.BossLootTableProtoRef;
        }

        private static List<MythicRiftRewardExtraLootTable> ResolveAdditionalBossWaveLootTables(MythicRiftRunState runState, string defaultDelivery)
        {
            List<MythicRiftRewardExtraLootTable> resolvedTables = new();
            if (runState?.Config?.BossWaveContent == null)
                return resolvedTables;

            foreach (MythicRiftContentEntry bossContent in runState.Config.BossWaveContent.Skip(1))
            {
                if (bossContent?.BossLootTableProtoRef == PrototypeId.Invalid)
                    continue;

                resolvedTables.Add(new()
                {
                    Id = $"wave-boss/{bossContent.Id}",
                    LootTableProtoRef = bossContent.BossLootTableProtoRef,
                    Rolls = 1,
                    ChancePercent = 100f,
                    Delivery = MythicRiftRewardTuning.NormalizeDelivery(defaultDelivery)
                });
            }

            return resolvedTables;
        }

        private List<MythicRiftRewardExtraLootTable> ResolveExtraRewardLootTables(MythicRiftRunState runState, MythicRiftRewardTuning tuning, bool timedSuccess, bool checkpointSuccess)
        {
            List<MythicRiftRewardExtraLootTable> resolvedTables = new();
            if (runState?.Config == null || tuning?.ExtraLootTables == null)
                return resolvedTables;

            foreach (MythicRiftExtraLootTableTuning entry in tuning.ExtraLootTables)
            {
                if (entry == null || entry.AppliesTo(runState, timedSuccess, checkpointSuccess) == false)
                    continue;

                if (entry.ChancePercent <= 0f)
                    continue;

                if (entry.ChancePercent < 100f && Game.Random.NextFloat() * 100f >= entry.ChancePercent)
                    continue;

                string lootTablePrototype = tuning.ResolveLootTableReference(entry.LootTablePrototype);
                PrototypeId lootTableProtoRef = ResolvePrototype(lootTablePrototype);
                if (lootTableProtoRef == PrototypeId.Invalid || lootTableProtoRef.As<LootTablePrototype>() == null)
                {
                    Logger.Warn($"Mythic Rift reward tuning skipped invalid extra loot table id={entry.Id} lootTable={lootTablePrototype}");
                    continue;
                }

                resolvedTables.Add(new()
                {
                    Id = entry.Id,
                    LootTableProtoRef = lootTableProtoRef,
                    Rolls = Math.Max(entry.Rolls, 1),
                    ChancePercent = entry.ChancePercent,
                    Delivery = MythicRiftRewardTuning.NormalizeDelivery(entry.Delivery)
                });
            }

            foreach (MythicRiftRewardRecipeTuning recipe in tuning.RewardRecipes)
            {
                if (recipe == null || recipe.AppliesTo(runState, timedSuccess, checkpointSuccess) == false)
                    continue;

                foreach (MythicRiftRewardRecipeTableTuning table in recipe.Tables)
                {
                    if (table == null || table.ChancePercent <= 0f)
                        continue;

                    if (table.ChancePercent < 100f && Game.Random.NextFloat() * 100f >= table.ChancePercent)
                        continue;

                    string lootTablePrototype = tuning.ResolveLootTableReference(table.LootTable);
                    PrototypeId lootTableProtoRef = ResolvePrototype(lootTablePrototype);
                    if (lootTableProtoRef == PrototypeId.Invalid || lootTableProtoRef.As<LootTablePrototype>() == null)
                    {
                        Logger.Warn($"Mythic Rift reward tuning skipped invalid recipe table recipe={recipe.Id} table={table.Id} lootTable={lootTablePrototype}");
                        continue;
                    }

                    resolvedTables.Add(new()
                    {
                        Id = $"{recipe.Id}/{table.Id}",
                        LootTableProtoRef = lootTableProtoRef,
                        Rolls = Math.Max(table.Rolls, 1),
                        ChancePercent = table.ChancePercent,
                        Delivery = MythicRiftRewardTuning.NormalizeDelivery(table.Delivery)
                    });
                }
            }

            return resolvedTables;
        }

        private List<MythicRiftRewardGuaranteedItem> ResolveGuaranteedRewardItems(
            MythicRiftRunState runState,
            MythicRiftRewardTuning tuning,
            bool timedSuccess,
            bool checkpointSuccess)
        {
            List<MythicRiftRewardGuaranteedItem> resolvedItems = new();
            if (runState?.Config == null || tuning?.GuaranteedItems == null)
                return resolvedItems;

            foreach (MythicRiftGuaranteedItemTuning entry in tuning.GuaranteedItems)
            {
                if (entry == null || entry.AppliesTo(runState, timedSuccess, checkpointSuccess) == false)
                    continue;

                PrototypeId itemProtoRef = (PrototypeId)entry.ItemPrototypeRuntimeId;
                if (itemProtoRef == PrototypeId.Invalid || itemProtoRef.As<ItemPrototype>() == null)
                {
                    Logger.Warn($"Mythic Rift reward tuning skipped invalid guaranteed item id={entry.Id} runtimeId={entry.ItemPrototypeRuntimeId}");
                    continue;
                }

                resolvedItems.Add(new()
                {
                    Id = entry.Id,
                    ItemProtoRef = itemProtoRef,
                    Quantity = Math.Max(entry.Quantity, 1),
                    Delivery = MythicRiftRewardTuning.NormalizeDelivery(entry.Delivery)
                });
            }

            return resolvedItems;
        }

        private List<MythicRiftRewardGuaranteedItem> ResolveRandomItemPoolRewards(
            MythicRiftRunState runState,
            MythicRiftRewardTuning tuning,
            bool timedSuccess,
            bool checkpointSuccess)
        {
            List<MythicRiftRewardGuaranteedItem> resolvedItems = new();
            if (runState?.Config == null || tuning?.RandomItemPools == null)
                return resolvedItems;

            foreach (MythicRiftRandomItemPoolTuning entry in tuning.RandomItemPools)
            {
                if (entry == null || entry.AppliesTo(runState, timedSuccess, checkpointSuccess) == false)
                    continue;

                IReadOnlyList<PrototypeId> candidates = ResolveRewardItemPool(entry.PrototypeDirectoryPrefix);
                if (candidates.Count == 0)
                {
                    Logger.Warn($"Mythic Rift reward tuning found no eligible items for random pool id={entry.Id} directory={entry.PrototypeDirectoryPrefix}");
                    continue;
                }

                for (int roll = 0; roll < entry.Rolls; roll++)
                {
                    if (entry.ChancePercent <= 0f ||
                        (entry.ChancePercent < 100f && Game.Random.NextFloat() * 100f >= entry.ChancePercent))
                    {
                        continue;
                    }

                    PrototypeId itemProtoRef = candidates[Game.Random.Next(0, candidates.Count)];
                    resolvedItems.Add(new()
                    {
                        Id = $"{entry.Id}/{itemProtoRef.GetNameFormatted()}",
                        ItemProtoRef = itemProtoRef,
                        Quantity = 1,
                        ItemLevel = entry.ItemLevel,
                        Delivery = MythicRiftRewardTuning.NormalizeDelivery(entry.Delivery)
                    });
                }
            }

            return resolvedItems;
        }

        private IReadOnlyList<PrototypeId> ResolveRewardItemPool(string prototypeDirectoryPrefix)
        {
            string normalizedPrefix = prototypeDirectoryPrefix?.Trim().Replace('\\', '/') ?? string.Empty;
            if (normalizedPrefix.Length > 0 && normalizedPrefix.EndsWith('/') == false)
                normalizedPrefix += "/";

            if (string.IsNullOrWhiteSpace(normalizedPrefix))
                return Array.Empty<PrototypeId>();

            if (_rewardItemPoolsByDirectory.TryGetValue(normalizedPrefix, out IReadOnlyList<PrototypeId> cachedPool))
                return cachedPool;

            List<PrototypeId> candidates = new();
            foreach (PrototypeId itemProtoRef in GameDatabase.DataDirectory.IteratePrototypesInHierarchy<ItemPrototype>(
                         PrototypeIterateFlags.NoAbstractApprovedOnly))
            {
                string prototypeName = GameDatabase.GetPrototypeName(itemProtoRef);
                if (prototypeName.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase) == false)
                    continue;

                ItemPrototype itemProto = itemProtoRef.As<ItemPrototype>();
                if (itemProto?.IsLiveTuningEnabled() != true)
                    continue;

                candidates.Add(itemProtoRef);
            }

            candidates.Sort((left, right) => string.Compare(
                GameDatabase.GetPrototypeName(left),
                GameDatabase.GetPrototypeName(right),
                StringComparison.OrdinalIgnoreCase));
            _rewardItemPoolsByDirectory[normalizedPrefix] = candidates;
            Logger.Info($"Mythic Rift resolved random reward item pool directory={normalizedPrefix} candidates={candidates.Count}.");
            return candidates;
        }

        private void GrantProgressionForSuccessfulRun(MythicRiftRunState runState)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Success)
                return;

            HashSet<ulong> recipientDbIds = new(runState.ProgressionEligiblePlayerDbIds);
            if (recipientDbIds.Count == 0)
            {
                Logger.Info($"Mythic Rift run {runState.Config.RunId} completed but no players satisfied the competitive progression rule for level unlocks.");
                return;
            }

            foreach (ulong playerDbId in recipientDbIds)
            {
                int unlockedLevel = GrantNextRiftLevel(playerDbId, runState.Config.RiftLevel);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} unlocked Rift level {unlockedLevel} for playerDbId=0x{playerDbId:X}.");
            }
        }

        private HashSet<string> BuildRandomMapExclusions(Player requester, Party party)
        {
            HashSet<string> excludedContentIds = new(StringComparer.OrdinalIgnoreCase);
            TryAddPlayerRandomMapExclusions(excludedContentIds, requester);
            if (party == null)
                return excludedContentIds;

            foreach (var kvp in party)
            {
                Player partyMember = Game.EntityManager.GetEntityByDbGuid<Player>(kvp.Value.PlayerDbId);
                TryAddPlayerRandomMapExclusions(excludedContentIds, partyMember, kvp.Value.PlayerDbId);
            }

            return excludedContentIds;
        }

        private void TryAddPlayerRandomMapExclusions(HashSet<string> excludedContentIds, Player player, ulong playerDbId = 0)
        {
            if (excludedContentIds == null)
                return;

            ulong resolvedPlayerDbId = player?.DatabaseUniqueId ?? playerDbId;
            TryAddLastCompletedMapContentId(excludedContentIds, resolvedPlayerDbId);
            TryAddRecentRandomMapContentIds(excludedContentIds, resolvedPlayerDbId);
            TryAddCurrentRegionMapContentId(excludedContentIds, player?.GetRegion());
        }

        private void TryAddLastCompletedMapContentId(HashSet<string> excludedContentIds, ulong playerDbId)
        {
            if (excludedContentIds == null || playerDbId == 0)
                return;

            if (_lastCompletedMapContentIdByPlayer.TryGetValue(playerDbId, out string contentId) == false)
                return;

            if (string.IsNullOrWhiteSpace(contentId))
                return;

            excludedContentIds.Add(contentId);
        }

        private void TryAddRecentRandomMapContentIds(HashSet<string> excludedContentIds, ulong playerDbId)
        {
            if (excludedContentIds == null || playerDbId == 0)
                return;

            if (_recentRandomMapContentIdsByPlayer.TryGetValue(playerDbId, out List<string> recentContentIds) == false)
                return;

            foreach (string recentContentId in recentContentIds)
            {
                if (string.IsNullOrWhiteSpace(recentContentId) == false)
                    excludedContentIds.Add(recentContentId);
            }
        }

        private void TryAddCurrentRegionMapContentId(HashSet<string> excludedContentIds, Region region)
        {
            if (excludedContentIds == null || region == null)
                return;

            MythicRiftContentEntry currentContent = ResolveContentByRegion(region);
            string contentId = currentContent?.Id;
            if (string.IsNullOrWhiteSpace(contentId))
                return;

            excludedContentIds.Add(contentId);
        }

        private MythicRiftContentEntry ResolveContentByRegion(Region region)
        {
            if (region == null)
                return null;

            return _contentPool.FirstOrDefault(content => ContentMatchesRegion(content, region));
        }

        private static bool ContentMatchesRegion(MythicRiftContentEntry content, Region region)
        {
            if (content == null || region == null)
                return false;

            if (region.PrototypeDataRef == content.RegionProtoRef)
                return true;

            RegionPrototype currentRegionProto = region.Prototype;
            RegionPrototype expectedRegionProto = content.RegionProtoRef.As<RegionPrototype>();
            if (AreRegionsEquivalent(expectedRegionProto, currentRegionProto))
                return true;

            RegionConnectionTargetPrototype startTargetProto = content.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();
            RegionPrototype startTargetRegionProto = startTargetProto?.Region.As<RegionPrototype>();
            return AreRegionsEquivalent(startTargetRegionProto, currentRegionProto);
        }

        private void TrackLastCompletedMapContent(MythicRiftRunState runState)
        {
            string contentId = runState?.Config?.Content?.Id;
            if (string.IsNullOrWhiteSpace(contentId))
                return;

            foreach (ulong playerDbId in runState.ParticipantPlayerDbIds)
            {
                if (playerDbId != 0)
                {
                    _lastCompletedMapContentIdByPlayer[playerDbId] = contentId;
                    TrackRecentRandomMapContentId(playerDbId, contentId);
                }
            }
        }

        private void TrackRecentlySelectedMapContent(MythicRiftRunState runState)
        {
            string contentId = runState?.Config?.Content?.Id;
            if (string.IsNullOrWhiteSpace(contentId))
                return;

            foreach (ulong playerDbId in runState.ParticipantPlayerDbIds)
                TrackRecentRandomMapContentId(playerDbId, contentId);
        }

        private void TrackRecentRandomMapContentId(ulong playerDbId, string contentId)
        {
            if (playerDbId == 0 || string.IsNullOrWhiteSpace(contentId))
                return;

            if (_recentRandomMapContentIdsByPlayer.TryGetValue(playerDbId, out List<string> recentContentIds) == false)
            {
                recentContentIds = new();
                _recentRandomMapContentIdsByPlayer[playerDbId] = recentContentIds;
            }

            recentContentIds.RemoveAll(existingContentId => string.Equals(existingContentId, contentId, StringComparison.OrdinalIgnoreCase));
            recentContentIds.Add(contentId);

            while (recentContentIds.Count > RecentRandomMapHistoryLimit)
                recentContentIds.RemoveAt(0);
        }

        private void CaptureBossUnlockEligibility(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            runState.SnapshotBossUnlockEligiblePlayers(GetCurrentRunRegionPlayerDbIds(runState));
        }

        private void CaptureSuccessfulCompletionEligibility(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            HashSet<ulong> currentRunRegionPlayerDbIds = GetCurrentRunRegionPlayerDbIds(runState);
            if (runState.Config.Content.BossOnlyCheckpointEligible)
                runState.SnapshotBossUnlockEligiblePlayers(currentRunRegionPlayerDbIds);

            runState.SnapshotProgressionEligiblePlayers(currentRunRegionPlayerDbIds);
        }

        private HashSet<ulong> GetCurrentRunRegionPlayerDbIds(MythicRiftRunState runState)
        {
            HashSet<ulong> playerDbIds = new();
            if (runState == null || runState.RegionId == 0)
                return playerDbIds;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return playerDbIds;

            foreach (Player player in new PlayerIterator(region))
            {
                if (player?.DatabaseUniqueId != 0 &&
                    runState.IsParticipant(player.DatabaseUniqueId) &&
                    runState.HasParticipantLeftEarly(player.DatabaseUniqueId) == false)
                    playerDbIds.Add(player.DatabaseUniqueId);
            }

            return playerDbIds;
        }

        private static bool ShouldCountKill(in EntityDeadGameEvent evt)
        {
            if (evt.Defender == null)
                return false;

            if (evt.Defender is Avatar)
                return false;

            if (evt.Defender is Agent == false)
                return false;

            if (evt.Defender.IsHostileToPlayers() == false)
                return false;

            if (evt.Defender.WorldEntityPrototype?.MissionEntityDeathCredit == false)
                return false;

            if (evt.Killer != null)
                return true;

            if (evt.Attacker?.GetOwnerOfType<Player>() != null)
                return true;

            return evt.Defender.TagPlayers.HasTags;
        }

        private static int GetKillCountCredit(in EntityDeadGameEvent evt)
        {
            RankPrototype rankProto = evt.Defender?.GetRankPrototype();
            if (rankProto == null)
                return 1;

            return rankProto.Rank switch
            {
                Rank.Champion => ChampionKillCountCredit,
                Rank.Elite => EliteKillCountCredit,
                Rank.MiniBoss => MiniBossKillCountCredit,
                _ => 1
            };
        }

        private MythicRiftRunConfig CreateRunConfig(
            MythicRiftContentEntry content,
            MythicRiftContentEntry bossContent,
            int riftLevel,
            int requestedPlayerCount,
            int killQuota,
            TimeSpan timeLimit,
            MythicRiftMode mode)
        {
            if (content == null || bossContent == null)
                return null;

            if (content.HasValidMap == false ||
                content.SupportsPlayerCount(requestedPlayerCount) == false ||
                bossContent.HasValidBossSource == false)
                return null;

            bool useThirtyWaveMode = mode == MythicRiftMode.Endless;
            MythicRiftWaveProfile waveProfile = MythicRiftScaling.GetThirtyWaveProfile(riftLevel);
            MythicRiftDifficultySnapshot difficulty = MythicRiftScaling.BuildSnapshot(riftLevel, requestedPlayerCount, useThirtyWaveMode);
            int requestedBossCount = useThirtyWaveMode ? waveProfile.BossCount : 1;
            IReadOnlyList<MythicRiftContentEntry> bossWaveContent = MythicRiftBossWaveSelector.BuildDistinctRoster(
                bossContent,
                _contentPool.Where(entry => entry.RandomBossEligible && entry.HasValidBossSource),
                requestedBossCount,
                count => Game.Random.Next(0, count));
            if (bossWaveContent.Count == 0)
                return null;

            int resolvedKillQuota = content.BossOnlyCheckpointEligible
                ? 1
                : ResolveKillQuota(content, killQuota);

            return new MythicRiftRunConfig
            {
                RunId = _nextRunId++,
                RiftLevel = Math.Max(riftLevel, 1),
                Content = content,
                BossContent = bossContent,
                BossWaveContent = bossWaveContent,
                RequestedPlayerCount = Math.Max(requestedPlayerCount, 1),
                EffectivePlayerCount = difficulty.EffectivePlayerCount,
                KillQuota = resolvedKillQuota,
                TimeLimit = timeLimit <= TimeSpan.Zero ? TimeSpan.FromMinutes(10) : timeLimit,
                RegionProtoRef = content.RegionProtoRef,
                StartTargetProtoRef = content.StartTargetProtoRef,
                MissionProtoRef = content.MissionProtoRef,
                BossProtoRef = bossContent.BossProtoRef,
                BossLootTableProtoRef = bossContent.BossLootTableProtoRef,
                Difficulty = difficulty,
                Mode = mode,
                WaveNumber = useThirtyWaveMode ? waveProfile.Wave : Math.Max(riftLevel, 1),
                RequiredBossKillCount = bossWaveContent.Count
            };
        }

        private bool TryStartBossOnlyCheckpoint(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState?.Config?.Content?.BossOnlyCheckpointEligible != true)
                return false;

            if (runState.Status != MythicRiftRunStatus.Active ||
                runState.RegionId == 0 ||
                runState.BossSpawnCount >= runState.Config.RequiredBossKillCount)
                return false;

            if (_nextCheckpointBossSpawnRetryAt.TryGetValue(runState.Config.RunId, out TimeSpan nextRetryAt) && currentTime < nextRetryAt)
                return false;

            _nextCheckpointBossSpawnRetryAt[runState.Config.RunId] = currentTime + CheckpointBossSpawnRetryInterval;
            runState.UnlockBoss();
            if (TrySpawnConfiguredBoss(runState) == false)
            {
                Logger.Debug($"Mythic Rift checkpoint run {runState.Config.RunId} could not spawn boss {runState.Config.BossProtoRef.GetNameFormatted() ?? "unknown"} yet; retrying.");
                return false;
            }

            _nextCheckpointBossSpawnRetryAt.Remove(runState.Config.RunId);
            CaptureBossUnlockEligibility(runState);
            RefreshRiftHudWidgets(runState, currentTime);
            NotifyBossUnlocked(runState);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} started boss-only checkpoint at level {runState.Config.RiftLevel} in {runState.Config.Content.Id}.");
            return true;
        }

        private bool TryMaintainBossWave(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState?.Config?.Content?.BossOnlyCheckpointEligible == true)
                return false;

            if (runState == null ||
                runState.Status != MythicRiftRunStatus.Active ||
                runState.BossUnlocked == false ||
                runState.RegionId == 0 ||
                runState.BossSpawnCount >= runState.Config.RequiredBossKillCount)
            {
                return false;
            }

            if (_nextCheckpointBossSpawnRetryAt.TryGetValue(runState.Config.RunId, out TimeSpan nextRetryAt) && currentTime < nextRetryAt)
                return false;

            _nextCheckpointBossSpawnRetryAt[runState.Config.RunId] = currentTime + CheckpointBossSpawnRetryInterval;
            if (TrySpawnConfiguredBoss(runState) == false)
                return false;

            _nextCheckpointBossSpawnRetryAt.Remove(runState.Config.RunId);
            CaptureBossUnlockEligibility(runState);
            NotifyBossUnlocked(runState);
            return true;
        }

        private void MaintainCustomRiftPopulation(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState?.Config?.Content?.UseCustomPopulation != true)
                return;

            if (runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0 || runState.BossUnlocked)
                return;

            if (currentTime < runState.NextCustomPopulationSpawnAt)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            int liveCount = CountLiveCustomRiftPopulationEntities(runState, region);
            int maxAlive = GetCustomRiftPopulationMaxAlive(runState);
            if (liveCount >= maxAlive)
            {
                runState.SetNextCustomPopulationSpawnAt(currentTime + CustomRiftPopulationSpawnInterval);
                return;
            }

            int targetAlive = GetCustomRiftPopulationTargetAlive(runState);
            if (liveCount >= targetAlive)
            {
                runState.SetNextCustomPopulationSpawnAt(currentTime + CustomRiftPopulationSpawnInterval);
                return;
            }

            int spawnBatch = Math.Min(GetCustomRiftPopulationSpawnBatch(runState), maxAlive - liveCount);
            int spawned = 0;
            for (int i = 0; i < spawnBatch; i++)
            {
                if (TrySpawnCustomRiftPopulationMob(runState, region))
                    spawned++;
            }

            runState.SetNextCustomPopulationSpawnAt(currentTime + CustomRiftPopulationSpawnInterval);

            if (spawned > 0)
                Logger.Debug($"Mythic Rift run {runState.Config.RunId} spawned {spawned} custom population mob(s) for {runState.Config.Content.Id}. liveBefore={liveCount} totalSpawned={runState.CustomPopulationTotalSpawned}");
        }

        private int CountLiveCustomRiftPopulationEntities(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return 0;

            int liveCount = 0;
            foreach (ulong entityId in runState.CustomPopulationEntityIds.ToArray())
            {
                WorldEntity entity = Game.EntityManager.GetEntity<WorldEntity>(entityId);
                if (entity == null || entity.IsDestroyed || entity.IsDead || entity.Region != region)
                {
                    runState.RemoveCustomPopulationEntity(entityId);
                    continue;
                }

                if (entity is Agent && entity.IsHostileToPlayers())
                    liveCount++;
            }

            return liveCount;
        }

        private bool TrySpawnCustomRiftPopulationMob(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return false;

            Player anchorPlayer = PickCustomRiftPopulationAnchorPlayer(runState, region);
            Avatar anchorAvatar = anchorPlayer?.CurrentAvatar;
            if (anchorAvatar == null || anchorAvatar.IsAliveInWorld == false || anchorAvatar.Region != region)
                return false;

            AgentPrototype mobProto = PickCustomRiftPopulationMobPrototype();
            if (mobProto == null || mobProto.Bounds == null)
                return false;

            Vector3 spawnPosition = anchorAvatar.RegionLocation.Position + (anchorAvatar.Forward * CustomRiftPopulationFallbackSpawnDistance);
            Bounds spawnBounds = new(mobProto.Bounds, spawnPosition);
            PathFlags pathFlags = Region.GetPathFlagsForEntity(mobProto);

            bool foundPosition = region.ChooseRandomPositionNearPoint(
                ref spawnBounds,
                pathFlags,
                PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.CheckSpawns,
                CustomRiftPopulationSpawnMinDistance,
                CustomRiftPopulationSpawnMaxDistance,
                out spawnPosition,
                maxPositionTests: 96);

            if (foundPosition == false)
            {
                spawnBounds.Center = anchorAvatar.RegionLocation.Position + (anchorAvatar.Forward * CustomRiftPopulationFallbackSpawnDistance);
                foundPosition = region.ChoosePositionAtOrNearPoint(
                    ref spawnBounds,
                    pathFlags,
                    PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.PreferNoEntity,
                    BlockingCheckFlags.None,
                    CustomRiftPopulationFallbackSpawnDistance,
                    out spawnPosition,
                    maxPositionTests: 48);
            }

            if (foundPosition == false)
                return false;

            Cell spawnCell = region.GetCellAtPosition(spawnPosition);
            if (spawnCell == null)
                return false;

            spawnPosition = RegionLocation.ProjectToFloor(region, spawnPosition);
            spawnPosition.Z += mobProto.Bounds.GetBoundHalfHeight();

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = mobProto.DataRef;
            settings.Position = spawnPosition;
            settings.Orientation = anchorAvatar.RegionLocation.Orientation;
            settings.RegionId = region.Id;
            settings.Cell = spawnCell;
            settings.IsPopulation = true;

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            int level = spawnCell.Area.GetCharacterLevel(mobProto);
            settingsProperties[PropertyEnum.CharacterLevel] = level;
            settingsProperties[PropertyEnum.CombatLevel] = level;
            settingsProperties[PropertyEnum.DifficultyTier] = region.DifficultyTierRef;
            settingsProperties[PropertyEnum.Rank] = mobProto.Rank;
            settingsProperties[PropertyEnum.MissionXEncounterHostilityOk] = true;
            settings.Properties = settingsProperties;

            Agent spawnedAgent = Game.EntityManager.CreateEntity(settings) as Agent;
            if (spawnedAgent == null)
                return false;

            if (mobProto.ModifiersGuaranteed != null && mobProto.ModifiersGuaranteed.Length > 0)
            {
                foreach (PrototypeId boost in mobProto.ModifiersGuaranteed)
                    spawnedAgent.Properties[PropertyEnum.EnemyBoost, boost] = true;
            }

            runState.RegisterCustomPopulationEntity(spawnedAgent.Id);
            return true;
        }

        private Player PickCustomRiftPopulationAnchorPlayer(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return null;

            List<Player> players = new();
            foreach (Player player in new PlayerIterator(region))
            {
                if (player?.DatabaseUniqueId == 0 ||
                    runState.IsParticipant(player.DatabaseUniqueId) == false ||
                    runState.HasParticipantLeftEarly(player.DatabaseUniqueId))
                {
                    continue;
                }

                if (player.CurrentAvatar?.IsAliveInWorld == true)
                    players.Add(player);
            }

            if (players.Count == 0)
                return null;

            return players[Game.Random.Next(0, players.Count)];
        }

        private static Player PickBossSpawnAnchorPlayer(MythicRiftRunState runState, Region region)
        {
            if (runState == null || region == null)
                return null;

            List<Player> players = new();
            foreach (Player player in new PlayerIterator(region))
            {
                if (IsValidBossSpawnAnchorPlayer(player, runState, region))
                    players.Add(player);
            }

            if (players.Count <= 1)
                return players.FirstOrDefault();

            Player bestPlayer = null;
            float bestTotalDistanceSquared = float.MaxValue;
            foreach (Player candidate in players)
            {
                Vector3 candidatePosition = candidate.CurrentAvatar.RegionLocation.Position;
                float totalDistanceSquared = 0f;
                foreach (Player other in players)
                    totalDistanceSquared += Vector3.DistanceSquared2D(candidatePosition, other.CurrentAvatar.RegionLocation.Position);

                if (totalDistanceSquared < bestTotalDistanceSquared)
                {
                    bestPlayer = candidate;
                    bestTotalDistanceSquared = totalDistanceSquared;
                }
            }

            return bestPlayer;
        }

        private static bool IsValidBossSpawnAnchorPlayer(Player player, MythicRiftRunState runState, Region region)
        {
            if (player == null || player.DatabaseUniqueId == 0 || runState == null || region == null)
                return false;

            if (runState.HasParticipantLeftEarly(player.DatabaseUniqueId))
                return false;

            Avatar avatar = player.CurrentAvatar;
            return runState.IsParticipant(player.DatabaseUniqueId) &&
                   avatar?.IsAliveInWorld == true &&
                   avatar.Region == region;
        }

        private AgentPrototype PickCustomRiftPopulationMobPrototype()
        {
            PrototypeId[] mobRefs = GetCustomRiftPopulationMobPrototypeRefs();
            if (mobRefs.Length == 0)
                return null;

            PrototypeId mobRef = mobRefs[Game.Random.Next(0, mobRefs.Length)];
            return mobRef.As<AgentPrototype>();
        }

        private static PrototypeId[] GetCustomRiftPopulationMobPrototypeRefs()
        {
            if (_cachedCustomRiftPopulationMobPrototypeRefs != null)
                return _cachedCustomRiftPopulationMobPrototypeRefs;

            List<PrototypeId> resolvedRefs = new();
            foreach (string prototypeName in CustomRiftPopulationMobPrototypeNames)
            {
                PrototypeId prototypeRef = ResolvePrototype(prototypeName);
                if (prototypeRef != PrototypeId.Invalid && prototypeRef.As<AgentPrototype>() != null)
                    resolvedRefs.Add(prototypeRef);
            }

            _cachedCustomRiftPopulationMobPrototypeRefs = resolvedRefs.ToArray();
            return _cachedCustomRiftPopulationMobPrototypeRefs;
        }

        private static int GetCustomRiftPopulationTargetAlive(MythicRiftRunState runState)
        {
            int extraPlayers = Math.Max((runState?.EffectivePlayerCount ?? 1) - 1, 0);
            return CustomRiftPopulationBaseTargetAlive + (extraPlayers * CustomRiftPopulationTargetAlivePerExtraPlayer);
        }

        private static int GetCustomRiftPopulationMaxAlive(MythicRiftRunState runState)
        {
            int extraPlayers = Math.Max((runState?.EffectivePlayerCount ?? 1) - 1, 0);
            return CustomRiftPopulationBaseMaxAlive + (extraPlayers * CustomRiftPopulationMaxAlivePerExtraPlayer);
        }

        private static int GetCustomRiftPopulationSpawnBatch(MythicRiftRunState runState)
        {
            int extraPlayers = Math.Max((runState?.EffectivePlayerCount ?? 1) - 1, 0);
            return CustomRiftPopulationBaseSpawnBatch + (extraPlayers * CustomRiftPopulationSpawnBatchPerExtraPlayer);
        }

        private bool TrySpawnConfiguredBoss(MythicRiftRunState runState)
        {
            if (runState == null ||
                runState.RegionId == 0 ||
                runState.BossSpawnCount >= runState.Config.RequiredBossKillCount)
                return false;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return false;

            int requiredBossCount = Math.Max(runState.Config.RequiredBossKillCount, 1);
            int spawnedThisCall = 0;
            while (runState.BossSpawnCount < requiredBossCount)
            {
                int spawnIndex = runState.BossSpawnCount;
                MythicRiftContentEntry bossContent = runState.Config.BossWaveContent.ElementAtOrDefault(spawnIndex);
                AgentPrototype bossProto = bossContent?.BossProtoRef.As<AgentPrototype>();
                if (bossProto == null)
                    break;

                if (TryResolveBossSpawnLocation(runState, region, bossProto, spawnIndex, out Vector3 spawnPosition, out Orientation spawnOrientation, out Cell spawnCell) == false)
                    break;

                using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
                settings.EntityRef = bossProto.DataRef;
                settings.Position = spawnPosition;
                settings.Orientation = spawnOrientation;
                settings.RegionId = region.Id;
                settings.Cell = spawnCell;
                settings.IsPopulation = true;

                using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
                int level = spawnCell.Area.GetCharacterLevel(bossProto);
                settingsProperties[PropertyEnum.CharacterLevel] = level;
                settingsProperties[PropertyEnum.CombatLevel] = level;
                settingsProperties[PropertyEnum.DifficultyTier] = region.DifficultyTierRef;
                settingsProperties[PropertyEnum.Rank] = bossProto.Rank;
                settingsProperties[PropertyEnum.NoLootDrop] = true;
                settings.Properties = settingsProperties;

                Agent bossAgent = Game.EntityManager.CreateEntity(settings) as Agent;
                if (bossAgent == null)
                    break;

                if (bossProto.ModifiersGuaranteed != null && bossProto.ModifiersGuaranteed.Length > 0)
                {
                    foreach (PrototypeId boost in bossProto.ModifiersGuaranteed)
                        bossAgent.Properties[PropertyEnum.EnemyBoost, boost] = true;
                }

                ApplyCheckpointBossTuning(runState, bossAgent);
                runState.AttachBoss(bossAgent.Id);
                spawnedThisCall++;
                Logger.Info($"Mythic Rift run {runState.Config.RunId} spawned boss {runState.BossSpawnCount}/{requiredBossCount}: {bossAgent.PrototypeName} from boss pool entry {bossContent.Id}.");
            }

            return spawnedThisCall > 0 && runState.BossSpawnCount >= requiredBossCount;
        }

        private void TrySpawnPendingMilestoneEncounters(MythicRiftRunState runState, bool allowAfterBossUnlock = false)
        {
            if (runState == null ||
                runState.Status != MythicRiftRunStatus.Active ||
                runState.Config.Content.BossOnlyCheckpointEligible ||
                (runState.BossUnlocked && allowAfterBossUnlock == false))
            {
                return;
            }

            int requiredCount = Math.Max(runState.Config.KillQuota, 1);
            int progressPercent = (int)Math.Floor((double)runState.CurrentKillCount * 100d / requiredCount);
            foreach (int milestonePercent in KillProgressMilestonePercents)
            {
                if (progressPercent < milestonePercent || runState.HasSpawnedMilestoneEncounter(milestonePercent))
                    continue;

                if (TrySpawnMilestoneEncounter(runState, milestonePercent) == false)
                    continue;

                runState.MarkMilestoneEncounterSpawned(milestonePercent);
            }
        }

        private bool TrySpawnMilestoneEncounter(MythicRiftRunState runState, int milestonePercent)
        {
            Region region = runState != null && runState.RegionId > 0
                ? Game.RegionManager.GetRegion(runState.RegionId)
                : null;
            if (region == null)
                return false;

            int spawnedCount;
            string encounterName;
            switch (milestonePercent)
            {
                case 25:
                    spawnedCount = TrySpawnRankedMilestoneSquad(runState, region, Rank.Champion, 3);
                    encounterName = "Champion invasion";
                    break;

                case 50:
                    spawnedCount = TrySpawnMilestoneMiniBoss(runState, region) ? 1 : 0;
                    encounterName = "Rift mini-boss";
                    break;

                case 75:
                    spawnedCount = TrySpawnRankedMilestoneSquad(runState, region, Rank.Elite, 3);
                    encounterName = "Elite strike team";
                    break;

                default:
                    return false;
            }

            if (spawnedCount <= 0)
                return false;

            NotifyRunPlayers(runState, $"[Cosmic Rift] {milestonePercent}% milestone: {encounterName} incoming.");
            Logger.Info($"Mythic Rift run {runState.Config.RunId} spawned milestone encounter {milestonePercent}% ({encounterName}), entities={spawnedCount}.");
            return true;
        }

        private int TrySpawnRankedMilestoneSquad(MythicRiftRunState runState, Region region, Rank rank, int count)
        {
            int spawnedCount = 0;
            for (int i = 0; i < Math.Max(count, 1); i++)
            {
                AgentPrototype agentProto = PickCustomRiftPopulationMobPrototype();
                if (agentProto != null && TrySpawnMilestoneAgent(runState, region, agentProto, rank, i))
                    spawnedCount++;
            }

            return spawnedCount;
        }

        private bool TrySpawnMilestoneMiniBoss(MythicRiftRunState runState, Region region)
        {
            List<MythicRiftContentEntry> candidates = _contentPool
                .Where(entry => entry.RandomBossEligible && entry.HasValidBossSource)
                .Where(entry => runState.Config.BossWaveContent.Any(
                    waveBoss => string.Equals(waveBoss.Id, entry.Id, StringComparison.OrdinalIgnoreCase)) == false)
                .ToList();

            MythicRiftContentEntry selected = candidates.Count > 0
                ? candidates[Game.Random.Next(0, candidates.Count)]
                : runState.Config.BossWaveContent.FirstOrDefault();
            AgentPrototype agentProto = selected?.BossProtoRef.As<AgentPrototype>();
            return agentProto != null && TrySpawnMilestoneAgent(runState, region, agentProto, Rank.MiniBoss, 0);
        }

        private bool TrySpawnMilestoneAgent(
            MythicRiftRunState runState,
            Region region,
            AgentPrototype agentProto,
            Rank rank,
            int spawnIndex)
        {
            if (runState == null || region == null || agentProto == null)
                return false;

            if (TryResolvePlayerAnchoredBossSpawnLocation(
                    runState,
                    region,
                    agentProto,
                    spawnIndex,
                    out Vector3 spawnPosition,
                    out Orientation spawnOrientation,
                    out Cell spawnCell) == false)
            {
                return false;
            }

            RankPrototype rankProto = GameDatabase.PopulationGlobalsPrototype?.GetRankByEnum(rank);

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = agentProto.DataRef;
            settings.Position = spawnPosition;
            settings.Orientation = spawnOrientation;
            settings.RegionId = region.Id;
            settings.Cell = spawnCell;
            settings.IsPopulation = true;

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            int level = spawnCell.Area.GetCharacterLevel(agentProto);
            settingsProperties[PropertyEnum.CharacterLevel] = level;
            settingsProperties[PropertyEnum.CombatLevel] = level;
            settingsProperties[PropertyEnum.DifficultyTier] = region.DifficultyTierRef;
            settingsProperties[PropertyEnum.Rank] = rankProto?.DataRef ?? agentProto.Rank;
            settingsProperties[PropertyEnum.NoLootDrop] = true;
            settingsProperties[PropertyEnum.MissionXEncounterHostilityOk] = true;
            settings.Properties = settingsProperties;

            Agent spawnedAgent = Game.EntityManager.CreateEntity(settings) as Agent;
            if (spawnedAgent == null)
                return false;

            if (agentProto.ModifiersGuaranteed != null)
            {
                foreach (PrototypeId boost in agentProto.ModifiersGuaranteed)
                    spawnedAgent.Properties[PropertyEnum.EnemyBoost, boost] = true;
            }

            runState.RegisterCustomPopulationEntity(spawnedAgent.Id);
            return true;
        }

        private bool TryResolveBossSpawnLocation(MythicRiftRunState runState, Region region, AgentPrototype bossProto, int spawnIndex, out Vector3 spawnPosition, out Orientation spawnOrientation, out Cell spawnCell)
        {
            return TryResolvePlayerAnchoredBossSpawnLocation(runState, region, bossProto, spawnIndex, out spawnPosition, out spawnOrientation, out spawnCell);
        }

        private bool TryResolvePlayerAnchoredBossSpawnLocation(MythicRiftRunState runState, Region region, AgentPrototype bossProto, int spawnIndex, out Vector3 spawnPosition, out Orientation spawnOrientation, out Cell spawnCell)
        {
            spawnPosition = Vector3.Zero;
            spawnOrientation = Orientation.Zero;
            spawnCell = null;

            Player anchorPlayer = PickBossSpawnAnchorPlayer(runState, region);
            Avatar anchorAvatar = anchorPlayer?.CurrentAvatar;
            if (anchorAvatar == null || anchorAvatar.IsAliveInWorld == false || anchorAvatar.Region != region)
                return false;

            Vector3 anchorPosition = anchorAvatar.RegionLocation.Position;
            Cell anchorCell = anchorAvatar.Cell ?? region.GetCellAtPosition(anchorPosition);
            if (anchorCell == null)
                return false;

            spawnOrientation = anchorAvatar.RegionLocation.Orientation;
            if (bossProto.Bounds == null)
            {
                spawnPosition = anchorPosition;
                spawnCell = anchorCell;
                return true;
            }

            PathFlags pathFlags = Region.GetPathFlagsForEntity(bossProto);
            Vector3 forward = Vector3.SafeNormalize2D(anchorAvatar.Forward, Vector3.XAxis);
            Vector3 right = Vector3.Perp2D(forward);
            Vector3[] directions =
            {
                forward,
                -forward,
                right,
                -right,
                Vector3.SafeNormalize2D(forward + right, forward),
                Vector3.SafeNormalize2D(forward - right, forward),
                Vector3.SafeNormalize2D(-forward + right, -forward),
                Vector3.SafeNormalize2D(-forward - right, -forward)
            };

            for (int attempt = 0; attempt < directions.Length; attempt++)
            {
                Vector3 direction = directions[(spawnIndex + attempt) % directions.Length];
                Vector3 preferredPosition = anchorPosition + (direction * CheckpointBossSpawnDistance);
                if (TryChooseCheckpointBossSpawnPosition(region, bossProto, preferredPosition, anchorCell, pathFlags, out spawnPosition, out spawnCell))
                    return true;
            }

            if (TryChooseCheckpointBossSpawnPosition(region, bossProto, anchorPosition, anchorCell, pathFlags, out spawnPosition, out spawnCell))
                return true;

            spawnPosition = anchorPosition;
            spawnCell = anchorCell;
            return true;
        }

        private static bool TryChooseCheckpointBossSpawnPosition(Region region, AgentPrototype bossProto, Vector3 preferredPosition, Cell preferredCell, PathFlags pathFlags, out Vector3 spawnPosition, out Cell spawnCell)
        {
            spawnPosition = Vector3.Zero;
            spawnCell = null;

            if (region == null || bossProto?.Bounds == null || preferredCell == null)
                return false;

            Bounds spawnBounds = new(bossProto.Bounds, preferredPosition);
            if (region.ChoosePositionAtOrNearPoint(
                ref spawnBounds,
                pathFlags,
                PositionCheckFlags.CanBeBlockedEntity | PositionCheckFlags.PreferNoEntity,
                BlockingCheckFlags.None,
                CheckpointBossSpawnSearchDistance,
                out Vector3 resolvedPosition,
                maxPositionTests: 64) == false)
            {
                return false;
            }

            Cell resolvedCell = region.GetCellAtPosition(resolvedPosition);
            if (resolvedCell == null || resolvedCell != preferredCell)
                return false;

            spawnPosition = resolvedPosition;
            spawnCell = resolvedCell;
            return true;
        }

        private static void ApplyCheckpointBossTuning(MythicRiftRunState runState, Agent bossAgent)
        {
            if (runState?.Config?.Content?.BossOnlyCheckpointEligible != true || bossAgent == null)
                return;

            if (runState.Config.UseThirtyWaveMode)
                return;

            float healthMultiplier = GetCheckpointBossHealthMultiplier(runState.Config.RiftLevel);
            if (healthMultiplier <= 1f)
                return;

            float existingHealthBonus = bossAgent.Properties[PropertyEnum.HealthPctBonus];
            bossAgent.Properties[PropertyEnum.HealthPctBonus] = existingHealthBonus + healthMultiplier - 1f;
            bossAgent.Properties[PropertyEnum.Health] = bossAgent.Properties[PropertyEnum.HealthMax];
        }

        private static float GetCheckpointBossHealthMultiplier(int riftLevel)
        {
            int checkpointTier = Math.Max(riftLevel / CheckpointBossHealthTierInterval, 1);
            float multiplier = CheckpointBossBaseHealthMultiplier + ((checkpointTier - 1) * CheckpointBossHealthMultiplierPerTier);
            return Math.Clamp(multiplier, CheckpointBossBaseHealthMultiplier, CheckpointBossMaxHealthMultiplier);
        }

        private void TryAutoGrantCompletionRewards(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            if (runState.Status is not (MythicRiftRunStatus.Success or MythicRiftRunStatus.Failed))
                return;

            if (runState.RewardsGranted)
                return;

            int grantedCount = GrantRewardsToRunPlayers(runState.Config.RunId);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} auto-granted completion rewards to {grantedCount} player(s).");
        }

        private bool CompleteRunSuccess(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null)
                return false;

            CaptureSuccessfulCompletionEligibility(runState);
            runState.SnapshotRewardEligiblePlayers(GetCurrentRunRegionPlayerDbIds(runState));
            runState.MarkSuccess(currentTime);
            if (runState.Status != MythicRiftRunStatus.Success)
                return false;

            ResolveRewardOutcome(runState);
            GrantProgressionForSuccessfulRun(runState);
            TrackLastCompletedMapContent(runState);
            TryAutoGrantCompletionRewards(runState);
            TryRestoreRegionDifficultyScaling(runState);
            ClearRiftObjectiveWidgets(runState);
            SendStopRiftTimer(runState);
            int eligibleUnlockCount = runState.ProgressionEligiblePlayerDbIds.Count;
            string successMessage = eligibleUnlockCount > 0
                ? $"Rift cleared. Next level unlocked for {eligibleUnlockCount} eligible player(s). Bonus loot granted."
                : "Rift cleared. Loot granted, but no players met the next-level unlock rule.";
            TrySendRiftClearedBanner(runState);
            NotifyRunCompleted(runState, success: true, successMessage);
            TrySpawnReturnPortal(runState);
            return true;
        }

        private bool CompleteRunFailure(MythicRiftRunState runState, TimeSpan currentTime, string reason, bool returnParticipantsToHub = false)
        {
            if (runState == null)
                return false;

            runState.SnapshotRewardEligiblePlayers(GetCurrentRunRegionPlayerDbIds(runState));
            runState.MarkFailed(currentTime);
            if (runState.Status != MythicRiftRunStatus.Failed)
                return false;

            ResolveRewardOutcome(runState);
            TrackLastCompletedMapContent(runState);
            TryAutoGrantCompletionRewards(runState);
            TryRestoreRegionDifficultyScaling(runState);
            ClearRiftObjectiveWidgets(runState);
            SendStopRiftTimer(runState);
            NotifyRunCompleted(runState, success: false, reason);

            if (returnParticipantsToHub)
                QueueFailedRunEvacuation(runState, currentTime);
            else
                RequestRunRegionShutdownWhenVacant(runState);

            Logger.Info($"Mythic Rift run {runState.Config.RunId} failed. reason={reason}");
            return true;
        }

        private bool AbortRun(MythicRiftRunState runState, TimeSpan currentTime, string reason)
        {
            if (runState == null)
                return false;

            runState.MarkAborted(currentTime);
            if (runState.Status != MythicRiftRunStatus.Aborted)
                return false;

            TrackLastCompletedMapContent(runState);
            TryRestoreRegionDifficultyScaling(runState);
            ClearRiftObjectiveWidgets(runState);
            SendStopRiftTimer(runState);
            RequestRunRegionShutdownWhenVacant(runState);
            NotifyRunCompleted(runState, success: false, reason);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} aborted. reason={reason}");
            return true;
        }

        private void TryAutoBindAndStartPendingRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Pending || runState.RegionId != 0)
                return;

            foreach (ulong participantPlayerDbId in runState.ParticipantPlayerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(participantPlayerDbId);
                Region region = player?.GetRegion();
                if (region == null)
                    continue;

                if (IsMatchingRunRegion(region, runState) == false)
                    continue;

                if (AttachRunToRegion(runState.Config.RunId, region) == false)
                    return;

                StartRun(runState.Config.RunId, currentTime);
                Logger.Info($"Mythic Rift run {runState.Config.RunId} auto-bound to region {region.PrototypeName} (0x{region.Id:X}) and started.");
                return;
            }
        }

        private static bool IsMatchingRunRegion(Region region, MythicRiftRunState runState)
        {
            if (region == null || runState?.Config == null)
                return false;

            if (region.PrototypeDataRef == runState.Config.RegionProtoRef)
                return true;

            RegionPrototype currentRegionProto = region.Prototype;
            RegionPrototype expectedRegionProto = runState.Config.RegionProtoRef.As<RegionPrototype>();
            if (AreRegionsEquivalent(expectedRegionProto, currentRegionProto))
                return true;

            RegionConnectionTargetPrototype startTargetProto = runState.Config.StartTargetProtoRef.As<RegionConnectionTargetPrototype>();
            RegionPrototype startTargetRegionProto = startTargetProto?.Region.As<RegionPrototype>();
            if (AreRegionsEquivalent(startTargetRegionProto, currentRegionProto))
                return true;

            return false;
        }

        private static bool AreRegionsEquivalent(RegionPrototype regionA, RegionPrototype regionB)
        {
            return RegionPrototype.Equivalent(regionA, regionB) || RegionPrototype.Equivalent(regionB, regionA);
        }

        private static bool IsPlayerInRunRegion(Player player, MythicRiftRunState runState)
        {
            Region region = player?.GetRegion();
            if (region == null || runState == null)
                return false;

            if (runState.RegionId != 0 && region.Id == runState.RegionId)
                return true;

            return IsMatchingRunRegion(region, runState);
        }

        private void UpdateParticipantPresence(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.ParticipantCount == 0)
                return;

            foreach (ulong participantPlayerDbId in runState.ParticipantPlayerDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(participantPlayerDbId);
                if (player == null)
                    continue;

                runState.TouchParticipantPresence(currentTime);
                return;
            }
        }

        private void RegisterBoundRegionPlayersAsParticipants(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return;

            RegisterRegionPlayersAsParticipants(runState, region);
        }

        private bool TryHandleParticipantExit(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.RegionId == 0)
                return false;

            List<ulong> exitedPlayerDbIds = null;
            foreach (ulong participantPlayerDbId in runState.ParticipantPlayerDbIds.ToList())
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(participantPlayerDbId);
                if (player == null || player.GetRegion() == null)
                    continue;

                if (runState.HasParticipantBeenSeenInRunRegion(participantPlayerDbId) == false)
                    continue;

                if (IsPlayerInRunRegion(player, runState))
                    continue;

                if (runState.MarkParticipantLeftEarly(participantPlayerDbId))
                {
                    exitedPlayerDbIds ??= new();
                    exitedPlayerDbIds.Add(participantPlayerDbId);
                }
            }

            if (exitedPlayerDbIds != null)
            {
                foreach (ulong exitedPlayerDbId in exitedPlayerDbIds)
                {
                    Player exitedPlayer = Game.EntityManager.GetEntityByDbGuid<Player>(exitedPlayerDbId);
                    string playerName = exitedPlayer?.GetName() ?? $"0x{exitedPlayerDbId:X}";
                    NotifyRunPlayers(runState, $"[Cosmic Rift] {playerName} left the Rift and is no longer eligible for rewards or level unlocks.");
                    Logger.Info($"Mythic Rift run {runState.Config.RunId} marked participant playerDbId=0x{exitedPlayerDbId:X} as left early.");
                }
            }

            if (runState.ParticipantCount > 0 || HasAnyPlayerInRunRegion(runState))
                return false;

            string reason = "All participants left the Rift before completion. The Rift has closed and a new Beacon is required.";
            if (AbortRun(runState, currentTime, reason) == false)
                return false;

            Logger.Info($"Mythic Rift run {runState.Config.RunId} aborted after all participants left the Rift region.");
            return true;
        }

        private bool HasAnyPlayerInRunRegion(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return false;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return false;

            foreach (Player _ in new PlayerIterator(region))
                return true;

            return false;
        }

        private void RequestRunRegionShutdownWhenVacant(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null || region.ShutdownRequested)
                return;

            region.RequestShutdown();
            Logger.Info($"Mythic Rift run {runState.Config.RunId} requested shutdown for region {region.PrototypeName} (0x{region.Id:X}).");
        }

        private bool TrySpawnReturnPortal(MythicRiftRunState runState)
        {
            if (runState == null || runState.RegionId == 0)
                return false;

            if (runState.ExitPortalEntityId != 0 && Game.EntityManager.GetEntity<Transition>(runState.ExitPortalEntityId) != null)
                return true;

            if (TryResolveDangerRoomHubStartTarget(out PrototypeId dangerRoomHubStartTarget) == false)
                return false;

            PrototypeId exitPortalProtoRef = GameDatabase.GetPrototypeRefByName(RiftExitPortalPrototypeName);
            if (exitPortalProtoRef == PrototypeId.Invalid)
                return Logger.WarnReturn(false, $"TrySpawnReturnPortal(): Failed to resolve {RiftExitPortalPrototypeName}");

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return false;

            if (TryGetReturnPortalSpawnLocation(runState, region, out Vector3 spawnPosition, out Orientation spawnOrientation, out Cell spawnCell) == false)
                return false;

            using EntitySettings settings = ObjectPoolManager.Instance.Get<EntitySettings>();
            settings.EntityRef = exitPortalProtoRef;
            settings.RegionId = region.Id;
            settings.Position = spawnPosition;
            settings.Orientation = spawnOrientation;
            settings.Cell = spawnCell;
            settings.Lifespan = CompletedRunRetention;
            settings.SourceEntityId = GetFirstRunAvatarId(region);

            using PropertyCollection settingsProperties = ObjectPoolManager.Instance.Get<PropertyCollection>();
            settingsProperties[PropertyEnum.Interactable] = (int)TriBool.True;
            settingsProperties[PropertyEnum.InteractableUsesLeft] = -1;
            settingsProperties[PropertyEnum.Visible] = true;
            settings.Properties = settingsProperties;

            Transition exitPortal = Game.EntityManager.CreateEntity(settings) as Transition;
            if (exitPortal == null)
                return Logger.WarnReturn(false, "TrySpawnReturnPortal(): Failed to create return portal entity.");

            if (exitPortal.ConfigureDirectTarget(dangerRoomHubStartTarget) == false)
            {
                exitPortal.Destroy();
                return false;
            }

            runState.AttachExitPortal(exitPortal.Id);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} spawned return portal {exitPortal.PrototypeName} (0x{exitPortal.Id:X}) to Danger Room hub.");
            return true;
        }

        public bool TryUseReturnPortal(Player player, Transition transition)
        {
            if (player == null || transition == null)
                return false;

            MythicRiftRunState runState = _activeRuns.Values.FirstOrDefault(run => run.ExitPortalEntityId == transition.Id);
            if (runState == null || runState.Status != MythicRiftRunStatus.Success)
                return false;

            if (TryResolveDangerRoomHubStartTarget(out PrototypeId dangerRoomHubStartTarget) == false)
                return false;

            using Teleporter teleporter = ObjectPoolManager.Instance.Get<Teleporter>();
            teleporter.Initialize(player, TeleportContextEnum.TeleportContext_Debug);
            teleporter.DifficultyTierRef = GameDatabase.GlobalsPrototype.DifficultyTierDefault;
            bool teleported = teleporter.TeleportToTarget(dangerRoomHubStartTarget);
            if (teleported)
                Logger.Info($"Mythic Rift run {runState.Config.RunId} used return portal 0x{transition.Id:X} for playerDbId=0x{player.DatabaseUniqueId:X}.");
            else
                Logger.Warn($"Mythic Rift run {runState.Config.RunId} failed to use return portal 0x{transition.Id:X} for playerDbId=0x{player.DatabaseUniqueId:X}.");

            return teleported;
        }

        public bool TryBlockUnsafeRiftTransition(Player player, Transition transition)
        {
            if (player == null || transition == null)
                return false;

            MythicRiftRunState runState = FindCheckpointRunForTransition(player, transition);
            if (runState == null)
                return false;

            string message = runState.Status == MythicRiftRunStatus.Success
                ? "[Cosmic Rift] Use the Cosmic Rift exit portal to return to the Danger Room hub."
                : "[Cosmic Rift] This room exit is disabled during checkpoint Rifts. Defeat the boss, then use the Cosmic Rift exit portal.";
            Game.ChatManager.SendChatFromCustomSystem(player, message, showSender: false);
            Logger.Info($"Mythic Rift run {runState.Config.RunId} blocked native checkpoint transition 0x{transition.Id:X} ({transition.PrototypeName}) for playerDbId=0x{player.DatabaseUniqueId:X}.");
            return true;
        }

        private MythicRiftRunState FindCheckpointRunForTransition(Player player, Transition transition)
        {
            Region playerRegion = player?.GetRegion();
            Region transitionRegion = transition?.Region;
            if (playerRegion == null || transitionRegion == null || playerRegion.Id != transitionRegion.Id)
                return null;

            foreach (MythicRiftRunState runState in _activeRuns.Values)
            {
                if (runState.RegionId != transitionRegion.Id)
                    continue;

                if (runState.Config.Content.BossOnlyCheckpointEligible == false)
                    continue;

                if (runState.Status != MythicRiftRunStatus.Active && runState.Status != MythicRiftRunStatus.Success)
                    continue;

                if (runState.ExitPortalEntityId != 0 && transition.Id == runState.ExitPortalEntityId)
                    continue;

                return runState;
            }

            return null;
        }

        private static ulong GetFirstRunAvatarId(Region region)
        {
            if (region == null)
                return 0;

            foreach (Player player in new PlayerIterator(region))
            {
                Avatar avatar = player.CurrentAvatar;
                if (avatar != null && avatar.IsInWorld)
                    return avatar.Id;
            }

            return 0;
        }

        private bool TryGetReturnPortalSpawnLocation(MythicRiftRunState runState, Region region, out Vector3 position, out Orientation orientation, out Cell cell)
        {
            position = Vector3.Zero;
            orientation = Orientation.Zero;
            cell = null;

            if (region == null)
                return false;

            if (runState?.BossEntityId != 0)
            {
                WorldEntity bossEntity = Game.EntityManager.GetEntity<WorldEntity>(runState.BossEntityId);
                if (bossEntity?.IsInWorld == true && bossEntity.Region == region)
                {
                    position = bossEntity.RegionLocation.Position;
                    orientation = bossEntity.RegionLocation.Orientation;
                    cell = bossEntity.Cell;
                    return FinalizeReturnPortalSpawnLocation(region, ref position, ref cell);
                }
            }

            foreach (Player player in new PlayerIterator(region))
            {
                Avatar avatar = player?.CurrentAvatar;
                if (avatar?.IsInWorld != true || avatar.Region != region)
                    continue;

                position = avatar.RegionLocation.Position + avatar.Forward * 150f;
                orientation = avatar.RegionLocation.Orientation;
                cell = avatar.Cell;
                return FinalizeReturnPortalSpawnLocation(region, ref position, ref cell);
            }

            return false;
        }

        private static bool FinalizeReturnPortalSpawnLocation(Region region, ref Vector3 position, ref Cell cell)
        {
            position = RegionLocation.ProjectToFloor(region, position);
            cell ??= region.GetCellAtPosition(position);
            return cell != null;
        }

        private bool TryAbortRunForDisconnectedParticipants(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.IsInProgress == false || runState.ParticipantCount == 0)
                return false;

            TimeSpan timeSinceLastParticipantSeen = currentTime - runState.LastParticipantOnlineAt;
            if (timeSinceLastParticipantSeen < ParticipantDisconnectAbortGracePeriod)
                return false;

            return AbortRun(
                runState,
                currentTime,
                "All participants disconnected. The Rift has closed.");
        }

        private bool TryAbortStalePendingRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Pending || runState.RegionId != 0)
                return false;

            TimeSpan pendingDuration = currentTime - runState.RegisteredAt;
            if (pendingDuration < PendingRunBindGracePeriod)
                return false;

            return AbortRun(
                runState,
                currentTime,
                "The Rift could not be opened correctly. Please try again with a new Beacon.");
        }

        private void NotifyRunStarted(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            string waveText = runState.Config.UseThirtyWaveMode
                ? $" | Wave {runState.Config.WaveNumber}/30 | Bosses: {runState.Config.RequiredBossKillCount}"
                : string.Empty;
            string message = runState.Config.Content.BossOnlyCheckpointEligible
                ? $"[Cosmic Rift] Checkpoint Rift started: {runState.Config.Content.DisplayName} | Level {runState.Config.RiftLevel}{waveText} | Timer: {FormatDuration(runState.Config.TimeLimit)}. Defeat the empowered boss wave to unlock the next tier."
                : $"[Cosmic Rift] Rift started: {runState.Config.Content.DisplayName} | Level {runState.Config.RiftLevel}{waveText} | Timer: {FormatDuration(runState.Config.TimeLimit)}. Defeat {runState.Config.KillQuota} enemies to summon the Rift boss wave.";
            NotifyRunPlayers(runState, message);
        }

        private void NotifyBossUnlocked(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            int bossCount = Math.Max(runState.Config.RequiredBossKillCount, 1);
            string bossLabel = bossCount == 1 ? "boss" : $"{bossCount} bosses";
            string message = runState.Config.Content.BossOnlyCheckpointEligible
                ? $"[Cosmic Rift] Checkpoint {bossLabel} summoned: {ResolveBossDisplayName(runState.Config)}. Defeat the full wave before the timer expires."
                : $"[Cosmic Rift] Enemy quota complete. Final {bossLabel} summoned: {ResolveBossDisplayName(runState.Config)}. Defeat the full wave before the timer expires.";
            NotifyRunPlayers(runState, message);
        }

        private void TryNotifyKillProgress(MythicRiftRunState runState)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.BossUnlocked)
                return;

            int requiredCount = Math.Max(runState.Config.KillQuota, 1);
            int progressPercent = (int)Math.Floor((double)runState.CurrentKillCount * 100d / requiredCount);
            foreach (int milestonePercent in KillProgressMilestonePercents)
            {
                if (progressPercent < milestonePercent)
                    continue;

                if (runState.MarkKillProgressMilestoneSent(milestonePercent) == false)
                    continue;

                int displayedKillCount = Math.Min(runState.CurrentKillCount, requiredCount);
                NotifyRunPlayers(runState, $"[Cosmic Rift] Progress: {displayedKillCount}/{requiredCount} enemy progress.");
            }
        }

        private void NotifyRunCompleted(MythicRiftRunState runState, bool success, string statusMessage)
        {
            if (runState == null)
                return;

            string successPrefix = runState.Config.Content.BossOnlyCheckpointEligible
                ? "Checkpoint cleared"
                : "Rift complete";

            string message = success
                ? $"[Cosmic Rift] {successPrefix}! {runState.BossKillCount} Rift boss(es) defeated in {runState.Config.Content.DisplayName}. Level {runState.Config.RiftLevel} cleared. {statusMessage}"
                : $"[Cosmic Rift] Rift closed: {runState.Config.Content.DisplayName} | Level {runState.Config.RiftLevel}. {statusMessage}";
            NotifyRunPlayers(runState, message);
        }

        private void TrySendRiftClearedBanner(MythicRiftRunState runState)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Success)
                return;

            LocaleStringId bannerText = GetRiftClearedBannerLocaleStringId(runState.Config.RunId);
            foreach (Player player in GetRunPlayers(runState))
            {
                player.SendBannerMessage(
                    bannerText,
                    TextStylePrototype.BannerMessageLarge,
                    RiftClearedBannerTimeToLiveMS,
                    BannerMessageStyle.FlyIn,
                    doNotQueue: true,
                    showImmediately: true);
            }
        }

        private static LocaleStringId GetRiftClearedBannerLocaleStringId(ulong runId)
        {
            ulong offset = runId % (ulong)RiftClearedBannerLocaleStringCount;
            return (LocaleStringId)(RiftClearedBannerLocaleStringBase + offset + 1UL);
        }

        private void TryNotifyRunTimeWarnings(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active || runState.ExpiresAt.HasValue == false)
                return;

            TimeSpan remaining = runState.GetTimeRemaining(currentTime);
            int remainingSeconds = (int)Math.Ceiling(remaining.TotalSeconds);
            foreach (int thresholdSeconds in TimeWarningThresholdSeconds)
            {
                if (remainingSeconds > thresholdSeconds)
                    continue;

                if (runState.MarkTimeWarningSent(thresholdSeconds) == false)
                    continue;

                NotifyRunPlayers(runState, $"[Cosmic Rift] Time remaining: {FormatDuration(TimeSpan.FromSeconds(thresholdSeconds))}.");
            }
        }

        private static string BuildJoinMessage(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null)
                return "[Cosmic Rift] You joined an active Rift.";

            string remaining = FormatDuration(runState.GetTimeRemaining(currentTime));
            if (runState.Config.Content.BossOnlyCheckpointEligible)
                return $"[Cosmic Rift] You joined a checkpoint Rift. Final boss: {ResolveBossDisplayName(runState.Config)}. Time remaining: {remaining}.";

            if (runState.BossUnlocked)
                return $"[Cosmic Rift] You joined an active Rift. Final boss: {ResolveBossDisplayName(runState.Config)}. Time remaining: {remaining}.";

            int remainingKills = Math.Max(runState.Config.KillQuota - runState.CurrentKillCount, 0);
            return $"[Cosmic Rift] You joined an active Rift. Defeat {remainingKills} more enemies to summon the Rift boss. Time remaining: {remaining}.";
        }

        private void SendStartRiftTimer(MythicRiftRunState runState, Player player = null)
        {
            if (runState == null || runState.Status != MythicRiftRunStatus.Active)
                return;

            TimeSpan remaining = runState.GetTimeRemaining(Game.CurrentTime);
            if (remaining <= TimeSpan.Zero)
                return;

            try
            {
                NetMessageStartPvPTimer message = NetMessageStartPvPTimer.CreateBuilder()
                    .SetMetaGameId(runState.Config.RunId)
                    .SetStartTime((uint)Math.Min(remaining.TotalMilliseconds, uint.MaxValue))
                    .SetEndTime(0)
                    .SetLowTimeWarning((uint)TimeSpan.FromMinutes(2).TotalMilliseconds)
                    .SetCriticalTimeWarning((uint)TimeSpan.FromMinutes(1).TotalMilliseconds)
                    .SetLabelOverrideTextId(0UL)
                    .Build();

                if (player != null)
                {
                    player.SendMessage(message);
                    return;
                }

                SendMessageToRunPlayers(runState, message);
            }
            catch (Exception e)
            {
                Logger.Warn($"Mythic Rift run {runState.Config.RunId} failed to send optional timer UI packet: {e.Message}");
            }
        }

        private void SendStopRiftTimer(MythicRiftRunState runState)
        {
            if (runState == null)
                return;

            NetMessageStopPvPTimer message = NetMessageStopPvPTimer.CreateBuilder()
                .SetMetaGameId(runState.Config.RunId)
                .Build();

            SendMessageToRunPlayers(runState, message);
        }

        private void NotifyRunPlayers(MythicRiftRunState runState, string message)
        {
            if (runState == null || string.IsNullOrWhiteSpace(message))
                return;

            foreach (Player player in GetRunPlayers(runState))
                Game.ChatManager.SendChatFromCustomSystem(player, message, showSender: false);
        }

        private void SendMessageToRunPlayers(MythicRiftRunState runState, Google.ProtocolBuffers.IMessage message)
        {
            if (runState == null || message == null)
                return;

            foreach (Player player in GetRunPlayers(runState))
                player.SendMessage(message);
        }

        private IEnumerable<Player> GetRunPlayers(MythicRiftRunState runState)
        {
            if (runState == null)
                yield break;

            HashSet<ulong> recipientDbIds = new(runState.ParticipantPlayerDbIds);
            if (runState.RegionId != 0)
            {
                Region region = Game.RegionManager.GetRegion(runState.RegionId);
                if (region != null)
                {
                    foreach (Player regionPlayer in new PlayerIterator(region))
                    {
                        if (runState.HasParticipantLeftEarly(regionPlayer.DatabaseUniqueId) == false)
                            recipientDbIds.Add(regionPlayer.DatabaseUniqueId);
                    }
                }
            }

            foreach (ulong playerDbId in recipientDbIds)
            {
                Player player = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
                if (player == null)
                    continue;

                yield return player;
            }
        }

        private static string ResolveBossDisplayName(MythicRiftRunConfig config)
        {
            if (config?.BossWaveContent?.Count > 1)
            {
                return string.Join(", ", config.BossWaveContent.Select(content =>
                    TrimTerminalSuffix(content?.DisplayName ?? content?.Id ?? "Unknown Boss")));
            }

            if (config?.BossContent?.DisplayName == null)
                return config?.BossProtoRef.GetNameFormatted() ?? "Unknown Boss";

            return TrimTerminalSuffix(config.BossContent.DisplayName);
        }

        private static string TrimTerminalSuffix(string bossName)
        {
            if (string.IsNullOrWhiteSpace(bossName))
                return "Unknown Boss";

            const string terminalSuffix = " Terminal";
            if (bossName.EndsWith(terminalSuffix, StringComparison.OrdinalIgnoreCase))
                return bossName[..^terminalSuffix.Length];

            return bossName;
        }

        private static string FormatDuration(TimeSpan duration)
        {
            if (duration < TimeSpan.Zero)
                duration = TimeSpan.Zero;

            int totalSeconds = (int)Math.Ceiling(duration.TotalSeconds);
            int minutes = totalSeconds / 60;
            int seconds = totalSeconds % 60;

            if (minutes > 0 && seconds > 0)
                return $"{minutes} min {seconds} sec";

            if (minutes > 0)
                return $"{minutes} min";

            return $"{seconds} sec";
        }

        private static bool ShouldAutoRemoveRun(MythicRiftRunState runState, TimeSpan currentTime)
        {
            if (runState == null || runState.CompletedAt.HasValue == false)
                return false;

            return currentTime - runState.CompletedAt.Value >= CompletedRunRetention;
        }

        private bool ShouldRemoveCompletedRunBecauseRegionIsEmpty(MythicRiftRunState runState)
        {
            if (runState == null || runState.CompletedAt.HasValue == false || runState.RegionId == 0)
                return false;

            Region region = Game.RegionManager.GetRegion(runState.RegionId);
            if (region == null)
                return true;

            foreach (Player _ in new PlayerIterator(region))
                return false;

            return true;
        }

        private static bool TryResolveDangerRoomHubStartTarget(out PrototypeId startTargetRef)
        {
            startTargetRef = PrototypeId.Invalid;

            RegionPrototype dangerRoomHubRegion = ((PrototypeId)RegionPrototypeId.DangerRoomHubRegion).As<RegionPrototype>();
            if (dangerRoomHubRegion == null || dangerRoomHubRegion.StartTarget == PrototypeId.Invalid)
                return false;

            startTargetRef = dangerRoomHubRegion.StartTarget;
            return true;
        }

        private void RegisterDefaultContent()
        {
            foreach (MythicRiftContentDefinition definition in DefaultContentDefinitions)
                RegisterContent(definition);
        }

        private void RegisterContent(MythicRiftContentDefinition definition)
        {
            if (definition == null)
                return;

            MythicRiftContentEntry content = new()
            {
                Id = definition.Id,
                DisplayName = definition.DisplayName,
                DefaultKillQuota = definition.DefaultKillQuota,
                RegionProtoRef = ResolvePrototype(definition.RegionPrototypeName),
                StartTargetProtoRef = ResolveStartTarget(definition.RegionPrototypeName),
                MissionProtoRef = ResolvePrototype(definition.MissionPrototypeName),
                BossProtoRef = ResolvePrototype(definition.BossPrototypeName),
                BossLootTableProtoRef = ResolvePrototype(definition.BossLootTablePrototypeName),
                RandomMapEligible = definition.RandomMapEligible,
                RandomBossEligible = definition.RandomBossEligible,
                IsSpecialRandomMap = definition.IsSpecialRandomMap,
                UseOwnBossSourceWhenSelected = definition.UseOwnBossSourceWhenSelected,
                UseCustomPopulation = definition.UseCustomPopulation,
                BossOnlyCheckpointEligible = definition.BossOnlyCheckpointEligible,
                MinRandomRiftLevel = definition.MinRandomRiftLevel,
                MaxRandomRiftLevel = definition.MaxRandomRiftLevel,
                MaxPlayerCount = definition.MaxPlayerCount
            };

            if (content.IsValid == false)
            {
                Logger.Warn($"RegisterContent(): failed to resolve mythic rift content id={definition.Id}");
                return;
            }

            RegisterContent(content);
        }

        private static PrototypeId ResolvePrototype(string prototypeName)
        {
            if (string.IsNullOrWhiteSpace(prototypeName))
                return PrototypeId.Invalid;

            PrototypeId prototypeRef = GameDatabase.GetPrototypeRefByName(prototypeName);
            if (prototypeRef == PrototypeId.Invalid)
                Logger.Warn($"ResolvePrototype(): failed to resolve {prototypeName}");

            return prototypeRef;
        }

        private static PrototypeId ResolveStartTarget(string regionPrototypeName)
        {
            PrototypeId regionProtoRef = ResolvePrototype(regionPrototypeName);
            if (regionProtoRef == PrototypeId.Invalid)
                return PrototypeId.Invalid;

            RegionPrototype regionProto = regionProtoRef.As<RegionPrototype>();
            if (regionProto == null)
                return PrototypeId.Invalid;

            return regionProto.StartTarget;
        }

        private static int ResolveKillQuota(MythicRiftContentEntry content, int killQuota)
        {
            if (killQuota > 0)
                return killQuota;

            if (content != null && content.DefaultKillQuota > 0)
                return content.DefaultKillQuota;

            return 50;
        }

        private void SyncOnlinePlayerRiftLevel(ulong playerDbId, int unlockedLevel)
        {
            if (playerDbId == 0)
                return;

            Player onlinePlayer = Game.EntityManager.GetEntityByDbGuid<Player>(playerDbId);
            if (onlinePlayer == null)
                return;

            onlinePlayer.MythicRiftHighestUnlockedLevel = unlockedLevel;
        }

        private bool TryFindInProgressRunConflict(IEnumerable<ulong> playerDbIds, out MythicRiftRunState conflictingRun)
        {
            conflictingRun = null;
            if (playerDbIds == null)
                return false;

            foreach (ulong playerDbId in playerDbIds)
            {
                conflictingRun = GetInProgressRunForPlayer(playerDbId);
                if (conflictingRun != null)
                    return true;
            }

            return false;
        }

        private sealed record MythicRiftContentDefinition(
            string Id,
            string DisplayName,
            int DefaultKillQuota,
            string RegionPrototypeName,
            string MissionPrototypeName,
            string BossPrototypeName,
            string BossLootTablePrototypeName,
            bool RandomMapEligible = true,
            bool RandomBossEligible = true,
            bool IsSpecialRandomMap = false,
            bool UseOwnBossSourceWhenSelected = false,
            bool UseCustomPopulation = false,
            bool BossOnlyCheckpointEligible = false,
            int MinRandomRiftLevel = 1,
            int MaxRandomRiftLevel = 0,
            int MaxPlayerCount = 0);
    }
}

//#define DEBUG
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Facepunch;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;
using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using Oxide.Game.Rust;
using Rust;
using UnityEngine;
using System.Reflection;
using Oxide.Core.Libraries.Covalence;

/*
    1.2.0:
    Configuration rewritten to fix issues with it resetting after an update, being difficult to read, and to meet standards. Your config will convert automatically.
    Added announcement to server console on additions and removals after updating
    Added `Settings -> Check Boundaries On Teleport X Y Z (true)`
    Added `Settings -> TPR > Allow TPB (true)`
    Added `Settings -> Draw Sphere On Set Home (true)`
    Added `Settings -> Block Teleport (NoEscape) (false)`
    Added `Settings -> Block Teleport (ZoneManager) (false)`
    Added multiple null checks
    Added use of cached variables for true, false, up, down, zero
    Added command `/spm` as a tool for admins to report inaccurate monument extents @Tanki
    Fixed `/tp player x y z` Credits @Marcus101RR
    Fixed AutoGenBandit not generating
    Fixed AutoGenBandit position
    Fixed AutoGenOutpost not generating
    Fixed issues with interrupt monument not working correctly at various monuments
    Removed test code for TPP
    Removed some unused variables
	Rewrote various bits of outdated code
    Converted all commands to covalence
    Added command tnt [commandname] to toggle enabling and disabling of specific commands. Usable from server console, in-game console and in-game chat. Requires admin.
    Valid command name: tp, home, sethome, listhomes, tpn, tpl, tpsave, tpremove, tpb, removehome, radiushome, deletehome, tphome, homehomes, tpt, tpr, tpa, wipehomes, tphelp, tpinfo, tpc, teleport.toplayer, teleport.topos, teleport.importhomes, spm, outpost, bandit

    1.1.7:
    Disabled debug, oops

    1.1.6:
    TPT settings are now correctly added to the config
    Fixed InterruptTPOnMonument not working at some monuments

    Added at the request of @Orange:
    - bool API_HavePendingRequest(BasePlayer player)
    - bool API_HaveAvailableHomes(BasePlayer player)
    - List<string> API_GetHomes(BasePlayer player)
    - Added console commands: tpr, home, tpa, tpc, sethome, removehome

    1.1.5:
    Disabled upgrade support for versions 1.0.89 and below until fixed.
    Added config setting TPT > UseClans (default: true) - If set false, users cannot TPT to clan mates even with nteleportation.tpt permission
    Added config setting TPT > UseFriends (default: true) - If set false, users cannot TPT to friends even with nteleportation.tpt permission
    Added config setting TPT > UseTeams (default: true) - If set false, users cannot TPT to team mates even with nteleportation.tpt permission
    Added command /tpt clan|team|friend FOR INDIVIDUAL PLAYERS to toggle allowing/blocking of players trying to instantly teleport to them when using /tpt name
    Added new language messages for these related TPT features (English only)
    Fix for desync of players after teleport when close to teleport point or offline. Credits @Death

    1.1.4:
    Fixed exploit where players could loot sleepers in safe zones after using teleport
    Fix for TPT command allowing teleports to all, admins are still allowed

    1.1.3:
    Rewrote and fixed /tpt command
    Allowed admins to teleport to self for testing purposes

    1.1.2:
    Fixed players being unable to move after teleport
    Replaced specific trigger removal to remove all triggers instead

    1.1.1: 
    Fixed being dragged under map after teleport while standing on a garbage pile plank!! Ty @Ryrzy for video to reproduce
    Fixed buildings not loading immediately after teleport. Credits @ctv
    Fixed not being able to sethome on foundation @rustkoyak
    Smoother transition on teleport
    Added command /TPT <player name> - teleport to a friend, clan or team member. Requires 'nteleportation.tpt' permission. Contribution & Credits @Mal.Speedie
    Clarification that homes/town/outpost/bandit may be invalid on map wipe when `WipeOnUpgradeOrChange` is `false` in config
    Added null check in OnPlayerSleepEnded
    Removed position check in OnPlayerSleepEnded as its obsolete now

    1.1.0:
    Fix for players from being dragged under the map after teleporting, by forcing them to their teleport position when they wake up if water is below them.

    1.0.9:
    Fix for players teleporting under the map when teleporting to a base
    Fix for players disconnecting with RigidBody.get_velocity() NullReferenceException
    Player ends looting before teleport, instead of when put to sleep.

    1.0.88:
    Crafting is no longer cancelled on teleport

    1.0.87:
    Fixed teleport from mounted entities (cargoship, boats, etc), garbage heap barrels, etc
    Added hook OnTeleportRequested(BasePlayer player, BasePlayer target) - no return behavior
    Commands /bandit and /outpost will not be registered if disabled or CompoundTeleport is loaded @Matt
    Added setting InterruptTPOnOilrig (false) @rustkoyak
    Bandit and Outpost locations will be properly reset on wipe
*/

namespace Oxide.Plugins
{
    [Info("NTeleportation", "Author Nogrod, Maintainer nivex", "1.2.0")]
    class NTeleportation : RustPlugin
    {
        private const bool True = true;
        private const bool False = false;
        private Vector3 Zero = default(Vector3);
        private static readonly Vector3 Up = Vector3.up;
        private static readonly Vector3 Down = Vector3.down;
        private const string NewLine = "\n";
        private const string ConfigDefaultPermVip = "nteleportation.vip";
        private const string PermHome = "nteleportation.home";
        private const string PermTpR = "nteleportation.tpr";
        private const string PermTpT = "nteleportation.tpt";
        private const string PermDeleteHome = "nteleportation.deletehome";
        private const string PermHomeHomes = "nteleportation.homehomes";
        private const string PermImportHomes = "nteleportation.importhomes";
        private const string PermRadiusHome = "nteleportation.radiushome";
        private const string PermTp = "nteleportation.tp";
        private const string PermTpB = "nteleportation.tpb";
        private const string PermTpConsole = "nteleportation.tpconsole";
        private const string PermTpHome = "nteleportation.tphome";
        private const string PermTpTown = "nteleportation.tptown";
        private const string PermTpOutpost = "nteleportation.tpoutpost";
        private const string PermTpBandit = "nteleportation.tpbandit";
        private const string PermTpN = "nteleportation.tpn";
        private const string PermTpL = "nteleportation.tpl";
        private const string PermTpRemove = "nteleportation.tpremove";
        private const string PermTpSave = "nteleportation.tpsave";
        private const string PermWipeHomes = "nteleportation.wipehomes";
        private const string PermCraftHome = "nteleportation.crafthome";
        private const string PermCraftTown = "nteleportation.crafttown";
        private const string PermCraftOutpost = "nteleportation.craftoutpost";
        private const string PermCraftBandit = "nteleportation.craftbandit";
        private const string PermCraftTpR = "nteleportation.crafttpr";
        private DynamicConfigFile dataFile;
        private DynamicConfigFile dataAdmin;
        private DynamicConfigFile dataHome;
        private DynamicConfigFile dataTPR;
        private DynamicConfigFile dataTPT;
        private DynamicConfigFile dataTown;
        private DynamicConfigFile dataOutpost;
        private DynamicConfigFile dataBandit;
        private Dictionary<ulong, AdminData> Admin;
        private Dictionary<ulong, HomeData> Home;
        private Dictionary<ulong, TeleportData> TPR;
        private Dictionary<ulong, List<string>> TPT;
        private Dictionary<ulong, TeleportData> Town;
        private Dictionary<ulong, TeleportData> Outpost;
        private Dictionary<ulong, TeleportData> Bandit;
        private bool changedAdmin;
        private bool changedHome;
        private bool changedTPR;
        private bool changedTPT;
        private bool changedTown;
        private bool changedOutpost;
        private bool changedBandit;
        private float boundary;
        private readonly int triggerLayer = LayerMask.GetMask("Trigger");
        private readonly int groundLayer = LayerMask.GetMask("Terrain", "World");
        private readonly int buildingLayer = LayerMask.GetMask("Terrain", "World", "Construction", "Deployed");
        private readonly int blockLayer = LayerMask.GetMask("Construction");
        private readonly Dictionary<ulong, TeleportTimer> TeleportTimers = new Dictionary<ulong, TeleportTimer>();
        private readonly Dictionary<ulong, Timer> PendingRequests = new Dictionary<ulong, Timer>();
        private readonly Dictionary<ulong, BasePlayer> PlayersRequests = new Dictionary<ulong, BasePlayer>();
        private readonly Dictionary<int, string> ReverseBlockedItems = new Dictionary<int, string>();
        private readonly Dictionary<ulong, Vector3> teleporting = new Dictionary<ulong, Vector3>();
        private SortedDictionary<string, Vector3> caves = new SortedDictionary<string, Vector3>();
        private SortedDictionary<string, MonInfo> monuments = new SortedDictionary<string, MonInfo>();

        [PluginReference]
        private Plugin Clans, Economics, ServerRewards, Friends, CompoundTeleport, ZoneManager, NoEscape;

        class MonInfo
        {
            public Vector3 Position;
            public float Radius;
        }

        #region Configuration

        private static Configuration _config;

        private class Configuration
        {
            public class InterruptSettings
            {
                [JsonProperty(PropertyName = "Above Water")]
                public bool AboveWater { get; set; }

                [JsonProperty(PropertyName = "Balloon")]
                public bool Balloon { get; set; }

                [JsonProperty(PropertyName = "Cargo Ship")]
                public bool Cargo { get; set; } = True;

                [JsonProperty(PropertyName = "Cold")]
                public bool Cold { get; set; }

                [JsonProperty(PropertyName = "Excavator")]
                public bool Excavator { get; set; }

                [JsonProperty(PropertyName = "Hot")]
                public bool Hot { get; set; }

                [JsonProperty(PropertyName = "Hostile")]
                public bool Hostile { get; set; }

                [JsonProperty(PropertyName = "Hurt")]
                public bool Hurt { get; set; }

                [JsonProperty(PropertyName = "Lift")]
                public bool Lift { get; set; }

                [JsonProperty(PropertyName = "Monument")]
                public bool Monument { get; set; }

                [JsonProperty(PropertyName = "Mounted")]
                public bool Mounted { get; set; }

                [JsonProperty(PropertyName = "Oil Rig")]
                public bool Oilrig { get; set; }

                [JsonProperty(PropertyName = "Safe Zone")]
                public bool Safe { get; set; }

                [JsonProperty(PropertyName = "Swimming")]
                public bool Swimming { get; set; }
            }

            public class PluginSettings
            {
                [JsonProperty(PropertyName = "Interrupt TP")]
                public InterruptSettings Interrupt { get; set; }

                [JsonProperty(PropertyName = "Block Teleport (NoEscape)")]
                public bool BlockNoEscape { get; set; }

                [JsonProperty(PropertyName = "Block Teleport (ZoneManager)")]
                public bool BlockZoneFlag { get; set; }

                [JsonProperty(PropertyName = "Chat Name")]
                public string ChatName { get; set; }

                [JsonProperty(PropertyName = "Check Boundaries On Teleport X Y Z")]
                public bool CheckBoundaries { get; set; }

                [JsonProperty(PropertyName = "Draw Sphere On Set Home")]
                public bool DrawHomeSphere { get; set; }

                [JsonProperty(PropertyName = "Homes Enabled")]
                public bool HomesEnabled { get; set; }

                [JsonProperty(PropertyName = "TPR Enabled")]
                public bool TPREnabled { get; set; }

                [JsonProperty(PropertyName = "Town Enabled")]
                public bool TownEnabled { get; set; }

                [JsonProperty(PropertyName = "Outpost Enabled")]
                public bool OutpostEnabled { get; set; }

                [JsonProperty(PropertyName = "Bandit Enabled")]
                public bool BanditEnabled { get; set; }

                [JsonProperty(PropertyName = "Strict Foundation Check")]
                public bool StrictFoundationCheck { get; set; }

                [JsonProperty(PropertyName = "Cave Distance Small")]
                public float CaveDistanceSmall { get; set; }

                [JsonProperty(PropertyName = "Cave Distance Medium")]
                public float CaveDistanceMedium { get; set; }

                [JsonProperty(PropertyName = "Cave Distance Large")]
                public float CaveDistanceLarge { get; set; }

                [JsonProperty(PropertyName = "Default Monument Size")]
                public float DefaultMonumentSize { get; set; }

                [JsonProperty(PropertyName = "Minimum Temp")]
                public float MinimumTemp { get; set; }

                [JsonProperty(PropertyName = "Maximum Temp")]
                public float MaximumTemp { get; set; }

                [JsonProperty(PropertyName = "Blocked Items")]
                public Dictionary<string, string> BlockedItems { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

                [JsonProperty(PropertyName = "Bypass CMD")]
                public string BypassCMD { get; set; }

                [JsonProperty(PropertyName = "Use Economics")]
                public bool UseEconomics { get; set; }

                [JsonProperty(PropertyName = "Use Server Rewards")]
                public bool UseServerRewards { get; set; }

                [JsonProperty(PropertyName = "Wipe On Upgrade Or Change")]
                public bool WipeOnUpgradeOrChange { get; set; }

                [JsonProperty(PropertyName = "Auto Generate Outpost Location")]
                public bool AutoGenOutpost { get; set; }

                [JsonProperty(PropertyName = "Auto Generate Bandit Location")]
                public bool AutoGenBandit { get; set; }
            }

            public class AdminSettings
            {
                [JsonProperty(PropertyName = "Announce Teleport To Target")]
                public bool AnnounceTeleportToTarget { get; set; }

                [JsonProperty(PropertyName = "Usable By Admins")]
                public bool UseableByAdmins { get; set; }

                [JsonProperty(PropertyName = "Usable By Moderators")]
                public bool UseableByModerators { get; set; }

                [JsonProperty(PropertyName = "Location Radius")]
                public int LocationRadius { get; set; }

                [JsonProperty(PropertyName = "Teleport Near Default Distance")]
                public int TeleportNearDefaultDistance { get; set; }
            }

            public class HomesSettings
            {
                [JsonProperty(PropertyName = "Homes Limit")]
                public int HomesLimit { get; set; }

                [JsonProperty(PropertyName = "VIP Homes Limits")]
                public Dictionary<string, int> VIPHomesLimits { get; set; }

                [JsonProperty(PropertyName = "Cooldown")]
                public int Cooldown { get; set; }

                [JsonProperty(PropertyName = "Countdown")]
                public int Countdown { get; set; }

                [JsonProperty(PropertyName = "Daily Limit")]
                public int DailyLimit { get; set; }

                [JsonProperty(PropertyName = "VIP Daily Limits")]
                public Dictionary<string, int> VIPDailyLimits { get; set; }

                [JsonProperty(PropertyName = "VIP Cooldowns")]
                public Dictionary<string, int> VIPCooldowns { get; set; }

                [JsonProperty(PropertyName = "VIP Countdowns")]
                public Dictionary<string, int> VIPCountdowns { get; set; }

                [JsonProperty(PropertyName = "Location Radius")]
                public int LocationRadius { get; set; }

                [JsonProperty(PropertyName = "Force On Top Of Foundation")]
                public bool ForceOnTopOfFoundation { get; set; }

                [JsonProperty(PropertyName = "Check Foundation For Owner")]
                public bool CheckFoundationForOwner { get; set; }

                [JsonProperty(PropertyName = "Use Friends")]
                public bool UseFriends { get; set; }

                [JsonProperty(PropertyName = "Use Clans")]
                public bool UseClans { get; set; }

                [JsonProperty(PropertyName = "Use Teams")]
                public bool UseTeams { get; set; }

                [JsonProperty(PropertyName = "Usable Out Of Building Blocked")]
                public bool UsableOutOfBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Usable Into Building Blocked")]
                public bool UsableIntoBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Allow Cupboard Owner When Building Blocked")]
                public bool CupOwnerAllowOnBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Allow Iceberg")]
                public bool AllowIceberg { get; set; }

                [JsonProperty(PropertyName = "Allow Cave")]
                public bool AllowCave { get; set; }

                [JsonProperty(PropertyName = "Allow Crafting")]
                public bool AllowCraft { get; set; }

                [JsonProperty(PropertyName = "Allow Above Foundation")]
                public bool AllowAboveFoundation { get; set; }

                [JsonProperty(PropertyName = "Check If Home Is Valid On Listhomes")]
                public bool CheckValidOnList { get; set; }

                [JsonProperty(PropertyName = "Pay")]
                public int Pay { get; set; }

                [JsonProperty(PropertyName = "Bypass")]
                public int Bypass { get; set; }
            }

            public class TPTSettings
            {
                [JsonProperty(PropertyName = "Use Friends")]
                public bool UseFriends { get; set; }

                [JsonProperty(PropertyName = "Use Clans")]
                public bool UseClans { get; set; }

                [JsonProperty(PropertyName = "Use Teams")]
                public bool UseTeams { get; set; }
            }

            public class TPRSettings
            {
                [JsonProperty(PropertyName = "Allow TPB")]
                public bool AllowTPB { get; set; }

                [JsonProperty(PropertyName = "Cooldown")]
                public int Cooldown { get; set; }

                [JsonProperty(PropertyName = "Countdown")]
                public int Countdown { get; set; }

                [JsonProperty(PropertyName = "Daily Limit")]
                public int DailyLimit { get; set; }

                [JsonProperty(PropertyName = "VIP Daily Limits")]
                public Dictionary<string, int> VIPDailyLimits { get; set; }

                [JsonProperty(PropertyName = "VIP Cooldowns")]
                public Dictionary<string, int> VIPCooldowns { get; set; }

                [JsonProperty(PropertyName = "VIP Countdowns")]
                public Dictionary<string, int> VIPCountdowns { get; set; }

                [JsonProperty(PropertyName = "Request Duration")]
                public int RequestDuration { get; set; }

                [JsonProperty(PropertyName = "Offset TPR Target")]
                public bool OffsetTPRTarget { get; set; }

                [JsonProperty(PropertyName = "Block TPA On Ceiling")]
                public bool BlockTPAOnCeiling { get; set; }

                [JsonProperty(PropertyName = "Usable Out Of Building Blocked")]
                public bool UsableOutOfBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Usable Into Building Blocked")]
                public bool UsableIntoBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Allow Cupboard Owner When Building Blocked")]
                public bool CupOwnerAllowOnBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Allow Crafting")]
                public bool AllowCraft { get; set; }

                [JsonProperty(PropertyName = "Pay")]
                public int Pay { get; set; }

                [JsonProperty(PropertyName = "Bypass")]
                public int Bypass { get; set; }
            }

            public class TownSettings
            {
                [JsonProperty(PropertyName = "Cooldown")]
                public int Cooldown { get; set; }

                [JsonProperty(PropertyName = "Countdown")]
                public int Countdown { get; set; }

                [JsonProperty(PropertyName = "Daily Limit")]
                public int DailyLimit { get; set; }

                [JsonProperty(PropertyName = "VIP Daily Limits")]
                public Dictionary<string, int> VIPDailyLimits { get; set; }

                [JsonProperty(PropertyName = "VIP Cooldowns")]
                public Dictionary<string, int> VIPCooldowns { get; set; }

                [JsonProperty(PropertyName = "VIP Countdowns")]
                public Dictionary<string, int> VIPCountdowns { get; set; }

                [JsonProperty(PropertyName = "Location")]
                public Vector3 Location { get; set; }

                [JsonProperty(PropertyName = "Usable Out Of Building Blocked")]
                public bool UsableOutOfBuildingBlocked { get; set; }

                [JsonProperty(PropertyName = "Allow Crafting")]
                public bool AllowCraft { get; set; }

                [JsonProperty(PropertyName = "Pay")]
                public int Pay { get; set; }

                [JsonProperty(PropertyName = "Bypass")]
                public int Bypass { get; set; }
            }

            [JsonProperty(PropertyName = "Settings")]
            public PluginSettings Settings = new PluginSettings
            {
                Interrupt = new InterruptSettings()
                {
                    AboveWater = True,
                    Balloon = True,
                    Cargo = True,
                    Cold = False,
                    Excavator = False,
                    Hot = False,
                    Hostile = False,
                    Hurt = True,
                    Lift = True,
                    Monument = False,
                    Mounted = True,
                    Oilrig = False,
                    Safe = True,
                    Swimming = False
                },
                BlockNoEscape = False,
                BlockZoneFlag = False,
                ChatName = "<color=red>Teleportation</color>: ",
                CheckBoundaries = True,
                DrawHomeSphere = True,
                HomesEnabled = True,
                TPREnabled = True,
                TownEnabled = True,
                OutpostEnabled = True,
                BanditEnabled = True,
                MinimumTemp = 0f,
                MaximumTemp = 40f,
                StrictFoundationCheck = False,
                CaveDistanceSmall = 40f,
                CaveDistanceMedium = 60f,
                CaveDistanceLarge = 100f,
                DefaultMonumentSize = 50f,
                BypassCMD = "pay",
                UseEconomics = False,
                UseServerRewards = False,
                WipeOnUpgradeOrChange = False,
                AutoGenOutpost = False,
                AutoGenBandit = False
            };

            [JsonProperty(PropertyName = "Admin")]
            public AdminSettings Admin = new AdminSettings
            {
                AnnounceTeleportToTarget = False,
                UseableByAdmins = True,
                UseableByModerators = True,
                LocationRadius = 25,
                TeleportNearDefaultDistance = 30
            };

            [JsonProperty(PropertyName = "Home")]
            public HomesSettings Home = new HomesSettings
            {
                HomesLimit = 2,
                VIPHomesLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                Cooldown = 600,
                Countdown = 15,
                DailyLimit = 5,
                VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                LocationRadius = 25,
                ForceOnTopOfFoundation = True,
                CheckFoundationForOwner = True,
                UseFriends = True,
                UseClans = True,
                UseTeams = True,
                AllowAboveFoundation = True,
                CheckValidOnList = False,
                CupOwnerAllowOnBuildingBlocked = True
            };

            [JsonProperty(PropertyName = "TPT")]
            public TPTSettings TPT = new TPTSettings
            {
                UseClans = True,
                UseFriends = True,
                UseTeams = True,
            };

            [JsonProperty(PropertyName = "TPR")]
            public TPRSettings TPR = new TPRSettings
            {
                AllowTPB = True,
                Cooldown = 600,
                Countdown = 15,
                DailyLimit = 5,
                VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                RequestDuration = 30,
                BlockTPAOnCeiling = True,
                OffsetTPRTarget = True,
                CupOwnerAllowOnBuildingBlocked = True
            };

            [JsonProperty(PropertyName = "Town")]
            public TownSettings Town = new TownSettings
            {
                Cooldown = 600,
                Countdown = 15,
                DailyLimit = 5,
                VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } }
            };

            [JsonProperty(PropertyName = "Outpost")]
            public TownSettings Outpost = new TownSettings
            {
                Cooldown = 600,
                Countdown = 15,
                DailyLimit = 5,
                VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } }
            };

            [JsonProperty(PropertyName = "Bandit")]
            public TownSettings Bandit = new TownSettings
            {
                Cooldown = 600,
                Countdown = 15,
                DailyLimit = 5,
                VIPDailyLimits = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCooldowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } },
                VIPCountdowns = new Dictionary<string, int> { { ConfigDefaultPermVip, 5 } }
            };

            [JsonProperty(PropertyName = "Version")]
            public VersionNumber Version;

            public string ToJson() => JsonConvert.SerializeObject(this);

            public Dictionary<string, object> ToDictionary() => JsonConvert.DeserializeObject<Dictionary<string, object>>(ToJson());
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            try
            {
                dataFile = GetFile(nameof(NTeleportation));
                storedData = dataFile.ReadObject<StoredData>();
            }
            catch { }
            if (storedData == null) storedData = new StoredData();

            try
            {
                Config.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
                Config.Settings.Converters = new JsonConverter[] { new UnityVector3Converter() };
                if (!storedData.Converted_1_2_0)
                {
                    try
                    {
                        ConfigurationConverter();
                    }
                    catch { }
                }
                _config = Config.ReadObject<Configuration>();
                if (_config == null) throw new Exception();

                var oldKeys = Config.ToDictionary(x => x.Key, x => x.Value).Keys;
                var newKeys = _config.ToDictionary().Keys;

                foreach(string key in newKeys)
                {
                    if (!oldKeys.Contains(key))
                    {
                        PrintWarning("Config setting was added this update: {0}", key);
                    }
                }

                foreach (string key in oldKeys)
                {
                    if (!newKeys.Contains(key))
                    {
                        PrintWarning("Config setting was removed this update: {0}", key);
                    }
                }
            }
            catch
            {
                PrintError("Your configuration file contains an error. Using default configuration values.");
                LoadDefaultConfig();
            }

            if (_config.Settings.MaximumTemp < 1)
            {
                _config.Settings.MaximumTemp = 40f;
            }

            if (_config.Settings.DefaultMonumentSize < 1)
            {
                _config.Settings.DefaultMonumentSize = 50f;
            }

            if (_config.Settings.CaveDistanceSmall < 1)
            {
                _config.Settings.CaveDistanceSmall = 40f;
            }

            if (_config.Settings.CaveDistanceMedium < 1)
            {
                _config.Settings.CaveDistanceMedium = 60f;
            }

            if (_config.Settings.CaveDistanceLarge < 1)
            {
                _config.Settings.CaveDistanceLarge = 100f;
            }

            _config.Version = Version;

            SaveConfig();
        }

        protected override void SaveConfig() => Config.WriteObject(_config);

        protected override void LoadDefaultConfig() => _config = new Configuration();
        
        #endregion

        class StoredData
        {
            [JsonProperty("Last version config was converted")]
            public bool Converted_1_2_0 { get; set; }

            [JsonProperty("List of disabled commands")]
            public List<string> DisabledCommands = new List<string>();

            public StoredData() { }
        }

        StoredData storedData = new StoredData();

        class AdminData
        {
            [JsonProperty("pl")]
            public Vector3 PreviousLocation { get; set; }

            [JsonProperty("l")]
            public Dictionary<string, Vector3> Locations { get; set; } = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);
        }

        class HomeData
        {
            [JsonProperty("l")]
            public Dictionary<string, Vector3> Locations { get; set; } = new Dictionary<string, Vector3>(StringComparer.OrdinalIgnoreCase);

            [JsonProperty("t")]
            public TeleportData Teleports { get; set; } = new TeleportData();
        }

        class TeleportData
        {
            [JsonProperty("a")]
            public int Amount { get; set; }

            [JsonProperty("d")]
            public string Date { get; set; }

            [JsonProperty("t")]
            public int Timestamp { get; set; }
        }

        class TeleportTimer
        {
            public Timer Timer { get; set; }
            public BasePlayer OriginPlayer { get; set; }
            public BasePlayer TargetPlayer { get; set; }
        }

        private enum checkmode
        {
            home, tpr, tpa, town
        };

        protected override void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"AdminTP", "You teleported to {0}!"},
                {"AdminTPTarget", "{0} teleported to you!"},
                {"AdminTPPlayers", "You teleported {0} to {1}!"},
                {"AdminTPPlayer", "{0} teleported you to {1}!"},
                {"AdminTPPlayerTarget", "{0} teleported {1} to you!"},
                {"AdminTPCoordinates", "You teleported to {0}!"},
                {"AdminTPTargetCoordinates", "You teleported {0} to {1}!"},
                {"AdminTPOutOfBounds", "You tried to teleport to a set of coordinates outside the map boundaries!"},
                {"AdminTPBoundaries", "X and Z values need to be between -{0} and {0} while the Y value needs to be between -100 and 2000!"},
                {"AdminTPLocation", "You teleported to {0}!"},
                {"AdminTPLocationSave", "You have saved the current location!"},
                {"AdminTPLocationRemove", "You have removed the location {0}!"},
                {"AdminLocationList", "The following locations are available:"},
                {"AdminLocationListEmpty", "You haven't saved any locations!"},
                {"AdminTPBack", "You've teleported back to your previous location!"},
                {"AdminTPBackSave", "Your previous location has been saved, use /tpb to teleport back!"},
                {"AdminTPTargetCoordinatesTarget", "{0} teleported you to {1}!"},
                {"AdminTPConsoleTP", "You were teleported to {0}"},
                {"AdminTPConsoleTPPlayer", "You were teleported to {0}"},
                {"AdminTPConsoleTPPlayerTarget", "{0} was teleported to you!"},
                {"HomeTP", "You teleported to your home '{0}'!"},
                {"HomeAdminTP", "You teleported to {0}'s home '{1}'!"},
                {"HomeSave", "You have saved the current location as your home!"},
                {"HomeNoFoundation", "You can only use a home location on a foundation!"},
                {"HomeFoundationNotOwned", "You can't use home on someone else's house."},
                {"HomeFoundationUnderneathFoundation", "You can't use home on a foundation that is underneath another foundation."},
                {"HomeFoundationNotFriendsOwned", "You or a friend need to own the house to use home!"},
                {"HomeRemovedInvalid", "Your home '{0}' was removed because not on a foundation or not owned!"},
                {"HighWallCollision", "High Wall Collision!"},
                {"HomeRemovedInsideBlock", "Your home '{0}' was removed because inside a foundation!"},
                {"HomeRemove", "You have removed your home {0}!"},
                {"HomeDelete", "You have removed {0}'s home '{1}'!"},
                {"HomeList", "The following homes are available:"},
                {"HomeListEmpty", "You haven't saved any homes!"},
                {"HomeMaxLocations", "Unable to set your home here, you have reached the maximum of {0} homes!"},
                {"HomeQuota", "You have set {0} of the maximum {1} homes!"},
                {"HomeTPStarted", "Teleporting to your home {0} in {1} seconds!"},
                {"PayToHome", "Standard payment of {0} applies to all home teleports!"},
                {"PayToTown", "Standard payment of {0} applies to all town teleports!"},
                {"PayToTPR", "Standard payment of {0} applies to all tprs!"},
                {"HomeTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"HomeTPCooldownBypass", "Your teleport was currently on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"HomeTPCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"HomeTPCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"HomeTPCooldownBypassP2", "Type /home NAME {0}." },
                {"HomeTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"HomeTPAmount", "You have {0} home teleports left today!"},
                {"HomesListWiped", "You have wiped all the saved home locations!"},
                {"HomeTPBuildingBlocked", "You can't set your home if you are not allowed to build in this zone!"},
                {"HomeTPSwimming", "You can't set your home while swimming!"},
                {"HomeTPCrafting", "You can't set your home while crafting!"},
                {"Request", "You've requested a teleport to {0}!"},
                {"RequestTarget", "{0} requested to be teleported to you! Use '/tpa' to accept!"},
                {"PendingRequest", "You already have a request pending, cancel that request or wait until it gets accepted or times out!"},
                {"PendingRequestTarget", "The player you wish to teleport to already has a pending request, try again later!"},
                {"NoPendingRequest", "You have no pending teleport request!"},
                {"AcceptOnRoof", "You can't accept a teleport while you're on a ceiling, get to ground level!"},
                {"Accept", "{0} has accepted your teleport request! Teleporting in {1} seconds!"},
                {"AcceptTarget", "You've accepted the teleport request of {0}!"},
                {"NotAllowed", "You are not allowed to use this command!"},
                {"Success", "You teleported to {0}!"},
                {"SuccessTarget", "{0} teleported to you!"},
                {"Cancelled", "Your teleport request to {0} was cancelled!"},
                {"CancelledTarget", "{0} teleport request was cancelled!"},
                {"TPCancelled", "Your teleport was cancelled!"},
                {"TPCancelledTarget", "{0} cancelled teleport!"},
                {"TPYouCancelledTarget", "You cancelled {0} teleport!"},
                {"TimedOut", "{0} did not answer your request in time!"},
                {"TimedOutTarget", "You did not answer {0}'s teleport request in time!"},
                {"TargetDisconnected", "{0} has disconnected, your teleport was cancelled!"},
                {"TPRCooldown", "Your teleport requests are currently on cooldown. You'll have to wait {0} to send your next teleport request."},
                {"TPRCooldownBypass", "Your teleport request was on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"TPRCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"TPRCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"TPMoney", "{0} deducted from your account!"},
                {"TPNoMoney", "You do not have {0} in any account!"},
                {"TPRCooldownBypassP2", "Type /tpr {0}." },
                {"TPRCooldownBypassP2a", "Type /tpr NAME {0}." },
                {"TPRLimitReached", "You have reached the daily limit of {0} teleport requests today!"},
                {"TPRAmount", "You have {0} teleport requests left today!"},
                {"TPRTarget", "Your target is currently not available!"},
                {"TPDead", "You can't teleport while being dead!"},
                {"TPWounded", "You can't teleport while wounded!"},
                {"TPTooCold", "You're too cold to teleport!"},
                {"TPTooHot", "You're too hot to teleport!"},
                {"TPHostile", "Can't teleport to outpost or bandit when hostile!"},
                {"HostileTimer", "Teleport available in {0} minutes."},
                {"TPMounted", "You can't teleport while seated!"},
                {"TPBuildingBlocked", "You can't teleport while in a building blocked zone!"},
                {"TPAboveWater", "You can't teleport while above water!"},
                {"TPTargetBuildingBlocked", "You can't teleport in a building blocked zone!"},
                {"TPTargetInsideBlock", "You can't teleport into a foundation!"},
                {"TPSwimming", "You can't teleport while swimming!"},
                {"TPCargoShip", "You can't teleport from the cargo ship!"},
                {"TPOilRig", "You can't teleport from the oil rig!"},
                {"TPExcavator", "You can't teleport from the excavator!"},
                {"TPHotAirBalloon", "You can't teleport to or from a hot air balloon!"},
                {"TPLift", "You can't teleport while in an elevator or bucket lift!"},
                {"TPBucketLift", "You can't teleport while in a bucket lift!"},
                {"TPRegLift", "You can't teleport while in an elevator!"},
                {"TPSafeZone", "You can't teleport from a safezone!"},
                {"TPFlagZone", "You can't teleport from this zone!"},
                {"TPNoEscapeBlocked", "You can't teleport while blocked!"},
                {"TPCrafting", "You can't teleport while crafting!"},
                {"TPBlockedItem", "You can't teleport while carrying: {0}!"},
                {"TooCloseToMon", "You can't teleport so close to the {0}!"},
                {"TooCloseToCave", "You can't teleport so close to a cave!"},
                {"HomeTooCloseToCave", "You can't set home so close to a cave!"},
                {"TownTP", "You teleported to town!"},
                {"TownTPNotSet", "Town is currently not set!"},
                {"TownTPDisabled", "Town is currently not enabled!"},
                {"TownTPLocation", "You have set the town location to {0}!"},
                {"TownTPStarted", "Teleporting to town in {0} seconds!"},
                {"TownTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"TownTPCooldownBypass", "Your teleport request was on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"TownTPCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"TownTPCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"TownTPCooldownBypassP2", "Type /town {0}." },
                {"TownTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"TownTPAmount", "You have {0} town teleports left today!"},

                {"OutpostTP", "You teleported to the outpost!"},
                {"OutpostTPNotSet", "Outpost is currently not set!"},
                {"OutpostTPDisabled", "Outpost is currently not enabled!"},
                {"OutpostTPLocation", "You have set the outpost location to {0}!"},
                {"OutpostTPStarted", "Teleporting to the outpost in {0} seconds!"},
                {"OutpostTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"OutpostTPCooldownBypass", "Your teleport request was on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"OutpostTPCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"OutpostTPCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"OutpostTPCooldownBypassP2", "Type /outpost {0}." },
                {"OutpostTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"OutpostTPAmount", "You have {0} outpost teleports left today!"},

                {"BanditTP", "You teleported to bandit town!"},
                {"BanditTPNotSet", "Bandit is currently not set!"},
                {"BanditTPDisabled", "Bandit is currently not enabled!"},
                {"BanditTPLocation", "You have set the bandit town location to {0}!"},
                {"BanditTPStarted", "Teleporting to bandit town in {0} seconds!"},
                {"BanditTPCooldown", "Your teleport is currently on cooldown. You'll have to wait {0} for your next teleport."},
                {"BanditTPCooldownBypass", "Your teleport request was on cooldown. You chose to bypass that by paying {0} from your balance."},
                {"BanditTPCooldownBypassF", "Your teleport is currently on cooldown. You do not have sufficient funds - {0} - to bypass."},
                {"BanditTPCooldownBypassP", "You may choose to pay {0} to bypass this cooldown." },
                {"BanditTPCooldownBypassP2", "Type /bandit {0}." },
                {"BanditTPLimitReached", "You have reached the daily limit of {0} teleports today!"},
                {"BanditTPAmount", "You have {0} bandit town teleports left today!"},

                {"Interrupted", "Your teleport was interrupted!"},
                {"InterruptedTarget", "{0}'s teleport was interrupted!"},
                {"Unlimited", "Unlimited"},
                {
                    "TPInfoGeneral", string.Join(NewLine, new[]
                    {
                        "Please specify the module you want to view the info of.",
                        "The available modules are: ",
                    })
                },
                {
                    "TPHelpGeneral", string.Join(NewLine, new[]
                    {
                        "/tpinfo - Shows limits and cooldowns.",
                        "Please specify the module you want to view the help of.",
                        "The available modules are: ",
                    })
                },
                {
                    "TPHelpadmintp", string.Join(NewLine, new[]
                    {
                        "As an admin you have access to the following commands:",
                        "/tp \"targetplayer\" - Teleports yourself to the target player.",
                        "/tp \"player\" \"targetplayer\" - Teleports the player to the target player.",
                        "/tp x y z - Teleports you to the set of coordinates.",
                        "/tpl - Shows a list of saved locations.",
                        "/tpl \"location name\" - Teleports you to a saved location.",
                        "/tpsave \"location name\" - Saves your current position as the location name.",
                        "/tpremove \"location name\" - Removes the location from your saved list.",
                        "/tpb - Teleports you back to the place where you were before teleporting.",
                        "/home radius \"radius\" - Find all homes in radius.",
                        "/home delete \"player name|id\" \"home name\" - Remove a home from a player.",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player.",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "TPHelphome", string.Join(NewLine, new[]
                    {
                        "With the following commands you can set your home location to teleport back to:",
                        "/home add \"name\" - Saves your current position as the location name.",
                        "/home list - Shows you a list of all the locations you have saved.",
                        "/home remove \"name\" - Removes the location of your saved homes.",
                        "/home \"name\" - Teleports you to the home location."
                    })
                },
                {
                    "TPHelptpr", string.Join(NewLine, new[]
                    {
                        "With these commands you can request to be teleported to a player or accept someone else's request:",
                        "/tpr \"player name\" - Sends a teleport request to the player.",
                        "/tpa - Accepts an incoming teleport request.",
                        "/tpc - Cancel teleport or request."
                    })
                },
                {
                    "TPSettingsGeneral", string.Join(NewLine, new[]
                    {
                        "Please specify the module you want to view the settings of. ",
                        "The available modules are:",
                    })
                },
                {
                    "TPSettingshome", string.Join(NewLine, new[]
                    {
                        "Home System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}",
                        "Amount of saved Home locations: {2}"
                    })
                },
                {
                    "TPSettingstpr", string.Join(NewLine, new[]
                    {
                        "TPR System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {
                    "TPSettingstown", string.Join(NewLine, new[]
                    {
                        "Town System has the current settings enabled:",
                        "Time between teleports: {0}",
                        "Daily amount of teleports: {1}"
                    })
                },
                {"TPT_True", "enabled"},
                {"TPT_False", "disabled"},
                {"TPT_clan", "TPT clan has been {0}."},
                {"TPT_friend", "TPT friend has been {0}."},
                {"TPT_team", "TPT team has been {0}."},
                {"NotValidTPT", "Not valid, player is not"},
                {"NotValidTPTFriend", " a friend!"},
                {"NotValidTPTTeam", " on your team!"},
                {"NotValidTPTClan", " in your clan!"},
                {"TPTInfo", "`/tpt clan|team|friend` - toggle allowing/blocking of players trying to TPT to you via one of these options."},
                {"PlayerNotFound", "The specified player couldn't be found please try again!"},
                {"MultiplePlayers", "Found multiple players: {0}"},
                {"CantTeleportToSelf", "You can't teleport to yourself!"},
                {"CantTeleportPlayerToSelf", "You can't teleport a player to himself!"},
                {"TeleportPending", "You can't initiate another teleport while you have a teleport pending!"},
                {"TeleportPendingTarget", "You can't request a teleport to someone who's about to teleport!"},
                {"LocationExists", "A location with this name already exists at {0}!"},
                {"LocationExistsNearby", "A location with the name {0} already exists near this position!"},
                {"LocationNotFound", "Couldn't find a location with that name!"},
                {"NoPreviousLocationSaved", "No previous location saved!"},
                {"HomeExists", "You have already saved a home location by this name!"},
                {"HomeExistsNearby", "A home location with the name {0} already exists near this position!"},
                {"HomeNotFound", "Couldn't find your home with that name!"},
                {"InvalidCoordinates", "The coordinates you've entered are invalid!"},
                {"InvalidHelpModule", "Invalid module supplied!"},
                {"InvalidCharacter", "You have used an invalid character, please limit yourself to the letters a to z and numbers."},
                {
                    "SyntaxCommandTP", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tp command as follows:",
                        "/tp \"targetplayer\" - Teleports yourself to the target player.",
                        "/tp \"player\" \"targetplayer\" - Teleports the player to the target player.",
                        "/tp x y z - Teleports you to the set of coordinates.",
                        "/tp \"player\" x y z - Teleports the player to the set of coordinates."
                    })
                },
                {
                    "SyntaxCommandTPL", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpl command as follows:",
                        "/tpl - Shows a list of saved locations.",
                        "/tpl \"location name\" - Teleports you to a saved location."
                    })
                },
                {
                    "SyntaxCommandTPSave", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpsave command as follows:",
                        "/tpsave \"location name\" - Saves your current position as 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPRemove", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpremove command as follows:",
                        "/tpremove \"location name\" - Removes the location with the name 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPN", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpn command as follows:",
                        "/tpn \"targetplayer\" - Teleports yourself the default distance behind the target player.",
                        "/tpn \"targetplayer\" \"distance\" - Teleports you the specified distance behind the target player."
                    })
                },
                {
                    "SyntaxCommandSetHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home add command as follows:",
                        "/home add \"name\" - Saves the current location as your home with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandRemoveHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home remove command as follows:",
                        "/home remove \"name\" - Removes the home location with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandHome", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home command as follows:",
                        "/home \"name\" - Teleports yourself to your home with the name 'name'.",
                        "/home \"name\" pay - Teleports yourself to your home with the name 'name', avoiding cooldown by paying for it.",
                        "/home add \"name\" - Saves the current location as your home with the name 'name'.",
                        "/home list - Shows you a list of all your saved home locations.",
                        "/home remove \"name\" - Removes the home location with the name 'name'."
                    })
                },
                {
                    "SyntaxCommandHomeAdmin", string.Join(NewLine, new[]
                    {
                        "/home radius \"radius\" - Shows you a list of all homes in radius(10).",
                        "/home delete \"player name|id\" \"name\" - Removes the home location with the name 'name' from the player.",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player.",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "SyntaxCommandTown", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /town command as follows:",
                        "/town - Teleports yourself to town.",
                        "/town pay - Teleports yourself to town, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandTownAdmin", string.Join(NewLine, new[]
                    {
                        "/town set - Saves the current location as town.",
                    })
                },
                {
                    "SyntaxCommandOutpost", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /town command as follows:",
                        "/outpost - Teleports yourself to the Outpost.",
                        "/outpost pay - Teleports yourself to the Outpost, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandOutpostAdmin", string.Join(NewLine, new[]
                    {
                        "/outpost set - Saves the current location as Outpost.",
                    })
                },
                {
                    "SyntaxCommandBandit", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /bandit command as follows:",
                        "/bandit - Teleports yourself to the Bandit Town.",
                        "/bandit pay - Teleports yourself to the Bandit Town, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandBanditAdmin", string.Join(NewLine, new[]
                    {
                        "/bandit set - Saves the current location as Bandit Town.",
                    })
                },
                {
                    "SyntaxCommandHomeDelete", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home delete command as follows:",
                        "/home delete \"player name|id\" \"name\" - Removes the home location with the name 'name' from the player."
                    })
                },
                {
                    "SyntaxCommandHomeAdminTP", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home tp command as follows:",
                        "/home tp \"player name|id\" \"name\" - Teleports you to the home location with the name 'name' from the player."
                    })
                },
                {
                    "SyntaxCommandHomeHomes", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home homes command as follows:",
                        "/home homes \"player name|id\" - Shows you a list of all homes from the player."
                    })
                },
                {
                    "SyntaxCommandListHomes", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /home list command as follows:",
                        "/home list - Shows you a list of all your saved home locations."
                    })
                },
                {
                    "SyntaxCommandTPT", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpt command as follows:",
                        "/tpt \"player name\" - Teleports you to a team or clan member."
                    })
                },
                {
                    "SyntaxCommandTPR", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpr command as follows:",
                        "/tpr \"player name\" - Sends out a teleport request to 'player name'."
                    })
                },
                {
                    "SyntaxCommandTPA", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpa command as follows:",
                        "/tpa - Accepts an incoming teleport request."
                    })
                },
                {
                    "SyntaxCommandTPC", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the /tpc command as follows:",
                        "/tpc - Cancels an teleport request."
                    })
                },
                {
                    "SyntaxConsoleCommandToPos", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the teleport.topos console command as follows:",
                        " > teleport.topos \"player\" x y z"
                    })
                },
                {
                    "SyntaxConsoleCommandToPlayer", string.Join(NewLine, new[]
                    {
                        "A Syntax Error Occurred!",
                        "You can only use the teleport.toplayer console command as follows:",
                        " > teleport.toplayer \"player\" \"target player\""
                    })
                },
                {"LogTeleport", "{0} teleported to {1}."},
                {"LogTeleportPlayer", "{0} teleported {1} to {2}."},
                {"LogTeleportBack", "{0} teleported back to previous location."}
            }, this);

            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"AdminTP", "Ты телепортировался в {0}!"},
                {"AdminTPTarget", "{0} телепортировался к тебе!"},
                {"AdminTPPlayers", "Ты телепортировался {0} к {1}!"},
                {"AdminTPPlayer", "{0} телепортировался вам {1}!"},
                {"AdminTPPlayerTarget", "{0} телепортированный {1} для вас!"},
                {"AdminTPCoordinates", "Ты телепортировался в {0}!"},
                {"AdminTPTargetCoordinates", "Ты телепортировался {0} к {1}!"},
                {"AdminTPOutOfBounds", "Вы пытались телепортироваться в набор координат за пределами границ карты!"},
                {"AdminTPBoundaries", "X и Z ценности должны быть между -{0} и {0} а Y значение должно быть между -100 и 2000!"},
                {"AdminTPLocation", "Ты телепортировался в {0}!"},
                {"AdminTPLocationSave", "Вы сохранили текущее местоположение!"},
                {"AdminTPLocationRemove", "Вы удалили местоположение {0}!"},
                {"AdminLocationList", "Доступны следующие местоположения,"},
                {"AdminLocationListEmpty", "Вы не сохранили никаких мест!"},
                {"AdminTPBack", "Ты телепортировался обратно на прежнее место!"},
                {"AdminTPBackSave", "Ваше предыдущее местоположение было сохранено, используйте /tpb телепортироваться обратно!"},
                {"AdminTPTargetCoordinatesTarget", "{0} телепортировался вам {1}!"},
                {"AdminTPConsoleTP", "Тебя телепортировали в {0}"},
                {"AdminTPConsoleTPPlayer", "Тебя телепортировали в {0}"},
                {"AdminTPConsoleTPPlayerTarget", "{0} телепортировался к тебе!"},
                {"HomeTP", "Ты телепортировался в свой дом '{0}'!"},
                {"HomeAdminTP", "Ты телепортировался в {0}'s дом '{1}'!"},
                {"HomeSave", "Вы сохранили текущее местоположение в качестве своего дома!"},
                {"HomeNoFoundation", "Вы можете использовать только расположение дома на фундаменте!"},
                {"HomeFoundationNotOwned", "Ты не можешь использовать дом на чужом доме."},
                {"HomeFoundationUnderneathFoundation", "Вы не можете использовать home на фундаменте, который находится под другим фундаментом."},
                {"HomeFoundationNotFriendsOwned", "Вы или друг должны владеть домом, чтобы использовать дом!"},
                {"HomeRemovedInvalid", "Твой дом '{0}' убрали, потому что не на фундаменте или не в собственности!"},
                {"HomeRemovedInsideBlock", "Твой дом '{0}' убрали, потому что внутри фундамент!"},
                {"HomeRemove", "Вы удалили свой дом {0}!"},
                {"HomeDelete", "Вы удалили {0}'s дом '{1}'!"},
                {"HomeList", "Доступны следующие дома,"},
                {"HomeListEmpty", "Вы не спасли ни одного дома!"},
                {"HomeMaxLocations", "Не в состоянии установить свой дом здесь, вы достигли максимума {0} дома!"},
                {"HomeQuota", "Вы установили {0} максимума {1} дома!"},
                {"HomeTPStarted", "Телепортация в ваш дом {0} в {1} секунды!"},
                {"PayToHome", "Стандартная оплата {0} применяется ко всем домашним телепортам!"},
                {"PayToTown", "Стандартная оплата {0} применяется ко всем городским телепортам!"},
                {"PayToTPR", "Стандартная оплата {0} распространяться на всех tprs!"},
                {"HomeTPCooldown", "Ваш телепорт в настоящее время находится на перезарядке. Вам придется подождать {0} для следующего телепорта."},
                {"HomeTPCooldownBypass", "Ваш телепорт в настоящее время был на перезарядке. Вы решили обойти это, заплатив {0} с вашего баланса."},
                {"HomeTPCooldownBypassF", "Ваш телепорт в настоящее время находится на перезарядке. У вас недостаточно средств - {0} - обходить."},
                {"HomeTPCooldownBypassP", "Вы можете заплатить {0} чтобы обойти это охлаждение."},
                {"HomeTPCooldownBypassP2", "Тип /home NAME {0}."},
                {"HomeTPLimitReached", "Вы достигли дневного предела {0} телепорты сегодня!"},
                {"HomeTPAmount", "У вас есть {0} домашние телепорты ушли сегодня!"},
                {"HomesListWiped", "Вы стерли все сохраненные домашние местоположения!"},
                {"HomeTPBuildingBlocked", "Вы не можете установить свой дом, если вам не разрешено строить в этой зоне!"},
                {"HomeTPSwimming", "Вы не можете установить свой дом во время плавания!"},
                {"HomeTPCrafting", "Вы не можете установить свой дом во время крафта!"},
                {"Request", "Вы запросили телепорт на {0}!"},
                {"RequestTarget", "{0} просят телепортироваться к вам! Использовать '/tpa' принять!"},
                {"PendingRequest", "У вас уже есть запрос в ожидании, отменить этот запрос или ждать, пока он не будет принят или тайм-аут!"},
                {"PendingRequestTarget", "Игрок, которому вы хотите телепортироваться, уже имеет ожидающий запрос, повторите попытку позже!"},
                {"NoPendingRequest", "У вас нет запроса на телепортацию!"},
                {"AcceptOnRoof", "Вы не можете принять телепорт, пока вы на потолке, добраться до уровня земли!"},
                {"Accept", "{0} принял ваш запрос на телепортацию! Телепортироваться в {1} секунды!"},
                {"AcceptTarget", "Вы приняли запрос на телепортацию {0}!"},
                {"NotAllowed", "Вы не имеете права использовать эту команду!"},
                {"Success", "Ты телепортировался в {0}!"},
                {"SuccessTarget", "{0} телепортировался к тебе!"},
                {"Cancelled", "Ваш телепорт запрос {0} был отменен!"},
                {"CancelledTarget", "{0} запрос на телепортацию был отменен!"},
                {"TPCancelled", "Твой телепорт был отменен!"},
                {"TPCancelledTarget", "{0} отменен телепорт!"},
                {"TPYouCancelledTarget", "Вы отменили {0} телепортацию!"},
                {"TimedOut", "{0} не ответил на ваш запрос!"},
                {"TimedOutTarget", "Ты не ответил. {0}'s телепортируйте запрос вовремя!"},
                {"TargetDisconnected", "{0} отключился, телепорт отменен!"},
                {"TPRCooldown", "Ваши запросы на телепортацию в настоящее время находятся на перезарядке. Вам придется подождать {0} отправить следующий запрос на телепортацию."},
                {"TPRCooldownBypass", "Ваш запрос на телепортацию был на перезарядке. Вы решили обойти это, заплатив {0} с вашего баланса."},
                {"TPRCooldownBypassF", "Ваш телепорт в настоящее время находится на перезарядке. У вас недостаточно средств - {0} - обходить."},
                {"TPRCooldownBypassP", "Вы можете заплатить {0} чтобы обойти это охлаждение."},
                {"TPMoney", "{0} вычитается из вашего счета!"},
                {"TPNoMoney", "У вас нет {0} в любом аккаунте!"},
                {"TPRCooldownBypassP2", "Тип /tpr {0}."},
                {"TPRCooldownBypassP2a", "Тип /tpr NAME {0}."},
                {"TPRLimitReached", "Вы достигли дневного предела {0} телепорт просит сегодня!"},
                {"TPRAmount", "У вас есть {0} телепорт просит оставить сегодня!"},
                {"TPRTarget", "Ваша цель в настоящее время недоступна!"},
                {"TPDead", "Ты не можешь телепортироваться, будучи мертвым!"},
                {"TPWounded", "Ты не можешь телепортироваться, пока ранен!"},
                {"TPTooCold", "Ты слишком замерз, чтобы телепортироваться!"},
                {"TPTooHot", "Ты слишком горячий, чтобы телепортироваться!"},
                {"TPMounted", "Ты не можешь телепортироваться сидя!"},
                {"TPBuildingBlocked", "Вы не можете телепортироваться в заблокированной зоне здания!"},
                {"TPTargetBuildingBlocked", "Вы не можете телепортироваться в заблокированной зоне здания!"},
                {"TPTargetInsideBlock", "Ты не можешь телепортироваться в Фонд!"},
                {"TPSwimming", "Ты не можешь телепортироваться во время плавания!"},
                {"TPCargoShip", "Ты не можешь телепортироваться с грузового корабля!"},
                {"TPOilRig", "Ты не можешь телепортироваться с нефтяная вышка!"},
                {"TPExcavator", "Ты не можешь телепортироваться с экскаватор!"},
                {"TPHotAirBalloon", "Вы не можете телепортироваться на воздушный шар или с него!"},
                {"TPLift", "Вы не можете телепортироваться в лифте или ковшовом подъемнике!"},
                {"TPBucketLift", "Вы не можете телепортироваться, находясь в ковшовом подъемнике!"},
                {"TPRegLift", "Ты не можешь телепортироваться в лифте!"},
                {"TPSafeZone", "Ты не можешь телепортироваться из безопасной зоны!"},
                {"TPCrafting", "Вы не можете телепортироваться во время крафта!"},
                {"TPBlockedItem", "Вы не можете телепортироваться во время переноски, {0}!"},
                {"TooCloseToMon", "Ты не можешь телепортироваться так близко к {0}!"},
                {"TooCloseToCave", "Ты не можешь телепортироваться так близко к пещере!"},
                {"HomeTooCloseToCave", "Нельзя сидеть дома так близко к пещере!"},
                {"TownTP", "Ты телепортировался в город!"},
                {"TownTPNotSet", "Город в настоящее время не установлен!"},
                {"TownTPDisabled", "Город в настоящее время не включен!"},
                {"TownTPLocation", "Вы установили местоположение города в {0}!"},
                {"TownTPStarted", "Телепортация в город в {0} секунды!"},
                {"TownTPCooldown", "Ваш телепорт в настоящее время находится на перезарядке. Вам придется подождать {0} для следующего телепорта."},
                {"TownTPCooldownBypass", "Ваш запрос на телепортацию был на перезарядке. Вы решили обойти это, заплатив {0} с вашего баланса."},
                {"TownTPCooldownBypassF", "Ваш телепорт в настоящее время находится на перезарядке. У вас недостаточно средств - {0} - обходить."},
                {"TownTPCooldownBypassP", "Вы можете заплатить {0} чтобы обойти это охлаждение."},
                {"TownTPCooldownBypassP2", "Тип /town {0}."},
                {"TownTPLimitReached", "Вы достигли дневного предела {0} телепорты сегодня!"},
                {"TownTPAmount", "У вас есть {0} городские телепорты ушли сегодня!"},
                {"Interrupted", "Ваш телепорт был прерван!"},
                {"InterruptedTarget", "{0}'s телепорт был прерван!"},
                {"Unlimited", "Unlimited"},
                {
                    "TPInfoGeneral", string.Join(NewLine, new[]
                    {
                        "Пожалуйста, укажите модуль, который вы хотите просмотреть.",
                        "Имеющиеся модули, "
                    })
                },
                {
                    "TPHelpGeneral", string.Join(NewLine, new[]
                    {
                        "/tpinfo - Показывает ограничения и кулдауны.",
                        "Пожалуйста, укажите модуль, который вы хотите просмотреть в справке.",
                        "Имеющиеся модули, "
                    })
                },
                {
                    "TPHelpadmintp", string.Join(NewLine, new[]
                    {
                        "Как администратор Вы имеете доступ к следующим командам,",
                        "/tp \"targetplayer\" - Телепортирует себя к целевому игроку.",
                        "/tp \"player\" \"targetplayer\" - Телепортирует игрока к целевому игроку.",
                        "/tp x y z - Телепортирует вас в набор координат.",
                        "/tpl - Показывает список сохраненных местоположений.",
                        "/tpl \"location name\" - Телепортирует вас в сохраненное место.",
                        "/tpsave \"location name\" - Сохраняет текущую позицию в качестве имени местоположения.",
                        "/tpremove \"location name\" - Удаляет местоположение из сохраненного списка.",
                        "/tpb - Телепортирует вас обратно в то место, где вы были до телепортации.",
                        "/home radius \"radius\" - Найти все дома в радиусе.",
                        "/home delete \"player name|id\" \"home name\" - Удалите дом из плеера.",
                        "/home tp \"player name|id\" \"name\" - Телепортирует вас на главную локацию с именем 'name' от игрока.",
                        "/home homes \"player name|id\" - Показывает список всех домов из плеера."
                    })
                },
                {
                    "TPHelphome", string.Join(NewLine, new[]
                    {
                        "С помощью следующих команд вы можете установить свое домашнее местоположение для телепортации обратно в,",
                        "/home add \"name\" - Сохраняет текущую позицию в качестве имени местоположения.",
                        "/home list - Показывает список всех сохраненных местоположений.",
                        "/home remove \"name\" - Удаляет местоположение сохраненных домов.",
                        "/home \"name\" - Телепортирует вас на родное место."
                    })
                },
                {
                    "TPHelptpr", string.Join(NewLine, new[]
                    {
                        "С помощью этих команд вы можете запросить телепортацию к игроку или принять чей-либо запрос,",
                        "/tpr \"player name\" - Отправляет запрос на телепортацию игроку.",
                        "/tpa - Принимает входящий запрос телепорта.",
                        "/tpc - Отменить телепортацию или запрос."
                    })
                },
                {
                    "TPSettingsGeneral", string.Join(NewLine, new[]
                    {
                        "Пожалуйста, укажите модуль, который вы хотите просмотреть настройки.",
                        "Имеющиеся модули,"
                    })
                },
                {
                    "TPSettingshome", string.Join(NewLine, new[]
                    {
                        "В домашней системе включены текущие настройки,",
                        "Время между телепортами, {0}",
                        "Ежедневное количество телепортов, {1}",
                        "Количество сохраненных домашних местоположений, {2}"
                    })
                },
                {
                    "TPSettingstpr", string.Join(NewLine, new[]
                    {
                        "TPR В системе включены текущие настройки,",
                        "Время между телепортами, {0}",
                        "Ежедневное количество телепортов, {1}"
                    })
                },
                {
                    "TPSettingstown", string.Join(NewLine, new[]
                    {
                        "Town В системе включены текущие настройки,",
                        "Время между телепортами, {0}",
                        "Ежедневное количество телепортов, {1}"
                    })
                },
                {"PlayerNotFound", "Не удалось найти указанного игрока, повторите попытку!"},
                {"MultiplePlayers", "Найдено несколько игроков, {0}"},
                {"CantTeleportToSelf", "Ты не можешь телепортироваться к себе!"},
                {"CantTeleportPlayerToSelf", "Вы не можете телепортировать игрока к себе!"},
                {"TeleportPending", "Вы не можете инициировать другой телепорт, пока у вас есть телепорт в ожидании!"},
                {"TeleportPendingTarget", "Ты не можешь просить телепортации у того, кто собирается телепортироваться!"},
                {"LocationExists", "Место с таким именем уже существует в {0}!"},
                {"LocationExistsNearby", "Место с именем {0} уже существует рядом с этой позицией!"},
                {"LocationNotFound", "Не мог найти место с таким именем!"},
                {"NoPreviousLocationSaved", "Предыдущее местоположение не сохранено!"},
                {"HomeExists", "Вы уже сохранили расположение дома под этим именем!"},
                {"HomeExistsNearby", "Расположение дома с именем {0} уже существует рядом с этой позицией!"},
                {"HomeNotFound", "Не мог найти свой дом с таким именем!"},
                {"InvalidCoordinates", "Введенные вами координаты недействительны!"},
                {"InvalidHelpModule", "Неверный модуль поставляется!"},
                {"InvalidCharacter", "Вы использовали недопустимый символ, пожалуйста, ограничьте себя буквами от А до Я и цифрами."},
                {
                    "SyntaxCommandTP", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tp команда следующим образом,",
                        "/tp \"targetplayer\" - Телепортирует себя к целевому игроку.",
                        "/tp \"player\" \"targetplayer\" - Телепортирует игрока к целевому игроку.",
                        "/tp x y z - Телепортирует вас в набор координат.",
                        "/tp \"player\" x y z - Телепортирует игрока в набор координат."
                    })
                },
                {
                    "SyntaxCommandTPL", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpl команда следующим образом,",
                        "/tpl - Показывает список сохраненных местоположений.",
                        "/tpl \"location name\" - Телепортирует вас в сохраненное место."
                    })
                },
                {
                    "SyntaxCommandTPSave", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpsave команда следующим образом,",
                        "/tpsave \"location name\" - Сохраняет текущую позицию как 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPRemove", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpremove команда следующим образом,",
                        "/tpremove \"location name\" - Удаляет расположение с именем 'location name'."
                    })
                },
                {
                    "SyntaxCommandTPN", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpn команда следующим образом,",
                        "/tpn \"targetplayer\" - Телепортирует себя на расстояние по умолчанию позади целевого игрока.",
                        "/tpn \"targetplayer\" \"distance\" - Телепортирует вас на указанное расстояние позади целевого игрока."
                    })
                },
                {
                    "SyntaxCommandSetHome", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home add команда следующим образом,",
                        "/home add \"name\" - Сохранение текущего местоположения в качестве вашего дома с именем 'name'."
                    })
                },
                {
                    "SyntaxCommandRemoveHome", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home remove команда следующим образом,",
                        "/home remove \"name\" - Удаляет расположение дома с именем 'name'."
                    })
                },
                {
                    "SyntaxCommandHome", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home команда следующим образом,",
                        "/home \"name\" - Телепортирует себя в свой дом с именем 'name'.",
                        "/home \"name\" pay - Телепортирует себя в свой дом с именем 'name', избежать перезарядки, заплатив за это.",
                        "/home add \"name\" - Сохранение текущего местоположения в качестве вашего дома с именем 'name'.",
                        "/home list - Показывает список всех сохраненных домашних местоположений.",
                        "/home remove \"name\" - Удаляет расположение дома с именем 'name'."
                    })
                },
                {
                    "SyntaxCommandHomeAdmin", string.Join(NewLine, new[]
                    {
                        "/home radius \"radius\" - Показывает список всех домов в radius(10).",
                        "/home delete \"player name|id\" \"name\" - Удаляет расположение дома с именем 'name' от игрока.",
                        "/home tp \"player name|id\" \"name\" - Телепортирует вас на главную локацию с именем 'name' от игрока.",
                        "/home homes \"player name|id\" - Показывает список всех домов из плеера."
                    })
                },
                {
                    "SyntaxCommandTown", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /town команда следующим образом,",
                        "/town - Teleports yourself to town.",
                        "/town pay - Teleports yourself to town, paying the penalty."
                    })
                },
                {
                    "SyntaxCommandTownAdmin", string.Join(NewLine, new[]
                    {
                        "/town set - Сохраняет текущее местоположение как town."
                    })
                },
                {
                    "SyntaxCommandHomeDelete", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home delete команда следующим образом,",
                        "/home delete \"player name|id\" \"name\" - Удаляет расположение дома с именем 'name' от игрока."
                    })
                },
                {
                    "SyntaxCommandHomeAdminTP", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home tp команда следующим образом,",
                        "/home tp \"player name|id\" \"name\" - Телепортирует вас на главную локацию с именем 'name' от игрока."
                    })
                },
                {
                    "SyntaxCommandHomeHomes", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home homes команда следующим образом,",
                        "/home homes \"player name|id\" - Показывает список всех домов из плеера."
                    })
                },
                {
                    "SyntaxCommandListHomes", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /home list команда следующим образом,",
                        "/home list - Показывает список всех сохраненных домашних местоположений."
                    })
                },
                {
                    "SyntaxCommandTPR", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpr команда следующим образом,",
                        "/tpr \"player name\" - Отправляет запрос на телепортацию 'player name'."
                    })
                },
                {
                    "SyntaxCommandTPA", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpa команда следующим образом,",
                        "/tpa - Принимает входящий запрос телепорта."
                    })
                },
                {
                    "SyntaxCommandTPC", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только /tpc команда следующим образом,",
                        "/tpc - Отменяет телепорт запросу."
                    })
                },
                {
                    "SyntaxConsoleCommandToPos", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только teleport.topos console команда следующим образом,",
                        " > teleport.topos \"player\" x y z"
                    })
                },
                {
                    "SyntaxConsoleCommandToPlayer", string.Join(NewLine, new[]
                    {
                        "Произошла Синтаксическая Ошибка!",
                        "Вы можете использовать только teleport.toplayer console команда следующим образом,",
                        " > teleport.toplayer \"player\" \"target player\""
                    })
                },
                {"LogTeleport", "{0} телепортироваться {1}."},
                {"LogTeleportPlayer", "{0} телепортированный {1} к {2}."},
                {"LogTeleportBack", "{0} телепортировался на прежнее место."}
            }, this, "ru");
        }

        private void Init()
        {
            Unsubscribe(nameof(OnEntityTakeDamage));
            Unsubscribe(nameof(OnPlayerSleepEnded));
            Unsubscribe(nameof(OnPlayerDisconnected));
        }

        private void Loaded()
        {
            dataAdmin = GetFile(nameof(NTeleportation) + "Admin");
            Admin = dataAdmin.ReadObject<Dictionary<ulong, AdminData>>();
            dataHome = GetFile(nameof(NTeleportation) + "Home");
            Home = dataHome.ReadObject<Dictionary<ulong, HomeData>>();
            dataTPT = GetFile(nameof(NTeleportation) + "TPT");
            TPT = dataTPT.ReadObject<Dictionary<ulong, List<string>>>();
            dataTPR = GetFile(nameof(NTeleportation) + "TPR");
            TPR = dataTPR.ReadObject<Dictionary<ulong, TeleportData>>();
            dataTown = GetFile(nameof(NTeleportation) + "Town");
            Town = dataTown.ReadObject<Dictionary<ulong, TeleportData>>();
            dataOutpost = GetFile(nameof(NTeleportation) + "Outpost");
            Outpost = dataOutpost.ReadObject<Dictionary<ulong, TeleportData>>();
            dataBandit = GetFile(nameof(NTeleportation) + "Bandit");
            Bandit = dataBandit.ReadObject<Dictionary<ulong, TeleportData>>();
            permission.RegisterPermission(PermDeleteHome, this);
            permission.RegisterPermission(PermHome, this);
            permission.RegisterPermission(PermHomeHomes, this);
            permission.RegisterPermission(PermImportHomes, this);
            permission.RegisterPermission(PermRadiusHome, this);
            permission.RegisterPermission(PermTp, this);
            permission.RegisterPermission(PermTpB, this);
            permission.RegisterPermission(PermTpR, this);
            permission.RegisterPermission(PermTpConsole, this);
            permission.RegisterPermission(PermTpHome, this);
            permission.RegisterPermission(PermTpTown, this);
            permission.RegisterPermission(PermTpT, this);
            permission.RegisterPermission(PermTpOutpost, this);
            permission.RegisterPermission(PermTpBandit, this);
            permission.RegisterPermission(PermTpN, this);
            permission.RegisterPermission(PermTpL, this);
            permission.RegisterPermission(PermTpRemove, this);
            permission.RegisterPermission(PermTpSave, this);
            permission.RegisterPermission(PermWipeHomes, this);
            permission.RegisterPermission(PermCraftHome, this);
            permission.RegisterPermission(PermCraftTown, this);
            permission.RegisterPermission(PermCraftOutpost, this);
            permission.RegisterPermission(PermCraftBandit, this);
            permission.RegisterPermission(PermCraftTpR, this);
            foreach (var key in _config.Home.VIPCooldowns.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.Home.VIPCountdowns.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.Home.VIPDailyLimits.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.Home.VIPHomesLimits.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.TPR.VIPCooldowns.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.TPR.VIPCountdowns.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.TPR.VIPDailyLimits.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.Town.VIPCooldowns.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.Town.VIPCountdowns.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);
            foreach (var key in _config.Town.VIPDailyLimits.Keys)
                if (!permission.PermissionExists(key, this)) permission.RegisterPermission(key, this);

            FindMonuments();
        }

        private DynamicConfigFile GetFile(string name)
        {
            var file = Interface.Oxide.DataFileSystem.GetFile(name);
            file.Settings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            file.Settings.Converters = new JsonConverter[] { new UnityVector3Converter(), new CustomComparerDictionaryCreationConverter<string>(StringComparer.OrdinalIgnoreCase) };
            return file;
        }

        void OnServerInitialized()
        {
            Subscribe(nameof(OnEntityTakeDamage));
            Subscribe(nameof(OnPlayerSleepEnded));
            Subscribe(nameof(OnPlayerDisconnected));

            boundary = TerrainMeta.Size.x / 2;
            CheckPerms(_config.Home.VIPHomesLimits);
            CheckPerms(_config.Home.VIPDailyLimits);
            CheckPerms(_config.Home.VIPCooldowns);
            CheckPerms(_config.TPR.VIPDailyLimits);
            CheckPerms(_config.TPR.VIPCooldowns);
            CheckPerms(_config.Town.VIPDailyLimits);
            CheckPerms(_config.Town.VIPCooldowns);
            CheckPerms(_config.Outpost.VIPDailyLimits);
            CheckPerms(_config.Outpost.VIPCooldowns);
            CheckPerms(_config.Bandit.VIPDailyLimits);
            CheckPerms(_config.Bandit.VIPCooldowns);

            foreach (var item in _config.Settings.BlockedItems)
            {
                var definition = ItemManager.FindItemDefinition(item.Key);
                if (definition == null)
                {
                    Puts("Blocked item not found: {0}", item.Key);
                    continue;
                }
                ReverseBlockedItems[definition.itemid] = item.Value;
            }

            if (_config.Settings.OutpostEnabled && CompoundTeleport == null)
                AddCovalenceCommand("outpost", nameof(CommandOutpost));

            if (_config.Settings.BanditEnabled && CompoundTeleport == null)
                AddCovalenceCommand("bandit", nameof(CommandBandit));

            AddCovalenceCommand("tnt", nameof(CommandToggle));
            AddCovalenceCommand("tp", nameof(CommandTeleport));
            AddCovalenceCommand("home", nameof(CommandHome));
            AddCovalenceCommand("sethome", nameof(CommandSetHome));
            AddCovalenceCommand("listhomes", nameof(CommandListHomes));
            AddCovalenceCommand("tpn", nameof(CommandTeleportNear));
            AddCovalenceCommand("tpl", nameof(CommandTeleportLocation));
            AddCovalenceCommand("tpsave", nameof(CommandSaveTeleportLocation));
            AddCovalenceCommand("tpremove", nameof(CommandRemoveTeleportLocation));
            AddCovalenceCommand("tpb", nameof(CommandTeleportBack));
            AddCovalenceCommand("removehome", nameof(CommandRemoveHome));
            AddCovalenceCommand("radiushome", nameof(CommandHomeRadius));
            AddCovalenceCommand("deletehome", nameof(CommandHomeDelete));
            AddCovalenceCommand("tphome", nameof(CommandHomeAdminTP));
            AddCovalenceCommand("homehomes", nameof(CommandHomeHomes));
            AddCovalenceCommand("tpt", nameof(CommandTeleportTeam));
            AddCovalenceCommand("tpr", nameof(CommandTeleportRequest));
            AddCovalenceCommand("tpa", nameof(CommandTeleportAccept));
            AddCovalenceCommand("wipehomes", nameof(CommandWipeHomes));
            AddCovalenceCommand("tphelp", nameof(CommandTeleportHelp));
            AddCovalenceCommand("tpinfo", nameof(CommandTeleportInfo));
            AddCovalenceCommand("tpc", nameof(CommandTeleportCancel));
            AddCovalenceCommand("teleport.toplayer", nameof(CommandTeleportII));
            AddCovalenceCommand("teleport.topos", nameof(CommandTeleportII));
            AddCovalenceCommand("teleport.importhomes", nameof(CommandImportHomes));
            AddCovalenceCommand("spm", nameof(CommandSphereMonuments));
        }

        List<string> validCommands = new List<string> { "outpost", "bandit", "tp", "home", "sethome", "listhomes", "tpn", "tpl", "tpsave", "tpremove", "tpb", "removehome", "radiushome", "deletehome", "tphome", "homehomes", "tpt", "tpr", "tpa", "wipehomes", "tphelp", "tpinfo", "tpc", "teleport.toplayer", "teleport.topos", "teleport.importhomes", "spm" };

        void OnNewSave(string strFilename)
        {
            if (_config.Settings.WipeOnUpgradeOrChange)
            {
                Puts("Rust was upgraded or map changed - clearing homes, town, outpost and bandit!");
                Home.Clear();
                changedHome = True;
                _config.Town.Location = Zero;
                _config.Outpost.Location = Zero;
                _config.Bandit.Location = Zero;
            }
            else
            {
                Puts("Rust was upgraded or map changed - homes, town, outpost and bandit may be invalid!");
            }
        }

        void OnServerSave()
        {
            SaveTeleportsAdmin();
            SaveTeleportsHome();
            SaveTeleportsTPR();
            SaveTeleportsTPT();
            SaveTeleportsTown();
            SaveTeleportsOutpost();
            SaveTeleportsBandit();
        }

        void OnServerShutdown() => OnServerSave();

        void Unload() => OnServerSave();

        void OnPluginLoaded(Plugin plugin)
        {
            if (plugin.Name == "Economics")
            {
                Economics = plugin;
            }
            if (plugin.Name == "ServerRewards")
            {
                ServerRewards = plugin;
            }
            if (plugin.Name == "Friends")
            {
                Friends = plugin;
            }
            if (plugin.Name == "Clans")
            {
                Clans = plugin;
            }
        }

        void OnPluginUnloaded(Plugin plugin)
        {
            if (plugin.Name == "Economics")
            {
                Economics = null;
            }
            if (plugin.Name == "ServerRewards")
            {
                ServerRewards = null;
            }
            if (plugin.Name == "Friends")
            {
                Friends = null;
            }
            if (plugin.Name == "Clans")
            {
                Clans = null;
            }
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            var player = entity.ToPlayer();
            if (player == null || hitInfo == null) return;
            if (hitInfo.damageTypes.Has(DamageType.Fall) && teleporting.ContainsKey(player.userID))
            {
                hitInfo.damageTypes = new DamageTypeList();
                teleporting.Remove(player.userID);
            }
            TeleportTimer teleportTimer;
            if (!TeleportTimers.TryGetValue(player.userID, out teleportTimer)) return;
            DamageType major = hitInfo.damageTypes.GetMajorityDamageType();
            if (!_config.Settings.Interrupt.Hurt) return;
            NextTick(() =>
            {
                if (!player) return;
                if (!hitInfo.hasDamage || hitInfo.damageTypes.Total() <= 0) return;
                // 1.0.84 new checks for cold/heat based on major damage for the player
                if (major == DamageType.Cold && _config.Settings.Interrupt.Cold)
                {
                    if (player.metabolism.temperature.value <= _config.Settings.MinimumTemp)
                    {
                        PrintMsgL(teleportTimer.OriginPlayer, "TPTooCold");
                        if (teleportTimer.TargetPlayer != null)
                        {
                            PrintMsgL(teleportTimer.TargetPlayer, "InterruptedTarget", teleportTimer.OriginPlayer?.displayName);
                        }
                        teleportTimer.Timer.Destroy();
                        TeleportTimers.Remove(player.userID);
                    }
                }
                else if (major == DamageType.Heat && _config.Settings.Interrupt.Hot)
                {
                    if (player.metabolism.temperature.value >= _config.Settings.MaximumTemp)
                    {
                        PrintMsgL(teleportTimer.OriginPlayer, "TPTooHot");
                        if (teleportTimer.TargetPlayer != null)
                        {
                            PrintMsgL(teleportTimer.TargetPlayer, "InterruptedTarget", teleportTimer.OriginPlayer?.displayName);
                        }
                        teleportTimer.Timer.Destroy();
                        TeleportTimers.Remove(player.userID);
                    }
                }
                else
                {
                    PrintMsgL(teleportTimer.OriginPlayer, "Interrupted");
                    if (teleportTimer.TargetPlayer != null)
                    {
                        PrintMsgL(teleportTimer.TargetPlayer, "InterruptedTarget", teleportTimer.OriginPlayer?.displayName);
                    }
                    teleportTimer.Timer.Destroy();
                    TeleportTimers.Remove(player.userID);
                }
            });
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (!player || !teleporting.ContainsKey(player.userID)) return;
            ulong userID = player.userID;
            timer.Once(3f, () => teleporting.Remove(userID));
        }

        void OnPlayerDisconnected(BasePlayer player)
        {
            if (!player) return;
            Timer reqTimer;
            if (PendingRequests.TryGetValue(player.userID, out reqTimer))
            {
                var originPlayer = PlayersRequests[player.userID];
                if (originPlayer)
                {
                    PlayersRequests.Remove(originPlayer.userID);
                    PrintMsgL(originPlayer, "RequestTargetOff");
                }
                reqTimer.Destroy();
                PendingRequests.Remove(player.userID);
                PlayersRequests.Remove(player.userID);
            }
            TeleportTimer teleportTimer;
            if (TeleportTimers.TryGetValue(player.userID, out teleportTimer))
            {
                teleportTimer.Timer.Destroy();
                TeleportTimers.Remove(player.userID);
            }
            teleporting.Remove(player.userID);
        }

        private void SaveTeleportsAdmin()
        {
            if (Admin == null || !changedAdmin) return;
            dataAdmin.WriteObject(Admin);
            changedAdmin = False;
        }

        private void SaveTeleportsHome()
        {
            if (Home == null || !changedHome) return;
            dataHome.WriteObject(Home);
            changedHome = False;
        }

        private void SaveTeleportsTPR()
        {
            if (TPR == null || !changedTPR) return;
            dataTPR.WriteObject(TPR);
            changedTPR = False;
        }

        private void SaveTeleportsTPT()
        {
            if (TPT == null || !changedTPT) return;
            dataTPT.WriteObject(TPT);
            changedTPT = False;
        }

        private void SaveTeleportsTown()
        {
            if (Town == null || !changedTown) return;
            dataTown.WriteObject(Town);
            changedTown = False;
        }

        private void SaveTeleportsOutpost()
        {
            if (Outpost == null || !changedOutpost) return;
            dataOutpost.WriteObject(Outpost);
            changedOutpost = False;
        }

        private void SaveTeleportsBandit()
        {
            if (Bandit == null || !changedBandit) return;
            dataBandit.WriteObject(Bandit);
            changedBandit = False;
        }

        private void SaveLocation(BasePlayer player)
        {
            if (!IsAllowed(player, PermTpB)) return;
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData))
                Admin[player.userID] = adminData = new AdminData();
            adminData.PreviousLocation = player.transform.position;
            changedAdmin = True;
            PrintMsgL(player, "AdminTPBackSave");
        }

        char[] chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz".ToCharArray();
        private readonly System.Text.StringBuilder _sb = new System.Text.StringBuilder();

        string RandomString(int minAmount = 5, int maxAmount = 10)
        {
            _sb.Length = 0;

            for (int i = 0; i <= UnityEngine.Random.Range(minAmount, maxAmount); i++)
                _sb.Append(chars[UnityEngine.Random.Range(0, chars.Length)]);

            return _sb.ToString();
        }
        
        bool setextra = False;
        void FindMonuments()
        {
            var realWidth = 0f;
            string name = null;
            foreach (var monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                name = monument.displayPhrase.english;
                realWidth = monument.name == "OilrigAI" ? 100f : monument.name == "OilrigAI2" ? 200f : 0f;
#if DEBUG
                Puts($"Found {name}, extents {monument.Bounds.extents}");
#endif

                if (realWidth > 0f)
                {
#if DEBUG
                    Puts($"  corrected to {realWidth}");
#endif
                }

                if (monument.name.Contains("cave"))
                {
#if DEBUG
                    Puts("  Adding to cave list");
#endif
                    if (caves.ContainsKey(name)) name += RandomString();
                    caves.Add(name, monument.transform.position);
                }
                else if (monument.name.Contains("compound") && _config.Settings.AutoGenOutpost)
                {
#if DEBUG
                    Puts("  Adding Outpost target");
#endif
                    var ents = Pool.Get<List<BaseEntity>>();
                    Vis.Entities<BaseEntity>(monument.transform.position, 50, ents);
                    foreach (BaseEntity entity in ents)
                    {
                        if (entity.name.Contains("piano") || entity.name.Contains("chair"))
                        {
                            _config.Outpost.Location = entity.transform.position + new Vector3(0f, 1f, 0f);
                            setextra = True;
                            break;
                        }
                    }
                    if (!setextra) _config.Settings.OutpostEnabled = False;
                    Pool.Free(ref ents);
                }
                else if (monument.name.Contains("bandit") && _config.Settings.AutoGenBandit)
                {
#if DEBUG
                    Puts("  Adding BanditTown target");
#endif
                    var ents = Pool.Get<List<BaseEntity>>();
                    Vis.Entities<BaseEntity>(monument.transform.position, 50, ents);
                    foreach (BaseEntity entity in ents)
                    {
                        if (entity.name.Contains("workbench") || entity.name.Contains("chair") || entity.name.Contains("piano"))
                        {
                            _config.Bandit.Location = entity.name.Contains("piano") ? entity.transform.position + new Vector3(-1f, 1f, -1f) : entity.transform.position;
                            setextra = True;
                            break;
                        }
                    }
                    if (!setextra) _config.Settings.BanditEnabled = False;
                    Pool.Free(ref ents);
                }
                else
                {
                    if (monuments.ContainsKey(name)) name += ":" + RandomString(5, 5);
                    if (monument.name.Contains("power_sub")) name = monument.name.Substring(monument.name.LastIndexOf("/") + 1).Replace(".prefab", "") + ":" + RandomString(5, 5);
                    float radius = GetMonumentFloat(name);
                    monuments[name] = new MonInfo() { Position = monument.transform.position, Radius = radius };
#if DEBUG
                    Puts($"Adding Monument: {name}, pos: {monument.transform.position}, size: {radius}");
#endif
                }
            }

            if (setextra)
            {
                // Write config so that the outpost and bandit autogen locations are available immediately.
                SaveConfig();
            }
        }

        private void CommandToggle(IPlayer p, string command, string[] args)
        {
            if (!p.IsAdmin) return;

            if (args.Length == 0)
            {
                p.Reply("tnt commandname");
                return;
            }

            if (!validCommands.Contains(args[0].ToLower()))
            {
                p.Reply("Invalid command name: {0}", null, string.Join(", ", validCommands.ToList()));
                return;
            }

            string arg = args[0].ToLower();

            if (arg == command.ToLower()) return;
            if (!storedData.DisabledCommands.Remove("tp")) storedData.DisabledCommands.Add("tp");

            p.Reply("{0} {1}", null, storedData.DisabledCommands.Contains("tp") ? "Disabled:" : "Enabled:", arg);
        }

        private void CommandTeleport(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTp)) return;
            BasePlayer target;
            float x, y, z;
            switch (args.Length)
            {
                case 1:
                    target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (target == player)
                    {
#if DEBUG
                        Puts("Debug mode - allowing self teleport.");
#else
                PrintMsgL(player, "CantTeleportToSelf");
                return;
#endif
                    }
                    Teleport(player, target);
                    PrintMsgL(player, "AdminTP", target.displayName);
                    Puts(_("LogTeleport", null, player.displayName, target.displayName));
                    if (_config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPTarget", player.displayName);
                    break;
                case 2:
                    var origin = FindPlayersSingle(args[0], player);
                    if (origin == null) return;
                    target = FindPlayersSingle(args[1], player);
                    if (target == null) return;
                    if (target == origin)
                    {
                        PrintMsgL(player, "CantTeleportPlayerToSelf");
                        return;
                    }
                    Teleport(origin, target);
                    PrintMsgL(player, "AdminTPPlayers", origin.displayName, target.displayName);
                    PrintMsgL(origin, "AdminTPPlayer", player.displayName, target.displayName);
                    if (_config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPPlayerTarget", player.displayName, origin.displayName);
                    Puts(_("LogTeleportPlayer", null, player.displayName, origin.displayName, target.displayName));
                    break;
                case 3:
                    if (!float.TryParse(args[0], out x) || !float.TryParse(args[1], out y) || !float.TryParse(args[2], out z))
                    {
                        PrintMsgL(player, "InvalidCoordinates");
                        return;
                    }
                    if (_config.Settings.CheckBoundaries && !CheckBoundaries(x, y, z)) // added this option because I HATE boundaries
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                        return;
                    }
                    Teleport(player, x, y, z);
                    PrintMsgL(player, "AdminTPCoordinates", player.transform.position);
                    Puts(_("LogTeleport", null, player.displayName, player.transform.position));
                    break;
                case 4:
                    target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (!float.TryParse(args[1], out x) || !float.TryParse(args[2], out y) || !float.TryParse(args[3], out z))
                    {
                        PrintMsgL(player, "InvalidCoordinates");
                        return;
                    }
                    if (!CheckBoundaries(x, y, z))
                    {
                        PrintMsgL(player, "AdminTPOutOfBounds");
                        PrintMsgL(player, "AdminTPBoundaries", boundary);
                        return;
                    }
                    Teleport(target, x, y, z);
                    if (player == target)
                    {
                        PrintMsgL(player, "AdminTPCoordinates", player.transform.position);
                        Puts(_("LogTeleport", null, player.displayName, player.transform.position));
                    }
                    else
                    {
                        PrintMsgL(player, "AdminTPTargetCoordinates", target.displayName, player.transform.position);
                        if (_config.Admin.AnnounceTeleportToTarget)
                            PrintMsgL(target, "AdminTPTargetCoordinatesTarget", player.displayName, player.transform.position);
                        Puts(_("LogTeleportPlayer", null, player.displayName, target.displayName, player.transform.position));
                    }
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTP");
                    break;
            }
        }

        private void CommandTeleportNear(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpN)) return;
            switch (args.Length)
            {
                case 1:
                case 2:
                    var target = FindPlayersSingle(args[0], player);
                    if (target == null) return;
                    if (target == player)
                    {
#if DEBUG
                        Puts("Debug mode - allowing self teleport.");
#else
                        PrintMsgL(player, "CantTeleportToSelf");
                        return;
#endif
                    }
                    int distance;
                    if (!int.TryParse(args[1], out distance))
                        distance = _config.Admin.TeleportNearDefaultDistance;
                    float x = UnityEngine.Random.Range(-distance, distance);
                    var z = (float)System.Math.Sqrt(System.Math.Pow(distance, 2) - System.Math.Pow(x, 2));
                    var destination = target.transform.position;
                    destination.x = destination.x - x;
                    destination.z = destination.z - z;
                    Teleport(player, GetGroundBuilding(destination));
                    PrintMsgL(player, "AdminTP", target.displayName);
                    Puts(_("LogTeleport", null, player.displayName, target.displayName));
                    if (_config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(target, "AdminTPTarget", player.displayName);
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTPN");
                    break;
            }
        }

        private void CommandTeleportLocation(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpL)) return;
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData) || adminData.Locations.Count <= 0)
            {
                PrintMsgL(player, "AdminLocationListEmpty");
                return;
            }
            switch (args.Length)
            {
                case 0:
                    PrintMsgL(player, "AdminLocationList");
                    foreach (var location in adminData.Locations)
                        PrintMsgL(player, $"{location.Key} {location.Value}");
                    break;
                case 1:
                    Vector3 loc;
                    if (!adminData.Locations.TryGetValue(args[0], out loc))
                    {
                        PrintMsgL(player, "LocationNotFound");
                        return;
                    }
                    Teleport(player, loc);
                    PrintMsgL(player, "AdminTPLocation", args[0]);
                    break;
                default:
                    PrintMsgL(player, "SyntaxCommandTPL");
                    break;
            }
        }

        private void CommandSaveTeleportLocation(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpSave)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPSave");
                return;
            }
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData))
                Admin[player.userID] = adminData = new AdminData();
            Vector3 location;
            if (adminData.Locations.TryGetValue(args[0], out location))
            {
                PrintMsgL(player, "LocationExists", location);
                return;
            }
            var positionCoordinates = player.transform.position;
            foreach (var loc in adminData.Locations)
            {
                if ((positionCoordinates - loc.Value).magnitude < _config.Admin.LocationRadius)
                {
                    PrintMsgL(player, "LocationExistsNearby", loc.Key);
                    return;
                }
            }
            adminData.Locations[args[0]] = positionCoordinates;
            PrintMsgL(player, "AdminTPLocationSave");
            changedAdmin = True;
        }

        private void CommandRemoveTeleportLocation(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpRemove)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandTPRemove");
                return;
            }
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData) || adminData.Locations.Count <= 0)
            {
                PrintMsgL(player, "AdminLocationListEmpty");
                return;
            }
            if (adminData.Locations.Remove(args[0]))
            {
                PrintMsgL(player, "AdminTPLocationRemove", args[0]);
                changedAdmin = True;
                return;
            }
            PrintMsgL(player, "LocationNotFound");
        }

        private void CommandTeleportBack(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpB)) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPB");
                return;
            }
            AdminData adminData;
            if (!Admin.TryGetValue(player.userID, out adminData) || adminData.PreviousLocation == Zero)
            {
                PrintMsgL(player, "NoPreviousLocationSaved");
                return;
            }

            Teleport(player, adminData.PreviousLocation);
            adminData.PreviousLocation = Zero;
            changedAdmin = True;
            PrintMsgL(player, "AdminTPBack");
            Puts(_("LogTeleportBack", null, player.displayName));
        }

        private void CommandSetHome(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowed(player, PermHome) || !_config.Settings.HomesEnabled) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandSetHome");
                return;
            }
            var err = CheckPlayer(player, False, CanCraftHome(player), True, "home");
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            if (!player.CanBuild())
            {
                PrintMsgL(player, "HomeTPBuildingBlocked");
                return;
            }
            if (!args[0].All(char.IsLetterOrDigit))
            {
                PrintMsgL(player, "InvalidCharacter");
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData))
                Home[player.userID] = homeData = new HomeData();
            var limit = GetHigher(player, _config.Home.VIPHomesLimits, _config.Home.HomesLimit);
            if (homeData.Locations.Count >= limit)
            {
                PrintMsgL(player, "HomeMaxLocations", limit);
                return;
            }
            Vector3 location;
            if (homeData.Locations.TryGetValue(args[0], out location))
            {
                PrintMsgL(player, "HomeExists", location);
                return;
            }
            var positionCoordinates = player.transform.position;
            foreach (var loc in homeData.Locations)
            {
                if ((positionCoordinates - loc.Value).magnitude < _config.Home.LocationRadius)
                {
                    PrintMsgL(player, "HomeExistsNearby", loc.Key);
                    return;
                }
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }

            if (player.IsAdmin && _config.Settings.DrawHomeSphere) player.SendConsoleCommand("ddraw.sphere", 30f, Color.blue, GetGround(positionCoordinates), 2.5f);

            err = CheckFoundation(player.userID, positionCoordinates);
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            err = CheckInsideBlock(positionCoordinates);
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            homeData.Locations[args[0]] = positionCoordinates;
            changedHome = True;
            PrintMsgL(player, "HomeSave");
            PrintMsgL(player, "HomeQuota", homeData.Locations.Count, limit);
        }

        private void CommandRemoveHome(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (!_config.Settings.HomesEnabled) return;
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowed(player, PermHome)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandRemoveHome");
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            if (homeData.Locations.Remove(args[0]))
            {
                changedHome = True;
                PrintMsgL(player, "HomeRemove", args[0]);
            }
            else
                PrintMsgL(player, "HomeNotFound");
        }

        private void CommandHome(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (!_config.Settings.HomesEnabled) return;
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowed(player, PermHome)) return;
            if (args.Length == 0)
            {
                PrintMsgL(player, "SyntaxCommandHome");
                if (IsAllowed(player)) PrintMsgL(player, "SyntaxCommandHomeAdmin");
                return;
            }
            switch (args[0].ToLower())
            {
                case "add":
                    CommandSetHome(p, command, args.Skip(1).ToArray());
                    break;
                case "list":
                    CommandListHomes(p, command, args.Skip(1).ToArray());
                    break;
                case "remove":
                    CommandRemoveHome(p, command, args.Skip(1).ToArray());
                    break;
                case "radius":
                    CommandHomeRadius(p, command, args.Skip(1).ToArray());
                    break;
                case "delete":
                    CommandHomeDelete(p, command, args.Skip(1).ToArray());
                    break;
                case "tp":
                    CommandHomeAdminTP(p, command, args.Skip(1).ToArray());
                    break;
                case "homes":
                    CommandHomeHomes(p, command, args.Skip(1).ToArray());
                    break;
                case "wipe":
                    CommandWipeHomes(p, command, args.Skip(1).ToArray());
                    break;
                default:
                    cmdChatHomeTP(player, command, args);
                    break;
            }
        }

        private void CommandHomeRadius(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermRadiusHome)) return;
            float radius;
            if (args.Length != 1 || !float.TryParse(args[0], out radius)) radius = 10;
            var found = False;
            foreach (var homeData in Home)
            {
                var toRemove = new List<string>();
                var target = RustCore.FindPlayerById(homeData.Key)?.displayName ?? homeData.Key.ToString();
                foreach (var location in homeData.Value.Locations)
                {
                    if ((player.transform.position - location.Value).magnitude <= radius)
                    {
                        if (CheckFoundation(homeData.Key, location.Value) != null)
                        {
                            toRemove.Add(location.Key);
                            continue;
                        }
                        var entity = GetFoundationOwned(location.Value, homeData.Key);
                        if (entity == null) continue;
                        player.SendConsoleCommand("ddraw.text", 30f, Color.blue, entity.CenterPoint() + new Vector3(0, .5f), $"<size=20>{target} - {location.Key} {location.Value}</size>");
                        DrawBox(player, entity.CenterPoint(), entity.transform.rotation, entity.bounds.size);
                        PrintMsg(player, $"{target} - {location.Key} {location.Value}");
                        found = True;
                    }
                }
                foreach (var loc in toRemove)
                {
                    homeData.Value.Locations.Remove(loc);
                    changedHome = True;
                }
            }
            if (!found)
                PrintMsgL(player, "HomeNoFound");
        }

        private void CommandHomeDelete(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermDeleteHome)) return;
            if (args.Length != 2)
            {
                PrintMsgL(player, "SyntaxCommandHomeDelete");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData targetHome;
            if (!Home.TryGetValue(userId, out targetHome) || !targetHome.Locations.Remove(args[1]))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            changedHome = True;
            PrintMsgL(player, "HomeDelete", args[0], args[1]);
        }

        private void CommandHomeAdminTP(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpHome)) return;
            if (args.Length != 2)
            {
                PrintMsgL(player, "SyntaxCommandHomeAdminTP");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData targetHome;
            Vector3 location;
            if (!Home.TryGetValue(userId, out targetHome) || !targetHome.Locations.TryGetValue(args[1], out location))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            Teleport(player, location);
            PrintMsgL(player, "HomeAdminTP", args[0], args[1]);
        }

        // Check that plugins are available and enabled for CheckEconomy()
        private bool UseEconomy()
        {
            if ((_config.Settings.UseEconomics && Economics) ||
                (_config.Settings.UseServerRewards && ServerRewards))
            {
                return True;
            }
            return False;
        }

        // Check balance on multiple plugins and optionally withdraw money from the player
        private bool CheckEconomy(BasePlayer player, double bypass, bool withdraw = False, bool deposit = False)
        {
            double balance = 0;
            bool foundmoney = False;

            // Check Economics first.  If not in use or balance low, check ServerRewards below
            if (_config.Settings.UseEconomics && Economics)
            {
                balance = (double)Economics?.CallHook("Balance", player.UserIDString);
                if (balance >= bypass)
                {
                    foundmoney = True;
                    if (withdraw)
                    {
                        var w = (bool)Economics?.CallHook("Withdraw", player.userID, bypass);
                        return w;
                    }
                    else if (deposit)
                    {
                        Economics?.CallHook("Deposit", player.userID, bypass);
                    }
                }
            }

            // No money via Economics, or plugin not in use.  Try ServerRewards.
            if (_config.Settings.UseServerRewards && ServerRewards)
            {
                object bal = ServerRewards?.Call("CheckPoints", player.userID);
                balance = Convert.ToDouble(bal);
                if (balance >= bypass && !foundmoney)
                {
                    foundmoney = True;
                    if (withdraw)
                    {
                        var w = (bool)ServerRewards?.Call("TakePoints", player.userID, (int)bypass);
                        return w;
                    }
                    else if (deposit)
                    {
                        ServerRewards?.Call("AddPoints", player.userID, (int)bypass);
                    }
                }
            }

            // Just checking balance without withdrawal - did we find anything?
            if (foundmoney)
            {
                return True;
            }
            return False;
        }

        private void cmdChatHomeTP(BasePlayer player, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { player.ChatMessage("Disabled command."); return; }
            if (!IsAllowed(player, PermHome)) return;
            bool paidmoney = False;
            if (!_config.Settings.HomesEnabled) return;
            if (args.Length < 1)
            {
                PrintMsgL(player, "SyntaxCommandHome");
                return;
            }
            var err = CheckPlayer(player, _config.Home.UsableOutOfBuildingBlocked, CanCraftHome(player), True, "home");
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            Vector3 location;
            if (!homeData.Locations.TryGetValue(args[0], out location))
            {
                PrintMsgL(player, "HomeNotFound");
                return;
            }
            err = CheckFoundation(player.userID, location) ?? CheckTargetLocation(player, location, _config.Home.UsableIntoBuildingBlocked, _config.Home.CupOwnerAllowOnBuildingBlocked);
            if (err != null)
            {
                PrintMsgL(player, "HomeRemovedInvalid", args[0]);
                homeData.Locations.Remove(args[0]);
                changedHome = True;
                return;
            }
            err = CheckInsideBlock(location);
            if (err != null)
            {
                PrintMsgL(player, "HomeRemovedInsideBlock", args[0]);
                homeData.Locations.Remove(args[0]);
                changedHome = True;
                return;
            }
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");
            if (homeData.Teleports.Date != currentDate)
            {
                homeData.Teleports.Amount = 0;
                homeData.Teleports.Date = currentDate;
            }
            var cooldown = GetLower(player, _config.Home.VIPCooldowns, _config.Home.Cooldown);

            if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
            {
                var cmdSent = "";
                bool foundmoney = CheckEconomy(player, _config.Home.Bypass);
                try
                {
                    cmdSent = args[1].ToLower();
                }
                catch { }

                bool payalso = False;
                if (_config.Home.Pay > 0)
                {
                    payalso = True;
                }
                if ((_config.Settings.BypassCMD != null) && (cmdSent == _config.Settings.BypassCMD.ToLower()))
                {
                    if (foundmoney)
                    {
                        CheckEconomy(player, _config.Home.Bypass, True);
                        paidmoney = True;
                        PrintMsgL(player, "HomeTPCooldownBypass", _config.Home.Bypass);
                        if (payalso)
                        {
                            PrintMsgL(player, "PayToHome", _config.Home.Pay);
                        }
                    }
                    else
                    {
                        PrintMsgL(player, "HomeTPCooldownBypassF", _config.Home.Bypass);
                        return;
                    }
                }
                else if (UseEconomy())
                {
                    var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                    PrintMsgL(player, "HomeTPCooldown", FormatTime(remain));
                    if (_config.Home.Bypass > 0 && _config.Settings.BypassCMD != null)
                    {
                        PrintMsgL(player, "HomeTPCooldownBypassP", _config.Home.Bypass);
                        PrintMsgL(player, "HomeTPCooldownBypassP2", _config.Settings.BypassCMD);
                    }
                    return;
                }
                else
                {
                    var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                    PrintMsgL(player, "HomeTPCooldown", FormatTime(remain));
                    return;
                }
            }
            var limit = GetHigher(player, _config.Home.VIPDailyLimits, _config.Home.DailyLimit);
            if (limit > 0 && homeData.Teleports.Amount >= limit)
            {
                PrintMsgL(player, "HomeTPLimitReached", limit);
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPending");
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            err = CheckItems(player);
            if (err != null)
            {
                PrintMsgL(player, "TPBlockedItem", err);
                return;
            }

            var countdown = GetLower(player, _config.Home.VIPCountdowns, _config.Home.Countdown);
            TeleportTimers[player.userID] = new TeleportTimer
            {
                OriginPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
#if DEBUG
                    Puts("Calling CheckPlayer from cmdChatHomeTP");
#endif
                    err = CheckPlayer(player, _config.Home.UsableOutOfBuildingBlocked, CanCraftHome(player), True, "home");
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, err);
                        if (paidmoney)
                        {
                            paidmoney = False;
                            CheckEconomy(player, _config.Home.Bypass, False, True);
                        }
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CanPlayerTeleport(player);
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, err);
                        if (paidmoney)
                        {
                            paidmoney = False;
                            CheckEconomy(player, _config.Home.Bypass, False, True);
                        }
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CheckItems(player);
                    if (err != null)
                    {
                        PrintMsgL(player, "Interrupted");
                        PrintMsgL(player, "TPBlockedItem", err);
                        if (paidmoney)
                        {
                            paidmoney = False;
                            CheckEconomy(player, _config.Home.Bypass, False, True);
                        }
                        TeleportTimers.Remove(player.userID);
                        return;
                    }
                    err = CheckFoundation(player.userID, location) ?? CheckTargetLocation(player, location, _config.Home.UsableIntoBuildingBlocked, _config.Home.CupOwnerAllowOnBuildingBlocked);
                    if (err != null)
                    {
                        PrintMsgL(player, "HomeRemovedInvalid", args[0]);
                        homeData.Locations.Remove(args[0]);
                        changedHome = True;
                        if (paidmoney)
                        {
                            paidmoney = False;
                            CheckEconomy(player, _config.Home.Bypass, False, True);
                        }
                        return;
                    }
                    err = CheckInsideBlock(location);
                    if (err != null)
                    {
                        PrintMsgL(player, "HomeRemovedInsideBlock", args[0]);
                        homeData.Locations.Remove(args[0]);
                        changedHome = True;
                        if (paidmoney)
                        {
                            paidmoney = False;
                            CheckEconomy(player, _config.Home.Bypass, False, True);
                        }
                        return;
                    }
                    if (UseEconomy())
                    {
                        if (_config.Home.Pay > 0 && !CheckEconomy(player, _config.Home.Pay))
                        {
                            PrintMsgL(player, "Interrupted");
                            PrintMsgL(player, "TPNoMoney", _config.Home.Pay);

                            TeleportTimers.Remove(player.userID);
                            return;
                        }
                        else if (_config.Home.Pay > 0)
                        {
                            var w = CheckEconomy(player, (double)_config.Home.Pay, True);
                            PrintMsgL(player, "TPMoney", (double)_config.Home.Pay);
                        }
                    }
                    Teleport(player, location);
                    homeData.Teleports.Amount++;
                    homeData.Teleports.Timestamp = timestamp;
                    changedHome = True;
                    PrintMsgL(player, "HomeTP", args[0]);
                    if (limit > 0) PrintMsgL(player, "HomeTPAmount", limit - homeData.Teleports.Amount);
                    TeleportTimers.Remove(player.userID);
                })
            };
            PrintMsgL(player, "HomeTPStarted", args[0], countdown);
        }

        private void CommandListHomes(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !_config.Settings.HomesEnabled) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandListHomes");
                return;
            }
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            PrintMsgL(player, "HomeList");
            if (_config.Home.CheckValidOnList)
            {
                var toRemove = new List<string>();
                foreach (var location in homeData.Locations)
                {
                    var err = CheckFoundation(player.userID, location.Value);
                    if (err != null)
                    {
                        toRemove.Add(location.Key);
                        continue;
                    }
                    PrintMsgL(player, $"{location.Key} {location.Value}");
                }
                foreach (var loc in toRemove)
                {
                    PrintMsgL(player, "HomeRemovedInvalid", loc);
                    homeData.Locations.Remove(loc);
                    changedHome = True;
                }
                return;
            }
            foreach (var location in homeData.Locations)
                PrintMsgL(player, $"{location.Key} {location.Value}");
        }

        private void CommandHomeHomes(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermHomeHomes)) return;
            if (args.Length != 1)
            {
                PrintMsgL(player, "SyntaxCommandHomeHomes");
                return;
            }
            var userId = FindPlayersSingleId(args[0], player);
            if (userId <= 0) return;
            HomeData homeData;
            if (!Home.TryGetValue(userId, out homeData) || homeData.Locations.Count <= 0)
            {
                PrintMsgL(player, "HomeListEmpty");
                return;
            }
            PrintMsgL(player, "HomeList");
            var toRemove = new List<string>();
            foreach (var location in homeData.Locations)
            {
                var err = CheckFoundation(userId, location.Value);
                if (err != null)
                {
                    toRemove.Add(location.Key);
                    continue;
                }
                PrintMsgL(player, $"{location.Key} {location.Value}");
            }
            foreach (var loc in toRemove)
            {
                PrintMsgL(player, "HomeRemovedInvalid", loc);
                homeData.Locations.Remove(loc);
                changedHome = True;
            }
        }

        private void CommandTeleportTeam(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (!_config.TPT.UseClans && !_config.TPT.UseFriends && !_config.TPT.UseTeams)
                return;

            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpT))
                return;

            if (args.Length < 1)
            {
                PrintMsgL(player, "SyntaxCommandTPT");
                PrintMsgL(player, "TPTInfo");
                return;
            }
            else
            {
                switch (args[0].ToLower())
                {
                    case "friend":
                    case "clan":
                    case "team":
                        {
                            SetDisabled(player, args[0].ToLower());
                            return;
                        }
                }

                var target = FindPlayersSingle(args[0], player);
                if (target == null || target.IPlayer == null)
                {
                    return;
                }
                else if (target == player && !player.IsAdmin)
                {
                    PrintMsgL(player, "CantTeleportToSelf");
                    return;
                }

                string playerClan = Clans?.Call<string>("GetClanOf", player.UserIDString);
                string targetClan = Clans?.Call<string>("GetClanOf", target.UserIDString);
                bool enabledFriends = IsEnabled(target.userID, "friend");
                bool enabledClanMates = IsEnabled(target.userID, "clan");
                bool enabledTeamMates = IsEnabled(target.userID, "team");
                bool isFriends = _config.TPT.UseFriends && enabledFriends && (Friends?.Call<bool>("AreFriends", player.UserIDString, target.UserIDString) ?? False);
                bool isClanMates = _config.TPT.UseClans && enabledClanMates && !string.IsNullOrEmpty(playerClan) && !string.IsNullOrEmpty(targetClan) && playerClan == targetClan;
                bool isTeamMates = _config.TPT.UseTeams && enabledTeamMates && player.currentTeam != 0 && target.currentTeam != 0 && player.currentTeam == target.currentTeam;

                if (isClanMates || isTeamMates || isFriends)
                {
                    CommandTeleportRequest(p, command, new string[1] { target.UserIDString });
                    CommandTeleportAccept(target.IPlayer, command, new string[0]);
                }
                else
                {
                    string message = _("NotValidTPT", player);
                    if (_config.TPT.UseFriends && enabledFriends) message += _("NotValidTPTFriend", player);
                    if (_config.TPT.UseTeams && enabledTeamMates) message += _("NotValidTPTTeam", player);
                    if (_config.TPT.UseClans && enabledClanMates) message += _("NotValidTPTClan", player);
                    PrintMsg(player, message);
                }
            }
        }

        bool IsEnabled(ulong targetId, string value)
        {
            return !TPT.ContainsKey(targetId) || !TPT[targetId].Contains(value);
        }

        void SetDisabled(BasePlayer target, string value)
        {
            List<string> list;
            if (!TPT.TryGetValue(target.userID, out list))
            {
                TPT[target.userID] = list = new List<string>();
            }

            if (list.Contains(value))
            {
                list.Remove(value);
            }
            else
            {
                list.Add(value);
            }

            string status = lang.GetMessage($"TPT_{!list.Contains(value)}", this, target.UserIDString);
            string message = string.Format(lang.GetMessage($"TPT_{value}", this, target.UserIDString), status);

            PrintMsg(target, message);
            changedTPT = True;
        }

        private void CommandTeleportRequest(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermTpR) || !_config.Settings.TPREnabled) return;
            if (args.Length == 0)
            {
                PrintMsgL(player, "SyntaxCommandTPR");
                return;
            }
            var targets = FindPlayersOnline(args[0]);
            if (targets.Count <= 0)
            {
                PrintMsgL(player, "PlayerNotFound");
                return;
            }
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", string.Join(", ", targets.Select(x => x.displayName).ToArray()));
                return;
            }
            var target = targets[0];
            if (target == player && !player.IsAdmin)
            {
#if DEBUG
                Puts("Debug mode - allowing self teleport.");
#else
        PrintMsgL(player, "CantTeleportToSelf");
        return;
#endif
            }
#if DEBUG
            Puts("Calling CheckPlayer from cmdChatTeleportRequest");
#endif

            var err = CheckPlayer(player, _config.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(player), True, "tpr");
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            err = CheckTargetLocation(target, target.transform.position, _config.TPR.UsableIntoBuildingBlocked, _config.TPR.CupOwnerAllowOnBuildingBlocked);
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");
            TeleportData tprData;
            if (!TPR.TryGetValue(player.userID, out tprData))
                TPR[player.userID] = tprData = new TeleportData();
            if (tprData.Date != currentDate)
            {
                tprData.Amount = 0;
                tprData.Date = currentDate;
            }

            var cooldown = player.IsAdmin ? 0 : GetLower(player, _config.TPR.VIPCooldowns, _config.TPR.Cooldown);
            if (cooldown > 0 && timestamp - tprData.Timestamp < cooldown)
            {
                var cmdSent = "";
                bool foundmoney = CheckEconomy(player, _config.TPR.Bypass);
                try
                {
                    cmdSent = args[1].ToLower();
                }
                catch { }

                bool payalso = False;
                if (_config.TPR.Pay > 0)
                {
                    payalso = True;
                }
                if ((_config.Settings.BypassCMD != null) && (cmdSent == _config.Settings.BypassCMD.ToLower()))
                {
                    if (foundmoney)
                    {
                        CheckEconomy(player, _config.TPR.Bypass, True);
                        PrintMsgL(player, "TPRCooldownBypass", _config.TPR.Bypass);
                        if (payalso)
                        {
                            PrintMsgL(player, "PayToTPR", _config.TPR.Pay);
                        }
                    }
                    else
                    {
                        PrintMsgL(player, "TPRCooldownBypassF", _config.TPR.Bypass);
                        return;
                    }
                }
                else if (UseEconomy())
                {
                    var remain = cooldown - (timestamp - tprData.Timestamp);
                    PrintMsgL(player, "TPRCooldown", FormatTime(remain));
                    if (_config.TPR.Bypass > 0 && _config.Settings.BypassCMD != null)
                    {
                        PrintMsgL(player, "TPRCooldownBypassP", _config.TPR.Bypass);
                        if (payalso)
                        {
                            PrintMsgL(player, "PayToTPR", _config.TPR.Pay);
                        }
                        PrintMsgL(player, "TPRCooldownBypassP2a", _config.Settings.BypassCMD);
                    }
                    return;
                }
                else
                {
                    var remain = cooldown - (timestamp - tprData.Timestamp);
                    PrintMsgL(player, "TPRCooldown", FormatTime(remain));
                    return;
                }
            }
            var limit = GetHigher(player, _config.TPR.VIPDailyLimits, _config.TPR.DailyLimit);
            if (limit > 0 && tprData.Amount >= limit)
            {
                PrintMsgL(player, "TPRLimitReached", limit);
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPending");
                return;
            }
            if (TeleportTimers.ContainsKey(target.userID))
            {
                PrintMsgL(player, "TeleportPendingTarget");
                return;
            }
            if (PlayersRequests.ContainsKey(player.userID))
            {
                PrintMsgL(player, "PendingRequest");
                return;
            }
            if (PlayersRequests.ContainsKey(target.userID))
            {
                PrintMsgL(player, "PendingRequestTarget");
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            err = CanPlayerTeleport(target);
            if (err != null)
            {
                PrintMsgL(player, "TPRTarget");
                return;
            }
            err = CheckItems(player);
            if (err != null)
            {
                PrintMsgL(player, "TPBlockedItem", err);
                return;
            }

            PlayersRequests[player.userID] = target;
            PlayersRequests[target.userID] = player;
            PendingRequests[target.userID] = timer.Once(_config.TPR.RequestDuration, () => { RequestTimedOut(player, target); });
            PrintMsgL(player, "Request", target.displayName);
            PrintMsgL(target, "RequestTarget", player.displayName);
            Interface.CallHook("OnTeleportRequested", target, player);
        }

        private void CommandTeleportAccept(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !_config.Settings.TPREnabled) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPA");
                return;
            }
            Timer reqTimer;
            if (!PendingRequests.TryGetValue(player.userID, out reqTimer))
            {
                PrintMsgL(player, "NoPendingRequest");
                return;
            }
#if DEBUG
            Puts("Calling CheckPlayer from cmdChatTeleportAccept");
#endif
            var err = CheckPlayer(player, False, CanCraftTPR(player), False, "tpa");
            if (err != null)
            {
                PrintMsgL(player, err);
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            var originPlayer = PlayersRequests[player.userID];
            err = CheckTargetLocation(originPlayer, player.transform.position, _config.TPR.UsableIntoBuildingBlocked, _config.TPR.CupOwnerAllowOnBuildingBlocked);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            if (_config.TPR.BlockTPAOnCeiling)
            {
                List<BuildingBlock> entities = GetFloor(player.transform.position);
                if (entities.Count > 0)
                {
                    PrintMsgL(player, "AcceptOnRoof");
                    return;
                }
            }
            var countdown = GetLower(originPlayer, _config.TPR.VIPCountdowns, _config.TPR.Countdown);
            PrintMsgL(originPlayer, "Accept", player.displayName, countdown);
            PrintMsgL(player, "AcceptTarget", originPlayer.displayName);
            var timestamp = Facepunch.Math.Epoch.Current;
            TeleportTimers[originPlayer.userID] = new TeleportTimer
            {
                OriginPlayer = originPlayer,
                TargetPlayer = player,
                Timer = timer.Once(countdown, () =>
                {
#if DEBUG
                    Puts("Calling CheckPlayer from cmdChatTeleportAccept timer loop");
#endif
                    err = CheckPlayer(originPlayer, _config.TPR.UsableOutOfBuildingBlocked, CanCraftTPR(originPlayer), True, "tpa") ?? CheckPlayer(player, False, CanCraftTPR(player), True, "tpa");
                    if (err != null)
                    {
                        PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                        PrintMsgL(originPlayer, "Interrupted");
                        PrintMsgL(originPlayer, err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    err = CheckTargetLocation(originPlayer, player.transform.position, _config.TPR.UsableIntoBuildingBlocked, _config.TPR.CupOwnerAllowOnBuildingBlocked);
                    if (err != null)
                    {
                        SendReply(player, err);
                        PrintMsgL(originPlayer, "Interrupted");
                        SendReply(originPlayer, err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    err = CanPlayerTeleport(originPlayer) ?? CanPlayerTeleport(player);
                    if (err != null)
                    {
                        SendReply(player, err);
                        PrintMsgL(originPlayer, "Interrupted");
                        SendReply(originPlayer, err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    err = CheckItems(originPlayer);
                    if (err != null)
                    {
                        PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                        PrintMsgL(originPlayer, "Interrupted");
                        PrintMsgL(originPlayer, "TPBlockedItem", err);
                        TeleportTimers.Remove(originPlayer.userID);
                        return;
                    }
                    if (UseEconomy())
                    {
                        if (_config.TPR.Pay > 0)
                        {
                            if (!CheckEconomy(originPlayer, _config.TPR.Pay))
                            {
                                PrintMsgL(player, "InterruptedTarget", originPlayer.displayName);
                                PrintMsgL(originPlayer, "TPNoMoney", _config.TPR.Pay);
                                TeleportTimers.Remove(originPlayer.userID);
                                return;
                            }
                            else
                            {
                                CheckEconomy(originPlayer, _config.TPR.Pay, True);
                                PrintMsgL(originPlayer, "TPMoney", (double)_config.TPR.Pay);
                            }
                        }
                    }
                    Teleport(originPlayer, CheckPosition(player.transform.position), _config.TPR.AllowTPB);
                    var tprData = TPR[originPlayer.userID];
                    tprData.Amount++;
                    tprData.Timestamp = timestamp;
                    changedTPR = True;
                    PrintMsgL(player, "SuccessTarget", originPlayer.displayName);
                    PrintMsgL(originPlayer, "Success", player.displayName);
                    var limit = GetHigher(player, _config.TPR.VIPDailyLimits, _config.TPR.DailyLimit);
                    if (limit > 0) PrintMsgL(originPlayer, "TPRAmount", limit - tprData.Amount);
                    TeleportTimers.Remove(originPlayer.userID);
                })
            };
            reqTimer.Destroy();
            PendingRequests.Remove(player.userID);
            PlayersRequests.Remove(player.userID);
            PlayersRequests.Remove(originPlayer.userID);
        }

        private void CommandWipeHomes(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player || !IsAllowedMsg(player, PermWipeHomes)) return;
            Home.Clear();
            changedHome = True;
            PrintMsgL(player, "HomesListWiped");
        }

        private void CommandTeleportHelp(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player) return;
            if (!_config.Settings.HomesEnabled && !_config.Settings.TPREnabled && !IsAllowedMsg(player)) return;
            if (args.Length == 1)
            {
                var key = $"TPHelp{args[0].ToLower()}";
                var msg = _(key, player);
                if (key.Equals(msg))
                    PrintMsgL(player, "InvalidHelpModule");
                else
                    PrintMsg(player, msg);
            }
            else
            {
                var msg = _("TPHelpGeneral", player);
                if (IsAllowed(player))
                    msg += NewLine + "/tphelp AdminTP";
                if (_config.Settings.HomesEnabled)
                    msg += NewLine + "/tphelp Home";
                if (_config.Settings.TPREnabled)
                    msg += NewLine + "/tphelp TPR";
                PrintMsg(player, msg);
            }
        }

        private void CommandTeleportInfo(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (!_config.Settings.HomesEnabled && !_config.Settings.TPREnabled && !_config.Settings.TownEnabled) return;
            var player = p.Object as BasePlayer;
            if (!player) return;
            if (args.Length == 1)
            {
                var module = args[0].ToLower();
                var msg = _($"TPSettings{module}", player);
                var timestamp = Facepunch.Math.Epoch.Current;
                var currentDate = DateTime.Now.ToString("d");
                TeleportData teleportData;
                int limit;
                int cooldown;
                switch (module)
                {
                    case "home":
                        limit = GetHigher(player, _config.Home.VIPDailyLimits, _config.Home.DailyLimit);
                        cooldown = GetLower(player, _config.Home.VIPCooldowns, _config.Home.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player), GetHigher(player, _config.Home.VIPHomesLimits, _config.Home.HomesLimit)));
                        HomeData homeData;
                        if (!Home.TryGetValue(player.userID, out homeData))
                            Home[player.userID] = homeData = new HomeData();
                        if (homeData.Teleports.Date != currentDate)
                        {
                            homeData.Teleports.Amount = 0;
                            homeData.Teleports.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "HomeTPAmount", limit - homeData.Teleports.Amount);
                        if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - homeData.Teleports.Timestamp);
                            PrintMsgL(player, "HomeTPCooldown", FormatTime(remain));
                        }
                        break;
                    case "tpr":
                        limit = GetHigher(player, _config.TPR.VIPDailyLimits, _config.TPR.DailyLimit);
                        cooldown = GetLower(player, _config.TPR.VIPCooldowns, _config.TPR.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        if (!TPR.TryGetValue(player.userID, out teleportData))
                            TPR[player.userID] = teleportData = new TeleportData();
                        if (teleportData.Date != currentDate)
                        {
                            teleportData.Amount = 0;
                            teleportData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "TPRAmount", limit - teleportData.Amount);
                        if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "TPRCooldown", FormatTime(remain));
                        }
                        break;
                    case "town":
                        limit = GetHigher(player, _config.Town.VIPDailyLimits, _config.Town.DailyLimit);
                        cooldown = GetLower(player, _config.Town.VIPCooldowns, _config.Town.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        if (!Town.TryGetValue(player.userID, out teleportData))
                            Town[player.userID] = teleportData = new TeleportData();
                        if (teleportData.Date != currentDate)
                        {
                            teleportData.Amount = 0;
                            teleportData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "TownTPAmount", limit - teleportData.Amount);
                        if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "TownTPCooldown", FormatTime(remain));
                            PrintMsgL(player, "TownTPCooldownBypassP", _config.Town.Bypass);
                            PrintMsgL(player, "TownTPCooldownBypassP2", _config.Settings.BypassCMD);
                        }
                        break;
                    case "outpost":
                        limit = GetHigher(player, _config.Outpost.VIPDailyLimits, _config.Outpost.DailyLimit);
                        cooldown = GetLower(player, _config.Outpost.VIPCooldowns, _config.Outpost.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        if (!Outpost.TryGetValue(player.userID, out teleportData))
                            Outpost[player.userID] = teleportData = new TeleportData();
                        if (teleportData.Date != currentDate)
                        {
                            teleportData.Amount = 0;
                            teleportData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "OutpostTPAmount", limit - teleportData.Amount);
                        if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "OutpostTPCooldown", FormatTime(remain));
                            PrintMsgL(player, "OutpostTPCooldownBypassP", _config.Outpost.Bypass);
                            PrintMsgL(player, "OutpostTPCooldownBypassP2", _config.Settings.BypassCMD);
                        }
                        break;
                    case "bandit":
                        limit = GetHigher(player, _config.Bandit.VIPDailyLimits, _config.Bandit.DailyLimit);
                        cooldown = GetLower(player, _config.Bandit.VIPCooldowns, _config.Bandit.Cooldown);
                        PrintMsg(player, string.Format(msg, FormatTime(cooldown), limit > 0 ? limit.ToString() : _("Unlimited", player)));
                        if (!Bandit.TryGetValue(player.userID, out teleportData))
                            Bandit[player.userID] = teleportData = new TeleportData();
                        if (teleportData.Date != currentDate)
                        {
                            teleportData.Amount = 0;
                            teleportData.Date = currentDate;
                        }
                        if (limit > 0) PrintMsgL(player, "BanditTPAmount", limit - teleportData.Amount);
                        if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
                        {
                            var remain = cooldown - (timestamp - teleportData.Timestamp);
                            PrintMsgL(player, "BanditTPCooldown", FormatTime(remain));
                            PrintMsgL(player, "BanditTPCooldownBypassP", _config.Bandit.Bypass);
                            PrintMsgL(player, "BanditTPCooldownBypassP2", _config.Settings.BypassCMD);
                        }
                        break;
                    default:
                        PrintMsgL(player, "InvalidHelpModule");
                        break;
                }
            }
            else
            {
                var msg = _("TPInfoGeneral", player);
                if (_config.Settings.HomesEnabled)
                    msg += NewLine + "/tpinfo Home";
                if (_config.Settings.TPREnabled)
                    msg += NewLine + "/tpinfo TPR";
                if (_config.Settings.TownEnabled)
                    msg += NewLine + "/tpinfo Town";
                if (_config.Settings.OutpostEnabled)
                    msg += NewLine + "/tpinfo Outpost";
                if (_config.Settings.BanditEnabled)
                    msg += NewLine + "/tpinfo Bandit";
                PrintMsgL(player, msg);
            }
        }

        private void CommandTeleportCancel(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (!_config.Settings.TPREnabled) return;
            var player = p.Object as BasePlayer;
            if (!player) return;
            if (args.Length != 0)
            {
                PrintMsgL(player, "SyntaxCommandTPC");
                return;
            }
            TeleportTimer teleportTimer;
            if (TeleportTimers.TryGetValue(player.userID, out teleportTimer))
            {
                teleportTimer.Timer?.Destroy();
                PrintMsgL(player, "TPCancelled");
                PrintMsgL(teleportTimer.TargetPlayer, "TPCancelledTarget", player.displayName);
                TeleportTimers.Remove(player.userID);
                return;
            }
            foreach (var keyValuePair in TeleportTimers)
            {
                if (keyValuePair.Value.TargetPlayer != player) continue;
                keyValuePair.Value.Timer?.Destroy();
                PrintMsgL(keyValuePair.Value.OriginPlayer, "TPCancelledTarget", player.displayName);
                PrintMsgL(player, "TPYouCancelledTarget", keyValuePair.Value.OriginPlayer.displayName);
                TeleportTimers.Remove(keyValuePair.Key);
                return;
            }
            BasePlayer target;
            if (!PlayersRequests.TryGetValue(player.userID, out target))
            {
                PrintMsgL(player, "NoPendingRequest");
                return;
            }
            Timer reqTimer;
            if (PendingRequests.TryGetValue(player.userID, out reqTimer))
            {
                reqTimer.Destroy();
                PendingRequests.Remove(player.userID);
            }
            else if (PendingRequests.TryGetValue(target.userID, out reqTimer))
            {
                reqTimer.Destroy();
                PendingRequests.Remove(target.userID);
                var temp = player;
                player = target;
                target = temp;
            }
            PlayersRequests.Remove(target.userID);
            PlayersRequests.Remove(player.userID);
            PrintMsgL(player, "Cancelled", target.displayName);
            PrintMsgL(target, "CancelledTarget", player.displayName);
        }

        private void CommandOutpost(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (_config.Settings.OutpostEnabled)
            {
                CommandTown(p, "outpost", args);
            }
        }

        private void CommandBandit(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            if (_config.Settings.BanditEnabled)
            {
                CommandTown(p, "bandit", args);
            }
        }

        private void CommandTown(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (!player) return;
#if DEBUG
            Puts($"cmdChatTown: command={command}");
#endif
            switch (command)
            {
                case "outpost":
                    if (!IsAllowedMsg(player, PermTpOutpost)) return;
                    break;
                case "bandit":
                    if (!IsAllowedMsg(player, PermTpBandit)) return;
                    break;
                case "town":
                default:
                    if (!IsAllowedMsg(player, PermTpTown)) return;
                    break;
            }

            // For admin using set command
            if (args.Length == 1 && IsAllowed(player) && args[0].ToLower().Equals("set"))
            {
                switch (command)
                {
                    case "outpost":
                        _config.Outpost.Location = player.transform.position;
                        SaveConfig();
                        PrintMsgL(player, "OutpostTPLocation", _config.Outpost.Location);
                        break;
                    case "bandit":
                        _config.Bandit.Location = player.transform.position;
                        SaveConfig();
                        PrintMsgL(player, "BanditTPLocation", _config.Bandit.Location);
                        break;
                    case "town":
                    default:
                        _config.Town.Location = player.transform.position;
                        SaveConfig();
                        PrintMsgL(player, "TownTPLocation", _config.Town.Location);
                        break;
                }
                return;
            }

            bool paidmoney = False;

            // Is outpost/bandit/town usage enabled?
            if (!_config.Settings.OutpostEnabled && command == "outpost")
            {
                PrintMsgL(player, "OutpostTPDisabled");
                return;
            }
            else if (!_config.Settings.BanditEnabled && command == "bandit")
            {
                PrintMsgL(player, "BanditTPDisabled");
                return;
            }
            else if (!_config.Settings.TownEnabled && command == "town")
            {
                PrintMsgL(player, "TownTPDisabled");
                return;
            }

            // Are they trying to bypass cooldown or did they just type something else?
            if (args.Length == 1 && (args[0].ToLower() != _config.Settings.BypassCMD.ToLower()))
            {
                string com = command == null ? "town" : command;
                string msg = "SyntaxCommand" + char.ToUpper(com[0]) + com.Substring(1);
                PrintMsgL(player, msg);
                if (IsAllowed(player)) PrintMsgL(player, msg + "Admin");
                return;
            }

            // Is outpost/bandit/town location set?
            if (_config.Outpost.Location == Zero && command == "outpost")
            {
                PrintMsgL(player, "OutpostTPNotSet");
                return;
            }
            else if (_config.Bandit.Location == Zero && command == "bandit")
            {
                PrintMsgL(player, "BanditTPNotSet");
                return;
            }
            else if (_config.Town.Location == Zero && command == "town")
            {
                PrintMsgL(player, "TownTPNotSet");
                return;
            }

            TeleportData teleportData = new TeleportData();
            var timestamp = Facepunch.Math.Epoch.Current;
            var currentDate = DateTime.Now.ToString("d");

            string err = null;
            int cooldown = 0;
            int limit = 0;
            int targetPay = 0;
            int targetBypass = 0;
            string msgPay = null;
            string msgCooldown = null;
            string msgCooldownBypass = null;
            string msgCooldownBypassF = null;
            string msgCooldownBypassP = null;
            string msgCooldownBypassP2 = null;
            string msgLimitReached = null;
#if DEBUG
            Puts("Calling CheckPlayer from cmdChatTown");
#endif
            // Setup vars for checks below
            switch (command)
            {
                case "outpost":
                    err = CheckPlayer(player, _config.Outpost.UsableOutOfBuildingBlocked, CanCraftOutpost(player), True, "outpost");
                    if (err != null)
                    {
                        PrintMsgL(player, err);
                        if (err == "TPHostile")
                        {
                            string pt = ((int)Math.Abs(player.unHostileTime - Time.realtimeSinceStartup) / 60).ToString();
                            PrintMsgL(player, "HostileTimer", pt);
                        }
                        return;
                    }
                    cooldown = GetLower(player, _config.Outpost.VIPCooldowns, _config.Outpost.Cooldown);
                    if (!Outpost.TryGetValue(player.userID, out teleportData))
                    {
                        Outpost[player.userID] = teleportData = new TeleportData();
                    }
                    if (teleportData.Date != currentDate)
                    {
                        teleportData.Amount = 0;
                        teleportData.Date = currentDate;
                    }

                    targetPay = _config.Outpost.Pay;
                    targetBypass = _config.Outpost.Bypass;

                    msgPay = "PayToOutpost";
                    msgCooldown = "OutpostTPCooldown";
                    msgCooldownBypass = "OutpostTPCooldownBypass";
                    msgCooldownBypassF = "OutpostTPCooldownBypassF";
                    msgCooldownBypassP = "OutpostTPCooldownBypassP";
                    msgCooldownBypassP2 = "OutpostTPCooldownBypassP2";
                    msgLimitReached = "OutpostTPLimitReached";
                    limit = GetHigher(player, _config.Outpost.VIPDailyLimits, _config.Outpost.DailyLimit);
                    break;
                case "bandit":
                    err = CheckPlayer(player, _config.Bandit.UsableOutOfBuildingBlocked, CanCraftBandit(player), True, "bandit");
                    if (err != null)
                    {
                        PrintMsgL(player, err);
                        if (err == "TPHostile")
                        {
                            var pc = player as BaseCombatEntity;
                            string pt = ((int)Math.Abs(pc.unHostileTime - Time.realtimeSinceStartup) / 60).ToString();
                            PrintMsgL(player, "HostileTimer", pt);
                        }
                        return;
                    }
                    cooldown = GetLower(player, _config.Bandit.VIPCooldowns, _config.Bandit.Cooldown);
                    if (!Bandit.TryGetValue(player.userID, out teleportData))
                    {
                        Bandit[player.userID] = teleportData = new TeleportData();
                    }
                    if (teleportData.Date != currentDate)
                    {
                        teleportData.Amount = 0;
                        teleportData.Date = currentDate;
                    }
                    targetPay = _config.Bandit.Pay;
                    targetBypass = _config.Bandit.Bypass;

                    msgPay = "PayToBandit";
                    msgCooldown = "BanditTPCooldown";
                    msgCooldownBypass = "BanditTPCooldownBypass";
                    msgCooldownBypassF = "BanditTPCooldownBypassF";
                    msgCooldownBypassP = "BanditTPCooldownBypassP";
                    msgCooldownBypassP2 = "BanditTPCooldownBypassP2";
                    msgLimitReached = "BanditTPLimitReached";
                    limit = GetHigher(player, _config.Bandit.VIPDailyLimits, _config.Bandit.DailyLimit);
                    break;
                case "town":
                default:
                    err = CheckPlayer(player, _config.Town.UsableOutOfBuildingBlocked, CanCraftTown(player), True, "town");
                    if (err != null)
                    {
                        PrintMsgL(player, err);
                        return;
                    }
                    cooldown = GetLower(player, _config.Town.VIPCooldowns, _config.Town.Cooldown);
                    if (!Town.TryGetValue(player.userID, out teleportData))
                    {
                        Town[player.userID] = teleportData = new TeleportData();
                    }
                    if (teleportData.Date != currentDate)
                    {
                        teleportData.Amount = 0;
                        teleportData.Date = currentDate;
                    }
                    targetPay = _config.Town.Pay;
                    targetBypass = _config.Town.Bypass;

                    msgPay = "PayToTown";
                    msgCooldown = "TownTPCooldown";
                    msgCooldownBypass = "TownTPCooldownBypass";
                    msgCooldownBypassF = "TownTPCooldownBypassF";
                    msgCooldownBypassP = "TownTPCooldownBypassP";
                    msgCooldownBypassP2 = "TownTPCooldownBypassP2";
                    msgLimitReached = "TownTPLimitReached";
                    limit = GetHigher(player, _config.Town.VIPDailyLimits, _config.Town.DailyLimit);
                    break;
            }

            // Check and process cooldown, bypass, and payment for all modes
            if (cooldown > 0 && timestamp - teleportData.Timestamp < cooldown)
            {
                var cmdSent = "";
                bool foundmoney = CheckEconomy(player, targetBypass);
                try
                {
                    cmdSent = args[0].ToLower();
                }
                catch { }

                bool payalso = False;
                if (targetPay > 0)
                {
                    payalso = True;
                }
                if ((_config.Settings.BypassCMD != null) && (cmdSent == _config.Settings.BypassCMD.ToLower()))
                {
                    if (foundmoney)
                    {
                        CheckEconomy(player, targetBypass, True);
                        paidmoney = True;
                        PrintMsgL(player, msgCooldownBypass, targetBypass);
                        if (payalso)
                        {
                            PrintMsgL(player, msgPay, targetPay);
                        }
                    }
                    else
                    {
                        PrintMsgL(player, msgCooldownBypassF, targetBypass);
                        return;
                    }
                }
                else if (UseEconomy())
                {
                    var remain = cooldown - (timestamp - teleportData.Timestamp);
                    PrintMsgL(player, msgCooldown, FormatTime(remain));
                    if (targetBypass > 0 && _config.Settings.BypassCMD != null)
                    {
                        PrintMsgL(player, msgCooldownBypassP, targetBypass);
                        PrintMsgL(player, msgCooldownBypassP2, _config.Settings.BypassCMD);
                    }
                    return;
                }
                else
                {
                    var remain = cooldown - (timestamp - teleportData.Timestamp);
                    PrintMsgL(player, msgCooldown, FormatTime(remain));
                    return;
                }
            }

            if (limit > 0 && teleportData.Amount >= limit)
            {
                PrintMsgL(player, msgLimitReached, limit);
                return;
            }
            if (TeleportTimers.ContainsKey(player.userID))
            {
                PrintMsgL(player, "TeleportPending");
                return;
            }
            err = CanPlayerTeleport(player);
            if (err != null)
            {
                SendReply(player, err);
                return;
            }
            err = CheckItems(player);
            if (err != null)
            {
                PrintMsgL(player, "TPBlockedItem", err);
                return;
            }

            int countdown = 0;
            switch (command)
            {
                case "outpost":
                    countdown = GetLower(player, _config.Outpost.VIPCountdowns, _config.Outpost.Countdown);
                    TeleportTimers[player.userID] = new TeleportTimer
                    {
                        OriginPlayer = player,
                        Timer = timer.Once(countdown, () =>
                        {
#if DEBUG
                            Puts("Calling CheckPlayer from cmdChatTown outpost timer loop");
#endif
                            err = CheckPlayer(player, _config.Outpost.UsableOutOfBuildingBlocked, CanCraftOutpost(player), True, "outpost");
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Outpost.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            err = CanPlayerTeleport(player);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Outpost.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            err = CheckItems(player);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, "TPBlockedItem", err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Outpost.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            if (UseEconomy())
                            {
                                if (_config.Outpost.Pay > 0 && !CheckEconomy(player, _config.Outpost.Pay))
                                {
                                    PrintMsgL(player, "Interrupted");
                                    PrintMsgL(player, "TPNoMoney", _config.Outpost.Pay);
                                    TeleportTimers.Remove(player.userID);
                                    return;
                                }
                                else if (_config.Outpost.Pay > 0)
                                {
                                    CheckEconomy(player, _config.Outpost.Pay, True);
                                    PrintMsgL(player, "TPMoney", (double)_config.Outpost.Pay);
                                }
                            }
                            Teleport(player, _config.Outpost.Location);
                            teleportData.Amount++;
                            teleportData.Timestamp = timestamp;

                            changedOutpost = True;
                            PrintMsgL(player, "OutpostTP");
                            if (limit > 0) PrintMsgL(player, "OutpostTPAmount", limit - teleportData.Amount);
                            TeleportTimers.Remove(player.userID);
                        })
                    };
                    PrintMsgL(player, "OutpostTPStarted", countdown);
                    break;
                case "bandit":
                    countdown = GetLower(player, _config.Bandit.VIPCountdowns, _config.Bandit.Countdown);
                    TeleportTimers[player.userID] = new TeleportTimer
                    {
                        OriginPlayer = player,
                        Timer = timer.Once(countdown, () =>
                        {
#if DEBUG
                            Puts("Calling CheckPlayer from cmdChatTown bandit timer loop");
#endif
                            err = CheckPlayer(player, _config.Bandit.UsableOutOfBuildingBlocked, CanCraftBandit(player), True, "bandit");
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Bandit.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            err = CanPlayerTeleport(player);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Bandit.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            err = CheckItems(player);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, "TPBlockedItem", err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Bandit.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            if (UseEconomy())
                            {
                                if (_config.Bandit.Pay > 0 && !CheckEconomy(player, _config.Bandit.Pay))
                                {
                                    PrintMsgL(player, "Interrupted");
                                    PrintMsgL(player, "TPNoMoney", _config.Bandit.Pay);
                                    TeleportTimers.Remove(player.userID);
                                    return;
                                }
                                else if (_config.Bandit.Pay > 0)
                                {
                                    CheckEconomy(player, _config.Bandit.Pay, True);
                                    PrintMsgL(player, "TPMoney", (double)_config.Bandit.Pay);
                                }
                            }
                            Teleport(player, _config.Bandit.Location);
                            teleportData.Amount++;
                            teleportData.Timestamp = timestamp;

                            changedBandit = True;
                            PrintMsgL(player, "BanditTP");
                            if (limit > 0) PrintMsgL(player, "BanditTPAmount", limit - teleportData.Amount);
                            TeleportTimers.Remove(player.userID);
                        })
                    };
                    PrintMsgL(player, "BanditTPStarted", countdown);
                    break;
                case "town":
                default:
                    countdown = GetLower(player, _config.Town.VIPCountdowns, _config.Town.Countdown);
                    TeleportTimers[player.userID] = new TeleportTimer
                    {
                        OriginPlayer = player,
                        Timer = timer.Once(countdown, () =>
                        {
#if DEBUG
                            Puts("Calling CheckPlayer from cmdChatTown town timer loop");
#endif
                            err = CheckPlayer(player, _config.Town.UsableOutOfBuildingBlocked, CanCraftTown(player), True, "town");
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Town.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            err = CanPlayerTeleport(player);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Town.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            err = CheckItems(player);
                            if (err != null)
                            {
                                PrintMsgL(player, "Interrupted");
                                PrintMsgL(player, "TPBlockedItem", err);
                                if (paidmoney)
                                {
                                    paidmoney = False;
                                    CheckEconomy(player, _config.Town.Bypass, False, True);
                                }
                                TeleportTimers.Remove(player.userID);
                                return;
                            }
                            if (UseEconomy())
                            {
                                if (_config.Town.Pay > 0 && !CheckEconomy(player, _config.Town.Pay))
                                {
                                    PrintMsgL(player, "Interrupted");
                                    PrintMsgL(player, "TPNoMoney", _config.Town.Pay);
                                    TeleportTimers.Remove(player.userID);
                                    return;
                                }
                                else if (_config.Town.Pay > 0)
                                {
                                    CheckEconomy(player, _config.Town.Pay, True);
                                    PrintMsgL(player, "TPMoney", (double)_config.Town.Pay);
                                }
                            }
                            Teleport(player, _config.Town.Location);
                            teleportData.Amount++;
                            teleportData.Timestamp = timestamp;

                            changedTown = True;
                            PrintMsgL(player, "TownTP");
                            if (limit > 0) PrintMsgL(player, "TownTPAmount", limit - teleportData.Amount);
                            TeleportTimers.Remove(player.userID);
                        })
                    };
                    PrintMsgL(player, "TownTPStarted", countdown);
                    break;
            }
        }

        private void CommandTeleportII(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;
            if (player != null && !IsAllowedMsg(player, PermTpConsole)) return;
            HashSet<BasePlayer> players;
            switch (command)
            {
                case "teleport.topos":
                    if (args.Length < 4)
                    {
                        p.Reply(_("SyntaxConsoleCommandToPos", player));
                        return;
                    }
                    players = FindPlayers(args[0]);
                    if (players.Count <= 0)
                    {
                        p.Reply(_("PlayerNotFound", player));
                        return;
                    }
                    if (players.Count > 1)
                    {
                        p.Reply(_("MultiplePlayers", player, string.Join(", ", players.Select(t => t.displayName).ToArray())));
                        return;
                    }
                    var targetPlayer = players.First();
                    float x;
                    if (!float.TryParse(args[1], out x)) x = -10000f;
                    float y;
                    if (!float.TryParse(args[2], out y)) y = -10000f;
                    float z;
                    if (!float.TryParse(args[3], out z)) z = -10000f;
                    if (!CheckBoundaries(x, y, z))
                    {
                        p.Reply(_("AdminTPOutOfBounds", player) + Environment.NewLine + _("AdminTPBoundaries", player, boundary));
                        return;
                    }
                    Teleport(targetPlayer, x, y, z);
                    if (_config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(targetPlayer, "AdminTPConsoleTP", targetPlayer.transform.position);
                    p.Reply(_("AdminTPTargetCoordinates", player, targetPlayer.displayName, targetPlayer.transform.position));
                    Puts(_("LogTeleportPlayer", null, player?.displayName, targetPlayer.displayName, targetPlayer.transform.position));
                    break;
                case "teleport.toplayer":
                    if (args.Length < 2)
                    {
                        p.Reply(_("SyntaxConsoleCommandToPlayer", player));
                        return;
                    }
                    players = FindPlayers(args[0]);
                    if (players.Count <= 0)
                    {
                        p.Reply(_("PlayerNotFound", player));
                        return;
                    }
                    if (players.Count > 1)
                    {
                        p.Reply(_("MultiplePlayers", player, string.Join(", ", players.Select(t => t.displayName).ToArray())));
                        return;
                    }
                    var originPlayer = players.First();
                    players = FindPlayers(args[1]);
                    if (players.Count <= 0)
                    {
                        p.Reply(_("PlayerNotFound", player));
                        return;
                    }
                    if (players.Count > 1)
                    {
                        p.Reply(_("MultiplePlayers", player, string.Join(", ", players.Select(t => t.displayName).ToArray())));
                        return;
                    }
                    targetPlayer = players.First();
                    if (targetPlayer == originPlayer)
                    {
                        p.Reply(_("CantTeleportPlayerToSelf", player));
                        return;
                    }
                    Teleport(originPlayer, targetPlayer);
                    p.Reply(_("AdminTPPlayers", player, originPlayer.displayName, targetPlayer.displayName));
                    PrintMsgL(originPlayer, "AdminTPConsoleTPPlayer", targetPlayer.displayName);
                    if (_config.Admin.AnnounceTeleportToTarget)
                        PrintMsgL(targetPlayer, "AdminTPConsoleTPPlayerTarget", originPlayer.displayName);
                    Puts(_("LogTeleportPlayer", null, player?.displayName, originPlayer.displayName, targetPlayer.displayName));
                    break;
            }
        }

        float GetMonumentFloat(string monumentName)
        {
            string name = monumentName.Contains(":") ? monumentName.Substring(0, monumentName.LastIndexOf(":")) : monumentName.TrimEnd();

            switch (name)
            {
                case "Abandoned Cabins":
                    return 24f + 30f;
                case "Abandoned Supermarket":
                    return 50f;
                case "Airfield":
                    return 200f;
                case "Bandit Camp":
                    return 100f + 25f;
                case "Giant Excavator Pit":
                    return 200f + 25f;
                case "Harbor":
                    return 100f + 50f;
                case "HQM Quarry":
                    return 27.5f + 10f;
                case "Large Oil Rig":
                    return 200f;
                case "Launch Site":
                    return 200f + 100f;
                case "Lighthouse":
                    return 24f + 24f;
                case "Military Tunnel":
                    return 100f;
                case "Mining Outpost":
                    return 25f + 15f;
                case "Oil Rig":
                    return 100f;
                case "Outpost":
                    return 100f + 25f;
                case "Oxum's Gas Station":
                    return 50f + 15f;
                case "Power Plant":
                    return 100f + 40f;
                case "power_sub_small_1":
                case "power_sub_small_2":
                case "power_sub_big_1":
                case "power_sub_big_2":
                    return 30f;
                case "Satellite Dish":
                    return 75f + 15f;
                case "Sewer Branch":
                    return 75f + 25f;
                case "Stone Quarry":
                    return 27.5f;
                case "Sulfur Quarry":
                    return 27.5f;
                case "The Dome":
                    return 50f + 20f;
                case "Train Yard":
                    return 100 + 50f;
                case "Water Treatment Plant":
                    return 100f + 85f;
                case "Water Well":
                    return 24f;
                case "Wild Swamp":
                    return 24f;
            }

            return _config.Settings.DefaultMonumentSize;
        }

        private void CommandSphereMonuments(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p?.Object as BasePlayer;
            if (!player || !player.IsAdmin) return;

            foreach (var monument in monuments)
            {
                string name = monument.Key.Contains(":") ? monument.Key.Substring(0, monument.Key.LastIndexOf(":")) : monument.Key.TrimEnd();

                player.SendConsoleCommand("ddraw.sphere", 30f, Color.red, monument.Value.Position, GetMonumentFloat(name));
                player.SendConsoleCommand("ddraw.text", 30f, Color.blue, monument.Value.Position, name);
            }

            /*var dict = new SortedDictionary<string, Vector3>();

            foreach (var monument in UnityEngine.Object.FindObjectsOfType<MonumentInfo>())
            {
                if (string.IsNullOrEmpty(monument.displayPhrase.english)) continue;
                dict[monument.displayPhrase.english] = monument.Bounds.extents;
            }

            dict.OrderBy(entry => entry.Key);

            foreach(var entry in dict)
            {
                Puts("{0} : {1}", entry.Key, entry.Value);
            }*/
        }

        private void CommandImportHomes(IPlayer p, string command, string[] args)
        {
            if (storedData.DisabledCommands.Contains(command.ToLower())) { p.Reply("Disabled command."); return; }
            var player = p.Object as BasePlayer;

            if (player != null && !IsAllowedMsg(player, PermImportHomes))
            {
                p.Reply(_("NotAllowed", player));
                return;
            }
            var datafile = Interface.Oxide.DataFileSystem.GetFile("m-Teleportation");
            if (!datafile.Exists())
            {
                p.Reply("No m-Teleportation.json exists.");
                return;
            }
            datafile.Load();
            var allHomeData = datafile["HomeData"] as Dictionary<string, object>;
            if (allHomeData == null)
            {
                p.Reply(_("HomeListEmpty", player));
                return;
            }
            var count = 0;
            foreach (var kvp in allHomeData)
            {
                var homeDataOld = kvp.Value as Dictionary<string, object>;
                if (homeDataOld == null) continue;
                if (!homeDataOld.ContainsKey("HomeLocations")) continue;
                var homeList = homeDataOld["HomeLocations"] as Dictionary<string, object>;
                if (homeList == null) continue;
                var userId = Convert.ToUInt64(kvp.Key);
                HomeData homeData;
                if (!Home.TryGetValue(userId, out homeData))
                    Home[userId] = homeData = new HomeData();
                foreach (var kvp2 in homeList)
                {
                    var positionData = kvp2.Value as Dictionary<string, object>;
                    if (positionData == null) continue;
                    if (!positionData.ContainsKey("x") || !positionData.ContainsKey("y") || !positionData.ContainsKey("z")) continue;
                    var position = new Vector3(Convert.ToSingle(positionData["x"]), Convert.ToSingle(positionData["y"]), Convert.ToSingle(positionData["z"]));
                    homeData.Locations[kvp2.Key] = position;
                    changedHome = True;
                    count++;
                }
            }
            p.Reply(string.Format("Imported {0} homes.", count));
        }

        private void RequestTimedOut(BasePlayer player, BasePlayer target)
        {
            PlayersRequests.Remove(player.userID);
            PlayersRequests.Remove(target.userID);
            PendingRequests.Remove(target.userID);
            PrintMsgL(player, "TimedOut", target.displayName);
            PrintMsgL(target, "TimedOutTarget", player.displayName);
        }

#region Util
        private string FormatTime(long seconds)
        {
            var timespan = TimeSpan.FromSeconds(seconds);
            return string.Format(timespan.TotalHours >= 1 ? "{2:00}:{0:00}:{1:00}" : "{0:00}:{1:00}", timespan.Minutes, timespan.Seconds, System.Math.Floor(timespan.TotalHours));
        }

        private double ConvertToRadians(double angle)
        {
            return System.Math.PI / 180 * angle;
        }
#endregion

#region Teleport
        public void Teleport(BasePlayer player, BasePlayer target) => Teleport(player, target.transform.position);

        public void Teleport(BasePlayer player, float x, float y, float z) => Teleport(player, new Vector3(x, y, z));

        public void Teleport(BasePlayer player, Vector3 position, bool save = True)
        {
            if (save) SaveLocation(player);
            if (!teleporting.ContainsKey(player.userID))
                teleporting.Add(player.userID, position);
            else teleporting[player.userID] = position;

            try
            {
                player.EnsureDismounted(); // 1.1.2 @Def

                if (player.HasParent())
                {
                    player.SetParent(null, True, True);
                }

                if (player.IsConnected) // 1.1.2 @Def
                {
                    player.EndLooting();
                    StartSleeping(player);
                }

                player.RemoveFromTriggers(); // 1.1.2 @Def recommendation to use natural method for issue with triggers
                player.EnableServerFall(True); // redundant, in OnEntityTakeDamage hook
                player.Teleport(position); // 1.1.6

                if (player.IsConnected && !Network.Net.sv.visibility.IsInside(player.net.group, position))
                {
                    player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, True);
                    player.ClientRPCPlayer(null, player, "StartLoading");
                    player.UpdateNetworkGroup(); // 1.1.1 building fix @ctv
                    player.SendEntityUpdate();
                    player.SendNetworkUpdateImmediate(False);
                }
            }
            finally
            {
                player.EnableServerFall(False);
                player.ForceUpdateTriggers(); // 1.1.4 exploit fix for looting sleepers in safe zones
            }
        }

        public void StartSleeping(BasePlayer player) // custom as to not cancel crafting, or remove player from vanish
        {
            if (!player.IsSleeping())
            {
                Interface.CallHook("OnPlayerSleep", this);
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, True);
                player.sleepStartTime = Time.time;
                BasePlayer.sleepingPlayerList.Add(player);
                BasePlayer.bots.Remove(player);
                player.CancelInvoke("InventoryUpdate");
                player.CancelInvoke("TeamUpdate");
            }
        }
#endregion

#region Checks
        // Used by tpa only to provide for offset from the target to avoid overlap
        private Vector3 CheckPosition(Vector3 position)
        {
            var hits = Physics.OverlapSphere(position, 2, blockLayer);
            var distance = 5f;
            BuildingBlock buildingBlock = null;
            for (var i = 0; i < hits.Length; i++)
            {
                var block = hits[i].GetComponentInParent<BuildingBlock>();
                if (block == null) continue;
                var prefab = block.PrefabName;
                if (!prefab.Contains("foundation", CompareOptions.OrdinalIgnoreCase) && !prefab.Contains("floor", CompareOptions.OrdinalIgnoreCase) && !prefab.Contains("pillar", CompareOptions.OrdinalIgnoreCase)) continue;
                if (!((block.transform.position - position).magnitude < distance)) continue;
                buildingBlock = block;
                distance = (block.transform.position - position).magnitude;
            }
            if (buildingBlock == null || !_config.TPR.OffsetTPRTarget) return position;
            var blockRotation = buildingBlock.transform.rotation.eulerAngles.y;
            var angles = new[] { 360 - blockRotation, 180 - blockRotation };
            var location = Zero;
            const double r = 2.9;
            var locationDistance = 100f;

#if DEBUG
            Puts("CheckPosition: Finding suitable target position");
            var positions = position.ToString();
            Puts($"CheckPosition:   Old location {positions}");
#endif
            for (var i = 0; i < angles.Length; i++)
            {
                var radians = ConvertToRadians(angles[i]);
                var newX = r * System.Math.Cos(radians);
                var newZ = r * System.Math.Sin(radians);
#if DEBUG
                Puts($"CheckPosition:     Checking angle {i}");
                var newXs = newX.ToString();
                var newZs = newZ.ToString();
                Puts($"CheckPosition:     newX = {newXs}, newZ = {newZs}");
#endif
                var newLoc = new Vector3((float)(buildingBlock.transform.position.x + newX), buildingBlock.transform.position.y + .2f, (float)(buildingBlock.transform.position.z + newZ));
                if ((position - newLoc).magnitude < locationDistance)
                {
                    location = newLoc;
                    locationDistance = (position - newLoc).magnitude;
#if DEBUG
                    var locs = newLoc.ToString();
                    Puts($"CheckPosition:     possible new location at {locs}");
#endif
                }
            }
#if DEBUG
            var locations = location.ToString();
            Puts($"CheckPosition:   New location {locations}");
#endif
            return location;
        }

        private string CanPlayerTeleport(BasePlayer player)
        {
            return Interface.Oxide.CallHook("CanTeleport", player) as string;
        }

        private bool CanCraftHome(BasePlayer player)
        {
            return _config.Home.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftHome);
        }

        private bool CanCraftTown(BasePlayer player)
        {
            return _config.Town.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftTown);
        }

        private bool CanCraftOutpost(BasePlayer player)
        {
            return _config.Outpost.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftOutpost);
        }

        private bool CanCraftBandit(BasePlayer player)
        {
            return _config.Bandit.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftBandit);
        }

        private bool CanCraftTPR(BasePlayer player)
        {
            return _config.TPR.AllowCraft || permission.UserHasPermission(player.UserIDString, PermCraftTpR);
        }

        public bool AboveWater(BasePlayer player)
        {
            var pos = player.transform.position;
#if DEBUG
            Puts($"Player position: {pos.ToString()}.  Checking for water...");
#endif
            if ((TerrainMeta.HeightMap.GetHeight(pos) - TerrainMeta.WaterMap.GetHeight(pos)) >= 0)
            {
#if DEBUG
                Puts("Player not above water.");
#endif
                return False;
            }
            else
            {
#if DEBUG
                Puts("Player is above water!");
#endif
                return True;
            }
        }

        private string NearMonument(BasePlayer player)
        {
            foreach (var entry in monuments)
            {
                var pos = entry.Value.Position;
                pos.y = player.transform.position.y;
                float dist = (player.transform.position - pos).magnitude;
#if DEBUG
                Puts($"Checking {entry.Key} dist: {dist}, realdistance: {entry.Value.Radius}");
#endif
                if (dist < entry.Value.Radius)
                {
#if DEBUG
                    Puts($"Player in range of {entry.Key}");
#endif
                    return entry.Key;
                }
            }
            return null;
        }

        private string NearCave(BasePlayer player)
        {
            var pos = player.transform.position;
            var poss = pos.ToString();

            foreach (var entry in caves)
            {
                var cavename = entry.Key;
                float realdistance = 0f;

                if (cavename.Contains("Small"))
                {
                    realdistance = _config.Settings.CaveDistanceSmall;
                }
                else if (cavename.Contains("Large"))
                {
                    realdistance = _config.Settings.CaveDistanceLarge;
                }
                else if (cavename.Contains("Medium"))
                {
                    realdistance = _config.Settings.CaveDistanceMedium;
                }

                var cavevector = entry.Value;
                cavevector.y = pos.y;
                var cpos = cavevector.ToString();
                float dist = (pos - cavevector).magnitude;

                if (dist < realdistance)
                {
#if DEBUG
                    Puts($"NearCave: {cavename} nearby.");
#endif
                    return cavename;
                }
                else
                {
#if DEBUG
                    Puts("NearCave: Not near this cave.");
#endif
                }
            }
            return null;
        }

        private string CheckPlayer(BasePlayer player, bool build = False, bool craft = False, bool origin = True, string mode = "home")
        {
            var onship = player.GetComponentInParent<CargoShip>();
            var onballoon = player.GetComponentInParent<HotAirBalloon>();
            var inlift = player.GetComponentInParent<Lift>();
            var pos = player.transform.position;

            string monname = NearMonument(player);
            if (_config.Settings.Interrupt.Monument)
            {
                if (monname != null)
                {
                    return _("TooCloseToMon", player, monname);
                }
            }
            if (_config.Settings.Interrupt.Oilrig)
            {
                if (monname != null && monname.Contains("Oilrig"))
                {
                    return _("TooCloseToMon", player, monname);
                }
            }
            bool allowcave = True;

#if DEBUG
            Puts($"CheckPlayer(): called mode is {mode}");
#endif
            switch (mode)
            {
                case "home":
                    allowcave = _config.Home.AllowCave;
                    break;
                case "tpa":
                case "tpr":
                case "town":
                case "outpost":
                case "bandit":
                default:
#if DEBUG
                    Puts("Skipping cave check...");
#endif
                    break;
            }
            if (!allowcave)
            {
#if DEBUG
                Puts("Checking cave distance...");
#endif
                string cavename = NearCave(player);
                if (cavename != null)
                {
                    return "TooCloseToCave";
                }
            }

            if (_config.Settings.Interrupt.Hostile && (mode == "bandit" || mode == "outpost"))
            {
                if (player.IsHostile())
                {
                    return "TPHostile";
                }
            }
            if (player.isMounted && _config.Settings.Interrupt.Mounted)
                return "TPMounted";
            if (!player.IsAlive())
                return "TPDead";
            // Block if hurt if the config is enabled.  If the player is not the target in a tpa condition, allow.
            if ((player.IsWounded() && origin) && _config.Settings.Interrupt.Hurt)
                return "TPWounded";

            if (player.metabolism.temperature.value <= _config.Settings.MinimumTemp && _config.Settings.Interrupt.Cold)
            {
                return "TPTooCold";
            }
            if (player.metabolism.temperature.value >= _config.Settings.MaximumTemp && _config.Settings.Interrupt.Hot)
            {
                return "TPTooHot";
            }

            if (_config.Settings.Interrupt.AboveWater)
                if (AboveWater(player))
                    return "TPAboveWater";
            if (!build && !player.CanBuild())
                return "TPBuildingBlocked";
            if (player.IsSwimming() && _config.Settings.Interrupt.Swimming)
                return "TPSwimming";
            // This will have to do until we have a proper parent name for this
            if (monname != null && monname.Contains("Oilrig") && _config.Settings.Interrupt.Oilrig)
                return "TPOilRig";
            if (monname != null && monname.Contains("Excavator") && _config.Settings.Interrupt.Excavator)
                return "TPExcavator";
            if (onship && _config.Settings.Interrupt.Cargo)
                return "TPCargoShip";
            if (onballoon && _config.Settings.Interrupt.Balloon)
                return "TPHotAirBalloon";
            if (inlift && _config.Settings.Interrupt.Lift)
                return "TPBucketLift";
            if (GetLift(pos) && _config.Settings.Interrupt.Lift)
                return "TPRegLift";
            if (player.InSafeZone() && _config.Settings.Interrupt.Safe)
                return "TPSafeZone";
            if (!craft && player.inventory.crafting.queue.Count > 0)
                return "TPCrafting";

            if (_config.Settings.BlockZoneFlag && ZoneManager != null)
            {
                bool flag = ZoneManager.Call<bool>("PlayerHasFlag", player, "notp");
                if (flag)
                {
                    return "TPFlagZone";
                }
            }

            if (_config.Settings.BlockNoEscape && NoEscape != null)
            {
                bool flag = NoEscape.Call<bool>("IsBlocked", player);
                if (flag)
                {
                    return "TPNoEscapeBlocked";
                }
            }

            return null;
        }

        private string CheckTargetLocation(BasePlayer player, Vector3 targetLocation, bool ubb, bool obb)
        {
            // ubb == UsableIntoBuildingBlocked
            // obb == CupOwnerAllowOnBuildingBlocked
            var colliders = Pool.GetList<Collider>();
            Vis.Colliders(targetLocation, 0.2f, colliders, buildingLayer);
            bool denied = False;
            bool foundblock = False;
            int i = 0;

            foreach (var collider in colliders)
            {
                // First, check that there is a building block at the target
                var block = collider.GetComponentInParent<BuildingBlock>();
                i++;
                if (block != null)
                {
                    foundblock = True;
#if DEBUG
                    Puts($"Found a block {i.ToString()}");
#endif
                    if (foundblock)
                    {
                        if (CheckCupboardBlock(block, player, obb))
                        {
                            denied = False;
#if DEBUG
                            Puts("Cupboard either owned or there is no cupboard");
#endif
                        }
                        else if (ubb && (player.userID != block.OwnerID))
                        {
                            denied = False;
#if DEBUG
                            Puts("Player does not own block, but UsableIntoBuildingBlocked=true");
#endif
                        }
                        else if (player.userID == block.OwnerID)
                        {
#if DEBUG
                            Puts("Player owns block");
#endif

                            if (!player.IsBuildingBlocked(targetLocation, new Quaternion(), block.bounds))
                            {
#if DEBUG
                                Puts("Player not BuildingBlocked. Likely unprotected building.");
#endif
                                denied = False;
                                break;
                            }
                            else if (ubb)
                            {
#if DEBUG
                                Puts("Player not blocked because UsableIntoBuildingBlocked=true");
#endif
                                denied = False;
                                break;
                            }
                            else
                            {
#if DEBUG
                                Puts("Player owns block but blocked by UsableIntoBuildingBlocked=false");
#endif
                                denied = True;
                                break;
                            }
                        }
                        else
                        {
#if DEBUG
                            Puts("Player blocked");
#endif
                            denied = True;
                            break;
                        }
                    }
                }
            }
            Pool.FreeList(ref colliders);

            return denied ? "TPTargetBuildingBlocked" : null;
        }

        // Check that a building block is owned by/attached to a cupboard, allow tp if not blocked unless allowed by config
        private bool CheckCupboardBlock(BuildingBlock block, BasePlayer player, bool obb)
        {
            // obb == CupOwnerAllowOnBuildingBlocked
            BuildingManager.Building building = block.GetBuilding();
            if (building != null)
            {
#if DEBUG
                Puts("Found building, checking privileges...");
                Puts($"Building ID: {building.ID}");
#endif
                // cupboard overlap.  Check privs.
                if (building.buildingPrivileges == null)
                {
#if DEBUG
                    Puts("Player has no privileges");
#endif
                    return False;
                }

                ulong hitEntityOwnerID = block.OwnerID != 0 ? block.OwnerID : 0;
                foreach (var privs in building.buildingPrivileges)
                {
                    if (CupboardAuthCheck(privs, hitEntityOwnerID))
                    {
                        // player is authorized to the cupboard
#if DEBUG
                        Puts("Player owns cupboard with auth");
#endif
                        return True;
                    }
                    else if (obb && player.userID == hitEntityOwnerID)
                    {
#if DEBUG
                        // player set the cupboard and is allowed in by config
                        Puts("Player owns cupboard with no auth, but allowed by CupOwnerAllowOnBuildingBlocked=true");
#endif
                        return True;
                    }
                    else if (player.userID == hitEntityOwnerID)
                    {
#if DEBUG
                        // player set the cupboard but is blocked by config
                        Puts("Player owns cupboard with no auth, but blocked by CupOwnerAllowOnBuildingBlocked=false");
#endif
                        return False;
                    }
                }
#if DEBUG
                Puts("Building found but there was no auth.");
#endif
                return False;
            }
#if DEBUG
            Puts("No cupboard or building found - we cannot tell the status of this block");
#endif
            return True;
        }

        private bool CupboardAuthCheck(BuildingPrivlidge priv, ulong hitEntityOwnerID)
        {
            foreach (var auth in priv.authorizedPlayers.Select(x => x.userid).ToArray())
            {
                if (auth == hitEntityOwnerID)
                {
#if DEBUG
                    Puts("Player has auth");
#endif
                    return True;
                }
            }
#if DEBUG
            Puts("Found no auth");
#endif
            return False;
        }

        private string CheckInsideBlock(Vector3 targetLocation)
        {
            List<BuildingBlock> blocks = Pool.GetList<BuildingBlock>();
            Vis.Entities(targetLocation + new Vector3(0, 0.25f), 0.1f, blocks, blockLayer);
            bool inside = blocks.Count > 0;
            Pool.FreeList(ref blocks);

            return inside ? "TPTargetInsideBlock" : null;
        }

        private string CheckItems(BasePlayer player)
        {
            foreach (var blockedItem in ReverseBlockedItems)
            {
                if (player.inventory.containerMain.GetAmount(blockedItem.Key, True) > 0)
                    return blockedItem.Value;
                if (player.inventory.containerBelt.GetAmount(blockedItem.Key, True) > 0)
                    return blockedItem.Value;
                if (player.inventory.containerWear.GetAmount(blockedItem.Key, True) > 0)
                    return blockedItem.Value;
            }
            return null;
        }

        private string CheckFoundation(ulong userID, Vector3 position)
        {
            if (!_config.Home.ForceOnTopOfFoundation) return null; // Foundation/floor not required
            if (UnderneathFoundation(position))
            {
                return "HomeFoundationUnderneathFoundation";
            }

            var entities = new List<BuildingBlock>();
            if (_config.Home.AllowAboveFoundation) // Can set on a foundation or floor
            {
#if DEBUG
                Puts($"CheckFoundation() looking for foundation or floor at {position.ToString()}");
#endif
                entities = GetFoundationOrFloor(position);
            }
            else // Can only use foundation, not floor/ceiling
            {
#if DEBUG
                Puts($"CheckFoundation() looking for foundation at {position.ToString()}");
#endif
                entities = GetFoundation(position);
            }

            if (entities.Count == 0) return "HomeNoFoundation";

            if (!_config.Home.CheckFoundationForOwner) return null;
            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].OwnerID == userID) return null;
                else if (IsFriend(userID, entities[i].OwnerID)) return null;
            }

            return "HomeFoundationNotFriendsOwned";
        }

        private BuildingBlock GetFoundationOwned(Vector3 position, ulong userID)
        {
#if DEBUG
            Puts("GetFoundationOwned() called...");
#endif
            var entities = GetFoundation(position);
            if (entities.Count == 0) return null;
            if (!_config.Home.CheckFoundationForOwner) return entities[0];

            for (var i = 0; i < entities.Count; i++)
            {
                if (entities[i].OwnerID == userID) return entities[i];
                else if (IsFriend(userID, entities[i].OwnerID)) return entities[i];
            }
            return null;
        }

        // Borrowed/modified from PreventLooting and Rewards
        // playerid = active player, ownerid = owner of building block, who may be offline
        bool IsFriend(ulong playerid, ulong ownerid)
        {
            if (_config.Home.UseFriends && Friends != null)
            {
#if DEBUG
                Puts("Checking Friends...");
#endif
                var fr = Friends?.CallHook("AreFriends", playerid, ownerid);
                if (fr != null && (bool)fr)
                {
#if DEBUG
                    Puts("  IsFriend: true based on Friends plugin");
#endif
                    return True;
                }
            }
            if (_config.Home.UseClans && Clans != null)
            {
#if DEBUG
                Puts("Checking Clans...");
#endif
                string playerclan = (string)Clans?.CallHook("GetClanOf", playerid);
                string ownerclan = (string)Clans?.CallHook("GetClanOf", ownerid);
                if (playerclan == ownerclan && playerclan != null && ownerclan != null)
                {
#if DEBUG
                    Puts("  IsFriend: true based on Clans plugin");
#endif
                    return True;
                }
            }
            if (_config.Home.UseTeams)
            {
#if DEBUG
                Puts("Checking Rust teams...");
#endif
                BasePlayer player = BasePlayer.FindByID(playerid);
                if (player.currentTeam != (long)0)
                {
                    RelationshipManager.PlayerTeam playerTeam = RelationshipManager.Instance.FindTeam(player.currentTeam);
                    if (playerTeam == null) return False;
                    if (playerTeam.members.Contains(ownerid))
                    {
#if DEBUG
                        Puts("  IsFriend: true based on Rust teams");
#endif
                        return True;
                    }
                }
            }
            return False;
        }

        // Check that we are near the middle of a block.  Also check for high wall overlap
        private bool ValidBlock(BaseEntity entity, Vector3 position)
        {
            if (!_config.Settings.StrictFoundationCheck)
            {
                return True;
            }
#if DEBUG
            Puts($"ValidBlock() called for {entity.ShortPrefabName}");
#endif
            Vector3 center = entity.CenterPoint();

            List<BaseEntity> ents = new List<BaseEntity>();
            Vis.Entities<BaseEntity>(center, 1.5f, ents);
            foreach (BaseEntity wall in ents)
            {
                if (wall.name.Contains("external.high"))
                {
#if DEBUG
                    Puts($"    Found: {wall.name} @ center {center.ToString()}, pos {position.ToString()}");
#endif
                    return False;
                }
            }
#if DEBUG
            Puts($"  Checking block: {entity.name} @ center {center.ToString()}, pos: {position.ToString()}");
#endif
            if (entity.PrefabName.Contains("triangle.prefab"))
            {
                if (Math.Abs(center.x - position.x) < 0.45f && Math.Abs(center.z - position.z) < 0.45f)
                {
#if DEBUG
                    Puts($"    Found: {entity.ShortPrefabName} @ center: {center.ToString()}, pos: {position.ToString()}");
#endif
                    return True;
                }
            }
            else if (entity.PrefabName.Contains("foundation.prefab") || entity.PrefabName.Contains("floor.prefab"))
            {
                if (Math.Abs(center.x - position.x) < 0.7f && Math.Abs(center.z - position.z) < 0.7f)
                {
#if DEBUG
                    Puts($"    Found: {entity.ShortPrefabName} @ center: {center.ToString()}, pos: {position.ToString()}");
#endif
                    return True;
                }
            }

            return False;
        }

        private List<BuildingBlock> GetFoundation(Vector3 position)
        {
            RaycastHit hitinfo;
            var entities = new List<BuildingBlock>();

            if (Physics.Raycast(position, Down, out hitinfo, 0.2f, blockLayer))
            {
                var entity = hitinfo.GetEntity();
                if (entity.PrefabName.Contains("foundation") || position.y < entity.WorldSpaceBounds().ToBounds().max.y)
                {
                    if (ValidBlock(entity, position))
                    {
#if DEBUG
                        Puts($"  GetFoundation() found {entity.PrefabName} at {entity.transform.position}");
#endif
                        entities.Add(entity as BuildingBlock);
                    }
                }
            }
            else
            {
#if DEBUG
                Puts("  GetFoundation() none found.");
#endif
            }

            return entities;
        }

        private List<BuildingBlock> GetFloor(Vector3 position)
        {
            RaycastHit hitinfo;
            var entities = new List<BuildingBlock>();

            if (Physics.Raycast(position, Down, out hitinfo, 0.11f, blockLayer))
            {
                var entity = hitinfo.GetEntity();
                if (entity.PrefabName.Contains("floor"))
                {
#if DEBUG
                    Puts($"  GetFloor() found {entity.PrefabName} at {entity.transform.position}");
#endif
                    entities.Add(entity as BuildingBlock);
                }
            }
            else
            {
#if DEBUG
                Puts("  GetFloor() none found.");
#endif
            }

            return entities;
        }

        private List<BuildingBlock> GetFoundationOrFloor(Vector3 position)
        {
            RaycastHit hitinfo;
            var entities = new List<BuildingBlock>();

            if (Physics.Raycast(position, Down, out hitinfo, 0.11f, blockLayer))
            {
                var entity = hitinfo.GetEntity();
                if (entity.PrefabName.Contains("floor") || entity.PrefabName.Contains("foundation"))// || position.y < entity.WorldSpaceBounds().ToBounds().max.y))
                {
#if DEBUG
                    Puts($"  GetFoundationOrFloor() found {entity.PrefabName} at {entity.transform.position}");
#endif
                    if (ValidBlock(entity, position))
                    {
                        entities.Add(entity as BuildingBlock);
                    }
                }
            }
            else
            {
#if DEBUG
                Puts("  GetFoundationOrFloor() none found.");
#endif
            }

            return entities;
        }

        private bool CheckBoundaries(float x, float y, float z)
        {   
            return x <= boundary && x >= -boundary && y <= 2000 && y >= -100 && z <= boundary && z >= -boundary;
        }

        private Vector3 GetGround(Vector3 sourcePos)
        {
            if (!_config.Home.AllowAboveFoundation) return sourcePos;
            var newPos = sourcePos;
            newPos.y = TerrainMeta.HeightMap.GetHeight(newPos);
            sourcePos.y += .5f;
            RaycastHit hitinfo;
            var done = False;

#if DEBUG
            Puts("GetGround(): Looking for iceberg or cave");
#endif
            //if (Physics.SphereCast(sourcePos, .1f, down, out hitinfo, 250, groundLayer))
            if (Physics.Raycast(sourcePos, Down, out hitinfo, 250f, groundLayer))
            {
                if ((_config.Home.AllowIceberg && hitinfo.collider.name.Contains("iceberg")) || (_config.Home.AllowCave && hitinfo.collider.name.Contains("cave_")))
                {
#if DEBUG
                    Puts("GetGround():   found iceberg or cave");
#endif
                    sourcePos.y = hitinfo.point.y;
                    done = True;
                }
                else
                {
                    var mesh = hitinfo.collider.GetComponentInChildren<MeshCollider>();
                    if (mesh != null && mesh.sharedMesh.name.Contains("rock_"))
                    {
                        sourcePos.y = hitinfo.point.y;
                        done = True;
                    }
                }
            }
#if DEBUG
            Puts("GetGround(): Looking for cave or rock");
#endif
            //if (!_config.Home.AllowCave && Physics.SphereCast(sourcePos, .1f, up, out hitinfo, 250, groundLayer) && hitinfo.collider.name.Contains("rock_"))
            if (!_config.Home.AllowCave && Physics.Raycast(sourcePos, Up, out hitinfo, 250f, groundLayer) && hitinfo.collider.name.Contains("rock_"))
            {
#if DEBUG
                Puts("GetGround():   found cave or rock");
#endif
                sourcePos.y = newPos.y - 10;
                done = True;
            }
            return done ? sourcePos : newPos;
        }

        private bool GetLift(Vector3 position)
        {
            List<ProceduralLift> nearObjectsOfType = new List<ProceduralLift>();
            Vis.Entities<ProceduralLift>(position, 0.5f, nearObjectsOfType);
            if (nearObjectsOfType.Count > 0)
            {
                return True;
            }
            return False;
        }

        private Vector3 GetGroundBuilding(Vector3 sourcePos)
        {
            sourcePos.y = TerrainMeta.HeightMap.GetHeight(sourcePos);
            RaycastHit hitinfo;
            if (Physics.Raycast(sourcePos, Down, out hitinfo, buildingLayer))
            {
                sourcePos.y = System.Math.Max(hitinfo.point.y, sourcePos.y);
                return sourcePos;
            }
            if (Physics.Raycast(sourcePos, Up, out hitinfo, buildingLayer))
                sourcePos.y = System.Math.Max(hitinfo.point.y, sourcePos.y);
            return sourcePos;
        }

        private bool UnderneathFoundation(Vector3 position)
        {
            // Check for foundation half-height above where home was set
            foreach (var hit in Physics.RaycastAll(position, Up, 2f, buildingLayer))
            {
                if (hit.GetCollider().name.Contains("foundation"))
                {
                    return True;
                }
            }
            // Check for foundation full-height above where home was set
            // Since you can't see from inside via ray, start above.
            foreach (var hit in Physics.RaycastAll(position + Up + Up + Up + Up, Down, 2f, buildingLayer))
            {
                if (hit.GetCollider().name.Contains("foundation"))
                {
                    return True;
                }
            }

            return False;
        }

        private bool IsAllowed(BasePlayer player, string perm = null)
        {
            var playerAuthLevel = player.net?.connection?.authLevel;

            int requiredAuthLevel = 3;
            if (_config.Admin.UseableByModerators)
            {
                requiredAuthLevel = 1;
            }
            else if (_config.Admin.UseableByAdmins)
            {
                requiredAuthLevel = 2;
            }
            if (playerAuthLevel >= requiredAuthLevel) return True;

            return !string.IsNullOrEmpty(perm) && permission.UserHasPermission(player.UserIDString, perm);
        }

        private bool IsAllowedMsg(BasePlayer player, string perm = null)
        {
            if (IsAllowed(player, perm)) return True;
            PrintMsg(player, "NotAllowed");
            return False;
        }

        private int GetHigher(BasePlayer player, Dictionary<string, int> limits, int limit)
        {
            foreach (var l in limits)
            {
                if (permission.UserHasPermission(player.UserIDString, l.Key) && l.Value > limit)
                {
                    limit = l.Value;
                }
            }
            return limit;
        }

        private int GetLower(BasePlayer player, Dictionary<string, int> times, int time)
        {
            foreach (var l in times)
            {
                if (permission.UserHasPermission(player.UserIDString, l.Key) && l.Value < time)
                {
                    time = l.Value;
                }
            }
            return time;
        }

        private void CheckPerms(Dictionary<string, int> limits)
        {
            foreach (var limit in limits)
            {
                if (!permission.PermissionExists(limit.Key))
                {
                    permission.RegisterPermission(limit.Key, this);
                }
            }
        }
#endregion

#region Message
        private string _(string msgId, BasePlayer player, params object[] args)
        {
            var msg = lang.GetMessage(msgId, this, player?.UserIDString);
            return args.Length > 0 ? string.Format(msg, args) : msg;
        }

        private void PrintMsgL(BasePlayer player, string msgId, params object[] args)
        {
            if (player == null) return;
            PrintMsg(player, _(msgId, player, args));
        }

        private void PrintMsg(BasePlayer player, string msg)
        {
            if (player == null) return;
            SendReply(player, $"{_config.Settings.ChatName}{msg}");
        }
#endregion

#region DrawBox
        private static void DrawBox(BasePlayer player, Vector3 center, Quaternion rotation, Vector3 size)
        {
            size = size / 2;
            var point1 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z + size.z), center, rotation);
            var point2 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z + size.z), center, rotation);
            var point3 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y + size.y, center.z - size.z), center, rotation);
            var point4 = RotatePointAroundPivot(new Vector3(center.x + size.x, center.y - size.y, center.z - size.z), center, rotation);
            var point5 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z + size.z), center, rotation);
            var point6 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z + size.z), center, rotation);
            var point7 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y + size.y, center.z - size.z), center, rotation);
            var point8 = RotatePointAroundPivot(new Vector3(center.x - size.x, center.y - size.y, center.z - size.z), center, rotation);

            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point2);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point3);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point1, point5);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point2);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point3);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point4, point8);

            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point5, point6);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point5, point7);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point6, point2);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point8, point6);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point8, point7);
            player.SendConsoleCommand("ddraw.line", 30f, Color.blue, point7, point3);
        }

        private static Vector3 RotatePointAroundPivot(Vector3 point, Vector3 pivot, Quaternion rotation)
        {
            return rotation * (point - pivot) + pivot;
        }
#endregion

#region FindPlayer
        private ulong FindPlayersSingleId(string nameOrIdOrIp, BasePlayer player)
        {
            var targets = FindPlayers(nameOrIdOrIp);
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", string.Join(", ", targets.Select(p => p.displayName).ToArray()));
                return 0;
            }
            ulong userId;
            if (targets.Count <= 0)
            {
                if (ulong.TryParse(nameOrIdOrIp, out userId)) return userId;
                PrintMsgL(player, "PlayerNotFound");
                return 0;
            }
            else
                userId = targets.First().userID;
            return userId;
        }

        private BasePlayer FindPlayersSingle(string nameOrIdOrIp, BasePlayer player)
        {
            var targets = FindPlayers(nameOrIdOrIp);
            if (targets.Count <= 0)
            {
                PrintMsgL(player, "PlayerNotFound");
                return null;
            }
            if (targets.Count > 1)
            {
                PrintMsgL(player, "MultiplePlayers", string.Join(", ", targets.Select(p => p.displayName).ToArray()));
                return null;
            }
            return targets.First();
        }

        private static HashSet<BasePlayer> FindPlayers(string nameOrIdOrIp)
        {
            var players = new HashSet<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            foreach (var sleepingPlayer in BasePlayer.sleepingPlayerList)
            {
                if (sleepingPlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(sleepingPlayer);
                else if (!string.IsNullOrEmpty(sleepingPlayer.displayName) && sleepingPlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(sleepingPlayer);
            }
            return players;
        }

        private static List<BasePlayer> FindPlayersOnline(string nameOrIdOrIp)
        {
            var players = new List<BasePlayer>();
            if (string.IsNullOrEmpty(nameOrIdOrIp)) return players;
            foreach (var activePlayer in BasePlayer.activePlayerList)
            {
                if (activePlayer.UserIDString.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
                else if (!string.IsNullOrEmpty(activePlayer.displayName) && activePlayer.displayName.Contains(nameOrIdOrIp, CompareOptions.IgnoreCase))
                    players.Add(activePlayer);
                else if (activePlayer.net?.connection != null && activePlayer.net.connection.ipaddress.Equals(nameOrIdOrIp))
                    players.Add(activePlayer);
            }
            return players;
        }
#endregion

#region API
        private Dictionary<string, Vector3> GetHomes(object playerObj)
        {
            if (playerObj == null) return null;
            if (playerObj is string) playerObj = Convert.ToUInt64(playerObj);
            if (!(playerObj is ulong)) throw new ArgumentException("playerObj");
            var playerId = (ulong)playerObj;
            HomeData homeData;
            if (!Home.TryGetValue(playerId, out homeData) || homeData.Locations.Count == 0) return null;
            return homeData.Locations;
        }

        private int GetLimitRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type)) return 0;
            var currentDate = DateTime.Now.ToString("d");
            int limit;
            var remaining = -1;
            switch (type.ToLower())
            {
                case "home":
                    limit = GetHigher(player, _config.Home.VIPDailyLimits, _config.Home.DailyLimit);
                    HomeData homeData;
                    if (!Home.TryGetValue(player.userID, out homeData))
                    {
                        Home[player.userID] = homeData = new HomeData();
                    }
                    if (homeData.Teleports.Date != currentDate)
                    {
                        homeData.Teleports.Amount = 0;
                        homeData.Teleports.Date = currentDate;
                    }
                    if (limit > 0)
                    {
                        remaining = limit - homeData.Teleports.Amount;
                    }
                    break;
                case "town":
                    limit = GetHigher(player, _config.Town.VIPDailyLimits, _config.Town.DailyLimit);
                    TeleportData townData;
                    if (!Town.TryGetValue(player.userID, out townData))
                    {
                        Town[player.userID] = townData = new TeleportData();
                    }
                    if (townData.Date != currentDate)
                    {
                        townData.Amount = 0;
                        townData.Date = currentDate;
                    }
                    if (limit > 0)
                    {
                        remaining = limit - townData.Amount;
                    }
                    break;
                case "outpost":
                    limit = GetHigher(player, _config.Outpost.VIPDailyLimits, _config.Outpost.DailyLimit);
                    TeleportData outpostData;
                    if (!Outpost.TryGetValue(player.userID, out outpostData))
                    {
                        Outpost[player.userID] = outpostData = new TeleportData();
                    }
                    if (outpostData.Date != currentDate)
                    {
                        outpostData.Amount = 0;
                        outpostData.Date = currentDate;
                    }
                    if (limit > 0)
                    {
                        remaining = limit - outpostData.Amount;
                    }
                    break;
                case "bandit":
                    limit = GetHigher(player, _config.Bandit.VIPDailyLimits, _config.Bandit.DailyLimit);
                    TeleportData banditData;
                    if (!Bandit.TryGetValue(player.userID, out banditData))
                    {
                        Bandit[player.userID] = banditData = new TeleportData();
                    }
                    if (banditData.Date != currentDate)
                    {
                        banditData.Amount = 0;
                        banditData.Date = currentDate;
                    }
                    if (limit > 0)
                    {
                        remaining = limit - banditData.Amount;
                    }
                    break;
                case "tpr":
                    limit = GetHigher(player, _config.TPR.VIPDailyLimits, _config.TPR.DailyLimit);
                    TeleportData tprData;
                    if (!TPR.TryGetValue(player.userID, out tprData))
                    {
                        TPR[player.userID] = tprData = new TeleportData();
                    }
                    if (tprData.Date != currentDate)
                    {
                        tprData.Amount = 0;
                        tprData.Date = currentDate;
                    }
                    if (limit > 0)
                    {
                        remaining = limit - tprData.Amount;
                    }
                    break;
            }
            return remaining;
        }

        private int GetCooldownRemaining(BasePlayer player, string type)
        {
            if (player == null || string.IsNullOrEmpty(type)) return 0;
            var currentDate = DateTime.Now.ToString("d");
            var timestamp = Facepunch.Math.Epoch.Current;
            int cooldown;
            var remaining = -1;
            switch (type.ToLower())
            {
                case "home":
                    cooldown = GetLower(player, _config.Home.VIPCooldowns, _config.Home.Cooldown);
                    HomeData homeData;
                    if (!Home.TryGetValue(player.userID, out homeData))
                    {
                        Home[player.userID] = homeData = new HomeData();
                    }
                    if (homeData.Teleports.Date != currentDate)
                    {
                        homeData.Teleports.Amount = 0;
                        homeData.Teleports.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - homeData.Teleports.Timestamp < cooldown)
                    {
                        remaining = cooldown - (timestamp - homeData.Teleports.Timestamp);
                    }
                    break;
                case "town":
                    cooldown = GetLower(player, _config.Town.VIPCooldowns, _config.Town.Cooldown);
                    TeleportData townData;
                    if (!Town.TryGetValue(player.userID, out townData))
                    {
                        Town[player.userID] = townData = new TeleportData();
                    }
                    if (townData.Date != currentDate)
                    {
                        townData.Amount = 0;
                        townData.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - townData.Timestamp < cooldown)
                    {
                        remaining = cooldown - (timestamp - townData.Timestamp);
                    }
                    break;
                case "outpost":
                    cooldown = GetLower(player, _config.Outpost.VIPCooldowns, _config.Outpost.Cooldown);
                    TeleportData outpostData;
                    if (!Outpost.TryGetValue(player.userID, out outpostData))
                    {
                        Outpost[player.userID] = outpostData = new TeleportData();
                    }
                    if (outpostData.Date != currentDate)
                    {
                        outpostData.Amount = 0;
                        outpostData.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - outpostData.Timestamp < cooldown)
                    {
                        remaining = cooldown - (timestamp - outpostData.Timestamp);
                    }
                    break;
                case "bandit":
                    cooldown = GetLower(player, _config.Bandit.VIPCooldowns, _config.Bandit.Cooldown);
                    TeleportData banditData;
                    if (!Bandit.TryGetValue(player.userID, out banditData))
                    {
                        Bandit[player.userID] = banditData = new TeleportData();
                    }
                    if (banditData.Date != currentDate)
                    {
                        banditData.Amount = 0;
                        banditData.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - banditData.Timestamp < cooldown)
                    {
                        remaining = cooldown - (timestamp - banditData.Timestamp);
                    }
                    break;
                case "tpr":
                    cooldown = GetLower(player, _config.TPR.VIPCooldowns, _config.TPR.Cooldown);
                    TeleportData tprData;
                    if (!TPR.TryGetValue(player.userID, out tprData))
                    {
                        TPR[player.userID] = tprData = new TeleportData();
                    }
                    if (tprData.Date != currentDate)
                    {
                        tprData.Amount = 0;
                        tprData.Date = currentDate;
                    }
                    if (cooldown > 0 && timestamp - tprData.Timestamp < cooldown)
                    {
                        remaining = cooldown - (timestamp - tprData.Timestamp);
                    }
                    break;
            }
            return remaining;
        }
#endregion

        private class UnityVector3Converter : JsonConverter
        {
            public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
            {
                var vector = (Vector3)value;
                writer.WriteValue($"{vector.x} {vector.y} {vector.z}");
            }

            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                if (reader.TokenType == JsonToken.String)
                {
                    var values = reader.Value.ToString().Trim().Split(' ');
                    return new Vector3(Convert.ToSingle(values[0]), Convert.ToSingle(values[1]), Convert.ToSingle(values[2]));
                }
                var o = JObject.Load(reader);
                return new Vector3(Convert.ToSingle(o["x"]), Convert.ToSingle(o["y"]), Convert.ToSingle(o["z"]));
            }

            public override bool CanConvert(Type objectType)
            {
                return objectType == typeof(Vector3);
            }
        }

        private class CustomComparerDictionaryCreationConverter<T> : CustomCreationConverter<IDictionary>
        {
            private readonly IEqualityComparer<T> comparer;

            public CustomComparerDictionaryCreationConverter(IEqualityComparer<T> comparer)
            {
                if (comparer == null)
                    throw new ArgumentNullException(nameof(comparer));
                this.comparer = comparer;
            }

            public override bool CanConvert(Type objectType)
            {
                return HasCompatibleInterface(objectType) && HasCompatibleConstructor(objectType);
            }

            private static bool HasCompatibleInterface(Type objectType)
            {
                return objectType.GetInterfaces().Where(i => HasGenericTypeDefinition(i, typeof(IDictionary<,>))).Any(i => typeof(T).IsAssignableFrom(i.GetGenericArguments().First()));
            }

            private static bool HasGenericTypeDefinition(Type objectType, Type typeDefinition)
            {
                return objectType.GetTypeInfo().IsGenericType && objectType.GetGenericTypeDefinition() == typeDefinition;
            }

            private static bool HasCompatibleConstructor(Type objectType)
            {
                return objectType.GetConstructor(new[] { typeof(IEqualityComparer<T>) }) != null;
            }

            public override IDictionary Create(Type objectType)
            {
                return Activator.CreateInstance(objectType, comparer) as IDictionary;
            }
        }

        [HookMethod("SendHelpText")]
        private void SendHelpText(BasePlayer player)
        {
            PrintMsgL(player, "<size=14>NTeleportation</size> by <color=#ce422b>Nogrod</color>\n<color=#ffd479>/sethome NAME</color> - Set home on current foundation\n<color=#ffd479>/home NAME</color> - Go to one of your homes\n<color=#ffd479>/home list</color> - List your homes\n<color=#ffd479>/town</color> - Go to town, if set\n/tpb - Go back to previous location\n/tpr PLAYER - Request teleport to PLAYER\n/tpa - Accept teleport request");
        }

        private bool API_HavePendingRequest(BasePlayer player)
        {
            return PendingRequests.ContainsKey(player.userID) || PlayersRequests.ContainsKey(player.userID) || TeleportTimers.ContainsKey(player.userID);
        }

        private bool API_HaveAvailableHomes(BasePlayer player)
        {
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData))
            {
                Home[player.userID] = homeData = new HomeData();
            }

            var limit = GetHigher(player, _config.Home.VIPHomesLimits, _config.Home.HomesLimit);
            return homeData.Locations.Count < limit;
        }

        private List<string> API_GetHomes(BasePlayer player)
        {
            HomeData homeData;
            if (!Home.TryGetValue(player.userID, out homeData))
            {
                Home[player.userID] = homeData = new HomeData();
            }

            return homeData.Locations.Keys.ToList();
        }

        #region Config Converter

        class ConfigData
        {
            public SettingsData Settings { get; set; }
            public GameVersionData GameVersion { get; set; }
            public AdminSettingsData Admin { get; set; }
            public HomesSettingsData Home { get; set; }
            public TPTData TPT { get; set; }
            public TPRData TPR { get; set; }
            public TownData Town { get; set; }
            public TownData Outpost { get; set; }
            public TownData Bandit { get; set; }
            public VersionNumber Version { get; set; }
        }

        class SettingsData
        {
            public string ChatName { get; set; }
            public bool HomesEnabled { get; set; }
            public bool TPREnabled { get; set; }
            public bool TownEnabled { get; set; }
            public bool OutpostEnabled { get; set; }
            public bool BanditEnabled { get; set; }
            public bool InterruptTPOnHurt { get; set; }
            public bool InterruptTPOnCold { get; set; }
            public bool InterruptTPOnHot { get; set; }
            public bool InterruptTPOnHostile { get; set; }
            public bool InterruptTPOnSafe { get; set; }
            public bool InterruptTPOnBalloon { get; set; }
            public bool InterruptTPOnCargo { get; set; }
            public bool InterruptTPOnExcavator { get; set; }
            public bool InterruptTPOnLift { get; set; }
            public bool InterruptTPOnMonument { get; set; }
            public bool InterruptTPOnOilrig { get; set; }
            public bool InterruptTPOnMounted { get; set; }
            public bool InterruptTPOnSwimming { get; set; }
            public bool InterruptAboveWater { get; set; }
            public bool StrictFoundationCheck { get; set; }
            public float CaveDistanceSmall { get; set; }
            public float CaveDistanceMedium { get; set; }
            public float CaveDistanceLarge { get; set; }
            public float DefaultMonumentSize { get; set; }
            public float MinimumTemp { get; set; }
            public float MaximumTemp { get; set; }
            public Dictionary<string, string> BlockedItems { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            public string BypassCMD { get; set; }
            public bool UseEconomics { get; set; }
            public bool UseServerRewards { get; set; }
            public bool WipeOnUpgradeOrChange { get; set; }
            public bool AutoGenOutpost { get; set; }
            public bool AutoGenBandit { get; set; }
        }

        class GameVersionData
        {
            public int Network { get; set; }
            public int Save { get; set; }
            public string Level { get; set; }
            public string LevelURL { get; set; }
            public int WorldSize { get; set; }
            public int Seed { get; set; }
        }

        class AdminSettingsData
        {
            public bool AnnounceTeleportToTarget { get; set; }
            public bool UseableByAdmins { get; set; }
            public bool UseableByModerators { get; set; }
            public int LocationRadius { get; set; }
            public int TeleportNearDefaultDistance { get; set; }
        }

        class HomesSettingsData
        {
            public int HomesLimit { get; set; }
            public Dictionary<string, int> VIPHomesLimits { get; set; }
            public int Cooldown { get; set; }
            public int Countdown { get; set; }
            public int DailyLimit { get; set; }
            public Dictionary<string, int> VIPDailyLimits { get; set; }
            public Dictionary<string, int> VIPCooldowns { get; set; }
            public Dictionary<string, int> VIPCountdowns { get; set; }
            public int LocationRadius { get; set; }
            public bool ForceOnTopOfFoundation { get; set; }
            public bool CheckFoundationForOwner { get; set; }
            public bool UseFriends { get; set; }
            public bool UseClans { get; set; }
            public bool UseTeams { get; set; }
            public bool UsableOutOfBuildingBlocked { get; set; }
            public bool UsableIntoBuildingBlocked { get; set; }
            public bool CupOwnerAllowOnBuildingBlocked { get; set; }
            public bool AllowIceberg { get; set; }
            public bool AllowCave { get; set; }
            public bool AllowCraft { get; set; }
            public bool AllowAboveFoundation { get; set; }
            public bool CheckValidOnList { get; set; }
            public int Pay { get; set; }
            public int Bypass { get; set; }
        }

        class TPTData
        {
            public bool UseFriends { get; set; } = true;
            public bool UseClans { get; set; } = true;
            public bool UseTeams { get; set; } = true;
        }

        class TPRData
        {
            public int Cooldown { get; set; }
            public int Countdown { get; set; }
            public int DailyLimit { get; set; }
            public Dictionary<string, int> VIPDailyLimits { get; set; }
            public Dictionary<string, int> VIPCooldowns { get; set; }
            public Dictionary<string, int> VIPCountdowns { get; set; }
            public int RequestDuration { get; set; }
            public bool OffsetTPRTarget { get; set; }
            public bool BlockTPAOnCeiling { get; set; }
            public bool UsableOutOfBuildingBlocked { get; set; }
            public bool UsableIntoBuildingBlocked { get; set; }
            public bool CupOwnerAllowOnBuildingBlocked { get; set; }
            public bool AllowCraft { get; set; }
            public int Pay { get; set; }
            public int Bypass { get; set; }
        }

        class TownData
        {
            public int Cooldown { get; set; }
            public int Countdown { get; set; }
            public int DailyLimit { get; set; }
            public Dictionary<string, int> VIPDailyLimits { get; set; }
            public Dictionary<string, int> VIPCooldowns { get; set; }
            public Dictionary<string, int> VIPCountdowns { get; set; }
            public Vector3 Location { get; set; }
            public bool UsableOutOfBuildingBlocked { get; set; }
            public bool AllowCraft { get; set; }
            public int Pay { get; set; }
            public int Bypass { get; set; }
        }

        void ConfigurationConverter()
        {
            ConfigData oldConfig;
            try
            {
                oldConfig = Config.ReadObject<ConfigData>();
            }
            catch
            {
                return;
            }

            if (oldConfig == null)
            {
                return;
            }

            var config = new Configuration();
            
            config.Admin.AnnounceTeleportToTarget = oldConfig.Admin.AnnounceTeleportToTarget;
            config.Admin.LocationRadius = oldConfig.Admin.LocationRadius;
            config.Admin.TeleportNearDefaultDistance = oldConfig.Admin.TeleportNearDefaultDistance;
            config.Admin.UseableByAdmins = oldConfig.Admin.UseableByAdmins;
            config.Admin.UseableByModerators = oldConfig.Admin.UseableByModerators;

            config.Bandit.AllowCraft = oldConfig.Bandit.AllowCraft;
            config.Bandit.Bypass = oldConfig.Bandit.Bypass;
            config.Bandit.Cooldown = oldConfig.Bandit.Cooldown;
            config.Bandit.Countdown = oldConfig.Bandit.Countdown;
            config.Bandit.DailyLimit = oldConfig.Bandit.DailyLimit;
            config.Bandit.Location = oldConfig.Bandit.Location;
            config.Bandit.Pay = oldConfig.Bandit.Pay;
            config.Bandit.UsableOutOfBuildingBlocked = oldConfig.Bandit.UsableOutOfBuildingBlocked;

            config.Home.AllowAboveFoundation = oldConfig.Home.AllowAboveFoundation;
            config.Home.AllowCave = oldConfig.Home.AllowCave;
            config.Home.AllowCraft = oldConfig.Home.AllowCraft;
            config.Home.AllowIceberg = oldConfig.Home.AllowIceberg;
            config.Home.Bypass = oldConfig.Home.Bypass;
            config.Home.CheckFoundationForOwner = oldConfig.Home.CheckFoundationForOwner;
            config.Home.CheckValidOnList = oldConfig.Home.CheckValidOnList;
            config.Home.Cooldown = oldConfig.Home.Cooldown;
            config.Home.Countdown = oldConfig.Home.Countdown;
            config.Home.CupOwnerAllowOnBuildingBlocked = oldConfig.Home.CupOwnerAllowOnBuildingBlocked;
            config.Home.DailyLimit = oldConfig.Home.DailyLimit;
            config.Home.ForceOnTopOfFoundation = oldConfig.Home.ForceOnTopOfFoundation;
            config.Home.HomesLimit = oldConfig.Home.HomesLimit;
            config.Home.LocationRadius = oldConfig.Home.LocationRadius;
            config.Home.Pay = oldConfig.Home.Pay;
            config.Home.UsableIntoBuildingBlocked = oldConfig.Home.UsableIntoBuildingBlocked;
            config.Home.UsableOutOfBuildingBlocked = oldConfig.Home.UsableOutOfBuildingBlocked;
            config.Home.UseClans = oldConfig.Home.UseClans;
            config.Home.UseFriends = oldConfig.Home.UseFriends;
            config.Home.UseTeams = oldConfig.Home.UseTeams;

            config.Outpost.AllowCraft = oldConfig.Outpost.AllowCraft;
            config.Outpost.Bypass = oldConfig.Outpost.Bypass;
            config.Outpost.Cooldown = oldConfig.Outpost.Cooldown;
            config.Outpost.Countdown = oldConfig.Outpost.Countdown;
            config.Outpost.DailyLimit = oldConfig.Outpost.DailyLimit;
            config.Outpost.Location = oldConfig.Outpost.Location;
            config.Outpost.Pay = oldConfig.Outpost.Pay;
            config.Outpost.UsableOutOfBuildingBlocked = oldConfig.Outpost.UsableOutOfBuildingBlocked;

            config.Settings.AutoGenBandit = oldConfig.Settings.AutoGenBandit;
            config.Settings.AutoGenOutpost = oldConfig.Settings.AutoGenOutpost;
            config.Settings.BanditEnabled = oldConfig.Settings.BanditEnabled;
            config.Settings.BlockedItems = oldConfig.Settings.BlockedItems;
            config.Settings.BypassCMD = oldConfig.Settings.BypassCMD as string;
            config.Settings.CaveDistanceLarge = oldConfig.Settings.CaveDistanceLarge;
            config.Settings.CaveDistanceMedium = oldConfig.Settings.CaveDistanceMedium;
            config.Settings.CaveDistanceSmall = oldConfig.Settings.CaveDistanceSmall;
            config.Settings.ChatName = oldConfig.Settings.ChatName as string;
            config.Settings.DefaultMonumentSize = oldConfig.Settings.DefaultMonumentSize;
            config.Settings.HomesEnabled = oldConfig.Settings.HomesEnabled;
            config.Settings.Interrupt.AboveWater = oldConfig.Settings.InterruptAboveWater;
            config.Settings.Interrupt.Balloon = oldConfig.Settings.InterruptTPOnBalloon;
            config.Settings.Interrupt.Cargo = oldConfig.Settings.InterruptTPOnCargo;
            config.Settings.Interrupt.Cold = oldConfig.Settings.InterruptTPOnCold;
            config.Settings.Interrupt.Excavator = oldConfig.Settings.InterruptTPOnExcavator;
            config.Settings.Interrupt.Hostile = oldConfig.Settings.InterruptTPOnHostile;
            config.Settings.Interrupt.Hot = oldConfig.Settings.InterruptTPOnHot;
            config.Settings.Interrupt.Hurt = oldConfig.Settings.InterruptTPOnHurt;
            config.Settings.Interrupt.Lift = oldConfig.Settings.InterruptTPOnLift;
            config.Settings.Interrupt.Monument = oldConfig.Settings.InterruptTPOnMonument;
            config.Settings.Interrupt.Mounted = oldConfig.Settings.InterruptTPOnMounted;
            config.Settings.Interrupt.Oilrig = oldConfig.Settings.InterruptTPOnOilrig;
            config.Settings.Interrupt.Safe = oldConfig.Settings.InterruptTPOnSafe;
            config.Settings.Interrupt.Swimming = oldConfig.Settings.InterruptTPOnSwimming;
            config.Settings.MaximumTemp = oldConfig.Settings.MaximumTemp;
            config.Settings.MinimumTemp = oldConfig.Settings.MinimumTemp;
            config.Settings.OutpostEnabled = oldConfig.Settings.OutpostEnabled;
            config.Settings.StrictFoundationCheck = oldConfig.Settings.StrictFoundationCheck;
            config.Settings.TownEnabled = oldConfig.Settings.TownEnabled;
            config.Settings.TPREnabled = oldConfig.Settings.TPREnabled;
            config.Settings.UseEconomics = oldConfig.Settings.UseEconomics;
            config.Settings.UseServerRewards = oldConfig.Settings.UseServerRewards;
            config.Settings.WipeOnUpgradeOrChange = oldConfig.Settings.WipeOnUpgradeOrChange;
            
            config.Town.AllowCraft = oldConfig.Town.AllowCraft;
            config.Town.Bypass = oldConfig.Town.Bypass;
            config.Town.Cooldown = oldConfig.Town.Cooldown;
            config.Town.Countdown = oldConfig.Town.Countdown;
            config.Town.DailyLimit = oldConfig.Town.DailyLimit;
            config.Town.Location = oldConfig.Town.Location;
            config.Town.Pay = oldConfig.Town.Pay;
            config.Town.UsableOutOfBuildingBlocked = oldConfig.Town.UsableOutOfBuildingBlocked;

            config.TPR.AllowCraft = oldConfig.TPR.AllowCraft;
            config.TPR.BlockTPAOnCeiling = oldConfig.TPR.BlockTPAOnCeiling;
            config.TPR.Bypass = oldConfig.TPR.Bypass;
            config.TPR.Cooldown = oldConfig.TPR.Cooldown;
            config.TPR.Countdown = oldConfig.TPR.Countdown;
            config.TPR.CupOwnerAllowOnBuildingBlocked = oldConfig.TPR.CupOwnerAllowOnBuildingBlocked;
            config.TPR.DailyLimit = oldConfig.TPR.DailyLimit;
            config.TPR.OffsetTPRTarget = oldConfig.TPR.OffsetTPRTarget;
            config.TPR.Pay = oldConfig.TPR.Pay;
            config.TPR.RequestDuration = oldConfig.TPR.RequestDuration;
            config.TPR.UsableIntoBuildingBlocked = oldConfig.TPR.UsableIntoBuildingBlocked;
            config.TPR.UsableOutOfBuildingBlocked = oldConfig.TPR.UsableOutOfBuildingBlocked;

            config.Version = Version;

            config.TPT.UseClans = oldConfig.TPT?.UseClans ?? true;
            config.TPT.UseFriends = oldConfig.TPT?.UseFriends ?? true;
            config.TPT.UseTeams = oldConfig.TPT?.UseTeams ?? true;
            config.Bandit.VIPCooldowns = oldConfig.Bandit.VIPCooldowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Bandit.VIPCooldowns;
            config.Bandit.VIPCountdowns = oldConfig.Bandit.VIPCountdowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Bandit.VIPCountdowns;
            config.Bandit.VIPDailyLimits = oldConfig.Bandit.VIPDailyLimits?.ToDictionary(x => x.Key, x => x.Value) ?? config.Bandit.VIPDailyLimits;
            config.Home.VIPCooldowns = oldConfig.Home.VIPCooldowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Home.VIPCooldowns;
            config.Home.VIPCountdowns = oldConfig.Home.VIPCountdowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Home.VIPCountdowns;
            config.Home.VIPDailyLimits = oldConfig.Home.VIPDailyLimits?.ToDictionary(x => x.Key, x => x.Value) ?? config.Home.VIPDailyLimits;
            config.Home.VIPHomesLimits = oldConfig.Home.VIPHomesLimits?.ToDictionary(x => x.Key, x => x.Value) ?? config.Home.VIPHomesLimits;
            config.Outpost.VIPCooldowns = oldConfig.Outpost.VIPCooldowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Outpost.VIPCooldowns;
            config.Outpost.VIPCountdowns = oldConfig.Outpost.VIPCountdowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Outpost.VIPCountdowns;
            config.Outpost.VIPDailyLimits = oldConfig.Outpost.VIPDailyLimits?.ToDictionary(x => x.Key, x => x.Value) ?? config.Outpost.VIPDailyLimits;
            config.Town.VIPCooldowns = oldConfig.Town.VIPCooldowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Town.VIPCooldowns;
            config.Town.VIPCountdowns = oldConfig.Town.VIPCountdowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.Town.VIPCountdowns;
            config.Town.VIPDailyLimits = oldConfig.Town.VIPDailyLimits?.ToDictionary(x => x.Key, x => x.Value) ?? config.Town.VIPDailyLimits;
            config.TPR.VIPCooldowns = oldConfig.TPR.VIPCooldowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.TPR.VIPCooldowns;
            config.TPR.VIPCountdowns = oldConfig.TPR.VIPCountdowns?.ToDictionary(x => x.Key, x => x.Value) ?? config.TPR.VIPCountdowns;
            config.TPR.VIPDailyLimits = oldConfig.TPR.VIPDailyLimits?.ToDictionary(x => x.Key, x => x.Value) ?? config.TPR.VIPDailyLimits;

            storedData.Converted_1_2_0 = true;
            dataFile.WriteObject(storedData);
            Config.WriteObject<Configuration>(config);
            oldConfig = null;
            Puts("Converted config.");
        }

        #endregion Config Converter
    }
}

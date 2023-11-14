# NTeleportation

Oxide plugin for Rust. Multiple teleportation systems for admin and players.

See note below about `nteleportation.home` permission, which is now required for basic function. This finally allows control over who may use the basic commands.

## Players Unique ID

### Players are now assigned a unique random 4 digit ID each time they sign on

- This ID changes each time plugin is reloaded / server is restarted
- ***You can only find a players ID when you are shown the multiple players found list***
- Example:
- Found multiple players: **9910** - Ruptga, **8750** - GRYLLZ, **1133** - Roe Jogan

- /tp 9910 - will teleport you to Ruptga
- /tp 9910 nivex - will teleport Ruptga to nivex
- /tpr 9910 - will send a teleport request to Ruptga

## Configuration

- UseFriends  - Check via Friends API that owner and player are friends
- UseClans - Check via Clans API that owner and player are in the same clan *(support only for the Oxide Clans.cs and perhaps RustIO::Clans - other clan plugins status is unknown)*
- UseTeams - Check via Rust teams that owner and player are friends
- UseEconomics - Use the Economics plugin to pay for teleports and/or pay to bypass cooldowns
- UseServerRewards - Use the ServerRewards plugin to pay for teleports and/or pay to bypass cooldowns
- WipeOnUpgradeOrChange - If true, wipe homes, towns, islands, bandit and outpost location in the event of the server detecting a new save
- UsableOutOfBuildingBlocked  - Allows a player to teleport out of building blocked area (not into)  
- ForceOnTopOfFoundation - If true the player must set home on a foundation or floor.  If false, player can set a home anywhere not limited by another config *(e.g. nterruptTPOnMonument)*
- AllowAboveFoundation  - Allows to set homes on Nth floor of a building when above owned/shared foundation (floor/ceiling).  If false, player must use home/sethome on a foundation (1st floor)
- VIPCooldowns/VIPDailyLimits/VIPHomesLimits/VIPCountdowns:  
- TPT - contains settings for enabling  instant TPA for friends, clans and teams. Set these false to disable.

```json
{
  "Settings": {
    "Chat Command Color": "#FFFF00",
    "Chat Command Argument Color": "#FFA500",
    "Enable Popup Support": false,
    "Block All Teleporting From Inside Authorized Base": false,
    "TPB Available After X Seconds": 0.0,
    "Global Teleport Cooldown": 0.0,
    "Global VIP Teleport Cooldown": 0.0,
    "Play Sounds After Teleport": false,
    "Sounds To Play After Teleport": [
      "assets/prefabs/misc/xmas/presents/effects/unwrap.prefab",
      "assets/bundled/prefabs/fx/player/howl.prefab",
      "assets/content/vehicles/minicopter/debris_effect.prefab",
      "assets/prefabs/npc/patrol helicopter/damage_effect_debris.prefab",
      "assets/prefabs/npc/patrol helicopter/effects/rocket_fire.prefab"
    ],
    "Interrupt TP": {
      "Interrupt Teleport At Specific Monuments": [],
      "Above Water": true,
      "Balloon": true,
      "Boats": false,
      "Cargo Ship": true,
      "Cold": false,
      "Excavator": false,
      "Hot": false,
      "Hostile": false,
      "Hurt": true,
      "Junkpiles": false,
      "Lift": true,
      "Monument": false,
      "Ignore Monument Marker Prefab": false,
      "Mounted": true,
      "Oil Rig": false,
      "Safe Zone": true,
      "Swimming": false
    },
    "Block Teleport (NoEscape)": false,
    "Block Teleport (ZoneManager)": false,
    "Chat Name": "<color=red>Teleportation</color> \n\n",
    "Chat Steam64ID": 76561199056025689,
    "Check Boundaries On Teleport X Y Z": true,
    "Data File Directory (Blank = Default)": "",
    "Draw Sphere On Set Home": true,
    "Homes Enabled": true,
    "TPR Enabled": true,
    "Strict Foundation Check": false,
    "Cave Distance Small": 50.0,
    "Cave Distance Medium": 70.0,
    "Cave Distance Large": 110.0,
    "Default Monument Size": 50.0,
    "Minimum Temp": 0.0,
    "Maximum Temp": 40.0,
    "Blocked Items": {},
    "Bypass CMD": "pay",
    "Use Monument Topology Check": false,
    "Use Cave Topology Check": false,
    "Use Economics": false,
    "Use Server Rewards": false,
    "Wipe On Upgrade Or Change": true,
    "Auto Generate Outpost Location": true,
    "Auto Generate Bandit Location": true,
    "Show Time As Seconds Instead": false
  },
  "Admin": {
    "Announce Teleport To Target": false,
    "Usable By Admins": true,
    "Usable By Moderators": true,
    "Location Radius": 25,
    "Teleport Near Default Distance": 30
  },
  "Home": {
    "Homes Limit": 2,
    "VIP Homes Limits": {
      "nteleportation.vip": 5
    },
    "Allow Sethome At Specific Monuments": [
      "HQM Quarry",
      "Stone Quarry",
      "Sulfur Quarry",
      "Ice Lake"
    ],
    "Allow Sethome At All Monuments": false,
    "Allow TPB": true,
    "Cooldown": 600,
    "Countdown": 15,
    "Daily Limit": 5,
    "VIP Daily Limits": {
      "nteleportation.vip": 5
    },
    "VIP Cooldowns": {
      "nteleportation.vip": 5
    },
    "VIP Countdowns": {
      "nteleportation.vip": 5
    },
    "Location Radius": 25,
    "Force On Top Of Foundation": true,
    "Check Foundation For Owner": true,
    "Use Friends": true,
    "Use Clans": true,
    "Use Teams": true,
    "Usable Out Of Building Blocked": false,
    "Usable Into Building Blocked": false,
    "Usable From Safe Zone Only": false,
    "Allow Cupboard Owner When Building Blocked": true,
    "Allow Iceberg": false,
    "Allow Cave": false,
    "Allow Crafting": false,
    "Allow Above Foundation": true,
    "Check If Home Is Valid On Listhomes": false,
    "Pay": 0,
    "Bypass": 0
  },
  "TPT": {
    "Use Friends": false,
    "Use Clans": false,
    "Use Teams": false,
    "Allow Cave": false
  },
  "TPR": {
    "Require Player To Be Friend, Clan Mate, Or Team Mate": false,
    "Allow Cave": false,
    "Allow TPB": true,
    "Cooldown": 600,
    "Countdown": 15,
    "Daily Limit": 5,
    "VIP Daily Limits": {
      "nteleportation.vip": 5
    },
    "VIP Cooldowns": {
      "nteleportation.vip": 5
    },
    "VIP Countdowns": {
      "nteleportation.vip": 5
    },
    "Request Duration": 30,
    "Block TPA On Ceiling": true,
    "Usable Out Of Building Blocked": false,
    "Usable Into Building Blocked": false,
    "Allow Cupboard Owner When Building Blocked": true,
    "Allow Crafting": false,
    "Pay": 0,
    "Bypass": 0
  },
  "Dynamic Commands": {
    "Town": {
      "Command Enabled": true,
      "Allow TPB": true,
      "Allow Cave": false,
      "Cooldown": 600,
      "Countdown": 15,
      "Daily Limit": 5,
      "VIP Daily Limits": {
        "nteleportation.vip": 5
      },
      "VIP Cooldowns": {
        "nteleportation.vip": 5
      },
      "VIP Countdowns": {
        "nteleportation.vip": 5
      },
      "Location": "0 0 0",
      "Locations": [],
      "Teleport To Random Location": false,
      "Usable Out Of Building Blocked": false,
      "Allow Crafting": false,
      "Pay": 0,
      "Bypass": 0
    },
    "Island": {
      "Command Enabled": true,
      "Allow TPB": false,
      "Allow Cave": false,
      "Cooldown": 600,
      "Countdown": 15,
      "Daily Limit": 5,
      "VIP Daily Limits": {
        "nteleportation.vip": 5
      },
      "VIP Cooldowns": {
        "nteleportation.vip": 5
      },
      "VIP Countdowns": {
        "nteleportation.vip": 5
      },
      "Location": "0 0 0",
      "Locations": [],
      "Teleport To Random Location": true,
      "Usable Out Of Building Blocked": false,
      "Allow Crafting": false,
      "Pay": 0,
      "Bypass": 0
    },
    "Outpost": {
      "Command Enabled": true,
      "Allow TPB": true,
      "Allow Cave": false,
      "Cooldown": 600,
      "Countdown": 15,
      "Daily Limit": 5,
      "VIP Daily Limits": {
        "nteleportation.vip": 5
      },
      "VIP Cooldowns": {
        "nteleportation.vip": 5
      },
      "VIP Countdowns": {
        "nteleportation.vip": 5
      },
      "Location": "0 0 0",
      "Locations": [],
      "Teleport To Random Location": true,
      "Usable Out Of Building Blocked": false,
      "Allow Crafting": false,
      "Pay": 0,
      "Bypass": 0
    },
    "Bandit": {
      "Command Enabled": true,
      "Allow TPB": true,
      "Allow Cave": false,
      "Cooldown": 600,
      "Countdown": 15,
      "Daily Limit": 5,
      "VIP Daily Limits": {
        "nteleportation.vip": 5
      },
      "VIP Cooldowns": {
        "nteleportation.vip": 5
      },
      "VIP Countdowns": {
        "nteleportation.vip": 5
      },
      "Location": "0 0 0",
      "Locations": [],
      "Teleport To Random Location": true,
      "Usable Out Of Building Blocked": false,
      "Allow Crafting": false,
      "Pay": 0,
      "Bypass": 0
    }
  }
}
```
  
Multiple entries for different levels of VIP can be created here. The default and included entry is for `nteleportation.vip`.  Others added here will cause the plugin to register Oxide permissions for them upon plugin reload. After the permissions have been created, they can be assigned to oxide users or groups as desired.
  
If Pay is set for `/home`, `/tpr`, or `/town`, and Economics or ServerRewards is available, using these commands will withdraw the configured amount from their balance.
  
If Bypass is set for `/home`, `/tpr`, or `/town|outpost|bandit`, and Economics or ServerRewards is available, using these commands during a cooldown period will ask if the player wants to pay to bypass the cooldown. Note that if you elect to bypass cooldown by paying for a `/tpr`, you will pay the bypass cost even if the target does not accept via `/tpa`. Only after a successful `/tpa` and teleport will you pay the Pay cost.

The pay and bypass costs are 0 by default, which means they cost 0. Set to -1 to disable them. Set above 0 to add a cost.

This also requires the global setting Bypass CMD (default "pay"). This is a keyword to use for the bypass (set empty to disable bypasses), e.g.:
  
- /town pay
- /home 1 pay

You must also set UseEconomics to true to enable this usage of the Economics plugin.  You may also set UseServerRewards to true to enable usage of the ServerRewards plugin.  If both are set to true, Economics will be checked first.

For the InterruptTPOnCold/Hot settings, be careful adjusting the default values for MinimumTemp and MaximumTemp.  The user will only display Cold/Hot between 0 and 40C. Otherwise they will likely be confused.  However, negative values for MinimumTemp should be possible.  **Note**: If you want to actually change the defaults you need to set InterruptTPOnCold/Hot to true.  Then set the min/max temps. This is true even if you want to NOT interrupt on cold/hot - in that case set the temps to some extremes that would not likely be met such as -30 and 100.

If InterruptTPOnHostile is true, and the player is considered hostile, `/outpost` and `/bandit` will be blocked

If InterruptTpOnHurt is true, the teleport may still be interrupted when hot/cold even if InterruptTPOnCold/Hot are false.  This is because the player is taking damage when hot or cold.

The CaveDistance{Small/Medium/Large} settings are available to tweak distance from caves required when AllowCave == false inside of the Home/Town/TPR config sections. Anything within those distances (from the player) should be blocked.

StrictFoundationCheck: Default false.  If set to true, perform an additional check to ensure that the player is at least near the center of a foundation or floor. This is not run on tpa/tpr but is on sethome/home.

DefaultMonumentSize: This is required if InterruptTPOnMonument is set to true. Many monuments do not present their size when queried - at least how we are currently doing it. For those that do not, this will be the default distance required for using /home, etc.

## Permissions

- nteleportation.home - /home, /sethome, /removehome
- nteleportation.deletehome - /home delete & /deletehome  
- nteleportation.homehomes - /home homes & /homehomes  
- nteleportation.importhomes - teleport.importhomes  
- nteleportation.radiushome - /home radius & /radiushome  
- nteleportation.tp - /tp  - **DO NOT GIVE THIS TO PLAYERS! DO NOT GIVE THIS TO DEFAULT GROUP!**
- nteleportation.tpb - /tpb  
- nteleportation.tpr - /tpr
- nteleportation.tpconsole - teleport.topos & teleport.toplayer  
- nteleportation.tphome - /home tp and /tphome  
- nteleportation.tptown - /town  
- nteleportation.tpoutpost - /outpost  
- nteleportation.tpbandit - /bandit
- nteleportation.tpn - /tpn  
- nteleportation.tpl - /tpl  
- nteleportation.tpremove - /tpremove  
- nteleportation.tpsave - /tpsave  
- nteleportation.wipehomes - /wipehomes  
- nteleportation.crafthome - allow craft during home tp  
- nteleportation.crafttown - allow craft during town tp  
- nteleportation.craftoutpost - allow craft during outpost tp  
- nteleportation.craftbandit - allow craft during bandit tp  
- nteleportation.crafttpr - allow craft during tpr tp  
- nteleportation.tpt - allow instant tpa
- nteleportation.tpisland - allows using /island and /town island
- nteleportation.craftisland - allows crafting while using /island or /town island
- nteleportation.bypassfoundationcheck
- nteleportation.exemptfrominterruptcountdown - exempts the user from being interrupted during teleport countdown
- nteleportation.globalcooldownvip - the time this vip user has a global cooldown
- nteleportation.tpmarker - allows user to teleport by placing a marker on the map

## Commands

### Chat

- home add  NAME - Saves your current position as the location NAME. (alias sethome)  
- home list - Shows you a list of all the locations you have saved. (alias listhomes)  
- home remove NAME - Removes the location NAME from your saved homes. (alias removehome)  
- home NAME - Teleports you to the home location.  
- home NAME pay - Teleports you to the home location NAME, bypassing cooldown by paying from your Economics balance.  
- tpr  - Sends a teleport request to the player.  
- tpa - Accepts an incoming teleport request.  
- tpc - Cancel teleport or request.  
- town - Teleports yourself to town (if set).
- outpost - Teleports yourself to Outpost (if set).
- bandit - Teleports yourself to Bandit Town (if set).
- town/outpost/bandit pay - Teleports you to town/outpost/bandit, bypassing cooldown by paying from your Economics balance.  
  - e.g. /town pay
- tpinfo - Shows limits and cooldowns.  
- tphelp - Shows help.
- island <number> - teleports you to the specified island
- island add - adds a spawn point at the admins current location (DO NOT ADD WHERE PLAYERS CAN BUILD)
- tpat - toggles automatic tpa on/off for the specific player-

Admin:  

- tp  - Teleports yourself to the target player.  
- tp   - Teleports the player to the target player.  
- tp    - Teleports you to the set of coordinates.  
- tpl - Shows a list of saved locations.  
- tpl {name} - Teleports you to a saved location.  
- tpsave  - Saves your current position as the location name.  
- tpremove  - Removes the location from your saved list.  
- tpb - Teleports you back to the place where you were before teleporting.  
- home radius  - Find all homes in radius.  
- home delete   - Remove a home from a player.  
- home tp   - Teleports you to the home location with the name 'name' from the player.  
- home homes  - Shows you a list of all homes from the player.  
- home wipe - Removes all homes.  
- town set - Saves the current location as town.  
- outpost set - Saves the current location as Outpost.
- bandit set - Saves the current location as Bandit Town.
- wipehomes - Removes all homes.  
  
  Covalence commands can now be added via config:

- Reimplemented outpost, bandit, town and island commands as defaults in this list, as such their settings have been reset
- Reimplemented all relevant language messages - Russian translations provided by MoNaH
- Added covalence command:
- `ntp add/remove/list <name>` - requires permission nteleportation.admin
- Example:
- `ntp add farm` - adds command farm, requiring permissions:
- `nteleportation.tpfarm` - to teleport using this command
- `nteleportation.craftfarm` - to craft while using this command
- `ntp remove farm` - removes the command farm
- `ntp list` - lists all commands (same as `tpinfo`)
- `o.grant group default nteleportation.tpfarm` - allows all players to use this command
- `o.grant user nivex nteleportation.tpfarm` - allows `nivex` to use this command
- Commands are added to the configuration file where settings can be configured to your liking

### Console

- teleport.topos     - teleports player to position  
- teleport.toplayer   - teleports player to targetplayer  
- teleport.importhomes - imports homes from m-Teleportation  

## For Developers

```csharp
Dictionary GetHomes(object playerObj) // param playerObj string/ulong playerId
```

```csharp
int GetLimitRemaining(BasePlayer player, string type) // param type: home, tpr, town
```

```csharp
int GetCooldownRemaining(BasePlayer player, string type) // param type: home, tpr, town
```

```csharp
int GetCountdownRemaining(BasePlayer player, string type) // param type: home, tpr, town
```

## Tutorials

[SRT Bull NTeleportation Tutorial](https://www.youtube.com/watch?v=yXId_jCnup8)

## Credits

- **Nogrod**, the original author of this plugin
- **rfc1920**, for helping maintain the plugin
- **Loup-des-Neiges of [FR] Bestiaire.eu**, for helping and fundng this plugin
- **CMEPTb**, for the old Russian translation
- **MONaH**, for the new Russian translation
- **nivex**, for helping maintain the plugin

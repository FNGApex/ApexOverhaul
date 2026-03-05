# ApexOverhaul — 7 Days to Die Modding Project

## Decompiled Source Reference

The `decompiled/` directory contains the full decompiled source of the game's C# assemblies:

- `decompiled/Assembly-CSharp/` — Main game code (3,991 .cs files). All gameplay logic lives here.
- `decompiled/Assembly-CSharp-firstpass/` — Utility/third-party libs (InControl, FullSerializer, etc.). No game logic.

Source DLLs:
- macOS: `~/Library/Application Support/Steam/steamapps/common/7 Days To Die/7DaysToDie.app/Contents/Resources/Data/Managed/`
- Windows: `D:\SteamLibrary\steamapps\common\7 Days To Die\7DaysToDie_Data\Managed\`

Decompiled with: `ilspycmd` v9.1.0 (requires `DOTNET_ROLL_FORWARD=LatestMajor` on .NET 10+)

## Key Files for Quest/POI Cooldown Work

| File | What It Contains |
|---|---|
| `decompiled/Assembly-CSharp/QuestLockInstance.cs` | POI lockout timer (2000 ticks after quest unlock) |
| `decompiled/Assembly-CSharp/QuestTraderData.cs` | Per-trader completed POI tracking, 7-day reset for tiers 4-6 |
| `decompiled/Assembly-CSharp/QuestEventManager.cs` | Master quest event system, `CheckForPOILockouts()` |
| `decompiled/Assembly-CSharp/EntityTrader.cs` | `PopulateActiveQuests()`, `UpdateLocations()` |
| `decompiled/Assembly-CSharp/ObjectiveRandomPOIGoto.cs` | POI selection via `GetRandomPOINearTrader()` |
| `decompiled/Assembly-CSharp/DynamicPrefabDecorator.cs` | `GetRandomPOINearTrader()`, `GetRandomPOINearWorldPos()` — filters locked POIs |
| `decompiled/Assembly-CSharp/QuestJournal.cs` | Player quest state, `GetUsedPOIs()`, `AddPOIToTraderData()` |
| `decompiled/Assembly-CSharp/NPCQuestData.cs` | NPC quest list cache with `LastUpdate` timestamp |
| `decompiled/Assembly-CSharp/Quest.cs` | Quest instance, state machine, position data |
| `decompiled/Assembly-CSharp/QuestClass.cs` | Quest definitions parsed from XML |

## Mod Deployment Convention

Compiled `.dll` files go in the **mod's root folder**, NOT in a `Harmony/` subfolder.

```
Mods/
  MyMod/
    ModInfo.xml
    MyMod.dll        <-- DLL goes here, at the mod root
```

Game Mods folder: `D:\SteamLibrary\steamapps\common\7 Days To Die\Mods\`

Each mod has `build.sh` (compile only) and `deploy.sh` (compile + copy to Mods folder).

## Sub-Project Docs

- `QuestTweaks/Claude.md` — Detailed reference for the POI quest cooldown reduction mod

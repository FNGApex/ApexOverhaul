# NetworkCrafting — 7 Days to Die Mod

## Overview

A mod for **7 Days to Die V2.6 EXP** that allows players to craft using resources from all nearby containers within their Land Claim Block (LCB) range. Resources from broadcasting containers appear automatically in the crafting grid and all workstations.

## Core Requirements

### Container Broadcasting System

- Every container (storage boxes, writable storage, gun safes, desk safes, ALL container types) gets a `isBroadcasting` boolean property, **default: true**.
- Broadcasting state persists on save/load.
- Broadcasting containers share their inventory to a "network" scoped to the LCB they are within.
- Container name display format: `(Broadcasting) {Container Name}` when broadcasting is on.
- Toggle broadcasting via the **Hold E radial menu**, same location as Lock/Unlock and Rename.

### Network Inventory (Crafting Integration)

- When a player is inside their LCB range, all broadcasting container inventories are merged into the available resource pool.
- This works in:
  - Player crafting grid
  - Workbench
  - Chemistry Station (Chem Bench)
  - Cement Mixer
  - Forge (if applicable)
- **Resource pull priority**: Player inventory first, then network containers.
- When crafting consumes items, pull from player inventory first, then from broadcasting containers.

### Permissions

- Only players who have **edit permissions** on the LCB can use the container network.
- In multiplayer, each authorized player gets access to the network for that LCB.

### Status Effect / Buff

- When a player enters their LCB range, they receive a visible buff/status effect indicating "Network Crafting Active".
- Buff is removed when leaving LCB range.
- Buff is suppressed during Blood Moon.

### Restrictions

- **Blood Moon**: Network crafting is completely disabled during blood moon (horde night). Buff is removed/suppressed.
- **Block Upgrades**: Network does NOT pull resources for upgrading blocks.
- **Block Repairs**: Network DOES pull resources for repairing blocks.

## Architecture

### Folder Structure

```
Mods/
  NetworkCrafting/
    ModInfo.xml
    Config/
      buffs.xml
      localization.txt
    Harmony/
      NetworkCrafting.dll        (compiled output)
    src/
      Main.cs                    (Mod init, Harmony bootstrap)
      ContainerNetworkManager.cs (Tracks broadcasting containers per LCB)
      Patches/
        ContainerBroadcastPatch.cs   (Toggle, save/load, display name)
        CraftingNetworkPatch.cs      (Crafting grid + workstation integration)
        RepairUpgradePatch.cs        (Repair allowed, upgrade blocked)
        BloodMoonPatch.cs            (Disable during horde night)
        PlayerBuffPatch.cs           (Apply/remove status effect in LCB)
        RadialMenuPatch.cs           (Hold E menu toggle option)
```

### C# Project Setup

- Target the .NET framework version used by 7D2D V2.6 EXP.
- Reference game DLLs from `7DaysToDie_Data/Managed/`:
  - `Assembly-CSharp.dll` (core game code)
  - `0Harmony.dll` (Harmony patching library)
  - `UnityEngine.dll` and `UnityEngine.CoreModule.dll`
  - Any other required Unity/game assemblies
- Output the compiled DLL to `Harmony/NetworkCrafting.dll`.

### Key Classes & Patches

#### `Main.cs` — Mod Entry Point

- Implements `IModApi`.
- Calls `Harmony.PatchAll()` to apply all patches.
- Registers event listeners for game start/stop.

#### `ContainerNetworkManager.cs` — Core Network Logic

- Singleton or static manager.
- Maintains a dictionary of LCB positions → list of broadcasting containers.
- Methods:
  - `RegisterContainer(TileEntityLootContainer container)` — Add container to network.
  - `UnregisterContainer(TileEntityLootContainer container)` — Remove from network.
  - `GetNetworkInventory(EntityPlayer player)` — Returns merged item list from all broadcasting containers the player has access to.
  - `ConsumeFromNetwork(EntityPlayer player, ItemStack[] required)` — Pulls items from network containers after local inventory is exhausted.
  - `IsPlayerInOwnedLCB(EntityPlayer player)` — Check if player is within an LCB they have edit rights to.
  - `IsBloodMoonActive()` — Check current blood moon state.
- **Performance**: Cache container lists per LCB. Update on events (container placed, destroyed, toggled) rather than re-scanning every frame.
- Listen for chunk load/unload to register/unregister containers.

#### `ContainerBroadcastPatch.cs` — Container Toggle & Display

- **Patch targets**: `TileEntityLootContainer`, `TileEntityWorkstation`, and any other container tile entities.
- Add `isBroadcasting` field (stored in TileEntity NBT data).
- Patch `Read()`/`Write()` to persist the boolean.
- Patch display name generation to prepend `(Broadcasting)` when active.

#### `RadialMenuPatch.cs` — Hold E Menu Integration

- Patch the radial/interact menu to add "Toggle Broadcasting" option.
- Place it near Lock/Unlock and Rename.
- Toggling updates the `isBroadcasting` state and notifies `ContainerNetworkManager`.

#### `CraftingNetworkPatch.cs` — Crafting & Workstation Integration

- Patch the crafting resource check to include network inventory.
- Patch the crafting consumption to pull from network after local inventory.
- Target classes/methods:
  - `XUiC_CraftingWindowGroup` or equivalent crafting UI controller.
  - `XUiC_WorkstationWindowGroup` for workstations.
  - The underlying `RecipeCraftingUtils` or equivalent that checks if player has materials.
- All workstations: Workbench, Chemistry Station, Cement Mixer, Forge.

#### `RepairUpgradePatch.cs` — Repair vs Upgrade Logic

- **Repair**: Patch block repair to check network inventory for required materials. Allow pulling from network.
- **Upgrade**: Patch block upgrade to explicitly SKIP network inventory. Only use player inventory.
- Target: `ItemActionRepair`, `ItemActionUpgradeBlock`, or the relevant block interaction methods.

#### `BloodMoonPatch.cs` — Horde Night Restriction

- Check `GameManager.Instance.World.IsBloodMoon()` or equivalent V2.6 API.
- When blood moon is active:
  - `ContainerNetworkManager.GetNetworkInventory()` returns empty.
  - `ConsumeFromNetwork()` is a no-op.
  - Status buff is removed.
- When blood moon ends: re-enable everything, reapply buff if player is still in LCB.

#### `PlayerBuffPatch.cs` — Status Effect Management

- Patch player position update or tick to check LCB proximity.
- Apply buff `buffNetworkCraftingActive` when:
  - Player is inside owned LCB range AND
  - Blood moon is NOT active.
- Remove buff when:
  - Player leaves LCB range OR
  - Blood moon starts.
- Avoid checking every frame — use a timer (every 1-2 seconds) or event-driven approach.

### XML Configs

#### `Config/buffs.xml`

```xml
<append xpath="/buffs">
    <buff name="buffNetworkCraftingActive" name_key="buffNetworkCraftingActiveName" description_key="buffNetworkCraftingActiveDesc" icon="ui_game_symbol_container" icon_color="0,200,100">
        <stack_type value="ignore"/>
        <duration value="0"/>
    </buff>
</append>
```

#### `Config/localization.txt`

```
Key,File,Type,UsedInMainMenu,NoTranslate,english
buffNetworkCraftingActiveName,Localization,Buff,,,"Network Crafting Active"
buffNetworkCraftingActiveDesc,Localization,Buff,,,"You can craft using resources from nearby broadcasting containers."
uiToggleBroadcasting,Localization,UI,,,"Toggle Broadcasting"
```

#### `ModInfo.xml`

```xml
<?xml version="1.0" encoding="utf-8"?>
<xml>
    <Name value="NetworkCrafting" />
    <DisplayName value="Network Crafting" />
    <Description value="Craft using resources from broadcasting containers within your Land Claim." />
    <Author value="YourName" />
    <Version value="1.0.0" />
    <Website value="" />
</xml>
```

## Design Decisions

1. **Resource pull priority**: Player inventory first, then broadcasting containers (nearest first if possible).
2. **Container types**: ALL container tile entities, including storage boxes, writable storage, gun safes, desk safes, workstation input/output slots.
3. **LCB range**: Uses the same protection radius as the Land Claim Block (respects server settings for LCB size).
4. **Multiplayer**: Each authorized player on an LCB gets independent access to the same container network.
5. **Performance**: Event-driven container registration (place/destroy/toggle/chunk load). Cached per LCB. No per-frame scanning.

## Build Order (Suggested)

1. `Main.cs` + `ModInfo.xml` — Get the mod loading.
2. `ContainerBroadcastPatch.cs` — Add isBroadcasting to containers, persist it.
3. `RadialMenuPatch.cs` — Toggle broadcasting via Hold E menu.
4. `ContainerNetworkManager.cs` — Core tracking/caching logic.
5. `PlayerBuffPatch.cs` + `buffs.xml` — Status effect when in LCB.
6. `CraftingNetworkPatch.cs` — The big one: merge network inventory into crafting.
7. `RepairUpgradePatch.cs` — Allow repair, block upgrade.
8. `BloodMoonPatch.cs` — Disable during horde night.
9. Testing & polish.

## Important Game Classes to Investigate

These are the key game classes you will need to decompile and study (use dnSpy or ILSpy on `Assembly-CSharp.dll`):

- `TileEntityLootContainer` — Base container tile entity
- `TileEntitySecureLootContainer` — Lockable containers
- `TileEntityWorkstation` — Workbench, Chem Station, etc.
- `PersistentPlayerData` — LCB ownership and permissions
- `World.GetLandClaimOwner()` or similar — Check LCB ownership
- `XUiC_CraftingWindowGroup` — Crafting UI logic
- `XUiC_WorkstationWindowGroup` — Workstation UI logic
- `ItemActionRepair` — Block repair action
- `Block.UpgradeBlock()` or equivalent — Block upgrade action
- `GameManager` — Blood moon state
- `EntityBuffs` — Applying/removing buffs
- `XUiC_ItemActionList` — Hold E radial menu entries

## Notes

- Always decompile `Assembly-CSharp.dll` from V2.6 EXP to verify method signatures before writing patches. Class names and methods change between alpha versions.
- Use `[HarmonyPrefix]` and `[HarmonyPostfix]` strategically. Prefix for blocking/modifying behavior, postfix for extending.
- Use `[HarmonyTranspiler]` only when prefix/postfix can't achieve the goal cleanly.
- Test in both singleplayer and multiplayer (dedicated server) as TileEntity syncing behaves differently.

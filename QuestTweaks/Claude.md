# Claude.md — 7 Days to Die: POI Quest Cooldown Reduction Mod

## Project Goal

Create a mod/patch for 7 Days to Die (V1.0+) that allows POIs that have already been quested in to become available for questing again **much sooner** than the game's default cooldown allows.

---

## How the Vanilla Quest System Works

### Quest Types

The game has several quest categories:

- **Tutorial Quests** — Given on first spawn; teaches basics (Basic Survival, White River Citizen).
- **Treasure Quests** — Player digs up a randomly placed treasure chest.
- **Trader Quests** — Randomly generated quests from traders with tiered rewards. These are the quests that use POIs and are the focus of this mod.

### Trader Quest Flow

1. Player talks to a trader and accepts a quest.
2. The quest assigns a random POI based on tier, distance, and availability.
3. Player travels to the POI and interacts with the exclamation mark (!) rally point.
4. **Interacting with the rally point fully resets the POI** — all blocks, zombies, loot containers, and sleepers are restored to their original prefab state.
5. Player completes objectives (clear sleepers, fetch item, etc.).
6. Player returns to trader to turn in the quest.

### Quest Objective Types (from quests.xml)

- `RandomPOIGoto` — Selects and navigates to a random POI.
- `RallyPoint` — The (!) marker that activates and resets the POI.
- `ClearSleepers` — Kill all sleeper zombies in the POI.
- `FetchFromContainer` — Find a specific quest item in a container.
- `POIStayWithin` — Player must remain within a radius of the POI.
- `ReturnToNPC` — Go back to the trader to turn in.

### Quest Tiers

- **Tiers 1–6** in current versions (A21 / V1.0).
- Each tier corresponds to POI difficulty ratings.
- A20 only had Tiers 1–5 with very few T5 POIs (about 7), causing heavy repetition.
- A21+ added Tier 6 and more T5/T6 POIs to the quest pool.
- Players must complete a set number of quests per tier to unlock the next tier.

### Quest XML Structure (quests.xml)

Quests are defined in `Data/Config/quests.xml`. A typical trader quest looks like:

```xml
<quest id="tier1_clear">
    <property name="login_rally_reset" value="true" />
    <property name="completiontype" value="TurnIn" />

    <objective type="RandomPOIGoto">
        <property name="phase" value="1" />
    </objective>

    <objective type="RallyPoint">
        <property name="phase" value="2" />
    </objective>

    <objective type="ClearSleepers">
        <property name="phase" value="3" />
    </objective>

    <objective type="POIStayWithin">
        <property name="phase" value="3" />
        <property name="radius" value="25" />
    </objective>

    <objective type="ReturnToNPC">
        <property name="phase" value="4" />
    </objective>
</quest>
```

Key quest properties:
- `login_rally_reset` — If true, the rally point resets if the player logs out and back in before completing the quest.
- `completiontype` — Usually "TurnIn" (must return to trader).
- `phase` — Groups objectives that must be completed together.

---

## The POI Cooldown Problem

### What the Cooldown Does

After a quest is completed at a POI, the game places that POI on a cooldown timer. During this cooldown, that POI will **not** be offered as a quest target again by the same trader.

### Vanilla Cooldown Duration

- **Approximately 5 in-game days** per trader (some reports say up to 10 days in recent versions).
- This is a **per-trader** cooldown, NOT a global cooldown.
- Trader A won't send you to the same POI for ~5 days, but Trader B can still offer that same POI immediately.

### Distance Restriction (A21+)

- In A21+, traders will not assign quest POIs within a certain radius of their location (~10 km reported).
- This distance restriction does NOT extend across different traders.

### Why POIs Must Reset

- When a quest activates at a POI (player clicks the ! marker), the entire POI chunk is restored to its prefab default — from bedrock to sky limit.
- This is necessary because the player may have previously looted/damaged the POI, making quest objectives impossible to complete otherwise.
- The reset affects the entire chunk(s) the POI occupies (chunks are 16x16 blocks, and large POIs span multiple chunks).

### Land Claim Block Protection

- Placing a Land Claim Block (LCB) inside a POI prevents it from being assigned as a quest location (the ! marker won't activate).
- A bedroll also provides similar protection on a smaller area.
- This is important context: the game checks for LCBs/bedrolls before allowing quest activation.

---

## Where the Cooldown Is Controlled

### NOT in Vanilla XML

The POI quest cooldown timer value **does not appear in any vanilla XML configuration file**. It is not in:
- `quests.xml`
- `blocks.xml`
- `dialogs.xml`
- `traders.xml`
- `gamestages.xml`

Modders have confirmed that the cooldown properties used by mods like "No Repeat POIs" added custom properties to `blocks.xml` that don't exist in vanilla. Those mods also required a DLL component to read those custom properties.

### Hardcoded in C# (Assembly-CSharp.dll)

The cooldown logic is **hardcoded in the game's C# assembly**. Key details:

- The assembly file is located at: `7 Days To Die/7DaysToDie_Data/Managed/Assembly-CSharp.dll`
- The quest POI selection logic, including cooldown checks, lives in C# classes likely named something like `QuestEventManager`, `TileEntityTrader`, `QuestJournal`, or similar.
- A Nexus mod author confirmed: quest cooldown values are likely hardcoded and not exposed for XML modding. A Harmony patch would probably be required.
- The limit on selling identical items to traders is also hardcoded (similar pattern).

### Decompiling the Assembly

To find the exact cooldown logic, decompile `Assembly-CSharp.dll`:

**Tools:**
- **ILSpy** (free, open source): https://github.com/icsharpcode/ILSpy
- **dnSpy** (free, includes debugger + editor): great for browsing and editing .NET assemblies

**Steps:**
1. Open `Assembly-CSharp.dll` in your decompiler.
2. Search for keywords: `cooldown`, `lockout`, `QuestPOI`, `usedPOI`, `POILockout`, `questReset`, `TraderQuest`.
3. Look in classes related to quest management, trader interactions, and POI selection.
4. Identify the hardcoded day/tick count for the cooldown.
5. Note the exact class name and method name — you'll need these for a Harmony patch.

---

## Modding Approaches (Ranked by Feasibility)

### Approach 1: Harmony Patch (RECOMMENDED)

**What it does:** Patches the C# method that checks POI cooldown at runtime, reducing or eliminating the wait.

**Requirements:**
- Harmony library (7D2D supports it natively via `0_TFP_Harmony` or modders bundle it).
- EAC (Easy Anti-Cheat) must be **disabled** (required for all C# mods).
- Basic C# knowledge.

**Mod structure:**
```
Mods/
  ReducedPOICooldown/
    ModInfo.xml
    Harmony/
      ReducedPOICooldown.dll
```

**ModInfo.xml (V2 format for V1.0+):**
```xml
<?xml version="1.0" encoding="UTF-8"?>
<xml>
    <Name value="ReducedPOICooldown" />
    <DisplayName value="Reduced POI Quest Cooldown" />
    <Description value="Reduces the cooldown before a POI can be quested again" />
    <Author value="YourName" />
    <Version value="1.0.0" />
</xml>
```

**Example Harmony patch skeleton (C#):**
```csharp
using HarmonyLib;
using System.Reflection;

public class ReducedPOICooldownInit : IModApi
{
    public void InitMod(Mod _modInstance)
    {
        var harmony = new Harmony("com.yourname.reducedpoicooldown");
        harmony.PatchAll(Assembly.GetExecutingAssembly());
        Log.Out("[ReducedPOICooldown] Harmony patches applied.");
    }
}

// EXAMPLE — actual class/method names must be found by decompiling Assembly-CSharp.dll
// Look for the method that checks POI cooldown when a trader selects quest POIs

[HarmonyPatch(typeof(QuestEventManager), "IsPOIAvailableForQuest")]  // PLACEHOLDER NAME
public class PatchPOICooldown
{
    // Prefix: runs before the original method
    // Return false to skip original method entirely
    static bool Prefix(ref bool __result /* other params */)
    {
        // Option A: Always return true (POI is always available)
        __result = true;
        return false; // skip original method

        // Option B: Reduce cooldown (check a shorter time window)
        // Requires reading the cooldown timestamp and comparing
        // against a reduced value instead of the default
    }
}
```

**What to search for when decompiling:**
- A method that takes a POI reference and returns a bool (available/unavailable).
- A dictionary or list that stores "recently used POI" timestamps.
- A comparison against a day count (likely 5 or 10).
- Classes: `QuestEventManager`, `TileEntityTrader`, `QuestJournal`, `TraderData`, `ObjectiveRandomPOIGoto`.

### Approach 2: XML Dialog Reset Workaround

**What it does:** Adds a dialog option at the trader that lets you manually reset/refresh the quest pool on demand. Doesn't change the cooldown but works around it.

**Key files to modify via XPath:**
- `dialogs.xml` — Add new dialog options to the trader conversation tree.
- `quests.xml` — Potentially add a hidden quest that triggers a reset.

**Existing mod reference:** "Reset Quests" mod — a server-side XML mod that adds a quest reset option to the trader dialog menu. No admin rights needed.

**Mod structure:**
```
Mods/
  QuestResetDialog/
    ModInfo.xml
    Config/
      dialogs.xml    (XPath patches)
```

### Approach 3: Custom XML Properties + DLL (Advanced)

This is what the "No Repeat POIs" mod did — it added custom cooldown properties to `blocks.xml` and included a DLL to read them. You could do the reverse: add properties that set the cooldown to a very low value.

This requires both XML modding AND a C# DLL, making it more complex than a pure Harmony patch.

---

## XPath Modding Reference (for XML approaches)

7 Days to Die uses XPath to modify XML without replacing entire files. Modlets go in the `Mods/` folder.

### Key XPath Commands

```xml
<!-- Change an existing value -->
<set xpath="/quests/quest[@id='tier1_clear']/property[@name='some_property']/@value">new_value</set>

<!-- Add a new node -->
<append xpath="/quests/quest[@id='tier1_clear']">
    <property name="new_property" value="some_value" />
</append>

<!-- Remove a node -->
<remove xpath="/quests/quest[@id='tier1_clear']/property[@name='unwanted']" />

<!-- Insert before a specific node -->
<insertBefore xpath="/quests/quest[@id='tier2_clear']">
    <quest id="my_custom_quest">...</quest>
</insertBefore>

<!-- Set an attribute -->
<setattribute xpath="/quests/quest[@id='tier1_clear']" name="new_attr">value</setattribute>
```

### XPath File Naming

The XPath patch file must target a specific vanilla XML file. Name your file to match:
- To patch `quests.xml` → create `Config/quests.xml` in your modlet
- To patch `dialogs.xml` → create `Config/dialogs.xml` in your modlet
- To patch `blocks.xml` → create `Config/blocks.xml` in your modlet

### ModInfo.xml V2 Template (for V1.0+)

```xml
<?xml version="1.0" encoding="UTF-8"?>
<xml>
    <Name value="MyModName" />
    <DisplayName value="My Mod Display Name" />
    <Description value="What this mod does" />
    <Author value="YourName" />
    <Version value="1.0.0" />
</xml>
```

---

## Key Files and Locations

| File / Path | Purpose |
|---|---|
| `Data/Config/quests.xml` | Quest definitions, objectives, tiers, rewards |
| `Data/Config/dialogs.xml` | Trader dialog trees and conversation options |
| `Data/Config/traders.xml` | Trader inventory, restock, quest offering config |
| `Data/Config/blocks.xml` | Block definitions (POI cooldown props NOT in vanilla) |
| `Data/Config/gamestages.xml` | Gamestage progression |
| `Data/Config/rwgmixer.xml` | Random World Gen rules, POI placement |
| `Data/Prefabs/POIs/` | Individual POI prefab XML files (per-POI properties) |
| `7DaysToDie_Data/Managed/Assembly-CSharp.dll` | Main game C# assembly (cooldown logic lives here) |
| `Mods/` folder | Where modlets and Harmony mods are installed |

---

## POI Properties (Prefab XML)

Each POI has an XML file in `Data/Prefabs/POIs/` with properties like:

```xml
<property name="TraderArea" value="True" />           <!-- Is this a trader POI? -->
<property name="TraderAreaProtect" value="20,0,20" />  <!-- Protection zone size -->
<property name="TraderAreaTeleportSize" value="36, 20, 36" />
<property name="TraderAreaTeleportCenter" value="1, 1, 1" />
<property name="QuestReset" />                         <!-- May not exist in vanilla -->
```

POI properties like `ThemeTags` and `ThemeRepeatDistance` control spawning distance between similar POIs during world generation — not quest cooldowns.

---

## Important Gotchas

1. **EAC must be disabled** for any C# / Harmony / DLL mod. Launch using `7DaysToDie.exe` directly, not the EAC launcher.

2. **POI reset is chunk-based.** When a quest activates, the entire chunk(s) the POI occupies are restored. Large POIs can span multiple 16x16 chunks. Nearby player-built structures in the same chunk WILL be destroyed.

3. **LCB blocks quest activation.** If the player has a Land Claim Block in the POI, the quest ! marker won't activate. Your mod shouldn't need to worry about this, but be aware.

4. **Per-trader vs global cooldown.** The vanilla cooldown is per-trader. If your mod reduces it, players could still get the same POI from a different trader immediately (this is vanilla behavior). Consider whether you want to address both.

5. **Modlets survive game updates; direct XML edits don't.** Always use the XPath modlet system, never edit files in `Data/Config/` directly.

6. **Server-side vs client-side.** XML-only modlets that modify quests/dialogs/traders are typically server-side only. Harmony/DLL mods usually need to be on both server and client, with EAC disabled.

7. **`UnlockPOI` objective.** Some custom quest mods use an `UnlockPOI` objective to ensure POIs can be properly reset after quest completion. If you're adding custom quest logic, consider including this.

---

## Existing Mods for Reference

| Mod | What It Does | Type |
|---|---|---|
| **No Repeat POIs** (Nexus, deleted) | Added cooldown per POI via blocks.xml + DLL | DLL + XML |
| **Reset Quests** (7daystodiemods.com) | Adds trader dialog option to reset quest pool | XML only, server-side |
| **POI Perfectionist** (Nexus) | Makes every quest a one-time deal (opposite of your goal) | Unknown |
| **Quest System Revamp A21** (Nexus) | Overhauls tier progression to 10 tiers | XML only |
| **Trader Quest Addendum** (Nexus) | Adds new quest types (defend, speed clear, etc.) | XML, uses `UnlockPOI` |
| **Custom POI For Quests** (7daystodiemods.com) | Lets modders assign specific POIs to quests | DLL + XML |

---

## Recommended Development Steps

1. **Decompile `Assembly-CSharp.dll`** with dnSpy or ILSpy.
2. **Search for the cooldown logic** — keywords: `cooldown`, `lockout`, `usedPOI`, `POI`, `questTimer`, `availableForQuest`.
3. **Identify the exact class and method** that checks whether a POI is eligible for questing.
4. **Note the hardcoded cooldown value** (likely an integer representing game days).
5. **Write a Harmony Prefix patch** that either:
   - Reduces the cooldown value before the check runs.
   - Always returns "available" regardless of cooldown.
   - Makes the cooldown configurable via an XML property you define.
6. **Test in single player first** with EAC disabled.
7. **Package as a modlet** with proper `ModInfo.xml`.

---

## Tools Needed

- **dnSpy** or **ILSpy** — Decompile Assembly-CSharp.dll
- **Visual Studio** or **VS Code + .NET SDK** — Write Harmony patch C#
- **Notepad++** or similar — Edit XML files
- **7 Days to Die Mod Launcher** (optional) — Manage mod installations
- Reference: Harmony docs at https://harmony.pardeike.net/
- Reference: 7D2D XPath wiki at https://7daystodie.fandom.com/wiki/XPath_Explained
- Reference: 7D2D Modding Forum at https://community.7daystodie.com/

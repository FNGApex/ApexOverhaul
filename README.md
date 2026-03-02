# ApexOverhaul

A collection of 7 Days to Die mods.

---

## NetworkCrafting

Craft, repair, and upgrade using items stored in nearby containers — without moving everything into your backpack first.

### How It Works

Any loot container (chest, storage crate, secure storage) inside your **Land Claim Block** can be toggled to "broadcast" its inventory to the crafting network. While broadcasting, its contents are treated as part of your inventory for all crafting and repair operations.

### Features

- **Network Crafting** — Crafting recipes automatically draw from broadcasting containers when your backpack is short on materials. The exact deficit is pulled from the network; nothing extra is taken.

- **Network Repair** — Repairing items and damaged blocks works the same way. If your inventory is missing required resources, the network covers the shortfall.

- **Toggle Broadcasting** — Hold `E` on any loot container inside your claim to access the radial menu. A "Toggle Broadcasting" option lets you include or exclude individual containers from the network. The state is highlighted when active and persists through save/load.

- **Visual Indicator** — Broadcasting containers display `(Broadcasting)` in their name so you always know what's in the network at a glance.

- **Status Buff** — While you're inside your claim and the network is available, a `buffNetworkCraftingActive` buff is applied. The buff disappears the moment you leave the claim.

- **Blood Moon Lockout** — The network is disabled during horde night. The buff is removed instantly when the blood moon starts, with no delay.

### Constraints

- Only containers within your **own** LCB (or one you have edit access to) are included.
- Upgrade actions are intentionally **excluded** — only repairs draw from the network.
- The network is per-LCB; containers from different claims are independent.

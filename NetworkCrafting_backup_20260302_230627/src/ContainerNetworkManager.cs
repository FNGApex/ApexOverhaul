using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Core singleton that tracks all broadcasting containers grouped by their
/// containing LCB. Provides the merged network inventory to crafting patches.
/// </summary>
public class ContainerNetworkManager
{
    private static ContainerNetworkManager instance;
    public static ContainerNetworkManager Instance => instance ?? (instance = new ContainerNetworkManager());

    // LCB owner position (centre of the claim) → set of broadcasting containers in that claim
    private readonly Dictionary<Vector3i, HashSet<TileEntityLootContainer>> networkContainers
        = new Dictionary<Vector3i, HashSet<TileEntityLootContainer>>();

    // -----------------------------------------------------------------------
    // Game lifecycle
    // -----------------------------------------------------------------------

    public void OnGameStarted()
    {
        ScanAllLoadedContainers();
    }

    public void OnGameShutdown()
    {
        networkContainers.Clear();
    }

    // -----------------------------------------------------------------------
    // Container registration
    // -----------------------------------------------------------------------

    /// <summary>Add a container to the network for its LCB, if it is broadcasting.</summary>
    public void RegisterContainer(TileEntityLootContainer container)
    {
        if (container == null) return;
        if (!ContainerBroadcastHelper.IsBroadcasting(container)) return;

        Vector3i lcbPos;
        if (!TryGetOwningLCBPos(container.ToWorldPos(), out lcbPos)) return;

        if (!networkContainers.ContainsKey(lcbPos))
            networkContainers[lcbPos] = new HashSet<TileEntityLootContainer>();

        networkContainers[lcbPos].Add(container);
    }

    /// <summary>Remove a container from any network it belongs to.</summary>
    public void UnregisterContainer(TileEntityLootContainer container)
    {
        if (container == null) return;

        Vector3i lcbPos;
        if (!TryGetOwningLCBPos(container.ToWorldPos(), out lcbPos)) return;

        if (networkContainers.TryGetValue(lcbPos, out var set))
            set.Remove(container);
    }

    /// <summary>
    /// Called when the broadcasting toggle changes so the cache stays consistent.
    /// </summary>
    public void OnBroadcastingToggled(TileEntityLootContainer container)
    {
        if (ContainerBroadcastHelper.IsBroadcasting(container))
            RegisterContainer(container);
        else
            UnregisterContainer(container);
    }

    // -----------------------------------------------------------------------
    // Inventory queries
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns all ItemStacks available in the network for the given player.
    /// Returns an empty list when the player has no access or blood moon is active.
    /// </summary>
    public List<ItemStack> GetNetworkInventory(EntityPlayer player)
    {
        var result = new List<ItemStack>();
        if (player == null) return result;
        if (IsBloodMoonActive()) return result;

        Vector3i lcbPos;
        if (!IsPlayerInOwnedLCB(player, out lcbPos)) return result;

        if (!networkContainers.TryGetValue(lcbPos, out var containers)) return result;

        foreach (var container in containers)
        {
            if (container == null || !ContainerBroadcastHelper.IsBroadcasting(container)) continue;
            if (container.items == null) continue;

            foreach (var stack in container.items)
            {
                if (stack != null && !stack.IsEmpty())
                    result.Add(stack.Clone());
            }
        }

        return result;
    }

    /// <summary>
    /// Consumes <paramref name="required"/> items from networking containers
    /// AFTER the caller has already deducted what it could from player inventory.
    /// Returns true if the full requirement was satisfied.
    /// </summary>
    public bool ConsumeFromNetwork(EntityPlayer player, ItemStack[] required)
    {
        if (player == null || required == null) return false;
        if (IsBloodMoonActive()) return false;

        Vector3i lcbPos;
        if (!IsPlayerInOwnedLCB(player, out lcbPos)) return false;

        if (!networkContainers.TryGetValue(lcbPos, out var containers)) return false;

        // Build a mutable list of remaining amounts to pull
        var remaining = new Dictionary<int, int>(); // itemValue.type → count
        foreach (var req in required)
        {
            if (req == null || req.IsEmpty()) continue;
            int type = req.itemValue.type;
            if (remaining.ContainsKey(type))
                remaining[type] += req.count;
            else
                remaining[type] = req.count;
        }

        // First pass: check we have enough in the network
        foreach (var container in containers)
        {
            if (container == null || container.items == null) continue;
            foreach (var stack in container.items)
            {
                if (stack == null || stack.IsEmpty()) continue;
                int type = stack.itemValue.type;
                if (remaining.ContainsKey(type))
                    remaining[type] = System.Math.Max(0, remaining[type] - stack.count);
            }
        }

        foreach (var pair in remaining)
            if (pair.Value > 0) return false; // not enough in network

        // Rebuild remaining for actual consumption pass
        foreach (var req in required)
        {
            if (req == null || req.IsEmpty()) continue;
            int type = req.itemValue.type;
            if (remaining.ContainsKey(type))
                remaining[type] += req.count;
            else
                remaining[type] = req.count;
        }

        // Second pass: actually remove items from containers
        foreach (var container in containers)
        {
            if (container == null || container.items == null) continue;
            bool modified = false;

            for (int i = 0; i < container.items.Length; i++)
            {
                var stack = container.items[i];
                if (stack == null || stack.IsEmpty()) continue;

                int type = stack.itemValue.type;
                if (!remaining.ContainsKey(type) || remaining[type] <= 0) continue;

                int take = System.Math.Min(remaining[type], stack.count);
                remaining[type] -= take;
                stack.count -= take;
                if (stack.count <= 0)
                    container.items[i] = new ItemStack(ItemValue.None.Clone(), 0);

                modified = true;
            }

            if (modified)
                container.SetModified();
        }

        return true;
    }

    // -----------------------------------------------------------------------
    // LCB helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns true when the player is inside an LCB they have edit rights to,
    /// and outputs the LCB block position.
    ///
    /// Uses m_lpBlockMap (Vector3i → PersistentPlayerData) for a single flat pass:
    /// position check first, permission check only when the player is in range.
    /// </summary>
    public bool IsPlayerInOwnedLCB(EntityPlayer player, out Vector3i lcbPos)
    {
        lcbPos = Vector3i.zero;
        if (player == null) return false;

        PersistentPlayerList playerList = GameManager.Instance.GetPersistentPlayerList();
        if (playerList == null) return false;

        Vector3i playerBlockPos = new Vector3i(
            Mathf.FloorToInt(player.position.x),
            Mathf.FloorToInt(player.position.y),
            Mathf.FloorToInt(player.position.z));

        int lcbRadius = GamePrefs.GetInt(EnumGamePrefs.LandClaimSize) / 2;

        foreach (var kvp in playerList.m_lpBlockMap)
        {
            Vector3i claimPos = kvp.Key;

            // Fast spatial check first — skip if not in range
            if (Mathf.Abs(playerBlockPos.x - claimPos.x) > lcbRadius ||
                Mathf.Abs(playerBlockPos.z - claimPos.z) > lcbRadius)
                continue;

            // Player is in range — now check edit permission
            PersistentPlayerData data = kvp.Value;
            if (data == null) continue;

            bool canEdit = data.PrimaryId.Equals(Platform.PlatformManager.InternalLocalUserIdentifier) ||
                           (data.ACL != null && data.ACL.Contains(Platform.PlatformManager.InternalLocalUserIdentifier));
            if (!canEdit) continue;

            lcbPos = claimPos;
            return true;
        }

        return false;
    }

    // Overload without the out param for callers that just need a boolean
    public bool IsPlayerInOwnedLCB(EntityPlayer player)
    {
        Vector3i dummy;
        return IsPlayerInOwnedLCB(player, out dummy);
    }

    /// <summary>True when a blood moon (horde night) is currently active.</summary>
    public bool IsBloodMoonActive()
    {
        if (GameManager.Instance?.World == null) return false;
        return SkyManager.IsBloodMoonVisible();
    }

    // -----------------------------------------------------------------------
    // Private helpers
    // -----------------------------------------------------------------------

    private bool TryGetOwningLCBPos(Vector3i worldPos, out Vector3i lcbPos)
    {
        lcbPos = Vector3i.zero;
        PersistentPlayerList playerList = GameManager.Instance?.GetPersistentPlayerList();
        if (playerList == null) return false;

        int lcbRadius = GamePrefs.GetInt(EnumGamePrefs.LandClaimSize) / 2;

        foreach (var claimPos in playerList.m_lpBlockMap.Keys)
        {
            if (Mathf.Abs(worldPos.x - claimPos.x) <= lcbRadius &&
                Mathf.Abs(worldPos.z - claimPos.z) <= lcbRadius)
            {
                lcbPos = claimPos;
                return true;
            }
        }

        return false;
    }

    /// <summary>Walk all loaded chunks and register any broadcasting containers found.</summary>
    private void ScanAllLoadedContainers()
    {
        World world = GameManager.Instance?.World;
        if (world == null) return;

        foreach (var chunk in world.ChunkCache.GetChunkArray())
        {
            if (chunk == null) continue;
            foreach (var te in chunk.GetTileEntities().list)
            {
                if (te is TileEntityLootContainer lootTE)
                    RegisterContainer(lootTE);
            }
        }
    }
}

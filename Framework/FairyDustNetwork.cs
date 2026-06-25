using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using StardewModdingAPI;
using SObject = StardewValley.Object;

namespace AutoFairyDust.Framework;

internal class FairyDustNetwork
{
    private readonly IMonitor Monitor;
    private readonly ModConfig Config;

    private readonly Dictionary<string, List<NetworkGroup>> LocationGroups = new();
    private readonly HashSet<string> DirtyLocations = new();

    public Dictionary<string, HashSet<Vector2>> ConnectedMachineTiles { get; } = new();
    public Dictionary<string, HashSet<Vector2>> UnconnectedMachineTiles { get; } = new();
    public Dictionary<string, HashSet<Vector2>> ChestTiles { get; } = new();

    public FairyDustNetwork(IMonitor monitor, ModConfig config)
    {
        this.Monitor = monitor;
        this.Config = config;
    }

    public string GetLocationKey(GameLocation location)
    {
        return location.uniqueName.Value != null && location.uniqueName.Value != location.Name
            ? $"{location.Name} ({location.uniqueName.Value})"
            : location.Name;
    }

    public void MarkDirty(GameLocation location)
    {
        if (location != null)
            DirtyLocations.Add(GetLocationKey(location));
    }

    public void MarkAllDirty()
    {
        DirtyLocations.Clear();
        LocationGroups.Clear();
        ConnectedMachineTiles.Clear();
        UnconnectedMachineTiles.Clear();
        ChestTiles.Clear();
    }

    public List<NetworkGroup> GetGroups(GameLocation location)
    {
        if (location == null) return null;

        string key = GetLocationKey(location);
        if (DirtyLocations.Contains(key))
        {
            RebuildLocation(location);
            DirtyLocations.Remove(key);
        }

        return LocationGroups.GetValueOrDefault(key);
    }

    public void RebuildLocation(GameLocation location)
    {
        string key = GetLocationKey(location);
        LocationGroups.Remove(key);
        ConnectedMachineTiles.Remove(key);
        UnconnectedMachineTiles.Remove(key);
        ChestTiles.Remove(key);

        var allChestTiles = new HashSet<Vector2>();
        var allMachineTiles = new HashSet<Vector2>();
        var allEntityTiles = new HashSet<Vector2>();

        foreach (Vector2 tile in GetAllTiles(location))
        {
            if (IsChest(location, tile, out _))
            {
                allChestTiles.Add(tile);
                allEntityTiles.Add(tile);
            }
            else if (HasMachine(location, tile, out _))
            {
                allMachineTiles.Add(tile);
                allEntityTiles.Add(tile);
            }
            else if (IsConnector(location, tile))
            {
                allEntityTiles.Add(tile);
            }
        }

        var validGroups = new List<NetworkGroup>();
        var visited = new HashSet<Vector2>();
        var connectedMachineTiles = new HashSet<Vector2>();

        foreach (Vector2 tile in allEntityTiles)
        {
            if (visited.Contains(tile))
                continue;

            var group = FloodFillGroup(location, tile, visited);
            if (group != null && group.Machines.Count > 0 && group.Chests.Count > 0)
            {
                validGroups.Add(group);
                foreach (var m in group.Machines)
                    connectedMachineTiles.Add(m.TileLocation);
            }
        }

        var unconnectedMachineTiles = new HashSet<Vector2>(allMachineTiles);
        unconnectedMachineTiles.ExceptWith(connectedMachineTiles);

        LocationGroups[key] = validGroups;
        ConnectedMachineTiles[key] = connectedMachineTiles;
        UnconnectedMachineTiles[key] = unconnectedMachineTiles;
        ChestTiles[key] = allChestTiles;
    }

    public void RebuildAll()
    {
        LocationGroups.Clear();
        ConnectedMachineTiles.Clear();
        UnconnectedMachineTiles.Clear();
        ChestTiles.Clear();
        DirtyLocations.Clear();

        foreach (var location in Game1.locations)
        {
            RebuildLocationRecursive(location);
        }
    }

    private void RebuildLocationRecursive(GameLocation location)
    {
        RebuildLocation(location);

        foreach (var building in location.buildings)
        {
            var indoors = building.GetIndoors();
            if (indoors != null)
            {
                RebuildLocationRecursive(indoors);
            }
        }
    }

    private static IEnumerable<Vector2> GetAllTiles(GameLocation location)
    {
        if (location.Map?.Layers == null || location.Map.Layers.Count == 0)
            yield break;

        int w = location.Map.Layers[0].LayerWidth;
        int h = location.Map.Layers[0].LayerHeight;

        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                yield return new Vector2(x, y);
    }

    private bool HasAnyEntity(GameLocation location, Vector2 tile)
    {
        return HasMachine(location, tile) || IsChest(location, tile, out _) || IsConnector(location, tile);
    }

    private NetworkGroup FloodFillGroup(GameLocation location, Vector2 origin, HashSet<Vector2> visited)
    {
        var group = new NetworkGroup();
        var queue = new Queue<Vector2>();
        queue.Enqueue(origin);

        while (queue.Count > 0)
        {
            Vector2 tile = queue.Dequeue();
            if (!visited.Add(tile))
                continue;

            bool added = false;

            if (HasMachine(location, tile, out var machine) && machine != null)
            {
                group.Machines.Add(machine);
                group.Tiles.Add(tile);
                added = true;
            }

            if (IsChest(location, tile, out var chest) && chest != null)
            {
                group.Chests.Add(chest);
                group.Tiles.Add(tile);
                added = true;
            }

            if (IsConnector(location, tile))
            {
                group.Tiles.Add(tile);
                added = true;
            }

            if (added)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        if (dx == 0 && dy == 0) continue;
                        var next = new Vector2(tile.X + dx, tile.Y + dy);
                        if (!visited.Contains(next))
                            queue.Enqueue(next);
                    }
                }
            }
        }

        return group;
    }

    private bool HasMachine(GameLocation location, Vector2 tile)
    {
        return HasMachine(location, tile, out _);
    }

    private bool HasMachine(GameLocation location, Vector2 tile, out SObject machine)
    {
        machine = null;
        if (!location.objects.TryGetValue(tile, out SObject obj))
            return false;
        if (obj is Chest)
            return false;
        if (obj.GetMachineData() == null)
            return false;
        machine = obj;
        return true;
    }

    private bool IsChest(GameLocation location, Vector2 tile, out Chest chest)
    {
        chest = null;
        if (!location.objects.TryGetValue(tile, out SObject obj))
            return false;
        if (obj is not Chest c)
            return false;

        string id = c.QualifiedItemId;
        if (id == "(BC)248")
            return false;

        chest = c;
        return true;
    }

    private bool IsConnector(GameLocation location, Vector2 tile)
    {
        if (Config.ConnectorNames.Count == 0)
            return false;

        if (location.terrainFeatures.TryGetValue(tile, out var feature) && feature is Flooring floor)
        {
            string itemId = floor.GetData()?.ItemId;
            if (itemId != null)
            {
                var itemData = ItemRegistry.GetData(itemId);
                if (itemData != null)
                {
                    if (Config.ConnectorNames.Contains(itemData.QualifiedItemId) ||
                        Config.ConnectorNames.Contains(itemData.InternalName))
                        return true;
                }
            }
        }

        if (location.objects.TryGetValue(tile, out SObject obj))
        {
            if (Config.ConnectorNames.Contains(obj.QualifiedItemId) ||
                Config.ConnectorNames.Contains(obj.Name))
                return true;
        }

        return false;
    }
}

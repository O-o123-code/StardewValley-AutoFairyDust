using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;
using SObject = StardewValley.Object;

namespace AutoFairyDust
{
    internal class NetworkScanner
    {
        private readonly ModConfig _config;
        private HashSet<SObject> _connectedMachines = new();
        private HashSet<SObject> _allMachines = new();
        private HashSet<SObject> _chests = new();
        private HashSet<string> _connectorNames;

        private bool _dirty = true;
        private GameLocation _lastLocation;

        public HashSet<SObject> ConnectedMachines => _connectedMachines;
        public HashSet<SObject> AllMachines => _allMachines;
        public HashSet<SObject> Chests => _chests;

        public NetworkScanner(ModConfig config)
        {
            _config = config;
            ParseConnectors();
        }

        public void ReloadConfig()
        {
            ParseConnectors();
            MarkDirty();
        }

        private void ParseConnectors()
        {
            _connectorNames = new HashSet<string>(
                _config.ConnectorList.Split(',', System.StringSplitOptions.TrimEntries | System.StringSplitOptions.RemoveEmptyEntries),
                System.StringComparer.OrdinalIgnoreCase
            );
        }

        public void MarkDirty()
        {
            _dirty = true;
        }

        public void ScanNetwork(GameLocation location)
        {
            if (location == null)
                return;

            if (location != _lastLocation)
                _dirty = true;

            _lastLocation = location;

            if (!_dirty)
                return;

            _allMachines.Clear();
            _chests.Clear();
            _connectedMachines.Clear();

            CollectObjects(location);
            ComputeConnectivity(location);
            _dirty = false;
        }

        private void CollectObjects(GameLocation location)
        {
            foreach (var pair in location.Objects.Pairs)
            {
                SObject obj = pair.Value;
                if (obj == null)
                    continue;

                if (IsChest(obj))
                    _chests.Add(obj);
                else if (IsMachine(obj))
                    _allMachines.Add(obj);
            }
        }

        private static bool IsChest(SObject obj)
        {
            if (obj is Chest chest)
            {
                string name = chest.Name;
                if (name == "Shipping Bin" || name == "Mini-Shipping Bin")
                    return false;
                return true;
            }
            return false;
        }

        private static bool IsPathway(SObject obj)
        {
            string name = obj.Name;
            return name.Contains("Floor") || name.Contains("Path") || name.Contains("Pathway") || name == "Crystal Path" || name == "Gravel Path" || name == "Cobblestone Path";
        }

        private static bool IsMachine(SObject obj)
        {
            if (obj.IsSprinkler() || obj.IsScarecrow())
                return false;

            if (IsPathway(obj))
                return false;

            if (obj is Chest)
                return false;

            if (obj.Name == "Fairy Dust" || obj.Name == "Stone" || obj.Name == "Twig")
                return false;

            return true;
        }

        private bool IsConnectorTile(Vector2 tile, GameLocation location)
        {
            if (location.terrainFeatures.TryGetValue(tile, out TerrainFeature terrain) && terrain is Flooring)
                return true;

            if (location.Objects.TryGetValue(tile, out SObject obj))
            {
                if (IsPathway(obj))
                    return true;

                if (_connectorNames.Contains(obj.Name))
                    return true;
            }

            return false;
        }

        private void ComputeConnectivity(GameLocation location)
        {
            if (_chests.Count == 0)
                return;

            var visited = new HashSet<Vector2>();
            var frontier = new Queue<Vector2>();

            foreach (var chest in _chests)
            {
                Vector2 tile = chest.TileLocation;
                if (visited.Add(tile))
                    frontier.Enqueue(tile);
            }

            int[] dx = { -1, 0, 1, -1, 1, -1, 0, 1 };
            int[] dy = { -1, -1, -1, 0, 0, 1, 1, 1 };

            while (frontier.Count > 0)
            {
                Vector2 current = frontier.Dequeue();

                for (int i = 0; i < 8; i++)
                {
                    Vector2 neighbor = new Vector2(current.X + dx[i], current.Y + dy[i]);

                    if (!visited.Add(neighbor))
                        continue;

                    if (!location.isTileLocationOpen(neighbor))
                        continue;

                    bool isConnector = IsConnectorTile(neighbor, location);
                    if (!isConnector)
                    {
                        if (!location.Objects.TryGetValue(neighbor, out SObject neighborObj))
                            continue;

                        if (_allMachines.Contains(neighborObj))
                        {
                            _connectedMachines.Add(neighborObj);
                            frontier.Enqueue(neighbor);
                        }
                        else if (_chests.Contains(neighborObj))
                        {
                            frontier.Enqueue(neighbor);
                        }
                        else
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (location.Objects.TryGetValue(neighbor, out SObject neighborObj))
                        {
                            if (_allMachines.Contains(neighborObj))
                                _connectedMachines.Add(neighborObj);
                        }
                        frontier.Enqueue(neighbor);
                    }
                }
            }
        }
    }
}

using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace AutoFairyDust
{
    internal class FairyDustLogic
    {
        private const int FairyDustId = 917;

        private readonly ModConfig _config;
        private readonly NetworkScanner _scanner;
        private readonly IMonitor _monitor;

        private readonly Dictionary<SObject, long> _cooldowns = new();
        private int _tickCounter;

        public FairyDustLogic(ModConfig config, NetworkScanner scanner, IMonitor monitor)
        {
            _config = config;
            _scanner = scanner;
            _monitor = monitor;
        }

        public void Update()
        {
            if (!_config.Enabled || !Context.IsMainPlayer)
                return;

            _tickCounter++;

            int tickInterval = (int)(60f / _config.MaxPerSecond);
            if (tickInterval < 1)
                tickInterval = 1;

            if (_tickCounter % tickInterval != 0)
                return;

            foreach (var machine in _scanner.ConnectedMachines)
            {
                if (!CanApplyFairyDust(machine))
                    continue;

                if (IsOnCooldown(machine))
                    continue;

                Chest chest = FindNearestChestWithDust(machine);
                if (chest == null)
                    continue;

                try
                {
                    if (machine is Cask cask)
                    {
                        if (!cask.TryApplyFairyDust(true))
                            continue;

                        if (cask.TryApplyFairyDust(false))
                            RemoveOneFairyDust(chest);
                        else
                            continue;
                    }
                    else
                    {
                        machine.MinutesUntilReady = 1;
                        RemoveOneFairyDust(chest);
                    }

                    SetCooldown(machine);
                    break;
                }
                catch (Exception ex)
                {
                    _monitor.Log($"Failed to apply Fairy Dust: {ex.Message}", LogLevel.Warn);
                }
            }
        }

        private static bool CanApplyFairyDust(SObject machine)
        {
            if (machine.heldObject.Value == null)
                return false;

            if (machine.MinutesUntilReady <= 0)
                return false;

            if (machine.readyForHarvest.Value)
                return false;

            if (machine is Cask cask)
            {
                if (cask.heldObject.Value == null)
                    return false;
                if (cask.heldObject.Value.Quality >= 4)
                    return false;
            }

            return true;
        }

        private Chest FindNearestChestWithDust(SObject machine)
        {
            Chest best = null;
            float bestDist = float.MaxValue;
            Vector2 machineTile = machine.TileLocation;

            foreach (var chest in _scanner.Chests)
            {
                if (chest is not Chest c)
                    continue;

                if (!HasFairyDust(c))
                    continue;

                float dist = Vector2.DistanceSquared(machineTile, chest.TileLocation);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = c;
                }
            }

            return best;
        }

        private static bool HasFairyDust(Chest chest)
        {
            foreach (var item in chest.Items)
            {
                if (item != null && item.ParentSheetIndex == FairyDustId && item.Stack > 0)
                    return true;
            }
            return false;
        }

        private static void RemoveOneFairyDust(Chest chest)
        {
            for (int i = 0; i < chest.Items.Count; i++)
            {
                var item = chest.Items[i];
                if (item != null && item.ParentSheetIndex == FairyDustId && item.Stack > 0)
                {
                    item.Stack--;
                    if (item.Stack <= 0)
                        chest.Items[i] = null;
                    return;
                }
            }
        }

        private void SetCooldown(SObject machine)
        {
            _cooldowns[machine] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + (_config.CooldownSeconds * 1000);
        }

        private bool IsOnCooldown(SObject machine)
        {
            if (!_cooldowns.TryGetValue(machine, out long until))
                return false;

            return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() < until;
        }
    }
}

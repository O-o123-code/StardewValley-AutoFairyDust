using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.GameData.FloorsAndPaths;
using StardewValley.ItemTypeDefinitions;
using StardewValley.Objects;
using AutoFairyDust.Framework;
using SObject = StardewValley.Object;

namespace AutoFairyDust;

internal class ModEntry : Mod
{
    private ModConfig Config = null!;
    private FairyDustNetwork Network = null!;
    private OverlayMenu CurrentOverlay;

    private readonly PerScreen<int> UsesThisSecond = new();
    private readonly PerScreen<double> LastSecondTimestamp = new();
    private readonly Dictionary<string, double> MachineCooldowns = new();
    private int ConfigRegisterCountdown = 10;

    private bool EnableAutomation => Config.Enabled && Context.IsMainPlayer;

    public override void Entry(IModHelper helper)
    {
        I18n.Init(helper.Translation);
        Config = helper.ReadConfig<ModConfig>();
        Network = new FairyDustNetwork(Monitor, Config);

        helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;
        helper.Events.GameLoop.DayStarted += OnDayStarted;
        helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
        helper.Events.Input.ButtonsChanged += OnButtonsChanged;
        helper.Events.World.ObjectListChanged += OnObjectListChanged;
        helper.Events.World.BuildingListChanged += OnBuildingListChanged;
        helper.Events.World.TerrainFeatureListChanged += OnTerrainFeatureListChanged;
        helper.Events.World.LargeTerrainFeatureListChanged += OnLargeTerrainFeatureListChanged;
        helper.Events.Player.Warped += OnWarped;

        RemoveDuplicateModFolder();
    }

    private void RemoveDuplicateModFolder()
    {
        try
        {
            string modDir = Helper.DirectoryPath;
            string parent = Path.GetDirectoryName(Path.GetDirectoryName(modDir));
            if (parent != null)
            {
                string dupPath = Path.Combine(parent, "AutoFairyDust");
                if (Directory.Exists(dupPath) && !dupPath.Equals(modDir, StringComparison.OrdinalIgnoreCase))
                {
                    Monitor.Log($"Removing duplicate mod folder: {dupPath}", LogLevel.Info);
                    Directory.Delete(dupPath, true);
                }
            }
        }
        catch (Exception ex)
        {
            Monitor.Log($"Failed to clean up duplicate folder: {ex.Message}", LogLevel.Warn);
        }
    }

    private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
    {
        RegisterModConfigDelayed();
    }

    private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
    {
        RebuildNetwork();
    }

    private void OnDayStarted(object sender, DayStartedEventArgs e)
    {
        MachineCooldowns.Clear();
        RebuildNetwork();
    }

    private void OnWarped(object sender, WarpedEventArgs e)
    {
        if (e.IsLocalPlayer && CurrentOverlay != null)
        {
            Network.MarkDirty(e.NewLocation);
        }
    }

    private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
    {
        if (Context.IsMainPlayer)
            Network.MarkDirty(e.Location);
    }

    private void OnBuildingListChanged(object sender, BuildingListChangedEventArgs e)
    {
        if (Context.IsMainPlayer)
            Network.MarkDirty(e.Location);
    }

    private void OnTerrainFeatureListChanged(object sender, TerrainFeatureListChangedEventArgs e)
    {
        if (Context.IsMainPlayer)
            Network.MarkDirty(e.Location);
    }

    private void OnLargeTerrainFeatureListChanged(object sender, LargeTerrainFeatureListChangedEventArgs e)
    {
        if (Context.IsMainPlayer)
            Network.MarkDirty(e.Location);
    }

    private void RebuildNetwork()
    {
        Monitor.Log("Rebuilding network for all locations...", LogLevel.Trace);
        Network.RebuildAll();
    }

    private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
    {
        if (ConfigRegisterCountdown > 0)
        {
            ConfigRegisterCountdown--;
            if (ConfigRegisterCountdown == 0)
                RegisterModConfig();
        }

        if (!Context.IsWorldReady)
            return;

        if (!EnableAutomation)
            return;

        ProcessAutomation();
    }

    private void ProcessAutomation()
    {
        double now = Game1.currentGameTime.TotalGameTime.TotalSeconds;

        if (now - LastSecondTimestamp.Value >= 1.0)
        {
            UsesThisSecond.Value = 0;
            LastSecondTimestamp.Value = now;
        }
        if (UsesThisSecond.Value >= Config.MaxPerSecond)
            return;

        GameLocation location = Game1.currentLocation;
        var groups = Network.GetGroups(location);
        if (groups == null || groups.Count == 0)
            return;

        double curMs = Game1.currentGameTime.TotalGameTime.TotalMilliseconds;
        float cooldownMs = Config.MachineCooldownSeconds * 1000;

        foreach (var group in groups)
        {
            if (!group.HasFairyDust())
                continue;

            foreach (var machine in group.Machines)
            {
                if (UsesThisSecond.Value >= Config.MaxPerSecond)
                    return;

                if (machine.MinutesUntilReady <= 0)
                    continue;

                string key = GetMachineKey(machine);
                if (MachineCooldowns.TryGetValue(key, out double expiry) && curMs < expiry)
                    continue;

                if (!FairyDustHelper.CanAcceptFairyDust(machine))
                    continue;

                while (UsesThisSecond.Value < Config.MaxPerSecond)
                {
                    if (!FairyDustHelper.CanAcceptFairyDust(machine))
                        break;

                    if (!FairyDustHelper.TryApply(machine))
                        break;

                    if (!group.TryConsumeOneFairyDust())
                        break;

                    UsesThisSecond.Value++;
                    MachineCooldowns[key] = curMs + cooldownMs;

                    if (Config.ShowHudMessage)
                    {
                        Game1.addHUDMessage(new HUDMessage(I18n.Hud_FairyDustUsed(), 2));
                    }

                    Monitor.Log(
                        $"Applied fairy dust to {machine.DisplayName} at {location.Name} ({machine.TileLocation.X}, {machine.TileLocation.Y})",
                        LogLevel.Trace
                    );
                }
            }
        }
    }

    private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
    {
        if (!Context.IsWorldReady)
            return;

        if (Config.Controls.ToggleOverlay.JustPressed())
        {
            if (CurrentOverlay != null)
            {
                CurrentOverlay.Dispose();
                CurrentOverlay = null;
                Monitor.Log("Overlay disabled", LogLevel.Trace);
            }
            else
            {
                CurrentOverlay = new OverlayMenu(Helper.Events, Helper.Input, Network);
                Monitor.Log("Overlay enabled", LogLevel.Trace);
            }
        }
    }

    private static string GetMachineKey(SObject machine)
    {
        return $"{machine.TileLocation.X},{machine.TileLocation.Y}";
    }

    private void RegisterModConfig()
    {
        var gmcm = Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
        if (gmcm == null)
            return;

        gmcm.Register(
            mod: ModManifest,
            reset: () => Config = new ModConfig(),
            save: () =>
            {
                Helper.WriteConfig(Config);
                ReloadConfig();
            }
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: () => I18n.Get("config.title.main-options")
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            name: I18n.Config_Enabled_Name,
            tooltip: I18n.Config_Enabled_Desc,
            getValue: () => Config.Enabled,
            setValue: value => Config.Enabled = value
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            name: I18n.Config_MaxPerSecond_Name,
            tooltip: I18n.Config_MaxPerSecond_Desc,
            getValue: () => Config.MaxPerSecond,
            setValue: value => Config.MaxPerSecond = value,
            min: 1,
            max: 100,
            interval: 1
        );

        gmcm.AddNumberOption(
            mod: ModManifest,
            name: I18n.Config_MachineCooldown_Name,
            tooltip: I18n.Config_MachineCooldown_Desc,
            getValue: () => (int)(Config.MachineCooldownSeconds * 10),
            setValue: value => Config.MachineCooldownSeconds = value / 10f,
            min: 0,
            max: 100,
            interval: 5
        );

        gmcm.AddKeybindList(
            mod: ModManifest,
            name: I18n.Config_ToggleOverlayKey_Name,
            tooltip: I18n.Config_ToggleOverlayKey_Desc,
            getValue: () => Config.Controls.ToggleOverlay,
            setValue: value => Config.Controls.ToggleOverlay = value
        );

        gmcm.AddBoolOption(
            mod: ModManifest,
            name: I18n.Config_ShowHudMessage_Name,
            tooltip: I18n.Config_ShowHudMessage_Desc,
            getValue: () => Config.ShowHudMessage,
            setValue: value => Config.ShowHudMessage = value
        );

        gmcm.AddSectionTitle(
            mod: ModManifest,
            text: I18n.Config_Connectors_Title
        );

        foreach (var entry in Game1.floorPathData.Values.OrderBy(p => p.ItemId))
        {
            ParsedItemData itemData = ItemRegistry.GetData(entry.ItemId);
            if (itemData == null)
                continue;

            string itemId = entry.ItemId;
            gmcm.AddBoolOption(
                mod: ModManifest,
                name: () => itemData.DisplayName,
                tooltip: () => I18n.Config_Connector_Desc(),
                getValue: () => Config.ConnectorNames.Contains(itemData.QualifiedItemId) || Config.ConnectorNames.Contains(itemData.InternalName),
                setValue: enabled =>
                {
                    if (enabled)
                        Config.ConnectorNames.Add(itemData.QualifiedItemId);
                    else
                        Config.ConnectorNames.Remove(itemData.QualifiedItemId);
                }
            );
        }

        gmcm.AddTextOption(
            mod: ModManifest,
            name: I18n.Config_CustomConnectors_Name,
            tooltip: I18n.Config_CustomConnectors_Desc,
            getValue: () => string.Join(", ", Config.ConnectorNames.Where(IsCustomConnector)),
            setValue: value =>
            {
                var items = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
                foreach (string id in Config.ConnectorNames.Where(IsCustomConnector).ToList())
                    Config.ConnectorNames.Remove(id);
                foreach (string id in items)
                    Config.ConnectorNames.Add(id);
            }
        );
    }

    private bool IsCustomConnector(string idOrName)
    {
        foreach (var floor in Game1.floorPathData.Values)
        {
            var itemData = ItemRegistry.GetData(floor.ItemId);
            if (itemData == null)
                continue;
            if (idOrName.Equals(itemData.QualifiedItemId, StringComparison.OrdinalIgnoreCase) ||
                idOrName.Equals(itemData.InternalName, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    private void RegisterModConfigDelayed()
    {
        ConfigRegisterCountdown = 10;
    }

    private void ReloadConfig()
    {
        MachineCooldowns.Clear();
        UsesThisSecond.Value = 0;

        if (Config.Enabled)
        {
            Monitor.Log("Reloading network after config change...", LogLevel.Info);
            Network.RebuildAll();
        }
        else
        {
            Monitor.Log("Mod disabled via config change, clearing network state.", LogLevel.Info);
            Network.MarkAllDirty();
        }
    }
}

public interface IGenericModConfigMenuApi
{
    void Register(IManifest mod, Action reset, Action save, bool titleScreenOnly = false);
    void AddSectionTitle(IManifest mod, Func<string> text, Func<string> tooltip = null);
    void AddBoolOption(IManifest mod, Func<bool> getValue, Action<bool> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
    void AddNumberOption(IManifest mod, Func<int> getValue, Action<int> setValue, Func<string> name, Func<string> tooltip = null, int? min = null, int? max = null, int? interval = null, string fieldId = null);
    void AddKeybindList(IManifest mod, Func<KeybindList> getValue, Action<KeybindList> setValue, Func<string> name, Func<string> tooltip = null, string fieldId = null);
    void AddTextOption(IManifest mod, Func<string> getValue, Action<string> setValue, Func<string> name, Func<string> tooltip = null, string[] allowedValues = null, Func<string, string> formatAllowedValue = null, string fieldId = null);
}

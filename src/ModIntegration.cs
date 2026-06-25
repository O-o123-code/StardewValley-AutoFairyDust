using StardewModdingAPI;
using StardewModdingAPI.Events;

namespace AutoFairyDust
{
    internal class ModIntegration
    {
        private readonly ModConfig _config;
        private readonly NetworkScanner _scanner;
        private readonly OverlayRenderer _overlay;
        private readonly IModHelper _helper;
        private readonly IMonitor _monitor;
        private readonly IManifest _modManifest;

        private IGenericModConfigMenuApi _gmcm;

        public ModIntegration(ModConfig config, NetworkScanner scanner, OverlayRenderer overlay, IModHelper helper, IMonitor monitor, IManifest modManifest)
        {
            _config = config;
            _scanner = scanner;
            _overlay = overlay;
            _helper = helper;
            _monitor = monitor;
            _modManifest = modManifest;
        }

        public void Register()
        {
            _helper.Events.GameLoop.GameLaunched += OnGameLaunched;
        }

        private void OnGameLaunched(object sender, GameLaunchedEventArgs e)
        {
            _gmcm = _helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (_gmcm == null)
                return;

            _gmcm.Register(
                mod: _modManifest,
                reset: () =>
                {
                    _config.Enabled = true;
                    _config.MaxPerSecond = 10;
                    _config.CooldownSeconds = 1;
                    _config.ConnectorList = "Wood Floor, Weathered Floor, Straw Floor, Brick Floor, Stone Floor, Gravel Path, Cobblestone Path, Crystal Path";
                    _config.ToggleOverlayKey = "K";
                    _config.ShowOverlay = false;
                },
                save: () =>
                {
                    _helper.WriteConfig(_config);
                    _scanner.ReloadConfig();
                }
            );

            _gmcm.AddBoolOption(
                mod: _modManifest,
                name: () => _helper.Translation.Get("config.enabled.name"),
                tooltip: () => _helper.Translation.Get("config.enabled.desc"),
                getValue: () => _config.Enabled,
                setValue: value => _config.Enabled = value
            );

            _gmcm.AddNumberOption(
                mod: _modManifest,
                name: () => _helper.Translation.Get("config.max-per-second.name"),
                tooltip: () => _helper.Translation.Get("config.max-per-second.desc"),
                getValue: () => _config.MaxPerSecond,
                setValue: value => _config.MaxPerSecond = value,
                min: 1,
                max: 60,
                interval: 1
            );

            _gmcm.AddNumberOption(
                mod: _modManifest,
                name: () => _helper.Translation.Get("config.cooldown.name"),
                tooltip: () => _helper.Translation.Get("config.cooldown.desc"),
                getValue: () => _config.CooldownSeconds,
                setValue: value => _config.CooldownSeconds = value,
                min: 0,
                max: 60,
                interval: 1
            );

            _gmcm.AddTextOption(
                mod: _modManifest,
                name: () => _helper.Translation.Get("config.connectors.name"),
                tooltip: () => _helper.Translation.Get("config.connectors.desc"),
                getValue: () => _config.ConnectorList,
                setValue: value => _config.ConnectorList = value
            );

            _gmcm.AddTextOption(
                mod: _modManifest,
                name: () => _helper.Translation.Get("config.toggle-overlay-key.name"),
                tooltip: () => _helper.Translation.Get("config.toggle-overlay-key.desc"),
                getValue: () => _config.ToggleOverlayKey,
                setValue: value => _config.ToggleOverlayKey = value
            );

            _gmcm.AddBoolOption(
                mod: _modManifest,
                name: () => _helper.Translation.Get("config.show-overlay.name"),
                tooltip: () => _helper.Translation.Get("config.show-overlay.desc"),
                getValue: () => _config.ShowOverlay,
                setValue: value =>
                {
                    _config.ShowOverlay = value;
                    _overlay.Visible = value;
                }
            );
        }
    }
}

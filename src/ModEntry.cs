using System;
using System.Linq;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutoFairyDust
{
    internal class ModEntry : Mod
    {
        private ModConfig _config;
        private NetworkScanner _scanner;
        private FairyDustLogic _logic;
        private OverlayRenderer _overlay;
        private ModIntegration _integration;

        public override void Entry(IModHelper helper)
        {
            _config = helper.ReadConfig<ModConfig>();
            _scanner = new NetworkScanner(_config);
            _logic = new FairyDustLogic(_config, _scanner, Monitor);
            _overlay = new OverlayRenderer(_config, _scanner);
            _integration = new ModIntegration(_config, _scanner, _overlay, helper, Monitor, ModManifest);

            _integration.Register();

            helper.Events.GameLoop.UpdateTicked += OnUpdateTicked;
            helper.Events.Player.Warped += OnWarped;
            helper.Events.World.ObjectListChanged += OnObjectListChanged;
            helper.Events.Input.ButtonsChanged += OnButtonsChanged;
            helper.Events.Display.RenderedWorld += OnRenderedWorld;
        }

        private void OnUpdateTicked(object sender, UpdateTickedEventArgs e)
        {
            if (!Context.IsWorldReady)
                return;

            _logic.Update();
        }

        private void OnWarped(object sender, WarpedEventArgs e)
        {
            _scanner.MarkDirty();
        }

        private void OnObjectListChanged(object sender, ObjectListChangedEventArgs e)
        {
            _scanner.MarkDirty();
        }

        private void OnButtonsChanged(object sender, ButtonsChangedEventArgs e)
        {
            if (string.IsNullOrEmpty(_config.ToggleOverlayKey))
                return;

            if (Enum.TryParse<SButton>(_config.ToggleOverlayKey, true, out SButton key) && e.Pressed.Contains(key))
            {
                _overlay.Toggle();
                _config.ShowOverlay = _overlay.Visible;
                string state = _overlay.Visible
                    ? Helper.Translation.Get("overlay.key-toggle-on")
                    : Helper.Translation.Get("overlay.key-toggle-off");
                Monitor.Log(string.Format(Helper.Translation.Get("overlay.key-toggle"), state), LogLevel.Info);
            }
        }

        private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
        {
            _scanner.ScanNetwork(Game1.currentLocation);
            _overlay.Draw();
        }
    }
}

namespace AutoFairyDust
{
    internal class ModConfig
    {
        public bool Enabled { get; set; } = true;
        public int MaxPerSecond { get; set; } = 10;
        public int CooldownSeconds { get; set; } = 1;
        public string ConnectorList { get; set; } = "Wood Floor, Weathered Floor, Straw Floor, Brick Floor, Stone Floor, Gravel Path, Cobblestone Path, Crystal Path";
        public string ToggleOverlayKey { get; set; } = "K";
        public bool ShowOverlay { get; set; } = false;
    }
}

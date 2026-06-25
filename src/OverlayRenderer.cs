using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewValley;
using SObject = StardewValley.Object;

namespace AutoFairyDust
{
    internal class OverlayRenderer
    {
        private readonly ModConfig _config;
        private readonly NetworkScanner _scanner;
        private bool _visible;

        public bool Visible
        {
            get => _visible;
            set => _visible = value;
        }

        public OverlayRenderer(ModConfig config, NetworkScanner scanner)
        {
            _config = config;
            _scanner = scanner;
            _visible = config.ShowOverlay;
        }

        public void Toggle()
        {
            _visible = !_visible;
        }

        public void Draw()
        {
            if (!_visible)
                return;

            GameLocation location = Game1.currentLocation;
            if (location == null)
                return;

            foreach (var pair in location.Objects.Pairs)
            {
                SObject obj = pair.Value;
                if (obj == null)
                    continue;

                Color color;

                if (_scanner.Chests.Contains(obj))
                {
                    color = Color.Blue;
                }
                else if (_scanner.ConnectedMachines.Contains(obj))
                {
                    color = Color.Lime;
                }
                else if (_scanner.AllMachines.Contains(obj) || IsDrawableMachine(obj))
                {
                    color = Color.Red;
                }
                else
                {
                    continue;
                }

                Vector2 tile = obj.TileLocation;
                Vector2 screenPos = Game1.GlobalToLocal(Game1.viewport, tile * 64f);

                color *= 0.4f;
                Game1.spriteBatch.Draw(
                    Game1.staminaRect,
                    new Rectangle((int)screenPos.X, (int)screenPos.Y, 64, 64),
                    color
                );
            }
        }

        private static bool IsDrawableMachine(SObject obj)
        {
            if (obj.IsSprinkler() || obj.IsScarecrow())
                return false;

            if (obj is StardewValley.Objects.Chest)
                return false;

            if (obj.Name == "Fairy Dust" || obj.Name == "Stone" || obj.Name == "Twig")
                return false;

            return true;
        }
    }
}

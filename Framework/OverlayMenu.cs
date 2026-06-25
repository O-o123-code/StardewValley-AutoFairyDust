using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;

namespace AutoFairyDust.Framework;

internal class OverlayMenu : IDisposable
{
    private readonly IModEvents Events;
    private readonly IInputHelper InputHelper;
    private readonly FairyDustNetwork Network;
    private bool IsDisposed;

    public OverlayMenu(IModEvents events, IInputHelper inputHelper, FairyDustNetwork network)
    {
        this.Events = events;
        this.InputHelper = inputHelper;
        this.Network = network;

        events.Display.RenderedWorld += OnRenderedWorld;
    }

    public void Dispose()
    {
        if (!IsDisposed)
        {
            Events.Display.RenderedWorld -= OnRenderedWorld;
            IsDisposed = true;
        }
        GC.SuppressFinalize(this);
    }

    private void OnRenderedWorld(object sender, RenderedWorldEventArgs e)
    {
        if (!Context.IsPlayerFree)
            return;

        GameLocation location = Game1.currentLocation;
        if (location?.Map?.Layers == null || location.Map.Layers.Count == 0)
            return;

        string key = Network.GetLocationKey(location);
        var connected = Network.ConnectedMachineTiles.GetValueOrDefault(key);
        var unconnected = Network.UnconnectedMachineTiles.GetValueOrDefault(key);
        var chests = Network.ChestTiles.GetValueOrDefault(key);

        if (connected == null && unconnected == null && chests == null)
            return;

        var sb = e.SpriteBatch;
        int tileSize = Game1.tileSize;
        int viewX = Game1.viewport.X;
        int viewY = Game1.viewport.Y;

        foreach (Vector2 tile in GetVisibleTiles(location))
        {
            float x = tile.X * tileSize - viewX;
            float y = tile.Y * tileSize - viewY;

            Color? color = null;

            if (connected?.Contains(tile) == true)
                color = Color.Green * 0.2f;
            else if (unconnected?.Contains(tile) == true)
                color = Color.Red * 0.2f;
            else if (chests?.Contains(tile) == true)
                color = Color.Blue * 0.2f;

            if (color.HasValue)
            {
                sb.Draw(
                    Game1.staminaRect,
                    new Rectangle((int)x, (int)y, tileSize, tileSize),
                    color.Value
                );
            }
        }
    }

    private static IEnumerable<Vector2> GetVisibleTiles(GameLocation location)
    {
        if (location.Map?.Layers == null || location.Map.Layers.Count == 0)
            yield break;

        int layerWidth = location.Map.Layers[0].LayerWidth;
        int layerHeight = location.Map.Layers[0].LayerHeight;
        int expand = 1;

        int x1 = Math.Max(0, Game1.viewport.X / Game1.tileSize - expand);
        int y1 = Math.Max(0, Game1.viewport.Y / Game1.tileSize - expand);
        int x2 = Math.Min(layerWidth, (Game1.viewport.X + Game1.viewport.Width) / Game1.tileSize + 1 + expand);
        int y2 = Math.Min(layerHeight, (Game1.viewport.Y + Game1.viewport.Height) / Game1.tileSize + 1 + expand);

        for (int x = x1; x < x2; x++)
        {
            for (int y = y1; y < y2; y++)
            {
                yield return new Vector2(x, y);
            }
        }
    }
}

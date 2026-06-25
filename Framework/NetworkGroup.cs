using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace AutoFairyDust.Framework;

internal class NetworkGroup
{
    public List<SObject> Machines { get; } = new();
    public List<Chest> Chests { get; } = new();
    public HashSet<Vector2> Tiles { get; } = new();

    public bool HasFairyDust()
    {
        foreach (var chest in Chests)
        {
            foreach (var item in chest.Items)
            {
                if (item?.QualifiedItemId == "(O)872" && item.Stack > 0)
                    return true;
            }
        }
        return false;
    }

    public bool TryConsumeOneFairyDust()
    {
        foreach (var chest in Chests)
        {
            for (int i = 0; i < chest.Items.Count; i++)
            {
                var item = chest.Items[i];
                if (item?.QualifiedItemId == "(O)872" && item.Stack > 0)
                {
                    item.Stack--;
                    if (item.Stack <= 0)
                        chest.Items[i] = null;
                    chest.clearNulls();
                    return true;
                }
            }
        }
        return false;
    }
}

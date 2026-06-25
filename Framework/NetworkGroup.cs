using System.Collections.Generic;
using Microsoft.Xna.Framework;
using StardewValley;
using StardewValley.Objects;
using SObject = StardewValley.Object;

namespace AutoFairyDust.Framework;

internal class NetworkGroup
{
    internal const string FairyDustQualifiedId = "(O)872";

    public List<SObject> Machines { get; } = new();
    public List<Chest> Chests { get; } = new();
    public HashSet<Vector2> Tiles { get; } = new();

    public bool HasFairyDust()
    {
        foreach (var chest in Chests)
        {
            foreach (var item in chest.Items)
            {
                if (item?.QualifiedItemId == FairyDustQualifiedId && item.Stack > 0)
                    return true;
            }
        }
        return false;
    }

    public bool TryConsumeOneFairyDust(out int chestIndex, out int itemSlot, out int previousStack)
    {
        chestIndex = -1;
        itemSlot = -1;
        previousStack = 0;

        for (int ci = 0; ci < Chests.Count; ci++)
        {
            var chest = Chests[ci];
            for (int i = 0; i < chest.Items.Count; i++)
            {
                var item = chest.Items[i];
                if (item?.QualifiedItemId == FairyDustQualifiedId && item.Stack > 0)
                {
                    chestIndex = ci;
                    itemSlot = i;
                    previousStack = item.Stack;

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

    public void RollbackFairyDust(int chestIndex, int itemSlot, int previousStack)
    {
        if (chestIndex < 0 || chestIndex >= Chests.Count)
            return;

        var chest = Chests[chestIndex];

        if (itemSlot >= 0 && itemSlot < chest.Items.Count && chest.Items[itemSlot] != null)
        {
            var item = chest.Items[itemSlot];
            if (item.QualifiedItemId == FairyDustQualifiedId)
            {
                item.Stack = previousStack;
                return;
            }
        }

        foreach (var item in chest.Items)
        {
            if (item?.QualifiedItemId == FairyDustQualifiedId)
            {
                item.Stack = previousStack;
                return;
            }
        }

        if (chest.Items.Count < chest.GetActualCapacity())
        {
            var newItem = ItemRegistry.Create<SObject>(FairyDustQualifiedId);
            newItem.Stack = previousStack;
            chest.Items.Add(newItem);
        }
    }
}

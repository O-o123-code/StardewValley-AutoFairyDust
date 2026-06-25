using SObject = StardewValley.Object;

namespace AutoFairyDust.Framework;

internal static class FairyDustHelper
{
    public static bool CanAcceptFairyDust(SObject machine)
    {
        if (machine.MinutesUntilReady <= 0)
            return false;

        return machine.TryApplyFairyDust(probe: true);
    }

    public static bool TryApply(SObject machine)
    {
        return machine.TryApplyFairyDust();
    }
}

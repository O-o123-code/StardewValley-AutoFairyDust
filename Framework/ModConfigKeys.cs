using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
using StardewModdingAPI;
using StardewModdingAPI.Utilities;

namespace AutoFairyDust.Framework;

internal class ModConfigKeys
{
    public KeybindList ToggleOverlay { get; set; } = new(SButton.K);

    [OnDeserialized]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void OnDeserialized(StreamingContext context)
    {
        this.ToggleOverlay ??= new KeybindList();
    }
}

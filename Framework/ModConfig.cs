using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

namespace AutoFairyDust.Framework;

internal class ModConfig
{
    public bool Enabled { get; set; } = true;
    public int MaxPerSecond { get; set; } = 10;
    public float MachineCooldownSeconds { get; set; } = 1.0f;

    public HashSet<string> ConnectorNames { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public ModConfigKeys Controls { get; set; } = new();
    public bool ShowHudMessage { get; set; } = true;

    [OnDeserialized]
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public void OnDeserialized(StreamingContext context)
    {
        this.Controls ??= new ModConfigKeys();
        this.ConnectorNames ??= new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        this.ConnectorNames.RemoveWhere(string.IsNullOrWhiteSpace);
    }
}

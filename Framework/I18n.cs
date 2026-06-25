using StardewModdingAPI;

namespace AutoFairyDust.Framework;

internal static class I18n
{
    private static ITranslationHelper Translations { get; set; } = null!;

    public static void Init(ITranslationHelper translations)
    {
        Translations = translations;
    }

    public static string Get(string key, object tokens = null)
    {
        return Translations.Get(key, tokens);
    }

    public static string Config_Enabled_Name() => Get("config.enabled.name");
    public static string Config_Enabled_Desc() => Get("config.enabled.desc");
    public static string Config_MaxPerSecond_Name() => Get("config.max-per-second.name");
    public static string Config_MaxPerSecond_Desc() => Get("config.max-per-second.desc");
    public static string Config_MachineCooldown_Name() => Get("config.machine-cooldown.name");
    public static string Config_MachineCooldown_Desc() => Get("config.machine-cooldown.desc");
    public static string Config_ToggleOverlayKey_Name() => Get("config.toggle-overlay-key.name");
    public static string Config_ToggleOverlayKey_Desc() => Get("config.toggle-overlay-key.desc");
    public static string Config_ShowHudMessage_Name() => Get("config.show-hud-message.name");
    public static string Config_ShowHudMessage_Desc() => Get("config.show-hud-message.desc");
    public static string Config_Connectors_Title() => Get("config.connectors.title");
    public static string Config_Connector_Desc() => Get("config.connector.desc");
    public static string Config_CustomConnectors_Name() => Get("config.custom-connectors.name");
    public static string Config_CustomConnectors_Desc() => Get("config.custom-connectors.desc");
    public static string Hud_FairyDustUsed() => Get("hud.fairy-dust-used");
}

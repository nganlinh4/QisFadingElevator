namespace QisFadingElevator
{
    /// <summary>Generic Mod Config Menu registration, if GMCM is installed.</summary>
    internal static class ConfigMenu
    {
        public static void Register(ModEntry mod)
        {
            var gmcm = mod.Helper.ModRegistry.GetApi<IGenericModConfigMenuApi>("spacechase0.GenericModConfigMenu");
            if (gmcm is null)
                return;

            gmcm.Register(mod.ModManifest, mod.ResetConfig, mod.SaveConfig);

            gmcm.AddBoolOption(
                mod.ModManifest,
                () => mod.Config.Enabled,
                v => mod.Config.Enabled = v,
                () => mod.T("config.enabled.name"),
                () => mod.T("config.enabled.desc"));

            gmcm.AddSectionTitle(mod.ModManifest, () => mod.T("config.stops.title"));
            gmcm.AddNumberOption(
                mod.ModManifest,
                () => mod.Config.FloorInterval,
                v => mod.Config.FloorInterval = v,
                () => mod.T("config.floor-interval.name"),
                () => mod.T("config.floor-interval.desc"),
                1,
                25);

            gmcm.AddSectionTitle(mod.ModManifest, () => mod.T("config.fading.title"));
            gmcm.AddNumberOption(
                mod.ModManifest,
                () => (float)mod.Config.FadePercentPerHour,
                v => mod.Config.FadePercentPerHour = v,
                () => mod.T("config.fade-per-hour.name"),
                () => mod.T("config.fade-per-hour.desc"),
                0f,
                5f,
                0.05f,
                value => $"{value:0.##}%");
            gmcm.AddNumberOption(
                mod.ModManifest,
                () => (float)mod.Config.LuckInfluence,
                v => mod.Config.LuckInfluence = v,
                () => mod.T("config.luck-influence.name"),
                () => mod.T("config.luck-influence.desc"),
                0f,
                5f,
                0.25f,
                value => $"{value:0.##}×");
        }
    }
}

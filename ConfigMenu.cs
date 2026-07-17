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

            gmcm.AddBoolOption(mod.ModManifest, () => mod.Config.Enabled, v => mod.Config.Enabled = v, () => "Wake the old shaft");

            gmcm.AddSectionTitle(mod.ModManifest, () => "The Old Shaft");
            gmcm.AddNumberOption(mod.ModManifest, () => mod.Config.FloorInterval, v => mod.Config.FloorInterval = v, () => "Etched stopping marks", () => "The space between the floors carved into its panel. The deepest memory always remains.", 1, 25);

            gmcm.AddSectionTitle(mod.ModManifest, () => "The Cavern's Hunger");
            gmcm.AddNumberOption(mod.ModManifest, () => (float)mod.Config.FadePercentPerHour, v => mod.Config.FadePercentPerHour = v, () => "The cavern's hunger", () => "How much of the remembered path the stone reclaims each hour, including sleep.", 0f, 5f, 0.05f);
            gmcm.AddNumberOption(mod.ModManifest, () => (float)mod.Config.LuckInfluence, v => mod.Config.LuckInfluence = v, () => "Fortune's mercy", () => "How strongly fortune calms—or provokes—the cavern. At zero, fortune is silent.", 0f, 5f, 0.25f);
        }
    }
}

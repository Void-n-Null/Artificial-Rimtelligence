using Verse;

namespace ArtificialRimtelligenceCore
{
    /// <summary>
    /// Main mod class - entry point for the Artificial Rimtelligence Core mod.
    /// Inherits from Verse.Mod to integrate with RimWorld's mod system.
    /// </summary>
    public class ArtificialRimtelligenceCoreMod : Mod
    {
        /// <summary>
        /// Static reference to mod settings for easy access.
        /// </summary>
        public static ArtificialRimtelligenceCoreSettings Settings { get; private set; }

        /// <summary>
        /// Constructor called when the mod is loaded.
        /// </summary>
        /// <param name="content">Mod content pack containing mod metadata.</param>
        public ArtificialRimtelligenceCoreMod(ModContentPack content) : base(content)
        {
            Settings = GetSettings<ArtificialRimtelligenceCoreSettings>();
            Log.Message($"[ArtificialRimtelligenceCore] Mod loaded. Version: {content.ModMetaData.ModVersion}");
        }

        /// <summary>
        /// The name displayed in the mod settings menu.
        /// </summary>
        public override string SettingsCategory()
        {
            return "Artificial Rimtelligence Core";
        }

        /// <summary>
        /// Renders the mod settings window.
        /// </summary>
        /// <param name="inRect">The rect to draw settings within.</param>
        public override void DoSettingsWindowContents(UnityEngine.Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);
            
            // Add settings here as needed
            // Example:
            // listing.CheckboxLabeled("Enable Feature", ref Settings.EnableFeature, "Description");
            
            listing.End();
            base.DoSettingsWindowContents(inRect);
        }
    }
}

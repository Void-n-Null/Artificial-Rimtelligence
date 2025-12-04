using Verse;

namespace ArtificialRimtelligenceCore
{
    /// <summary>
    /// Mod settings that persist between game sessions.
    /// Automatically saved/loaded by RimWorld via ExposeData().
    /// </summary>
    public class ArtificialRimtelligenceCoreSettings : ModSettings
    {
        // Add your settings fields here
        // Example:
        // public bool EnableFeature = true;
        // public string SomeSetting = "";
        // public int SomeValue = 10;

        /// <summary>
        /// Called by RimWorld to save/load settings.
        /// Use Scribe methods to persist your settings.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            
            // Add your settings serialization here
            // Example:
            // Scribe_Values.Look(ref EnableFeature, "enableFeature", true);
            // Scribe_Values.Look(ref SomeSetting, "someSetting", "");
            // Scribe_Values.Look(ref SomeValue, "someValue", 10);
        }
    }
}

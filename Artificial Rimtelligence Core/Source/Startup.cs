using Verse;

namespace ArtificialRimtelligenceCore
{
    /// <summary>
    /// Static constructor class that runs early during game startup.
    /// Use [StaticConstructorOnStartup] for initialization that needs
    /// to happen after all defs are loaded.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class Startup
    {
        /// <summary>
        /// Static constructor - runs once when the class is first accessed.
        /// This happens after all mods are loaded and defs are resolved.
        /// </summary>
        static Startup()
        {
            Log.Message("[ArtificialRimtelligenceCore] Static initialization complete.");
            
            // Add any startup initialization here
            // Examples:
            // - Apply Harmony patches
            // - Initialize static data
            // - Register callbacks
        }
    }
}

using Verse;

namespace ArtificialRimtelligenceCore
{
    /// <summary>
    /// Mod settings that persist between game sessions.
    /// Automatically saved/loaded by RimWorld via ExposeData().
    /// </summary>
    public class ArtificialRimtelligenceCoreSettings : ModSettings
    {
        /// <summary>
        /// OpenRouter API key obtained via PKCE OAuth flow or manual entry.
        /// </summary>
        public string OpenRouterApiKey = "";

        /// <summary>
        /// Buffer for manual API key entry in settings UI.
        /// </summary>
        [Unsaved]
        public string ManualApiKeyBuffer = "";

        /// <summary>
        /// Returns true if an API key is configured.
        /// </summary>
        public bool HasApiKey => !string.IsNullOrEmpty(OpenRouterApiKey);

        /// <summary>
        /// Returns a masked version of the API key for display.
        /// </summary>
        public string MaskedApiKey
        {
            get
            {
                if (string.IsNullOrEmpty(OpenRouterApiKey))
                    return "(not set)";
                if (OpenRouterApiKey.Length <= 12)
                    return "****";
                return OpenRouterApiKey.Substring(0, 8) + "..." + OpenRouterApiKey.Substring(OpenRouterApiKey.Length - 4);
            }
        }

        /// <summary>
        /// Called by RimWorld to save/load settings.
        /// Use Scribe methods to persist your settings.
        /// </summary>
        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref OpenRouterApiKey, "openRouterApiKey", "");
        }

        /// <summary>
        /// Clears the stored API key.
        /// </summary>
        public void ClearApiKey()
        {
            OpenRouterApiKey = "";
            Write();
        }
    }
}

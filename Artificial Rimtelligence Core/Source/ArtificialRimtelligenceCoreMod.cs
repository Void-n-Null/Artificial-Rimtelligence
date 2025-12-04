using UnityEngine;
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

        // UI state
        private static bool _showTroubleshooting = false;

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
            return "Artificial Rimtelligence";
        }

        /// <summary>
        /// Renders the mod settings window.
        /// </summary>
        /// <param name="inRect">The rect to draw settings within.</param>
        public override void DoSettingsWindowContents(Rect inRect)
        {
            var listing = new Listing_Standard();
            listing.Begin(inRect);

            // ═══════════════════════════════════════════════════════════════
            // CONNECTION STATUS SECTION
            // ═══════════════════════════════════════════════════════════════
            
            DrawSectionHeader(listing, "OpenRouter Connection");
            listing.Gap(8f);

            // Status indicator
            DrawConnectionStatus(listing);
            listing.Gap(12f);

            // Primary action buttons
            DrawPrimaryActions(listing);

            listing.Gap(16f);

            // ═══════════════════════════════════════════════════════════════
            // TROUBLESHOOTING SECTION (Collapsible)
            // ═══════════════════════════════════════════════════════════════
            
            DrawTroubleshootingSection(listing);

            listing.End();
        }

        /// <summary>
        /// Draws a styled section header.
        /// </summary>
        private void DrawSectionHeader(Listing_Standard listing, string text)
        {
            Text.Font = GameFont.Medium;
            listing.Label(text);
            Text.Font = GameFont.Small;
        }

        /// <summary>
        /// Draws the connection status with visual indicator.
        /// </summary>
        private void DrawConnectionStatus(Listing_Standard listing)
        {
            string statusText;
            Color statusColor;

            if (OpenRouterAuth.State == OpenRouterAuth.AuthState.WaitingForBrowser)
            {
                statusText = "● Waiting for browser...";
                statusColor = new Color(1f, 0.8f, 0.2f); // Yellow
            }
            else if (OpenRouterAuth.State == OpenRouterAuth.AuthState.ExchangingCode)
            {
                statusText = "● Exchanging credentials...";
                statusColor = new Color(1f, 0.8f, 0.2f); // Yellow
            }
            else if (Settings.HasApiKey)
            {
                statusText = $"● Connected  —  {Settings.MaskedApiKey}";
                statusColor = new Color(0.4f, 0.9f, 0.4f); // Green
            }
            else
            {
                statusText = "○ Not connected";
                statusColor = new Color(0.6f, 0.6f, 0.6f); // Gray
            }

            GUI.color = statusColor;
            listing.Label(statusText);
            GUI.color = Color.white;

            // Show error if failed
            if (OpenRouterAuth.State == OpenRouterAuth.AuthState.Failed && !string.IsNullOrEmpty(OpenRouterAuth.LastError))
            {
                listing.Gap(4f);
                GUI.color = new Color(1f, 0.4f, 0.4f); // Red
                Text.Font = GameFont.Tiny;
                listing.Label($"Error: {OpenRouterAuth.LastError}");
                Text.Font = GameFont.Small;
                GUI.color = Color.white;
            }
        }

        /// <summary>
        /// Draws primary action buttons.
        /// </summary>
        private void DrawPrimaryActions(Listing_Standard listing)
        {
            bool isAuthInProgress = OpenRouterAuth.State == OpenRouterAuth.AuthState.WaitingForBrowser 
                                 || OpenRouterAuth.State == OpenRouterAuth.AuthState.ExchangingCode;

            if (isAuthInProgress)
            {
                if (listing.ButtonText("Cancel"))
                {
                    OpenRouterAuth.CancelAuth();
                }
            }
            else if (Settings.HasApiKey)
            {
                // Two buttons side by side
                var rect = listing.GetRect(30f);
                var leftRect = rect.LeftHalf().ContractedBy(2f, 0f);
                var rightRect = rect.RightHalf().ContractedBy(2f, 0f);

                if (Widgets.ButtonText(leftRect, "Reconnect"))
                {
                    OpenRouterAuth.StartAuthFlow();
                }
                if (Widgets.ButtonText(rightRect, "Disconnect"))
                {
                    Settings.ClearApiKey();
                }
            }
            else
            {
                if (listing.ButtonText("Connect to OpenRouter"))
                {
                    OpenRouterAuth.StartAuthFlow();
                }
                
                listing.Gap(4f);
                Text.Font = GameFont.Tiny;
                GUI.color = new Color(0.7f, 0.7f, 0.7f);
                listing.Label("Opens your browser to securely authenticate with OpenRouter.");
                GUI.color = Color.white;
                Text.Font = GameFont.Small;
            }
        }

        /// <summary>
        /// Draws the collapsible troubleshooting section.
        /// </summary>
        private void DrawTroubleshootingSection(Listing_Standard listing)
        {
            // Collapsible header
            var headerRect = listing.GetRect(24f);
            Widgets.DrawHighlightIfMouseover(headerRect);
            
            string arrow = _showTroubleshooting ? "▼" : "▶";
            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.7f, 0.7f, 0.7f);
            Widgets.Label(headerRect, $"{arrow}  Having issues? Click here for manual setup");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;

            if (Widgets.ButtonInvisible(headerRect))
            {
                _showTroubleshooting = !_showTroubleshooting;
            }

            if (!_showTroubleshooting)
                return;

            // Manual entry section
            listing.Gap(8f);
            
            var boxRect = listing.GetRect(100f);
            Widgets.DrawBoxSolid(boxRect, new Color(0.15f, 0.15f, 0.15f, 0.5f));
            Widgets.DrawBox(boxRect);

            var innerRect = boxRect.ContractedBy(10f);
            var innerListing = new Listing_Standard();
            innerListing.Begin(innerRect);

            Text.Font = GameFont.Tiny;
            GUI.color = new Color(0.8f, 0.8f, 0.8f);
            innerListing.Label("Get an API key from openrouter.ai/keys and paste below:");
            GUI.color = Color.white;
            Text.Font = GameFont.Small;
            
            innerListing.Gap(6f);
            Settings.ManualApiKeyBuffer = innerListing.TextEntry(Settings.ManualApiKeyBuffer);
            innerListing.Gap(6f);

            if (innerListing.ButtonText("Save API Key"))
            {
                if (!string.IsNullOrWhiteSpace(Settings.ManualApiKeyBuffer))
                {
                    Settings.OpenRouterApiKey = Settings.ManualApiKeyBuffer.Trim();
                    Settings.ManualApiKeyBuffer = "";
                    Settings.Write();
                    _showTroubleshooting = false;
                    Log.Message("[ArtificialRimtelligenceCore] API key saved manually");
                }
            }

            innerListing.End();
        }
    }
}

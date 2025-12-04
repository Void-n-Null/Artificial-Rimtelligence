using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Verse;

namespace ArtificialRimtelligenceCore
{
    /// <summary>
    /// Handles OpenRouter OAuth PKCE authentication flow.
    /// Opens a browser for user login, runs a local HTTP server to catch the callback,
    /// then exchanges the auth code for an API key.
    /// </summary>
    public static class OpenRouterAuth
    {
        private const string OpenRouterAuthUrl = "https://openrouter.ai/auth";
        private const string OpenRouterKeyExchangeUrl = "https://openrouter.ai/api/v1/auth/keys";
        private const int CallbackPort = 3000;
        private static readonly string CallbackUrl = $"http://localhost:{CallbackPort}/";

        private static HttpListener _listener;
        private static CancellationTokenSource _cancellationTokenSource;
        private static string _currentCodeVerifier;

        /// <summary>
        /// Current state of the authentication process.
        /// </summary>
        public static AuthState State { get; private set; } = AuthState.Idle;
        public static string LastError { get; private set; }

        public enum AuthState
        {
            Idle,
            WaitingForBrowser,
            ExchangingCode,
            Success,
            Failed
        }

        /// <summary>
        /// Starts the PKCE authentication flow.
        /// Opens the user's browser to OpenRouter for login.
        /// </summary>
        public static void StartAuthFlow()
        {
            if (State == AuthState.WaitingForBrowser || State == AuthState.ExchangingCode)
            {
                Log.Warning("[OpenRouterAuth] Auth flow already in progress");
                return;
            }

            try
            {
                State = AuthState.WaitingForBrowser;
                LastError = null;

                // Generate PKCE codes
                _currentCodeVerifier = GenerateCodeVerifier();
                string codeChallenge = GenerateCodeChallenge(_currentCodeVerifier);

                // Start local HTTP listener for callback
                StartCallbackListener();

                // Build auth URL
                string authUrl = $"{OpenRouterAuthUrl}?callback_url={Uri.EscapeDataString(CallbackUrl)}" +
                                $"&code_challenge={Uri.EscapeDataString(codeChallenge)}" +
                                $"&code_challenge_method=S256";

                // Open browser
                Log.Message($"[OpenRouterAuth] Opening browser for authentication...");
                OpenBrowser(authUrl);
            }
            catch (Exception ex)
            {
                State = AuthState.Failed;
                LastError = ex.Message;
                Log.Error($"[OpenRouterAuth] Failed to start auth flow: {ex}");
                StopCallbackListener();
            }
        }

        /// <summary>
        /// Cancels any in-progress authentication.
        /// </summary>
        public static void CancelAuth()
        {
            StopCallbackListener();
            State = AuthState.Idle;
            _currentCodeVerifier = null;
            Log.Message("[OpenRouterAuth] Authentication cancelled");
        }

        /// <summary>
        /// Generates a random code verifier for PKCE.
        /// </summary>
        private static string GenerateCodeVerifier()
        {
            byte[] randomBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomBytes);
            }
            return Base64UrlEncode(randomBytes);
        }

        /// <summary>
        /// Generates the code challenge from the verifier using SHA-256.
        /// </summary>
        private static string GenerateCodeChallenge(string codeVerifier)
        {
            using (var sha256 = SHA256.Create())
            {
                byte[] challengeBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(codeVerifier));
                return Base64UrlEncode(challengeBytes);
            }
        }

        /// <summary>
        /// Base64 URL encoding (no padding, URL-safe characters).
        /// </summary>
        private static string Base64UrlEncode(byte[] data)
        {
            return Convert.ToBase64String(data)
                .TrimEnd('=')
                .Replace('+', '-')
                .Replace('/', '_');
        }

        /// <summary>
        /// Opens a URL in the default browser.
        /// </summary>
        private static void OpenBrowser(string url)
        {
            try
            {
                // Works on Windows, Linux, and macOS
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            catch
            {
                // Fallback for Linux
                try
                {
                    Process.Start("xdg-open", url);
                }
                catch (Exception ex)
                {
                    throw new Exception($"Could not open browser: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Starts the local HTTP listener for the OAuth callback.
        /// </summary>
        private static void StartCallbackListener()
        {
            StopCallbackListener(); // Ensure any previous listener is stopped

            _cancellationTokenSource = new CancellationTokenSource();
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{CallbackPort}/");

            try
            {
                _listener.Start();
                Log.Message($"[OpenRouterAuth] Callback listener started on port {CallbackPort}");

                // Handle requests asynchronously
                Task.Run(() => ListenForCallback(_cancellationTokenSource.Token));
            }
            catch (Exception ex)
            {
                Log.Error($"[OpenRouterAuth] Failed to start listener: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Stops the callback listener.
        /// </summary>
        private static void StopCallbackListener()
        {
            _cancellationTokenSource?.Cancel();
            
            if (_listener != null)
            {
                try
                {
                    _listener.Stop();
                    _listener.Close();
                }
                catch { }
                _listener = null;
            }
        }

        /// <summary>
        /// Listens for the OAuth callback and processes it.
        /// </summary>
        private static async Task ListenForCallback(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var contextTask = _listener.GetContextAsync();
                    
                    // Wait for either a request or cancellation
                    var completedTask = await Task.WhenAny(contextTask, Task.Delay(-1, cancellationToken));
                    
                    if (cancellationToken.IsCancellationRequested)
                        break;

                    var context = await contextTask;
                    await HandleCallback(context);
                    break; // Only handle one callback
                }
            }
            catch (OperationCanceledException)
            {
                // Normal cancellation
            }
            catch (Exception ex)
            {
                Log.Error($"[OpenRouterAuth] Listener error: {ex.Message}");
                State = AuthState.Failed;
                LastError = ex.Message;
            }
            finally
            {
                StopCallbackListener();
            }
        }

        /// <summary>
        /// Handles the OAuth callback request.
        /// </summary>
        private static async Task HandleCallback(HttpListenerContext context)
        {
            string responseHtml;

            try
            {
                // Extract the code from the query string
                string code = context.Request.QueryString["code"];

                if (string.IsNullOrEmpty(code))
                {
                    string error = context.Request.QueryString["error"];
                    throw new Exception($"No code received. Error: {error ?? "Unknown"}");
                }

                State = AuthState.ExchangingCode;
                Log.Message("[OpenRouterAuth] Received auth code, exchanging for API key...");

                // Exchange code for API key
                string apiKey = await ExchangeCodeForKey(code, _currentCodeVerifier);

                // Store the API key
                ArtificialRimtelligenceCoreMod.Settings.OpenRouterApiKey = apiKey;
                ArtificialRimtelligenceCoreMod.Settings.Write();

                State = AuthState.Success;
                Log.Message("[OpenRouterAuth] Successfully obtained and stored API key!");

                responseHtml = GetSuccessHtml();
            }
            catch (Exception ex)
            {
                State = AuthState.Failed;
                LastError = ex.Message;
                Log.Error($"[OpenRouterAuth] Failed to exchange code: {ex}");

                responseHtml = GetErrorHtml(ex.Message);
            }
            finally
            {
                _currentCodeVerifier = null;
            }

            // Send response to browser
            byte[] buffer = Encoding.UTF8.GetBytes(responseHtml);
            context.Response.ContentType = "text/html";
            context.Response.ContentLength64 = buffer.Length;
            await context.Response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
            context.Response.Close();
        }

        /// <summary>
        /// Exchanges the authorization code for an API key.
        /// </summary>
        private static async Task<string> ExchangeCodeForKey(string code, string codeVerifier)
        {
            var request = WebRequest.CreateHttp(OpenRouterKeyExchangeUrl);
            request.Method = "POST";
            request.ContentType = "application/json";

            string jsonBody = $@"{{
                ""code"": ""{code}"",
                ""code_verifier"": ""{codeVerifier}"",
                ""code_challenge_method"": ""S256""
            }}";

            byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);
            request.ContentLength = bodyBytes.Length;

            using (var requestStream = await request.GetRequestStreamAsync())
            {
                await requestStream.WriteAsync(bodyBytes, 0, bodyBytes.Length);
            }

            using (var response = await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            using (var reader = new StreamReader(responseStream))
            {
                string responseJson = await reader.ReadToEndAsync();
                
                // Simple JSON parsing for the key field
                // Format: {"key": "sk-or-v1-xxx..."}
                int keyStart = responseJson.IndexOf("\"key\"");
                if (keyStart == -1)
                    throw new Exception("Response did not contain API key");

                int valueStart = responseJson.IndexOf("\"", keyStart + 6) + 1;
                int valueEnd = responseJson.IndexOf("\"", valueStart);
                
                if (valueStart <= 0 || valueEnd <= valueStart)
                    throw new Exception("Could not parse API key from response");

                return responseJson.Substring(valueStart, valueEnd - valueStart);
            }
        }

        /// <summary>
        /// Gets the success HTML from embedded resources.
        /// </summary>
        private static string GetSuccessHtml()
        {
            return GetEmbeddedResource("ArtificialRimtelligenceCore.Resources.auth_success.html");
        }

        /// <summary>
        /// Gets the error HTML from embedded resources, with error message placeholder replaced.
        /// </summary>
        private static string GetErrorHtml(string errorMessage)
        {
            string html = GetEmbeddedResource("ArtificialRimtelligenceCore.Resources.auth_error.html");
            return html.Replace("{{ERROR_MESSAGE}}", WebUtility.HtmlEncode(errorMessage));
        }

        /// <summary>
        /// Reads an embedded resource from the assembly.
        /// </summary>
        private static string GetEmbeddedResource(string resourceName)
        {
            var assembly = typeof(OpenRouterAuth).Assembly;
            using (var stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    Log.Warning($"[OpenRouterAuth] Could not find embedded resource: {resourceName}");
                    return $"<html><body><h1>Resource not found: {resourceName}</h1></body></html>";
                }
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }
    }
}

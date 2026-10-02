// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Azure.Identity;
using Newtonsoft.Json;

namespace NuGet.Services.EndToEnd.Support
{
    /// <summary>
    /// Exchanges the CloudTest VM's managed identity token for a short-lived Gallery key.
    /// This does not use the pipeline identity, developer credentials, or a stored API key.
    /// </summary>
    internal sealed class E2EPublishingKeyProvider
    {
        private readonly Func<string, TokenCredential> _createCredential;
        private readonly Func<HttpClient> _createHttpClient;
        private readonly Func<DateTimeOffset> _utcNow;

        internal E2EPublishingKeyProvider()
            : this(CreateCredential, CreateHttpClient, () => DateTimeOffset.UtcNow)
        {
        }

        internal E2EPublishingKeyProvider(
            Func<string, TokenCredential> createCredential,
            Func<HttpClient> createHttpClient,
            Func<DateTimeOffset> utcNow)
        {
            _createCredential = createCredential ?? throw new ArgumentNullException(nameof(createCredential));
            _createHttpClient = createHttpClient ?? throw new ArgumentNullException(nameof(createHttpClient));
            _utcNow = utcNow ?? throw new ArgumentNullException(nameof(utcNow));
        }

        internal async Task<PublishingKey> AcquireAsync(TestSettings settings)
        {
            // Validate everything before constructing a credential or making any network request.
            var context = ValidateConfiguration(settings);
            if (!Guid.TryParse(settings.ManagedIdentityClientId, out var clientId) || clientId == Guid.Empty)
            {
                throw new InvalidOperationException("E2E trusted publishing requires a non-empty managed identity client ID.");
            }

            AccessToken token;
            try
            {
                var credential = _createCredential(clientId.ToString());
                // Azure.Identity sends this Gallery application ID as the managed-identity token resource.
                var request = new TokenRequestContext(new[] { context.Audience });
                token = await credential.GetTokenAsync(request, CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception)
            {
                // SDK exceptions can contain HTTP details.
                throw new InvalidOperationException("Cannot acquire an Entra token using the CloudTest managed identity.");
            }

            if (string.IsNullOrWhiteSpace(token.Token))
            {
                throw new InvalidOperationException("Managed identity did not return an access token.");
            }

            using (var client = _createHttpClient())
            using (var request = new HttpRequestMessage(HttpMethod.Post, new Uri(context.GalleryUri, "api/v2/token")))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
                request.Headers.UserAgent.ParseAdd("NuGet-E2E-CloudTest/1.0");
                request.Content = new StringContent(JsonConvert.SerializeObject(new
                {
                    username = settings.TestAccountOwner,
                    tokenType = "ApiKey"
                }), Encoding.UTF8, "application/json");

                string content;
                try
                {
                    // Do not retry the exchange: the token can be consumed by the first attempt.
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            throw new InvalidOperationException(
                                $"Gallery token exchange failed (HTTP {(int)response.StatusCode}); not retrying. Check the environment policy and possible token replay.");
                        }

                        content = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    }
                }
                catch (HttpRequestException)
                {
                    throw new InvalidOperationException("Gallery token exchange failed; not retrying an ambiguous request. Details omitted.");
                }
                catch (OperationCanceledException)
                {
                    throw new InvalidOperationException("Gallery token exchange timed out or was canceled; not retrying an ambiguous request.");
                }

                PublishingKey result;
                try
                {
                    result = JsonConvert.DeserializeObject<PublishingKey>(content);
                }
                catch (JsonException)
                {
                    throw new InvalidOperationException("Gallery returned invalid JSON; response omitted.");
                }

                if (result == null || result.TokenType != "ApiKey" || string.IsNullOrWhiteSpace(result.ApiKey))
                {
                    throw new InvalidOperationException("Gallery returned an invalid token exchange response. Expected tokenType 'ApiKey' and a non-empty apiKey.");
                }

                if (!result.Expires.HasValue || result.Expires.Value <= _utcNow())
                {
                    throw new InvalidOperationException("Gallery returned a publishing key with a missing or expired timestamp.");
                }

                return result;
            }
        }

        private static (string Audience, Uri GalleryUri) ValidateConfiguration(TestSettings settings)
        {
            if (settings == null) { throw new ArgumentNullException(nameof(settings)); }
            var match = Regex.Match(settings.ConfigurationName ?? string.Empty,
                @"\A(Dev|Int|Prod)(?:-[A-Za-z0-9][A-Za-z0-9_-]*)?\z", RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
                TimeSpan.FromSeconds(1));
            if (!match.Success)
            {
                throw new InvalidOperationException("E2E trusted publishing requires a Dev, Int, or Prod configuration.");
            }
            if (string.IsNullOrWhiteSpace(settings.TestAccountOwner))
            {
                throw new InvalidOperationException("E2E trusted publishing requires a non-empty policy username.");
            }

            // Audience and destination allowlist come from NuGet.Services deployment configuration.
            var publishing = settings.TrustedPublishing;
            if (publishing == null ||
                !string.Equals(publishing.Environment, match.Groups[1].Value, StringComparison.OrdinalIgnoreCase) ||
                !Guid.TryParse(publishing.Audience, out var audience) || audience == Guid.Empty ||
                publishing.AllowedGalleryHosts == null || publishing.AllowedGalleryHosts.Length == 0 ||
                publishing.AllowedGalleryHosts.Any(host => Uri.CheckHostName(host) != UriHostNameType.Dns))
            {
                throw new InvalidOperationException("E2E trusted publishing requires deployment configuration with a matching environment, non-empty Gallery audience GUID, and approved Gallery hosts.");
            }

            var baseUrl = settings.GalleryConfiguration?.GetServiceBaseUrl();
            if (!TryGetHttpsRootUri(baseUrl, out var gallery) ||
                !publishing.AllowedGalleryHosts.Contains(gallery.Host, StringComparer.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("E2E trusted publishing requires an approved HTTPS Gallery root URL for the configuration environment.");
            }

            return (audience.ToString(), gallery);
        }

        private static bool TryGetHttpsRootUri(string value, out Uri uri)
        {
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                uri.Scheme == Uri.UriSchemeHttps && uri.IsDefaultPort &&
                string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) &&
                string.IsNullOrEmpty(uri.Fragment) && uri.AbsolutePath == "/";
        }

        private static TokenCredential CreateCredential(string clientId)
        {
            var options = new TokenCredentialOptions();
            options.Retry.MaxRetries = 2;
            options.Retry.NetworkTimeout = TimeSpan.FromSeconds(20);
            options.Diagnostics.IsLoggingContentEnabled = false;
            return new ManagedIdentityCredential(clientId, options);
        }

        private static HttpClient CreateHttpClient()
        {
            return new HttpClient(new HttpClientHandler { AllowAutoRedirect = false })
            {
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class E2EPublishingKeyProviderTests
    {
        private const string ClientId = "11111111-2222-4333-8444-555555555555";
        private const string DevAudience = "22222222-2222-4222-8222-222222222222";
        private const string IntAudience = "33333333-3333-4333-8333-333333333333";
        private const string ProdAudience = "44444444-4444-4444-8444-444444444444";
        private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 9, 30, 0, 0, 0, TimeSpan.Zero);

        [Theory]
        [InlineData("Dev-Test", "https://staging.dev.example.test", DevAudience)]
        [InlineData("Int-Test", "https://staging.int.example.test", IntAudience)]
        [InlineData("Prod-Test", "https://prod.example.test", ProdAudience)]
        [InlineData("dev-test", "https://staging.dev.example.test", DevAudience)]
        [InlineData("iNt-Test", "https://staging.int.example.test", IntAudience)]
        [InlineData("pRoD-test", "https://prod.example.test", ProdAudience)]
        public async Task ExchangesOnceWithExplicitIdentityAndEnvironmentScope(string name, string gallery, string audience)
        {
            var harness = new Harness();
            var settings = harness.Settings;
            settings.ConfigurationName = name;
            settings.TrustedPublishing.Environment = name.Split('-')[0].ToUpperInvariant();
            settings.TrustedPublishing.Audience = audience;
            settings.TrustedPublishing.AllowedGalleryHosts = new[] { new Uri(gallery).Host };
            settings.GalleryConfiguration.ServiceDetails.BaseUrl = gallery;
            settings.TestAccountOwner = "ConfiguredOwner";

            await settings.InitializePublishingKeyAsync();

            Assert.Equal(ClientId, harness.SelectedClientId);
            Assert.Equal(new[] { audience }, harness.Credential.Scopes);
            Assert.Equal(1, harness.Credential.Calls);
            Assert.Equal(1, harness.HttpCalls);
            Assert.Equal(gallery + "/api/v2/token", harness.RequestUri.AbsoluteUri);
            Assert.Equal(HttpMethod.Post, harness.Method);
            Assert.Equal("Bearer fake-entra-token", harness.Authorization);
            Assert.Equal("NuGet-E2E-CloudTest/1.0", harness.UserAgent);
            Assert.Equal("ConfiguredOwner", (string)JObject.Parse(harness.Body)["username"]);
            Assert.Equal("ApiKey", (string)JObject.Parse(harness.Body)["tokenType"]);
            Assert.Equal("fake-publishing-key", settings.ApiKey);
            Assert.Equal(Now.AddHours(1), settings.PublishingKeyExpires);
        }

        [Fact]
        public async Task RejectsInvalidDeploymentConfigurationBeforeAcquisition()
        {
            var invalidSettings = new Action<TestSettings>[]
            {
                settings => settings.ConfigurationName = "other-Test",
                settings => settings.ConfigurationName = "dev-test/other",
                settings => settings.ConfigurationName = "dev-test\n",
                settings => { settings.ConfigurationName = "Dev"; settings.TrustedPublishing = null; },
                settings => settings.TrustedPublishing = null,
                settings => settings.TrustedPublishing.Audience = null,
                settings => settings.TrustedPublishing.Audience = "",
                settings => settings.TrustedPublishing.Audience = "not-a-guid",
                settings => settings.TrustedPublishing.Audience = Guid.Empty.ToString(),
                settings => settings.TrustedPublishing.Audience = "https://dev.example.test",
                settings => settings.TrustedPublishing.Environment = "Prod",
                settings => settings.TrustedPublishing.AllowedGalleryHosts = null,
                settings => settings.TrustedPublishing.AllowedGalleryHosts = new[] { "*.dev.nugettest.org" },
                settings => settings.TrustedPublishing.AllowedGalleryHosts = new[] { "other.example.test" },
                settings => settings.GalleryConfiguration.ServiceDetails.BaseUrl = "http://dev.nugettest.org",
                settings => settings.GalleryConfiguration.ServiceDetails.BaseUrl = "https://dev.nugettest.org:8443",
                settings => settings.GalleryConfiguration.ServiceDetails.BaseUrl = "https://user@dev.nugettest.org",
                settings => settings.GalleryConfiguration.ServiceDetails.BaseUrl = "https://dev.nugettest.org/path",
                settings => settings.GalleryConfiguration.ServiceDetails.BaseUrl = "https://dev.nugettest.org?query=value",
                settings => settings.GalleryConfiguration.ServiceDetails.BaseUrl = "https://dev.nugettest.org#fragment"
            };
            foreach (var invalidate in invalidSettings)
            {
                var harness = new Harness();
                harness.Settings.ApiKey = "fake-stored-key";
                invalidate(harness.Settings);
                await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Settings.InitializePublishingKeyAsync());
                Assert.Equal(0, harness.Credential.Calls);
                Assert.Equal(0, harness.HttpCalls);
                Assert.Null(harness.Settings.ApiKey);
            }
        }

        [Fact]
        public async Task NeverRetriesOrFallsBackAfterExchangeFailure()
        {
            var harness = new Harness { Status = HttpStatusCode.Unauthorized, ResponseBody = "secret-response-must-not-appear" };
            harness.Settings.ApiKey = "fake-stored-key";
            var task = harness.Settings.InitializePublishingKeyAsync();
            var error = await Assert.ThrowsAsync<InvalidOperationException>(() => task);
            var repeated = harness.Settings.InitializePublishingKeyAsync();
            Assert.Same(task, repeated);
            await Assert.ThrowsAsync<InvalidOperationException>(() => repeated);
            Assert.Contains("HTTP 401", error.Message);
            Assert.DoesNotContain("secret-response", error.ToString());
            Assert.Equal(1, harness.Credential.Calls);
            Assert.Equal(1, harness.HttpCalls);
            Assert.Null(harness.Settings.ApiKey);
        }

        [Fact]
        public async Task RequiresIdentityWhenTrustedPublishingIsConfigured()
        {
            foreach (var name in new[] { "Dev", "Int", "Prod", "Dev-Test", "Int-Test", "Prod-Test" })
            {
                var harness = new Harness();
                var root = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "TrustedPublishing:Environment", name.Split('-')[0] },
                    { "TrustedPublishing:Audience", DevAudience },
                    { "TrustedPublishing:AllowedGalleryHosts:0", "gallery.example.test" },
                    { "TestSettings:ApiKey", "fake-stored-key" },
                    { "TestSettings:TestAccountOwner", "ConfiguredOwner" },
                    { "TestSettings:GalleryConfiguration:GalleryBaseUrl", "https://gallery.example.test" }
                }).Build();

                var settings = await TestSettings.CreateFromConfigurationAsync(root, name, harness.Provider);
                Assert.Equal("fake-stored-key", settings.ApiKey);
                Assert.Equal(0, harness.Credential.Calls);

                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => settings.InitializePublishingKeyAsync());
                Assert.Contains("managed identity client ID", error.Message);
                Assert.Null(settings.ApiKey);
                Assert.Equal(0, harness.Credential.Calls);
                Assert.Equal(0, harness.HttpCalls);
            }
        }

        [Fact]
        public async Task UsesSuppliedApiKeyOnlyForStandaloneLocalConfigurations()
        {
            foreach (var name in new[] { "Dev", "Int", "Prod", "dev", "iNt", "pRoD", "Dev-Test", "Int-Test", "Prod-Test", "Other" })
            {
                var harness = new Harness();
                var root = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
                {
                    { "TestSettings:ApiKey", "fake-local-key" }
                }).Build();
                var settings = await TestSettings.CreateFromConfigurationAsync(root, name, harness.Provider);
                var task = settings.InitializePublishingKeyAsync();
                if (name.Contains("-") || name == "Other")
                {
                    await Assert.ThrowsAsync<InvalidOperationException>(() => task);
                    Assert.Null(settings.ApiKey);
                }
                else
                {
                    await task;
                    Assert.Same(task, settings.InitializePublishingKeyAsync());
                    Assert.Equal("fake-local-key", settings.ApiKey);
                }

                Assert.Null(settings.PublishingKeyExpires);
                Assert.Null(harness.SelectedClientId);
                Assert.Equal(0, harness.Credential.Calls);
                Assert.Equal(0, harness.HttpCalls);
            }
        }

        [Fact]
        public async Task RejectsMissingOrPlaceholderLocalApiKeysWithoutAcquisition()
        {
            foreach (var key in new[] { null, "", "  ", "API_KEY" })
            {
                var harness = new Harness();
                var settings = new TestSettings(harness.Provider) { ConfigurationName = "Dev", ApiKey = key };
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => settings.InitializePublishingKeyAsync());
                Assert.Contains("Local E2E execution requires", error.Message);
                Assert.Null(harness.SelectedClientId);
                Assert.Equal(0, harness.Credential.Calls);
                Assert.Equal(0, harness.HttpCalls);
            }
        }

        [Fact]
        public async Task DefersAcquisitionUntilExecutionAndSharesOneInMemoryKey()
        {
            var harness = new Harness();
            var root = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string>
            {
                { "ManagedIdentityClientId", ClientId },
                { "TrustedPublishing:Environment", "Dev" },
                { "TrustedPublishing:Audience", IntAudience },
                { "TrustedPublishing:AllowedGalleryHosts:0", "dev.nugettest.org" },
                { "TestSettings:TestAccountOwner", "ConfiguredOwner" },
                { "TestSettings:GalleryConfiguration:GalleryBaseUrl", "https://dev.nugettest.org" }
            }).Build();

            var settings = await TestSettings.CreateFromConfigurationAsync(root, "Dev-Test", harness.Provider);
            Assert.Equal(ClientId, settings.ManagedIdentityClientId);
            Assert.Equal("Dev-Test", settings.ConfigurationName);
            Assert.Equal("Dev", settings.TrustedPublishing.Environment);
            Assert.Equal(IntAudience, settings.TrustedPublishing.Audience);
            Assert.Equal(new[] { "dev.nugettest.org" }, settings.TrustedPublishing.AllowedGalleryHosts);
            Assert.Null(settings.ApiKey);
            Assert.Equal(0, harness.Credential.Calls);
            Assert.Equal(0, harness.HttpCalls);

            var gate = new TaskCompletionSource<AccessToken>();
            harness.Credential.Acquire = () => gate.Task;
            var tasks = Enumerable.Range(0, 2).Select(_ => settings.InitializePublishingKeyAsync()).ToArray();
            Assert.Same(tasks[0], tasks[1]);
            Assert.Equal(1, harness.Credential.Calls);
            Assert.Equal(0, harness.HttpCalls);

            gate.SetResult(new AccessToken("fake-entra-token", Now.AddHours(1)));
            await Task.WhenAll(tasks);
            await settings.InitializePublishingKeyAsync();
            Assert.Equal(1, harness.HttpCalls);
            Assert.Equal(new[] { IntAudience }, harness.Credential.Scopes);
            Assert.Equal("fake-publishing-key", settings.ApiKey);
            Assert.Equal("https://dev.nugettest.org/api/v2/token", harness.RequestUri.AbsoluteUri);
            Assert.Null(root["TestSettings:ApiKey"]); // Key exists only on the in-memory settings object.
        }

        [Fact]
        public async Task DeserializesExpiryAndRejectsInvalidResponsesWithoutLeakingSecrets()
        {
            foreach (var remaining in new[] { TimeSpan.FromTicks(1), TimeSpan.FromHours(1) })
            {
                var expires = Now.Add(remaining).ToOffset(TimeSpan.FromHours(2));
                var valid = new Harness { ResponseBody = Response(expires) };
                await valid.Settings.InitializePublishingKeyAsync();
                Assert.Equal(expires, valid.Settings.PublishingKeyExpires);
                Assert.Equal("fake-publishing-key", valid.Settings.ApiKey);
            }

            var invalidResponses = new[]
            {
                "{\"tokenType\":\"ApiKey\",\"apiKey\":\"fake-publishing-key\"}",
                "{\"tokenType\":\"ApiKey\",\"apiKey\":\"fake-publishing-key\",\"expires\":null}",
                "{\"tokenType\":\"ApiKey\",\"apiKey\":\"fake-publishing-key\",\"expires\":\"secret-invalid-date\"}",
                Response(Now),
                Response(Now.AddMinutes(-1))
            };
            foreach (var response in invalidResponses)
            {
                var harness = new Harness { ResponseBody = response };
                var error = await Assert.ThrowsAsync<InvalidOperationException>(() => harness.Settings.InitializePublishingKeyAsync());
                Assert.DoesNotContain("fake-publishing-key", error.ToString());
                Assert.DoesNotContain("secret-invalid-date", error.ToString());
                Assert.Null(harness.Settings.ApiKey);
                Assert.Null(harness.Settings.PublishingKeyExpires);
                Assert.Equal(1, harness.HttpCalls);
            }
        }

        private static string Response(DateTimeOffset expires)
        {
            return JsonConvert.SerializeObject(new { tokenType = "ApiKey", apiKey = "fake-publishing-key", expires = expires.ToString("O") });
        }

        private sealed class Harness
        {
            internal Harness()
            {
                Provider = new E2EPublishingKeyProvider(id =>
                {
                    SelectedClientId = id;
                    return Credential;
                }, () => new HttpClient(new Handler(this)), () => Now);
                Settings = new TestSettings(Provider)
                {
                    ConfigurationName = "Dev-Test",
                    ManagedIdentityClientId = ClientId,
                    TrustedPublishing = new TrustedPublishingSettings
                    {
                        Environment = "Dev",
                        Audience = DevAudience,
                        AllowedGalleryHosts = new[] { "dev.nugettest.org", "gallery-usnc-ase-staging.dev.nugettest.org" }
                    },
                    TestAccountOwner = "ExamplePublishingOwner",
                    GalleryConfiguration = new GalleryConfiguration
                    {
                        GalleryBaseUrl = "https://dev.nugettest.org",
                        ServiceDetails = new ServiceDetails { BaseUrl = "https://gallery-usnc-ase-staging.dev.nugettest.org" }
                    }
                };
            }

            internal readonly FakeCredential Credential = new FakeCredential();
            internal E2EPublishingKeyProvider Provider { get; }
            internal TestSettings Settings { get; }
            internal string SelectedClientId;
            internal int HttpCalls;
            internal HttpStatusCode Status = HttpStatusCode.OK;
            internal string ResponseBody = Response(Now.AddHours(1));
            internal Uri RequestUri;
            internal HttpMethod Method;
            internal string Authorization;
            internal string UserAgent;
            internal string Body;

            private sealed class Handler : HttpMessageHandler
            {
                private readonly Harness _harness;
                internal Handler(Harness harness) { _harness = harness; }

                protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
                {
                    _harness.HttpCalls++;
                    _harness.RequestUri = request.RequestUri;
                    _harness.Method = request.Method;
                    _harness.Authorization = request.Headers.Authorization.ToString();
                    _harness.UserAgent = request.Headers.UserAgent.ToString();
                    _harness.Body = await request.Content.ReadAsStringAsync();
                    return new HttpResponseMessage(_harness.Status) { Content = new StringContent(_harness.ResponseBody) };
                }
            }
        }

        private sealed class FakeCredential : TokenCredential
        {
            internal int Calls;
            internal string[] Scopes;
            internal Func<Task<AccessToken>> Acquire = () => Task.FromResult(new AccessToken("fake-entra-token", Now.AddHours(1)));

            public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
                => throw new NotSupportedException("Only asynchronous acquisition is expected.");

            public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
            {
                Calls++;
                Scopes = requestContext.Scopes;
                return new ValueTask<AccessToken>(Acquire());
            }
        }
    }
}
// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using Newtonsoft.Json;
using Xunit;

namespace NuGet.Services.EndToEnd.Support
{
    public class TestSettingsTests
    {
        [Fact]
        public void TestSettingsManualConfigurationOverrideShouldBeEmpty()
        {
            /// This test is expected to fail when there is a local change to <see cref="TestSettings.ManualConfigurationOverride"/>.
            /// This is okay. We just want to be sure not to check this change in.
            Assert.Equal(string.Empty, TestSettings.ManualConfigurationOverride);
        }

        [Fact]
        public void TestSettingsAggressivePushShouldAlwaysBeTrue()
        {
            /// This test is expected to fail when there is a local change to <see cref="TestSettings.AggressivePush"/>.
            /// This is okay. We just want to be sure not to check this change in.
            Assert.True(TestSettings.AggressivePush);
        }

        [Theory]
        [InlineData("Dev")]
        [InlineData("Int")]
        [InlineData("Prod")]
        public void TestSettingsShouldNotMentionRealApiKeys(string configurationName)
        {
            // Arrange
            Assert.Equal("API_KEY", TestSettings.CreateLocalTestConfiguration(configurationName).ApiKey);
        }

        [Fact]
        public void RemoveMeTest()
        {
            //var a = new SearchServiceConfiguration();
            //a.IndexJsonMappedSearchServices = new System.Collections.Generic.Dictionary<string, AzureCloudServiceDetails>
            //{
            //    { "api-v2v3search-0.nuget.org", new AzureCloudServiceDetails()
            //    {
            //        Name = "bla",
            //        ResourceGroup = "res",
            //        Slot = "pro",
            //        Subscription = "asdsefs"
            //    }
            //} };

            var a = new List<string>() { "a", "b", "c" };
            string output = JsonConvert.SerializeObject(a);
        }
    }
}

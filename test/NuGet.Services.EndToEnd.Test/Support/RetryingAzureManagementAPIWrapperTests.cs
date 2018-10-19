// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using NuGet.Services.AzureManagement;
using Xunit;
using Xunit.Abstractions;

namespace NuGet.Services.EndToEnd.Support
{
    public class RetryingAzureManagementAPIWrapperTests
    {
        private readonly ITestOutputHelper _output;
        private readonly string _subscription;
        private readonly string _resourceGroup;
        private readonly string _name;
        private readonly string _slot;
        private readonly CancellationToken _token;
        private readonly string _properties;
        private readonly Mock<IAzureManagementAPIWrapper> _inner;
        private readonly RetryingAzureManagementAPIWrapper _target;

        public RetryingAzureManagementAPIWrapperTests(ITestOutputHelper output)
        {
            _output = output ?? throw new ArgumentNullException(nameof(output));

            _subscription = "test";
            _resourceGroup = "rg";
            _name = "name";
            _slot = "staging";
            _token = CancellationToken.None;
            _properties = "{\"key\":\"value\"}";

            _inner = new Mock<IAzureManagementAPIWrapper>();
            _target = new RetryingAzureManagementAPIWrapper(
                _inner.Object,
                sleepDuration: TimeSpan.Zero);
        }

        [Fact]
        public async Task PassesThroughParameters()
        {
            await _target.GetCloudServicePropertiesAsync(
                _subscription,
                _resourceGroup,
                _name,
                _slot,
                _output,
                _token);

            _inner.Verify(
                x => x.GetCloudServicePropertiesAsync(
                    _subscription,
                    _resourceGroup,
                    _name,
                    _slot,
                    _token),
                Times.Once);
            _inner.Verify(
                x => x.GetCloudServicePropertiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Theory]
        [MemberData(nameof(TransientExceptions))]
        public async Task RetriesWithWhenTransientExceptionTypeIsThrown(Exception exception)
        {
            _inner
                .SetupSequence(x => x.GetCloudServicePropertiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception)
                .ReturnsAsync(_properties);

            var actual = await _target.GetCloudServicePropertiesAsync(
                _subscription,
                _resourceGroup,
                _name,
                _slot,
                _output,
                _token);
            Assert.Equal(_properties, actual);
            _inner.Verify(
                x => x.GetCloudServicePropertiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        public static IEnumerable<object[]> TransientExceptions => new[]
        {
            new object[] { new AzureManagementException("Some message.") },
            new object[] { new TaskCanceledException("Timeout!") },
            new object[] { new SocketException(10060) },
        };

        [Fact]
        public async Task DoesNotRetryWhenOtherExceptionIsThrown()
        {
            var expected = new InvalidOperationException("Some message.");
            _inner
                .SetupSequence(x => x.GetCloudServicePropertiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(expected)
                .ReturnsAsync(_properties);

            var actual = await Assert.ThrowsAsync<InvalidOperationException>(() => _target.GetCloudServicePropertiesAsync(
                _subscription,
                _resourceGroup,
                _name,
                _slot,
                _output,
                _token));

            Assert.Same(expected, actual);
            _inner.Verify(
                x => x.GetCloudServicePropertiesAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }
    }
}

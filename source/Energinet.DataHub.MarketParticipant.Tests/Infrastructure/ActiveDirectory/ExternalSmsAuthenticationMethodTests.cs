// Copyright 2020 Energinet DataHub A/S
//
// Licensed under the Apache License, Version 2.0 (the "License2");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.

using System.Collections.Generic;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Microsoft.Graph;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Infrastructure.ActiveDirectory;

[UnitTest]
public sealed class ExternalSmsAuthenticationMethodTests
{
    private readonly SmsAuthenticationMethod _smsAuthenticationMethod = new(new PhoneNumber("+45 00000000"));

    [Fact]
    public async Task AssignAsync_GivenBuilder_AddsPhoneAuthentication()
    {
        // Arrange
        var phoneMethodsRequestMock = new Mock<IAuthenticationPhoneMethodsCollectionRequest>();
        phoneMethodsRequestMock
            .Setup(phoneMethodsRequest => phoneMethodsRequest.MiddlewareOptions)
            .Returns(new Dictionary<string, IMiddlewareOption>());

        var phoneMethodsMock = new Mock<IAuthenticationPhoneMethodsCollectionRequestBuilder>();
        phoneMethodsMock
            .Setup(phoneMethods => phoneMethods.Request())
            .Returns(phoneMethodsRequestMock.Object);

        var authenticationRequestMock = new Mock<IAuthenticationRequestBuilder>();
        authenticationRequestMock
            .Setup(authenticationRequest => authenticationRequest.PhoneMethods)
            .Returns(phoneMethodsMock.Object);

        var target = new ExternalSmsAuthenticationMethod(_smsAuthenticationMethod);

        // Act
        await target.AssignAsync(authenticationRequestMock.Object);

        // Assert
        phoneMethodsRequestMock.Verify(phoneMethodsRequest => phoneMethodsRequest.AddAsync(
            It.Is<PhoneAuthenticationMethod>(pam =>
                pam.PhoneNumber == _smsAuthenticationMethod.PhoneNumber.Number &&
                pam.PhoneType == AuthenticationPhoneType.Mobile),
            default));
    }

    [Fact]
    public async Task VerifyAsync_GivenMatch_ReturnsTrue()
    {
        // Arrange
        var expected = new PhoneAuthenticationMethod
        {
            PhoneNumber = _smsAuthenticationMethod.PhoneNumber.Number,
            PhoneType = AuthenticationPhoneType.Mobile
        };

        var phoneMethodsPage = new AuthenticationPhoneMethodsCollectionPage { CurrentPage = { expected } };

        var phoneMethodsRequestMock = new Mock<IAuthenticationPhoneMethodsCollectionRequest>();
        phoneMethodsRequestMock
            .Setup(phoneMethodsRequest => phoneMethodsRequest.MiddlewareOptions)
            .Returns(new Dictionary<string, IMiddlewareOption>());
        phoneMethodsRequestMock
            .Setup(phoneMethodsRequest => phoneMethodsRequest.GetAsync(default))
            .ReturnsAsync(phoneMethodsPage);

        var phoneMethodsMock = new Mock<IAuthenticationPhoneMethodsCollectionRequestBuilder>();
        phoneMethodsMock
            .Setup(phoneMethods => phoneMethods.Request())
            .Returns(phoneMethodsRequestMock.Object);

        var authenticationRequestMock = new Mock<IAuthenticationRequestBuilder>();
        authenticationRequestMock
            .Setup(authenticationRequest => authenticationRequest.PhoneMethods)
            .Returns(phoneMethodsMock.Object);

        authenticationRequestMock
            .Setup(authenticationRequest => authenticationRequest.Client)
            .Returns(new Mock<IBaseClient>().Object);

        var target = new ExternalSmsAuthenticationMethod(_smsAuthenticationMethod);

        // Act
        var result = await target.VerifyAsync(authenticationRequestMock.Object);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task VerifyAsync_GivenNoMatch_ReturnsFalse()
    {
        // Arrange
        var notExpected = new PhoneAuthenticationMethod
        {
            PhoneNumber = "+45 00000001",
            PhoneType = AuthenticationPhoneType.Mobile
        };

        var phoneMethodsPage = new AuthenticationPhoneMethodsCollectionPage { CurrentPage = { notExpected } };

        var phoneMethodsRequestMock = new Mock<IAuthenticationPhoneMethodsCollectionRequest>();
        phoneMethodsRequestMock
            .Setup(phoneMethodsRequest => phoneMethodsRequest.MiddlewareOptions)
            .Returns(new Dictionary<string, IMiddlewareOption>());
        phoneMethodsRequestMock
            .Setup(phoneMethodsRequest => phoneMethodsRequest.GetAsync(default))
            .ReturnsAsync(phoneMethodsPage);

        var phoneMethodsMock = new Mock<IAuthenticationPhoneMethodsCollectionRequestBuilder>();
        phoneMethodsMock
            .Setup(phoneMethods => phoneMethods.Request())
            .Returns(phoneMethodsRequestMock.Object);

        var authenticationRequestMock = new Mock<IAuthenticationRequestBuilder>();
        authenticationRequestMock
            .Setup(authenticationRequest => authenticationRequest.PhoneMethods)
            .Returns(phoneMethodsMock.Object);

        authenticationRequestMock
            .Setup(authenticationRequest => authenticationRequest.Client)
            .Returns(new Mock<IBaseClient>().Object);

        var target = new ExternalSmsAuthenticationMethod(_smsAuthenticationMethod);

        // Act
        var result = await target.VerifyAsync(authenticationRequestMock.Object);

        // Assert
        Assert.False(result);
    }
}

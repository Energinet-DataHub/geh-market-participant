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

using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Organization;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Organization;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetOrganizationIdentityHandlerTests
{
    [Fact]
    public async Task GetOrganizationIdentityHandler_WhenOrganizationIdentityExist_ReturnsFoundOrganizationIdentity()
    {
        // arrange
        var organizationIdentityRepositoryMock = new Mock<IOrganizationIdentityRepository>();
        organizationIdentityRepositoryMock
            .Setup(organizationIdentityRepository => organizationIdentityRepository.GetAsync(new BusinessRegisterIdentifier("12345678")))
            .ReturnsAsync(new OrganizationIdentity("OrganizationName"));

        var target = new GetOrganizationIdentityHandler(organizationIdentityRepositoryMock.Object);

        // act
        var actual = await target.Handle(new GetOrganizationIdentityCommand("12345678"), CancellationToken.None);

        // assert
        Assert.NotNull(actual);
        Assert.True(actual.OrganizationFound);
        Assert.NotNull(actual.OrganizationIdentity);
        Assert.Equal("OrganizationName", actual.OrganizationIdentity.Name);
    }

    [Fact]
    public async Task GetOrganizationIdentityHandler_WhenOrganizationIdentityDoesNotExist_ReturnsNotFound()
    {
        // arrange
        var organizationIdentityRepositoryMock = new Mock<IOrganizationIdentityRepository>();
        organizationIdentityRepositoryMock
            .Setup(organizationIdentityRepository => organizationIdentityRepository.GetAsync(It.IsAny<BusinessRegisterIdentifier>()))
            .ReturnsAsync((OrganizationIdentity?)null);

        var target = new GetOrganizationIdentityHandler(organizationIdentityRepositoryMock.Object);

        // act
        var actual = await target.Handle(new GetOrganizationIdentityCommand("12345678"), CancellationToken.None);

        // assert
        Assert.NotNull(actual);
        Assert.False(actual.OrganizationFound);
        Assert.Null(actual.OrganizationIdentity);
    }
}

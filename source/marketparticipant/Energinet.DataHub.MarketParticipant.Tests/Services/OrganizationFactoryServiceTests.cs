﻿// Copyright 2020 Energinet DataHub A/S
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

using System;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Handlers;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services;

[UnitTest]
public sealed class OrganizationFactoryServiceTests
{
    private readonly Address _validAddress = new(
        "test Street",
        "1",
        "1111",
        "Test City",
        "DK");

    private readonly OrganizationDomain _validDomain = new("energinet.dk");

    private readonly BusinessRegisterIdentifier _validCvrBusinessRegisterIdentifier = new("12345678");

    [Fact]
    public async Task CreateAsync_NullName_ThrowsException()
    {
        // Arrange
        var organizationRepository = new Mock<IOrganizationRepository>();
        var target = new OrganizationFactoryService(
            organizationRepository.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IUniqueOrganizationBusinessRegisterIdentifierRuleService>().Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => target.CreateAsync(
            null!,
            _validCvrBusinessRegisterIdentifier,
            _validAddress,
            [_validDomain]));
    }

    [Fact]
    public async Task CreateAsync_NullBusinessRegisterIdentifier_ThrowsException()
    {
        // Arrange
        var organizationRepository = new Mock<IOrganizationRepository>();
        var target = new OrganizationFactoryService(
            organizationRepository.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IUniqueOrganizationBusinessRegisterIdentifierRuleService>().Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => target.CreateAsync(
            "fake_value",
            null!,
            _validAddress,
            [_validDomain]));
    }

    [Fact]
    public async Task CreateAsync_NullAddress_ThrowsException()
    {
        // Arrange
        var organizationRepository = new Mock<IOrganizationRepository>();
        var target = new OrganizationFactoryService(
            organizationRepository.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IUniqueOrganizationBusinessRegisterIdentifierRuleService>().Object);

        // Act + Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => target.CreateAsync(
            "fake_value",
            _validCvrBusinessRegisterIdentifier,
            null!,
            [_validDomain]));
    }

    [Fact]
    public async Task CreateAsync_NewOrganization_AddsAndReturnsOrganization()
    {
        // Arrange
        var organizationRepository = new Mock<IOrganizationRepository>();
        var target = new OrganizationFactoryService(
            organizationRepository.Object,
            UnitOfWorkProviderMock.Create(),
            new Mock<IUniqueOrganizationBusinessRegisterIdentifierRuleService>().Object);

        var orgId = new OrganizationId(Guid.NewGuid());
        var organization = new Organization(
            orgId,
            "fake_value",
            _validCvrBusinessRegisterIdentifier,
            _validAddress,
            [_validDomain],
            OrganizationStatus.Active);

        organizationRepository
            .Setup(x => x.AddOrUpdateAsync(It.IsAny<Organization>()))
            .ReturnsAsync(new Result<OrganizationId, OrganizationError>(organization.Id));

        organizationRepository
            .Setup(x => x.GetAsync(organization.Id))
            .ReturnsAsync(organization);

        // Act
        var response = await target
            .CreateAsync(
                "fake_value",
                _validCvrBusinessRegisterIdentifier,
                _validAddress,
                [_validDomain]);

        // Assert
        Assert.NotNull(response);
        Assert.Equal(_validCvrBusinessRegisterIdentifier, organization.BusinessRegisterIdentifier);
        Assert.Equal(_validCvrBusinessRegisterIdentifier, organization.BusinessRegisterIdentifier);
        Assert.Equal(_validAddress.City, organization.Address.City);
        Assert.Equal(_validAddress.Country, organization.Address.Country);
        Assert.Equal(_validAddress.Number, organization.Address.Number);
        Assert.Equal(_validAddress.StreetName, organization.Address.StreetName);
        Assert.Equal(_validAddress.ZipCode, organization.Address.ZipCode);
    }
}

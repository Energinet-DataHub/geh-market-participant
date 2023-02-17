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

using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Tests.Common;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Services
{
    [UnitTest]
    public sealed class OrganizationDomainValidationServiceTests
    {
        [Fact]
        public async Task ValidationDomain_EmailAndOrganizationMatch()
        {
            // Arrange
            var organizationRepositoryMock = new Mock<IOrganizationRepository>();
            var actorQueryRepository = new Mock<IActorQueryRepository>();

            var actor = new Actor(
                Guid.NewGuid(),
                null,
                new MockedGln(),
                ActorStatus.New,
                Array.Empty<ActorMarketRole>(),
                new ActorName("fake_value"));

            var actorEmail = new EmailAddress("newuser@testdomain.dk");
            var orgDomainToTest = new OrganizationDomain("testdomain.dk");

            SetupActorMock(actor.Id, actorQueryRepository);
            SetupOrganizationMock(orgDomainToTest, organizationRepositoryMock);

            var organizationDomainValidationService = new OrganizationDomainValidationService(organizationRepositoryMock.Object, actorQueryRepository.Object);

            // Act + Assert
            await organizationDomainValidationService
                .ValidateUserEmailInsideOrganizationDomainsAsync(actor, actorEmail)
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task ValidationDomain_EmailDomain_IsWrong()
        {
            // Arrange
            var organizationRepositoryMock = new Mock<IOrganizationRepository>();
            var actorQueryRepository = new Mock<IActorQueryRepository>();

            var actor = new Actor(
                Guid.NewGuid(),
                null,
                new MockedGln(),
                ActorStatus.New,
                Array.Empty<ActorMarketRole>(),
                new ActorName("fake_value"));

            var actorEmail = new EmailAddress("newuser@wrongdomain.dk");
            var orgDomainToTest = new OrganizationDomain("testdomain.dk");

            SetupActorMock(actor.Id, actorQueryRepository);
            SetupOrganizationMock(orgDomainToTest, organizationRepositoryMock);

            var organizationDomainValidationService = new OrganizationDomainValidationService(organizationRepositoryMock.Object, actorQueryRepository.Object);

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() => organizationDomainValidationService
                .ValidateUserEmailInsideOrganizationDomainsAsync(actor, actorEmail))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task ValidationDomain_OrganizationDomain_IsWrong()
        {
            // Arrange
            var organizationRepositoryMock = new Mock<IOrganizationRepository>();
            var actorQueryRepository = new Mock<IActorQueryRepository>();

            var actor = new Actor(
                Guid.NewGuid(),
                null,
                new MockedGln(),
                ActorStatus.New,
                Array.Empty<ActorMarketRole>(),
                new ActorName("fake_value"));

            var actorEmail = new EmailAddress("newuser@testdomain.dk");
            var orgDomainToTest = new OrganizationDomain("test2domain.dk");

            SetupActorMock(actor.Id, actorQueryRepository);
            SetupOrganizationMock(orgDomainToTest, organizationRepositoryMock);

            var organizationDomainValidationService = new OrganizationDomainValidationService(organizationRepositoryMock.Object, actorQueryRepository.Object);

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() => organizationDomainValidationService
                    .ValidateUserEmailInsideOrganizationDomainsAsync(actor, actorEmail))
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task ValidationDomain_SpecialTest_ExceptionExpected()
        {
            // Arrange
            var organizationRepositoryMock = new Mock<IOrganizationRepository>();
            var actorQueryRepository = new Mock<IActorQueryRepository>();

            var actor = new Actor(
                Guid.NewGuid(),
                null,
                new MockedGln(),
                ActorStatus.New,
                Array.Empty<ActorMarketRole>(),
                new ActorName("fake_value"));

            var actorEmail = new EmailAddress("newuser@shouldfail-testdomain.dk");
            var orgDomainToTest = new OrganizationDomain("testdomain.dk");

            actorQueryRepository
                .Setup(a => a.GetActorAsync(actor.Id))
                .ReturnsAsync(new Domain.Model.Query.Actor(new OrganizationId(Guid.NewGuid()), actor.Id, ActorStatus.Active));

            SetupOrganizationMock(orgDomainToTest, organizationRepositoryMock);
            var organizationDomainValidationService = new OrganizationDomainValidationService(organizationRepositoryMock.Object, actorQueryRepository.Object);

            // Act + Assert
            await Assert.ThrowsAsync<ValidationException>(() => organizationDomainValidationService
                    .ValidateUserEmailInsideOrganizationDomainsAsync(actor, actorEmail))
                .ConfigureAwait(false);
        }

        private static void SetupActorMock(Guid actorId, Mock<IActorQueryRepository> actorQueryRepository)
        {
            var actor = new Domain.Model.Query.Actor(new OrganizationId(Guid.NewGuid()), actorId, ActorStatus.Active);

            actorQueryRepository
                .Setup(a => a.GetActorAsync(actorId))
                .ReturnsAsync(actor);
        }

        private static void SetupOrganizationMock(
            OrganizationDomain orgDomainToTest,
            Mock<IOrganizationRepository> organizationRepositoryMock)
        {
            var organization = new Organization(
                "TestOrg",
                new BusinessRegisterIdentifier("identifier"),
                new Address(string.Empty, string.Empty, string.Empty, string.Empty, string.Empty),
                orgDomainToTest,
                "comment");

            organizationRepositoryMock
                .Setup(o => o.GetAsync(It.IsAny<OrganizationId>()))
                .ReturnsAsync(organization);
        }
    }
}

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
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Flurl.Http;
using Flurl.Http.Configuration;
using Flurl.Http.Testing;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Libraries.Tests.Clients
{
    [UnitTest]
    public sealed class MarketParticipantClientTests
    {
        private readonly AddressDto _validAddress = new(
            "Testvej",
            "2",
            "4321",
            "Testby",
            "Testland");

        private readonly string _validCvr = "87654321";
        [Fact]
        public async Task GetOrganizationsAsync_Unauthorized_ThrowsException()
        {
            // Arrange
            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith("unauthorized", (int)HttpStatusCode.Unauthorized);

            // Act + Assert
            var exception = await Assert
                .ThrowsAsync<FlurlHttpException>(() => target.GetOrganizationsAsync())
                .ConfigureAwait(false);
            Assert.Equal(exception.StatusCode, (int)HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task GetOrganizationsAsync_All_ReturnsOrganization()
        {
            // Arrange
            const string incomingJson = @"
[
        {
            ""OrganizationId"": ""fb6665a1-b7be-4744-a8ce-08da0272c916"",
            ""Name"": ""unit test"",
            ""Actors"": [
                {
                    ""ActorId"": ""8a46b5ac-4c7d-48c0-3f16-08da0279759b"",
                    ""ExternalActorId"": ""75ea715f-381e-46fd-831b-5b61b9db7862"",
                    ""Gln"": {
                        ""Value"": ""9656626091925""
                    },
                    ""Status"": ""Active"",
                    ""MarketRoles"": [
                        {
                            ""Function"": ""Consumer""
                        }
                    ]
                }
            ],
            ""cvr"": ""87654321"",
            ""address"": {
                ""streetName"": ""Testvej"",
                ""number"": ""2"",
                ""zipCode"": ""4321"",
                ""city"": ""Testby"",
                ""country"": ""Testland""
            }
        }
    ]}";
            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith(incomingJson);

            // Act
            var actual = await target.GetOrganizationsAsync().ConfigureAwait(false);

            // Assert
            var actualOrganization = actual.Single();
            Assert.Equal(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"), actualOrganization.OrganizationId);
            Assert.Equal("unit test", actualOrganization.Name);

            var actualActor = actualOrganization.Actors.Single();
            Assert.Equal(Guid.Parse("8a46b5ac-4c7d-48c0-3f16-08da0279759b"), actualActor.ActorId);
            Assert.Equal(Guid.Parse("75ea715f-381e-46fd-831b-5b61b9db7862"), actualActor.ExternalActorId);
            Assert.Equal("9656626091925", actualActor.Gln.Value);
            Assert.Equal(ActorStatus.Active, actualActor.Status);
            Assert.Equal(_validCvr, actualOrganization.Cvr);
            Assert.Equal(_validAddress.City, actualOrganization.Address.City);
            Assert.Equal(_validAddress.Country, actualOrganization.Address.Country);
            Assert.Equal(_validAddress.Number, actualOrganization.Address.Number);
            Assert.Equal(_validAddress.StreetName, actualOrganization.Address.StreetName);
            Assert.Equal(_validAddress.ZipCode, actualOrganization.Address.ZipCode);

            var actualMarketRole = actualActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Consumer, actualMarketRole.Function);
        }

        [Fact]
        public async Task GetOrganizationsAsync_All_Returns2Organizations()
        {
            // Arrange
            const string incomingJson = @"
                [
                    {
                        ""OrganizationId"": ""fb6665a1-b7be-4744-a8ce-08da0272c916"",
                        ""Name"": ""unit test"",
                        ""Actors"": [
                            {
                                ""ActorId"": ""8a46b5ac-4c7d-48c0-3f16-08da0279759b"",
                                ""ExternalActorId"": ""75ea715f-381e-46fd-831b-5b61b9db7862"",
                                ""Gln"": {
                                    ""Value"": ""9656626091925""
                                },
                                ""Status"": ""Active"",
                                ""MarketRoles"": [
                                    {
                                        ""Function"": ""Consumer""
                                    }
                                ]
                            }
                        ],
                        ""cvr"": ""87654321"",
                        ""address"": {
                            ""streetName"": ""Testvej"",
                            ""number"": ""2"",
                            ""zipCode"": ""4321"",
                            ""city"": ""Testby"",
                            ""country"": ""Testland""
                        }
                    },
                    {
                        ""OrganizationId"": ""c4d950f7-0acf-439b-9bb6-610255218c6e"",
                        ""Name"": ""unit test 2"",
                        ""Actors"": [
                            {
                                ""ActorId"": ""f6792b0b-7dee-4e70-b9d9-46b727e6748b"",
                                ""ExternalActorId"": ""dfef92e2-923e-43aa-8706-ac7445cddfb3"",
                                ""Gln"": {
                                    ""Value"": ""8574664796620""
                                },
                                ""Status"": ""New"",
                                ""MarketRoles"": [
                                    {
                                        ""Function"": ""Producer""
                                    }
                                ]
                            }
                        ],
                        ""cvr"": ""87654321"",
                        ""address"": {
                            ""streetName"": ""Testvej"",
                            ""number"": ""2"",
                            ""zipCode"": ""4321"",
                            ""city"": ""Testby"",
                            ""country"": ""Testland""
                        }
                    }
                ]";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith(incomingJson);

            // Act
            var actual = await target.GetOrganizationsAsync().ConfigureAwait(false);

            // Assert
            Assert.NotNull(actual);
            actual = actual.ToList();
            Assert.Equal(2, actual.Count());
            var firstOrganization = actual.First();
            Assert.Equal(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"), firstOrganization.OrganizationId);
            Assert.Equal("unit test", firstOrganization.Name);
            Assert.Equal(_validCvr, firstOrganization.Cvr);
            Assert.Equal(_validAddress.City, firstOrganization.Address.City);
            Assert.Equal(_validAddress.Country, firstOrganization.Address.Country);
            Assert.Equal(_validAddress.Number, firstOrganization.Address.Number);
            Assert.Equal(_validAddress.StreetName, firstOrganization.Address.StreetName);
            Assert.Equal(_validAddress.ZipCode, firstOrganization.Address.ZipCode);

            var firstActor = firstOrganization.Actors.Single();
            Assert.Equal(Guid.Parse("8a46b5ac-4c7d-48c0-3f16-08da0279759b"), firstActor.ActorId);
            Assert.Equal(Guid.Parse("75ea715f-381e-46fd-831b-5b61b9db7862"), firstActor.ExternalActorId);
            Assert.Equal("9656626091925", firstActor.Gln.Value);
            Assert.Equal(ActorStatus.Active, firstActor.Status);

            var firstMarketRole = firstActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Consumer, firstMarketRole.Function);

            var secondOrganization = actual.Skip(1).First();
            Assert.Equal(Guid.Parse("c4d950f7-0acf-439b-9bb6-610255218c6e"), secondOrganization.OrganizationId);
            Assert.Equal("unit test 2", secondOrganization.Name);
            Assert.Equal(_validCvr, secondOrganization.Cvr);
            Assert.Equal(_validAddress.City, secondOrganization.Address.City);
            Assert.Equal(_validAddress.Country, secondOrganization.Address.Country);
            Assert.Equal(_validAddress.Number, secondOrganization.Address.Number);
            Assert.Equal(_validAddress.StreetName, secondOrganization.Address.StreetName);
            Assert.Equal(_validAddress.ZipCode, secondOrganization.Address.ZipCode);

            var secondActor = secondOrganization.Actors.Single();
            Assert.Equal(Guid.Parse("f6792b0b-7dee-4e70-b9d9-46b727e6748b"), secondActor.ActorId);
            Assert.Equal(Guid.Parse("dfef92e2-923e-43aa-8706-ac7445cddfb3"), secondActor.ExternalActorId);
            Assert.Equal("8574664796620", secondActor.Gln.Value);
            Assert.Equal(ActorStatus.New, secondActor.Status);

            var secondMarketRole = secondActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Producer, secondMarketRole.Function);
        }

        [Fact]
        public async Task GetOrganizationAsync_ReturnsOrganization()
        {
            // Arrange
            const string incomingJson = @"
                {
	                ""OrganizationId"": ""fb6665a1-b7be-4744-a8ce-08da0272c916"",
	                ""Name"": ""unit test"",
	                ""Actors"": [
		                {
			                ""ActorId"": ""8a46b5ac-4c7d-48c0-3f16-08da0279759b"",
			                ""ExternalActorId"": ""75ea715f-381e-46fd-831b-5b61b9db7862"",
			                ""Gln"": {
				                ""Value"": ""9656626091925""
			                },
			                ""Status"": ""Active"",
			                ""MarketRoles"": [
				                {
					                ""Function"": ""Consumer""
				                }
			                ]
		                }
                    ],
                    ""cvr"": ""87654321"",
                    ""address"": {
                        ""streetName"": ""Testvej"",
                        ""number"": ""2"",
                        ""zipCode"": ""4321"",
                        ""city"": ""Testby"",
                        ""country"": ""Testland""
                    }
                }";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith(incomingJson);

            // Act
            var actual = await target.GetOrganizationAsync(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916")).ConfigureAwait(false);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"), actual.OrganizationId);
            Assert.Equal("unit test", actual.Name);
            Assert.Equal(_validCvr, actual.Cvr);
            Assert.Equal(_validAddress.City, actual.Address.City);
            Assert.Equal(_validAddress.Country, actual.Address.Country);
            Assert.Equal(_validAddress.Number, actual.Address.Number);
            Assert.Equal(_validAddress.StreetName, actual.Address.StreetName);
            Assert.Equal(_validAddress.ZipCode, actual.Address.ZipCode);

            var actualActor = actual.Actors.Single();
            Assert.Equal(Guid.Parse("8a46b5ac-4c7d-48c0-3f16-08da0279759b"), actualActor.ActorId);
            Assert.Equal(Guid.Parse("75ea715f-381e-46fd-831b-5b61b9db7862"), actualActor.ExternalActorId);
            Assert.Equal("9656626091925", actualActor.Gln.Value);
            Assert.Equal(ActorStatus.Active, actualActor.Status);

            var actualMarketRole = actualActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Consumer, actualMarketRole.Function);
        }

        [Fact]
        public async Task CreateOrganizationAsync_ReturnsOrganizationId_CanReadBack()
        {
            // Arrange
            const string incomingOrgJson = @"
                {
	                ""OrganizationId"": ""fb6665a1-b7be-4744-a8ce-08da0272c916"",
	                ""Name"": ""unit test"",
	                ""Actors"": [
		                {
			                ""ActorId"": ""8a46b5ac-4c7d-48c0-3f16-08da0279759b"",
			                ""ExternalActorId"": ""75ea715f-381e-46fd-831b-5b61b9db7862"",
			                ""Gln"": {
				                ""Value"": ""9656626091925""
			                },
			                ""Status"": ""Active"",
			                ""MarketRoles"": [
				                {
					                ""Function"": ""Consumer""
				                }
			                ]
		                }
                    ],
                    ""cvr"": ""87654321"",
                    ""address"": {
                        ""streetName"": ""Testvej"",
                        ""number"": ""2"",
                        ""zipCode"": ""4321"",
                        ""city"": ""Testby"",
                        ""country"": ""Testland""
                    }
                }";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith("fb6665a1-b7be-4744-a8ce-08da0272c916");
            httpTest.RespondWith(incomingOrgJson);

            // Act
            var orgId = await target
                .CreateOrganizationAsync(new ChangeOrganizationDto("Created", _validCvr, _validAddress))
                .ConfigureAwait(false);

            var createdOrg = await target
                .GetOrganizationAsync(orgId.GetValueOrDefault())
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(orgId);
            Assert.Equal(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"), orgId);
            Assert.NotNull(createdOrg);
            Assert.Equal(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"), createdOrg.OrganizationId);
            Assert.Equal("unit test", createdOrg.Name);
            Assert.Equal(_validCvr, createdOrg.Cvr);
            Assert.Equal(_validAddress.City, createdOrg.Address.City);
            Assert.Equal(_validAddress.Country, createdOrg.Address.Country);
            Assert.Equal(_validAddress.Number, createdOrg.Address.Number);
            Assert.Equal(_validAddress.StreetName, createdOrg.Address.StreetName);
            Assert.Equal(_validAddress.ZipCode, createdOrg.Address.ZipCode);

            var actualActor = createdOrg.Actors.Single();
            Assert.Equal(Guid.Parse("8a46b5ac-4c7d-48c0-3f16-08da0279759b"), actualActor.ActorId);
            Assert.Equal(Guid.Parse("75ea715f-381e-46fd-831b-5b61b9db7862"), actualActor.ExternalActorId);
            Assert.Equal("9656626091925", actualActor.Gln.Value);
            Assert.Equal(ActorStatus.Active, actualActor.Status);

            var actualMarketRole = actualActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Consumer, actualMarketRole.Function);
        }

        [Fact]
        public async Task UpdateOrganizationAsync_CanReadBack()
        {
            // Arrange
            const string incomingOrgJson = @"
                {
	                ""OrganizationId"": ""fb6665a1-b7be-4744-a8ce-08da0272c916"",
	                ""Name"": ""unit test 2"",
	                ""Actors"": [
		                {
			                ""ActorId"": ""8a46b5ac-4c7d-48c0-3f16-08da0279759b"",
			                ""ExternalActorId"": ""75ea715f-381e-46fd-831b-5b61b9db7862"",
			                ""Gln"": {
				                ""Value"": ""9656626091925""
			                },
			                ""Status"": ""Active"",
			                ""MarketRoles"": [
				                {
					                ""Function"": ""Consumer""
				                }
			                ]
		                }
                    ],
                    ""cvr"": ""87654321"",
                    ""address"": {
                        ""streetName"": ""Testvej"",
                        ""number"": ""2"",
                        ""zipCode"": ""1234"",
                        ""city"": ""Testby"",
                        ""country"": ""Testland""
                    }
                }";
            var orgId = Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916");
            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith(string.Empty);
            httpTest.RespondWith(incomingOrgJson);

            var changedAddress = _validAddress with { ZipCode = "1234" };

            // Act
            await target
                .UpdateOrganizationAsync(orgId, new ChangeOrganizationDto("unit test 2", _validCvr, changedAddress))
                .ConfigureAwait(false);

            var changedOrg = await target
                .GetOrganizationAsync(orgId)
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(changedOrg);
            Assert.Equal(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"), changedOrg.OrganizationId);
            Assert.Equal("unit test 2", changedOrg.Name);
            Assert.Equal(_validCvr, changedOrg.Cvr);
            Assert.Equal(_validAddress.City, changedOrg.Address.City);
            Assert.Equal(_validAddress.Country, changedOrg.Address.Country);
            Assert.Equal(_validAddress.Number, changedOrg.Address.Number);
            Assert.Equal(_validAddress.StreetName, changedOrg.Address.StreetName);
            Assert.Equal(changedAddress.ZipCode, changedOrg.Address.ZipCode);

            var actualActor = changedOrg.Actors.Single();
            Assert.Equal(Guid.Parse("8a46b5ac-4c7d-48c0-3f16-08da0279759b"), actualActor.ActorId);
            Assert.Equal(Guid.Parse("75ea715f-381e-46fd-831b-5b61b9db7862"), actualActor.ExternalActorId);
            Assert.Equal("9656626091925", actualActor.Gln.Value);
            Assert.Equal(ActorStatus.Active, actualActor.Status);

            var actualMarketRole = actualActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Consumer, actualMarketRole.Function);
        }

        [Fact]
        public async Task GetActorAsync_ReturnsActor()
        {
            // Arrange
            const string incomingJson = @"
                {
                    ""actorId"": ""361fb10a-4204-46b6-bf9e-171ab2e61a59"",
                    ""externalActorId"": ""c7e6c6d1-a23a-464b-bebd-c1a9c5ceebe1"",
                    ""gln"": {
                        ""value"": ""123456""
                    },
                    ""status"": ""New"",
                    ""marketRoles"": []
                }";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith(incomingJson);

            // Act
            var actual = await target.GetActorAsync(
                Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"),
                Guid.Parse("361fb10a-4204-46b6-bf9e-171ab2e61a59"))
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(Guid.Parse("361fb10a-4204-46b6-bf9e-171ab2e61a59"), actual.ActorId);
            Assert.Equal(Guid.Parse("c7e6c6d1-a23a-464b-bebd-c1a9c5ceebe1"), actual.ExternalActorId);
            Assert.Equal("123456", actual.Gln.Value);
            Assert.Equal(ActorStatus.New, actual.Status);
            Assert.True(actual.MarketRoles.Count == 0);
        }

        [Fact]
        public async Task GetActorsAsync_ReturnsActors()
        {
            // Arrange
            const string incomingJson = @"
                [
                    {
                        ""actorId"": ""a03774de-e867-47a4-317d-08da0cc07910"",
                        ""externalActorId"": ""41df62f6-67dc-42de-9ab3-dfeb7d7a7aab"",
                        ""gln"": {
                          ""value"": ""5790000555550""
                        },
                        ""status"": ""New"",
                        ""marketRoles"": [
                          {
                            ""function"": ""MarketOperator""
                          }
                        ]
                      },
                    {
                        ""actorId"": ""02f0d6c9-96f5-4078-a754-fcc589b04937"",
                        ""externalActorId"": ""5a20e113-4d17-44ec-8014-4d032b700a73"",
                        ""gln"": {
                          ""value"": ""5790000701414""
                        },
                        ""status"": ""Active"",
                        ""marketRoles"": [
                          {
                            ""function"": ""EnergySupplier""
                          }
                        ]
                    }
                ]";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            httpTest.RespondWith(incomingJson);

            // Act
            var actual = await target.GetActorsAsync(Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916"))
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(actual);
            actual = actual.ToList();
            Assert.Equal(2, actual.Count());

            var firstActor = actual.First();
            Assert.Equal(Guid.Parse("a03774de-e867-47a4-317d-08da0cc07910"), firstActor.ActorId);
            Assert.Equal(Guid.Parse("41df62f6-67dc-42de-9ab3-dfeb7d7a7aab"), firstActor.ExternalActorId);
            Assert.Equal("5790000555550", firstActor.Gln.Value);
            Assert.Equal(ActorStatus.New, firstActor.Status);
            Assert.True(firstActor.MarketRoles.Count == 1);

            var firstMarketRole = firstActor.MarketRoles[0];
            Assert.Equal(EicFunction.MarketOperator, firstMarketRole.Function);

            var secondActor = actual.Skip(1).First();
            Assert.Equal(Guid.Parse("02f0d6c9-96f5-4078-a754-fcc589b04937"), secondActor.ActorId);
            Assert.Equal(Guid.Parse("5a20e113-4d17-44ec-8014-4d032b700a73"), secondActor.ExternalActorId);
            Assert.Equal("5790000701414", secondActor.Gln.Value);
            Assert.Equal(ActorStatus.Active, secondActor.Status);
            Assert.True(secondActor.MarketRoles.Count == 1);

            var secondMarketRole = secondActor.MarketRoles[0];
            Assert.Equal(EicFunction.EnergySupplier, secondMarketRole.Function);
        }

        [Fact]
        public async Task CreateActorAsync_CanReadBackActor()
        {
            // Arrange
            const string createdActorJson = @"
                {
                    ""actorId"": ""361fb10a-4204-46b6-bf9e-171ab2e61a59"",
                    ""externalActorId"": ""c7e6c6d1-a23a-464b-bebd-c1a9c5ceebe1"",
                    ""gln"": {
                        ""value"": ""123456""
                    },
                    ""status"": ""New"",
                    ""marketRoles"": [
                        {
                            ""function"": ""EnergySupplier""
                        }
                    ]
                }";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            var orgId = Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916");
            httpTest.RespondWith("361fb10a-4204-46b6-bf9e-171ab2e61a59");
            httpTest.RespondWith(createdActorJson);

            // Act
            var createdActorId = await target.CreateActorAsync(
                    orgId,
                    new CreateActorDto(new GlobalLocationNumberDto("123456"), Array.Empty<MarketRoleDto>()))
                .ConfigureAwait(false);

            var actual = await target
                .GetActorAsync(orgId, createdActorId.GetValueOrDefault())
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(Guid.Parse("361fb10a-4204-46b6-bf9e-171ab2e61a59"), actual.ActorId);
            Assert.Equal(Guid.Parse("c7e6c6d1-a23a-464b-bebd-c1a9c5ceebe1"), actual.ExternalActorId);
            Assert.Equal("123456", actual.Gln.Value);
            Assert.Equal(ActorStatus.New, actual.Status);
            Assert.True(actual.MarketRoles.Count == 1);

            var marketRole = actual.MarketRoles[0];
            Assert.Equal(EicFunction.EnergySupplier, marketRole.Function);
        }

        [Fact]
        public async Task UpdateActorAsync_CanReadBackActor()
        {
            // Arrange
            const string createdActorJson = @"
                {
                    ""actorId"": ""361fb10a-4204-46b6-bf9e-171ab2e61a59"",
                    ""externalActorId"": ""c7e6c6d1-a23a-464b-bebd-c1a9c5ceebe1"",
                    ""gln"": {
                        ""value"": ""1234567""
                    },
                    ""status"": ""Active"",
                    ""marketRoles"": [
                        {
                            ""function"": ""BillingAgent""
                        }
                    ]
                }";

            using var httpTest = new HttpTest();
            using var clientFactory = new PerBaseUrlFlurlClientFactory();
            var target = new MarketParticipantClient(clientFactory.Get("https://localhost"));
            var orgId = Guid.Parse("fb6665a1-b7be-4744-a8ce-08da0272c916");
            var actorId = Guid.Parse("361fb10a-4204-46b6-bf9e-171ab2e61a59");
            httpTest.RespondWith(createdActorJson);

            // Act
            await target.UpdateActorAsync(
                    orgId,
                    actorId,
                    new ChangeActorDto(
                        ActorStatus.Active.ToString(),
                        new[] { new MarketRoleDto(EicFunction.EnergySupplier) }))
                .ConfigureAwait(false);

            var actual = await target
                .GetActorAsync(orgId, actorId)
                .ConfigureAwait(false);

            // Assert
            Assert.NotNull(actual);
            Assert.Equal(Guid.Parse("361fb10a-4204-46b6-bf9e-171ab2e61a59"), actual.ActorId);
            Assert.Equal(Guid.Parse("c7e6c6d1-a23a-464b-bebd-c1a9c5ceebe1"), actual.ExternalActorId);
            Assert.Equal("1234567", actual.Gln.Value);
            Assert.Equal(ActorStatus.Active, actual.Status);
            Assert.True(actual.MarketRoles.Count == 1);

            var marketRole = actual.MarketRoles[0];
            Assert.Equal(EicFunction.BillingAgent, marketRole.Function);
        }
    }
}

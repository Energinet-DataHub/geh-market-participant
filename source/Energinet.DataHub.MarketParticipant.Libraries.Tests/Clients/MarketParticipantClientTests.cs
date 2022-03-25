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
using System.Net.Http;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Client;
using Energinet.DataHub.MarketParticipant.Client.Models;
using Energinet.DataHub.MarketParticipant.Libraries.Tests.Common;
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
{
    ""Organizations"": [
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
            ]
        }
    ]
}";
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

            var actualMarketRole = actualActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Consumer, actualMarketRole.Function);
        }

        [Fact]
        public async Task GetOrganizationsAsync_All_Returns2Organizations()
        {
            // Arrange
            const string incomingJson = @"
{
    ""Organizations"": [
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
            ]
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
            ]
        }
    ]
}";
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

            var secondActor = secondOrganization.Actors.Single();
            Assert.Equal(Guid.Parse("f6792b0b-7dee-4e70-b9d9-46b727e6748b"), secondActor.ActorId);
            Assert.Equal(Guid.Parse("dfef92e2-923e-43aa-8706-ac7445cddfb3"), secondActor.ExternalActorId);
            Assert.Equal("8574664796620", secondActor.Gln.Value);
            Assert.Equal(ActorStatus.New, secondActor.Status);

            var secondMarketRole = secondActor.MarketRoles.Single();
            Assert.Equal(EicFunction.Producer, secondMarketRole.Function);
        }
    }
}

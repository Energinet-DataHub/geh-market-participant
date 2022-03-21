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
            using var messageHandler = new MockedHttpMessageHandler(HttpStatusCode.Unauthorized);
            using var httpClient = messageHandler.CreateHttpClient();

            var target = new MarketParticipantClient(httpClient);

            // Act + Assert
            await Assert
                .ThrowsAsync<HttpRequestException>(() => target.GetOrganizationsAsync())
                .ConfigureAwait(false);
        }

        [Fact]
        public async Task GetOrganizationsAsync_All_ReturnsOrganizations()
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

            using var messageHandler = new MockedHttpMessageHandler(incomingJson);
            using var httpClient = messageHandler.CreateHttpClient();

            var target = new MarketParticipantClient(httpClient);

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
    }
}

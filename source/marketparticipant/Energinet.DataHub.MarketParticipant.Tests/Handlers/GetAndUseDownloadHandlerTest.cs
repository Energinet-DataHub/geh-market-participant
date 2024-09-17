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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Token;
using Energinet.DataHub.MarketParticipant.Application.Handlers.Token;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class GetAndUseDownloadHandlerTest
{
    [Fact]
    public async Task HandlerTestAsync()
    {
        // arrange
        var accessToken = "AccessToken";
        var repositoryMock = new Mock<IDownloadTokenRespository>();
        repositoryMock
            .Setup(x => x.GetAndUseDownloadTokenAsync(It.IsAny<Guid>()))
            .ReturnsAsync(accessToken);

        var target = new GetAndUseDownloadHandler(repositoryMock.Object);

        // act
        var actual = await target.Handle(new GetAndUseDownloadTokenCommand(Guid.NewGuid()), CancellationToken.None);

        // assert
        Assert.Equal(accessToken, actual.AccessToken);
    }
}

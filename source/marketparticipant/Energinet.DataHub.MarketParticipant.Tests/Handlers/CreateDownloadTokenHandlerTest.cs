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
using Energinet.DataHub.MarketParticipant.Application.Handlers.Token;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Moq;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Handlers;

[UnitTest]
public sealed class CreateDownloadTokenHandlerTest
{
    [Fact]
    public async Task HandleCommandCallsRepositoryAsync()
    {
        // arrange
        var newGuid = Guid.NewGuid();
        var repositoryMock = new Mock<IDownloadTokenRespository>();
        repositoryMock
            .Setup(x => x.CreateDownloadTokenAsync(It.IsAny<string>()))
            .ReturnsAsync(newGuid);

        var target = new CreateDownloadTokenHandler(repositoryMock.Object);

        // act
        var actual = await target.Handle(new Application.Commands.Token.CreateDownloadTokenCommand("accessToken"), CancellationToken.None);

        // assert
        Assert.Equal(newGuid, actual);
    }
}

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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;
using Energinet.DataHub.MarketParticipant.Application.Security;
using Moq;
using SimpleInjector;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Common;

internal static class FrontendUserHelper
{
    public static void MockFrontendUser(this Container? container, Guid frontendExternalUserId)
    {
        var frontendUser = new FrontendUser(
            frontendExternalUserId,
            Guid.NewGuid(),
            Guid.NewGuid(),
            false);

        var userContextMock = new Mock<IUserContext<FrontendUser>>();
        userContextMock
            .Setup(userContext => userContext.CurrentUser)
            .Returns(frontendUser);

        container?.Register(() => userContextMock.Object);
    }
}

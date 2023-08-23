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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services;

public class ActiveDirectoryB2BRolesProvider : IActiveDirectoryB2BRolesProvider
{
    private readonly GraphServiceClient _graphClient;
    private readonly string _appObjectId;
    private ActiveDirectoryB2BRoles? _roleCache;

    public ActiveDirectoryB2BRolesProvider(
        GraphServiceClient graphClient,
        string appObjectId)
    {
        _graphClient = graphClient;
        _appObjectId = appObjectId;
    }

    public async Task<ActiveDirectoryB2BRoles> GetB2BRolesAsync()
    {
        if (_roleCache is not null)
            return _roleCache;

        var roles = await GetRolesAsync().ConfigureAwait(false);
        var eicFunctions = Enum.GetNames(typeof(EicFunction));

        if (roles.EicRolesMapped.Count != eicFunctions.Length)
            throw new InvalidOperationException("Some EIC functions are missing their corresponding app role.");

        return _roleCache = roles;
    }

    private async Task<ActiveDirectoryB2BRoles> GetRolesAsync()
    {
        var roleApplicationRegistration = await _graphClient
            .Applications[_appObjectId]
            .GetAsync(request =>
            {
                request.QueryParameters.Select = new[]
                {
                    "displayName",
                    "appRoles"
                };
            })
            .ConfigureAwait(false);

        if (roleApplicationRegistration?.AppRoles is null)
        {
            throw new InvalidOperationException($"No application registration '{_appObjectId}' was found in Active Directory.");
        }

        var mapping = roleApplicationRegistration
            .AppRoles
            .ToDictionary(
                appRole => Enum.Parse<EicFunction>(appRole.Value!, true),
                appRole => appRole.Id!.Value);

        return new ActiveDirectoryB2BRoles(mapping);
    }
}

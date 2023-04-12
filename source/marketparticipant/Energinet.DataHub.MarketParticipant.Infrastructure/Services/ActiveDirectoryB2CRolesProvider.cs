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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;
using Microsoft.Graph;
using Microsoft.Graph.Models;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Services
{
    public class ActiveDirectoryB2CRolesProvider : IActiveDirectoryB2CRolesProvider
    {
        private readonly GraphServiceClient _graphClient;
        private readonly string _appObjectId;
        private readonly ActiveDirectoryB2CRoles _activeDirectoryB2CRoles;

        public ActiveDirectoryB2CRolesProvider(
            GraphServiceClient graphClient,
            string appObjectId)
        {
            _graphClient = graphClient;
            _appObjectId = appObjectId;
            _activeDirectoryB2CRoles = new ActiveDirectoryB2CRoles();
        }

        public async Task<ActiveDirectoryB2CRoles> GetB2CRolesAsync()
        {
            if (_activeDirectoryB2CRoles.IsLoaded)
            {
                return _activeDirectoryB2CRoles;
            }

            var application = await _graphClient.Applications[_appObjectId]
                .GetAsync(x =>
                {
                    x.QueryParameters.Select = new[]
                    {
                        "displayName",
                        "appRoles"
                    };
                })
                .ConfigureAwait(false);

            if (application is null)
            {
                throw new InvalidOperationException(
                    $"No application, '{nameof(application)}', was found in Active Directory.");
            }

            var lookup = ((EicFunction[])typeof(EicFunction).GetEnumValues()).ToDictionary(x => x.ToString().ToUpperInvariant());

            if (application.AppRoles is not null)
            {
                foreach (var appRole in application.AppRoles)
                {
                    if (!string.IsNullOrWhiteSpace(appRole.Value) && lookup.TryGetValue(appRole.Value.ToUpperInvariant(), out var val))
                    {
                        _activeDirectoryB2CRoles.EicRolesMapped.Add(val, appRole.Id!.Value);
                    }
                }
            }

            // Verify that all EIC functions has a corresponding app role
            if (_activeDirectoryB2CRoles.EicRolesMapped.Count != Enum.GetNames(typeof(EicFunction)).Length)
                throw new InvalidOperationException("Not all Eic Functions have an AppRole defined");

            return _activeDirectoryB2CRoles;
        }
    }
}

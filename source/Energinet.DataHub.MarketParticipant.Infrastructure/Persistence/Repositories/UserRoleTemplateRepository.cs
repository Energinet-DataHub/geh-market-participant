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

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;
using Microsoft.EntityFrameworkCore;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories;

public sealed class UserRoleTemplateRepository : IUserRoleTemplateRepository
{
    private readonly IMarketParticipantDbContext _marketParticipantDbContext;

    public UserRoleTemplateRepository(IMarketParticipantDbContext marketParticipantDbContext)
    {
        _marketParticipantDbContext = marketParticipantDbContext;
    }

    public async Task<UserRoleTemplate?> GetAsync(UserRoleTemplateId userRoleTemplateId)
    {
        var userRoleTemplate = await BuildUserRoleQuery()
            .SingleOrDefaultAsync(t => t.Id == userRoleTemplateId.Value)
            .ConfigureAwait(false);

        return userRoleTemplate == null
            ? null
            : MapUserRoleTemplate(userRoleTemplate);
    }

    public async Task<IEnumerable<UserRoleTemplate>> GetAsync(IEnumerable<EicFunction> eicFunctions)
    {
        var userRoleTemplates = await BuildUserRoleQuery()
            .Where(t => t
                .EicFunctions
                .Select(f => f.EicFunction)
                .All(f => eicFunctions.Contains(f)))
            .ToListAsync()
            .ConfigureAwait(false);

        return userRoleTemplates.Select(MapUserRoleTemplate);
    }

    private static UserRoleTemplate MapUserRoleTemplate(UserRoleTemplateEntity userRoleTemplate)
    {
        return new UserRoleTemplate(
            new UserRoleTemplateId(userRoleTemplate.Id),
            userRoleTemplate.Name,
            userRoleTemplate.EicFunctions.Select(f => f.EicFunction),
            userRoleTemplate.Permissions.Select(p => p.Permission));
    }

    private IQueryable<UserRoleTemplateEntity> BuildUserRoleQuery()
    {
        return _marketParticipantDbContext
            .UserRoleTemplates
            .Include(x => x.EicFunctions)
            .Include(x => x.Permissions);
    }
}

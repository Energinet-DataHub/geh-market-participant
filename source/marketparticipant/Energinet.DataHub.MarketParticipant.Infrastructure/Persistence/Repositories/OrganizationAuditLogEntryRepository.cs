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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Permissions;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Model;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Persistence.Repositories
{
    public sealed class OrganizationAuditLogEntryRepository : IOrganizationAuditLogEntryRepository
    {
        private readonly IMarketParticipantDbContext _context;

        public OrganizationAuditLogEntryRepository(IMarketParticipantDbContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<OrganizationAuditLogEntry>> GetAsync(OrganizationId organization)
        {
            var historicEntities = await _context.Organizations
                .ReadAllHistoryForAsync(entity => entity.Id == organization.Value)
                .ConfigureAwait(false);

            var auditedProperties = new[]
            {
                new
                {
                    Property = OrganizationChangeType.Name,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Name)
                },
                new
                {
                    Property = OrganizationChangeType.DomainChange,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Domain)
                },
                new
                {
                    Property = OrganizationChangeType.BusinessRegisterIdentifier,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.BusinessRegisterIdentifier)
                },
                new
                {
                    Property = OrganizationChangeType.AddressCountry,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Address?.Country ?? string.Empty)
                },
                new
                {
                    Property = OrganizationChangeType.AddressCity,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Address?.City ?? string.Empty)
                },
                new
                {
                    Property = OrganizationChangeType.AddressNumber,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Address?.Number ?? string.Empty)
                },
                new
                {
                    Property = OrganizationChangeType.AddressStreetName,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Address?.StreetName ?? string.Empty)
                },
                new
                {
                    Property = OrganizationChangeType.AddressZipCode,
                    ReadValue = new Func<OrganizationEntity, object?>(entity => entity.Address?.ZipCode ?? string.Empty)
                },
            };

            var auditEntries = new List<OrganizationAuditLogEntry>();

            for (var i = 0; i < historicEntities.Count; i++)
            {
                var isFirst = i == 0;
                var current = historicEntities[i];
                var previous = isFirst ? current : historicEntities[i - 1];

                foreach (var auditedProperty in auditedProperties)
                {
                    var currentValue = auditedProperty.ReadValue(current.Entity);
                    var previousValue = auditedProperty.ReadValue(previous.Entity);

                    if (!Equals(currentValue, previousValue) || isFirst)
                    {
                        auditEntries.Add(new OrganizationAuditLogEntry(
                            organization,
                            new AuditIdentity(current.Entity.ChangedByIdentityId),
                            auditedProperty.Property,
                            current.PeriodStart,
                            currentValue?.ToString() ?? string.Empty));
                    }
                }
            }

            return auditEntries;
        }
    }
}

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
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;

namespace Energinet.DataHub.MarketParticipant.Domain.Services
{
    public sealed class GridAreaFactoryService : IGridAreaFactoryService
    {
        private readonly IGridAreaLinkRepository _gridAreaLinkRepository;
        private readonly IGridAreaRepository _gridAreaRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;

        public GridAreaFactoryService(
            IGridAreaLinkRepository gridAreaLinkRepository,
            IGridAreaRepository gridAreaRepository,
            IUnitOfWorkProvider unitOfWorkProvider)
        {
            _gridAreaLinkRepository = gridAreaLinkRepository;
            _gridAreaRepository = gridAreaRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
        }

        public async Task<GridArea> CreateAsync(GridAreaCode code, GridAreaName name, PriceAreaCode priceAreaCode, DateTimeOffset validFrom, DateTimeOffset? validTo)
        {
            ArgumentNullException.ThrowIfNull(code, nameof(code));
            ArgumentNullException.ThrowIfNull(name, nameof(name));
            ArgumentNullException.ThrowIfNull(priceAreaCode, nameof(priceAreaCode));

            var newGridArea = new GridArea(
                name,
                code,
                priceAreaCode,
                validFrom,
                validTo);

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            var savedGridArea = await SaveGridAreaAsync(newGridArea).ConfigureAwait(false);

            var newGridAreaLink = new GridAreaLink(savedGridArea.Id);

            await SaveGridAreaLinkAsync(newGridAreaLink).ConfigureAwait(false);

            await uow.CommitAsync().ConfigureAwait(false);

            return savedGridArea;
        }

        private async Task<GridArea> SaveGridAreaAsync(GridArea gridArea)
        {
            var orgId = await _gridAreaRepository
                .AddOrUpdateAsync(gridArea)
                .ConfigureAwait(false);

            var savedGridArea = await _gridAreaRepository
                .GetAsync(orgId)
                .ConfigureAwait(false);

            return savedGridArea!;
        }

        private async Task SaveGridAreaLinkAsync(GridAreaLink gridAreaLink)
        {
            await _gridAreaLinkRepository
                .AddOrUpdateAsync(gridAreaLink)
                .ConfigureAwait(false);
        }
    }
}

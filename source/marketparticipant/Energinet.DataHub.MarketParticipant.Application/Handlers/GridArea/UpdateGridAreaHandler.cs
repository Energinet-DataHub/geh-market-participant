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
using Energinet.DataHub.MarketParticipant.Application.Commands.GridArea;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.GridArea
{
    public sealed class UpdateGridAreaHandler : IRequestHandler<UpdateGridAreaCommand>
    {
        private readonly IGridAreaRepository _gridAreaRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;

        public UpdateGridAreaHandler(IGridAreaRepository gridAreaRepository, IUnitOfWorkProvider unitOfWorkProvider)
        {
            _gridAreaRepository = gridAreaRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
        }

        public async Task Handle(UpdateGridAreaCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                // Get from db
                var gridArea = await _gridAreaRepository.GetAsync(new GridAreaId(request.Id)).ConfigureAwait(false);
                NotFoundValidationException.ThrowIfNull(gridArea, request.Id);

                // Event should be sent
                var nameChanged = gridArea.Name.Value != request.GridAreaDto.Name;

                // update and send events
                if (nameChanged)
                {
                    var updatedGridArea = new Domain.Model.GridArea(
                        gridArea.Id,
                        new GridAreaName(request.GridAreaDto.Name),
                        gridArea.Code,
                        gridArea.PriceAreaCode,
                        gridArea.ValidFrom,
                        gridArea.ValidTo);

                    await _gridAreaRepository.AddOrUpdateAsync(updatedGridArea).ConfigureAwait(false);
                }

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }
    }
}

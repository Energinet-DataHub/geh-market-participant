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
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.AdditionalRecipients;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories.Query;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.AdditionalRecipients;

public sealed class GetAdditionalRecipientsOfMeteringPointHandler : IRequestHandler<GetAdditionalRecipientsOfMeteringPointCommand, GetAdditionalRecipientsOfMeteringPointResponse>
{
    private readonly IAdditionalRecipientQueryRepository _additionalRecipientQueryRepository;

    public GetAdditionalRecipientsOfMeteringPointHandler(IAdditionalRecipientQueryRepository additionalRecipientQueryRepository)
    {
        _additionalRecipientQueryRepository = additionalRecipientQueryRepository;
    }

    public async Task<GetAdditionalRecipientsOfMeteringPointResponse> Handle(GetAdditionalRecipientsOfMeteringPointCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var additionalRecipients = await _additionalRecipientQueryRepository
            .GetAsync(new MeteringPointIdentification(request.MeteringPointIdentification))
            .ConfigureAwait(false);

        // TODO: For next PR, ar.MarketRole should be mapped to nuget package.
        var recipients = additionalRecipients.Select(ar =>
            new AdditionalRecipientDto(ar.ActorNumber.Value, ar.MarketRole.ToString()));

        return new GetAdditionalRecipientsOfMeteringPointResponse(recipients);
    }
}

﻿// Copyright 2020 Energinet DataHub A/S
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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actors;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actors;

public sealed class GetActorCredentialsHandler : IRequestHandler<GetActorCredentialsCommand, GetActorCredentialsResponse?>
{
    private readonly IActorRepository _actorRepository;

    public GetActorCredentialsHandler(IActorRepository actorRepository)
    {
        _actorRepository = actorRepository;
    }

    public async Task<GetActorCredentialsResponse?> Handle(GetActorCredentialsCommand request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var actor = await _actorRepository
            .GetAsync(new ActorId(request.ActorId))
            .ConfigureAwait(false);

        NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

        return actor.Credentials switch
        {
            null => null,
            ActorCertificateCredentials actorCertificateCredentials =>
                new GetActorCredentialsResponse(
                    new ActorCredentialsDto(
                        new ActorCertificateCredentialsDto(
                            actorCertificateCredentials.CertificateThumbprint,
                            actorCertificateCredentials.ExpirationDate.ToDateTimeOffset()),
                        null)),
            ActorClientSecretCredentials actorClientSecretCredentials =>
                new GetActorCredentialsResponse(
                    new ActorCredentialsDto(
                        null,
                        new ActorClientSecretCredentialsDto(
                            actorClientSecretCredentials.ClientId,
                            actorClientSecretCredentials.ExpirationDate.ToDateTimeOffset()))),
            _ => throw new InvalidOperationException($"Unknown credentials type: {actor.Credentials.GetType()}")
        };
    }
}

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
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Mappers;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using Energinet.DataHub.MarketParticipant.Domain.Services;
using Energinet.DataHub.MarketParticipant.Domain.Services.ActiveDirectory;
using FluentValidation;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor
{
    public sealed class ActorRequestSecretHandler : IRequestHandler<ActorRequestSecretCommand, ActorRequestSecretResponse>
    {
        private readonly IActiveDirectoryB2CService _activeDirectoryB2CService;
        private readonly IActorRepository _actorRepository;
        private readonly ICertificateService _certificateService;

        public ActorRequestSecretHandler(
            IActiveDirectoryB2CService activeDirectoryB2CService,
            IActorRepository actorRepository,
            ICertificateService certificateService)
        {
            _activeDirectoryB2CService = activeDirectoryB2CService;
            _actorRepository = actorRepository;
            _certificateService = certificateService;
        }

        public async Task<ActorRequestSecretResponse> Handle(ActorRequestSecretCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request);

            var actor = await _actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

            if (actor.ExternalActorId is null)
                throw new ValidationException("Can't request secret to actor which doesn't have an external id");

            switch (actor.Credentials)
            {
                case ActorCertificateCredentials certificateCredentials:
                    await _certificateService
                        .RemoveCertificateAsync(certificateCredentials.KeyVaultSecretIdentifier)
                        .ConfigureAwait(false);
                    break;
                case ActorClientSecretCredentials clientSecretCredentials:
                    await _activeDirectoryB2CService
                        .RemoveSecretsForAppRegistrationAsync(actor.ExternalActorId)
                        .ConfigureAwait(false);
                    break;
                default:
                    throw new InvalidOperationException("Unknown credentials type");
            }

            actor.Credentials = null;
            await _actorRepository.AddOrUpdateAsync(actor).ConfigureAwait(false);

            var secretForApp = await _activeDirectoryB2CService
                .CreateSecretForAppRegistrationAsync(actor.ExternalActorId)
                .ConfigureAwait(false);
            actor.Credentials = new ActorClientSecretCredentials(secretForApp.SecretId, secretForApp.ExpirationDate);
            await _actorRepository.AddOrUpdateAsync(actor).ConfigureAwait(false);

            return new ActorRequestSecretResponse(secretForApp.SecretText);
        }
    }
}

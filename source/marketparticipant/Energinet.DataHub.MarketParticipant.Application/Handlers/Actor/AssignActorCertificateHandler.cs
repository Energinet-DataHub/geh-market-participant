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
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Application.Commands.Actor;
using Energinet.DataHub.MarketParticipant.Application.Services;
using Energinet.DataHub.MarketParticipant.Domain;
using Energinet.DataHub.MarketParticipant.Domain.Exception;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Repositories;
using MediatR;

namespace Energinet.DataHub.MarketParticipant.Application.Handlers.Actor
{
    public sealed class AssignActorCertificateHandler : IRequestHandler<AssignActorCertificateCommand>
    {
        private readonly IActorRepository _actorRepository;
        private readonly IUnitOfWorkProvider _unitOfWorkProvider;
        private readonly IDomainEventRepository _domainEventRepository;
        private readonly ICertificateService _certificateService;

        public AssignActorCertificateHandler(
            IActorRepository actorRepository,
            IUnitOfWorkProvider unitOfWorkProvider,
            IDomainEventRepository domainEventRepository,
            ICertificateService certificateService)
        {
            _actorRepository = actorRepository;
            _unitOfWorkProvider = unitOfWorkProvider;
            _domainEventRepository = domainEventRepository;
            _certificateService = certificateService;
        }

        public async Task Handle(AssignActorCertificateCommand request, CancellationToken cancellationToken)
        {
            ArgumentNullException.ThrowIfNull(request, nameof(request));

            // Find actor
            var actor = await _actorRepository
                .GetAsync(new ActorId(request.ActorId))
                .ConfigureAwait(false);

            NotFoundValidationException.ThrowIfNull(actor, request.ActorId);

            // Certificate should not exist for actor

            // Add certificate to keyvault, here ??
            await AddToKeyVaultAsync(actor.Name.Value, request.Certificate).ConfigureAwait(false);

            // Add certificate to actor Uow
            var uow = await _unitOfWorkProvider
                .NewUnitOfWorkAsync()
                .ConfigureAwait(false);

            await using (uow.ConfigureAwait(false))
            {
                await _actorRepository
                    .AssignCertificate(actor)
                    // with certificate event added,
                    // Add domain event (ActorCertificateAssigned) only if actor is active
                    .ConfigureAwait(false);

                await _domainEventRepository
                    .EnqueueAsync(actor)
                    .ConfigureAwait(false);

                await uow.CommitAsync().ConfigureAwait(false);
            }
        }

        private async Task AddToKeyVaultAsync(string certificateName, Stream certificate)
        {
            ArgumentNullException.ThrowIfNull(certificateName);

            using var reader = new BinaryReader(certificate);
            var certificateBytes = reader.ReadBytes((int)certificate.Length);
            await _certificateService.AddCertificateToKeyVaultAsync("s", certificateBytes).ConfigureAwait(false);
        }
    }
}

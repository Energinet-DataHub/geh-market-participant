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
using System.ComponentModel.DataAnnotations;
using Energinet.DataHub.MarketParticipant.Domain.Exception;

namespace Energinet.DataHub.MarketParticipant.Domain.Repositories;

public static class ActorErrorHandler
{
    public static System.Exception HandleActorError(ActorError source) => source switch
    {
        ActorError.ThumbprintCredentialsConflict => new ValidationException("An actor with the same certificate thumbprint already exists.")
            .WithErrorCode("actor.credentials.thumbprint_reserved"),
        _ => throw new ArgumentOutOfRangeException(nameof(source))
    };
}

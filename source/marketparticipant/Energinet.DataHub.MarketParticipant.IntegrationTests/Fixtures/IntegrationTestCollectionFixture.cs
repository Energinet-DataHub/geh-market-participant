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

using Xunit;

namespace Energinet.DataHub.MarketParticipant.IntegrationTests.Fixtures;

[CollectionDefinition(nameof(IntegrationTestCollectionFixture))]
public sealed class IntegrationTestCollectionFixture :
    ICollectionFixture<MarketParticipantDatabaseFixture>,
    ICollectionFixture<GraphServiceClientFixture>,
    ICollectionFixture<B2CFixture>,
    ICollectionFixture<CertificateFixture>,
    ICollectionFixture<ActorClientSecretFixture>,
    ICollectionFixture<AuthorizationDatabaseFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}

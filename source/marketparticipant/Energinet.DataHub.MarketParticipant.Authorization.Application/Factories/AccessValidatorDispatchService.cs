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

using Energinet.DataHub.MarketParticipant.Authorization.Application.Authorization.AccessValidators;
using Energinet.DataHub.MarketParticipant.Authorization.Model.AccessValidationRequests;
using Microsoft.Extensions.DependencyInjection;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Factories;

public class AccessValidatorDispatchService : IAccessValidatorDispatchService
{
    private readonly IServiceProvider _provider;

    public AccessValidatorDispatchService(IServiceProvider provider)
    {
        _provider = provider;
    }

    public Task<bool> ValidateAsync(AccessValidationRequest request)
    {
       return CreateCoreAsync((dynamic)request);
    }

    private Task<bool> CreateCoreAsync<T>(T request)
        where T : AccessValidationRequest
    {
       var validator = _provider.GetRequiredService<IAccessValidator<T>>();
       return validator.ValidateAsync(request);
    }
}

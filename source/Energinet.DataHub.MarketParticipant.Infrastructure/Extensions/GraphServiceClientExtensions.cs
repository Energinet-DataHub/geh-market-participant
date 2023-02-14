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
using System.Net;
using Microsoft.Graph;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;

public static class GraphServiceClientExtensions
{
    public static TRequest WithRetryOnNotFound<TRequest>(this TRequest request)
        where TRequest : IBaseRequest
    {
        ArgumentNullException.ThrowIfNull(request);
        return request.WithShouldRetry((_, _, message) => message.StatusCode == HttpStatusCode.NotFound);
    }
}

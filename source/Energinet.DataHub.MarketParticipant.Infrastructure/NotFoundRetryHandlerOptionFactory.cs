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

using System.Net;
using Microsoft.Kiota.Http.HttpClientLibrary.Middleware.Options;

namespace Energinet.DataHub.MarketParticipant.Infrastructure;

public static class NotFoundRetryHandlerOptionFactory
{
    public static RetryHandlerOption CreateNotFoundRetryHandlerOption()
    {
        return new RetryHandlerOption
        {
            ShouldRetry = (_, _, response) => response.StatusCode == HttpStatusCode.NotFound
        };
    }
}

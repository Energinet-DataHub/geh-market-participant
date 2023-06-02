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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Energinet.DataHub.MarketParticipant.Domain.Model.Users.Authentication;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Graph;
using Microsoft.Graph.Models;
using Microsoft.Graph.Models.ODataErrors;
using Microsoft.Graph.Users.Item.Authentication;
using Microsoft.Kiota.Abstractions;

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory;

public sealed class ExternalSmsAuthenticationMethod : IExternalAuthenticationMethod
{
    private readonly SmsAuthenticationMethod _smsAuthenticationMethod;

    public ExternalSmsAuthenticationMethod(SmsAuthenticationMethod smsAuthenticationMethod)
    {
        _smsAuthenticationMethod = smsAuthenticationMethod;
    }

    public Task AssignAsync(AuthenticationRequestBuilder authenticationBuilder)
    {
        ArgumentNullException.ThrowIfNull(authenticationBuilder);

        return authenticationBuilder
            .PhoneMethods
            .PostAsync(
                new PhoneAuthenticationMethod
                {
                    PhoneNumber = _smsAuthenticationMethod.PhoneNumber.Number,
                    PhoneType = AuthenticationPhoneType.Mobile
                },
                configuration => configuration.Options = new List<IRequestOption>
                {
                    NotFoundRetryHandlerOptionFactory.CreateNotFoundRetryHandlerOption()
                });
    }

    public async Task<bool> DoesAlreadyExistAsync(IBaseClient client, AuthenticationRequestBuilder authenticationBuilder)
    {
        ArgumentNullException.ThrowIfNull(authenticationBuilder);

        var collection = await authenticationBuilder
            .PhoneMethods
            .GetAsync(configuration => configuration.Options = new List<IRequestOption>
            {
                NotFoundRetryHandlerOptionFactory.CreateNotFoundRetryHandlerOption()
            })
            .ConfigureAwait(false);

        var phoneMethods = await collection!
            .IteratePagesAsync<PhoneAuthenticationMethod>(client)
            .ConfigureAwait(false);

        return phoneMethods
            .Any(method =>
                method.PhoneType == AuthenticationPhoneType.Mobile &&
                method.PhoneNumber == _smsAuthenticationMethod.PhoneNumber.Number);
    }

    public void EnsureNoValidationException(Exception exception)
    {
        if (exception is ODataError { Error: { Code: "invalidPhoneNumber", Message: { } } } error)
        {
            throw new ValidationException(
                error.Error.Message,
                new[]
                {
                    new ValidationFailure("phoneNumber", error.Error.Message)
                    {
                        ErrorCode = "invalidPhoneNumber"
                    }
                });
        }
    }
}

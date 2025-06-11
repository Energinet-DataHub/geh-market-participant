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
using Energinet.DataHub.Core.App.Common.Abstractions.Users;

namespace Energinet.DataHub.MarketParticipant.Application.Security;

/// <summary>
/// An IUserContext 'FrontendUser' should always be registered so that all dependencies are valid and can be validated.
/// If frontend users are not to be used, register <see cref="ForbiddenUser"/>.
/// This will satisfy the dependencies, but throw an exception is something tries to access the current user.
/// </summary>
public sealed class ForbiddenUser : IUserContext<FrontendUser>
{
    public FrontendUser CurrentUser => throw new InvalidOperationException("A component that does not support frontend users tried to access CurrentUser.");
}

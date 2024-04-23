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
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Infrastructure.Extensions;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Infrastructure.Extensions;

[UnitTest]
public class EicFunctionExtensionsTests
{
    [Fact]
    public void MapToContract_AllEicFunctions_MustBeMapped()
    {
        // arrange
        var eicFunctions = Enum.GetValues<EicFunction>();

        // act, assert
        foreach (var eicFunction in eicFunctions)
        {
            eicFunction.MapToContract();
        }

        Assert.Equal(eicFunctions.Length, Enum.GetValues<MarketParticipant.Infrastructure.Model.Contracts.EicFunction>().Length - 1);
    }
}

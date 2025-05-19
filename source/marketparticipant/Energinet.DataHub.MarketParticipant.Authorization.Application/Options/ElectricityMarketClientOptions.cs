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
using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.MarketParticipant.Authorization.Application.Options;
public sealed class ElectricityMarketClientOptions
{
    public const string SectionName = "ElectricityMarketClientOptions";

    [Required]
    public Uri BaseUrl { get; set; } = null!;

    [Required(AllowEmptyStrings = false)]
#pragma warning disable CA1056
    public string ApplicationIdUri { get; set; } = string.Empty!;
#pragma warning restore CA1056
}

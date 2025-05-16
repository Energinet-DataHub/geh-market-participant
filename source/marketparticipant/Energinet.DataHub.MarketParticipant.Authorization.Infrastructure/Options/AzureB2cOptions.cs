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

using System.ComponentModel.DataAnnotations;

namespace Energinet.DataHub.MarketParticipant.Authorization.Infrastructure.Options;

public sealed record AzureB2COptions
{
    public const string SectionName = "AzureB2c";

    [Required]
    public string Tenant { get; set; } = null!;

    [Required]
    public string SpnId { get; set; } = null!;

    [Required]
    public string SpnSecret { get; set; } = null!;

    [Required]
    public string BackendObjectId { get; set; } = null!;

    [Required]
    public string BackendSpnObjectId { get; set; } = null!;

    [Required]
    public string BackendId { get; set; } = null!;
}

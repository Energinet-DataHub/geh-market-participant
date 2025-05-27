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

using System.Text.RegularExpressions;

namespace Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Helpers;

internal static class NamingConvention
{
    // Matches                                                         {type} {timestamp } {name}
    // Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Scripts.Model.202103021434 First.sql
    public static readonly Regex Regex = new Regex(@".*Scripts\.(?<environment>TEST_001|TEST_002|PREPROD_001|PREPROD_002|PROD_001|SANDBOX_002|DEV_001|DEV_002|DEV_003|LocalDB|LocalDev|ALL)(\.(?<type>Model|Seed|Test))?\.(?<timestamp>\d{12}) (?<name>\D*).sql");
}

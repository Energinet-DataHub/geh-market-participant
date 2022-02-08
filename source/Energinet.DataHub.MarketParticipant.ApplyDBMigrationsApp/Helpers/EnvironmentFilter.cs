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

namespace Energinet.DataHub.MarketParticipant.ApplyDBMigrationsApp.Helpers
{
    public static class EnvironmentFilter
    {
        private static readonly string[] _validEnvironments = { "u-001", "u-002", "t-001", "b-001", "b-002", "p-001"};

        public static Func<string, bool> GetFilter(string[] args)
        {
            var environment = GetEnvironmentArgument(args).Replace("-", "_").ToUpper();
            if (string.IsNullOrEmpty(environment))
            {
                return file => file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
                               ((file.Contains(".Scripts.Seed.", StringComparison.OrdinalIgnoreCase) && args.Contains("includeSeedData")) ||
                                (file.Contains(".Scripts.Test.", StringComparison.OrdinalIgnoreCase) && args.Contains("includeTestData")) ||
                                file.Contains(".Scripts.Model.", StringComparison.OrdinalIgnoreCase));
            }

            return file => file.EndsWith(".sql", StringComparison.OrdinalIgnoreCase) &&
                           ((file.Contains($".Scripts.{environment}.Seed.", StringComparison.OrdinalIgnoreCase) && args.Contains("includeSeedData")) ||
                            (file.Contains($".Scripts.{environment}.Test.", StringComparison.OrdinalIgnoreCase) && args.Contains("includeTestData")) ||
                            file.Contains($".Scripts.{environment}.Model.", StringComparison.OrdinalIgnoreCase));
        }

        private static string GetEnvironmentArgument(IReadOnlyList<string> args)
        {
            return args.Count > 1 && _validEnvironments.Contains(args[1].ToLower())
                ? args[1]
                : string.Empty;
        }
    }
}

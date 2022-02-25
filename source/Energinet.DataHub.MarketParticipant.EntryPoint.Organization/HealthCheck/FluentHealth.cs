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
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Energinet.DataHub.MarketParticipant.EntryPoint.Organization.HealthCheck
{
    internal sealed class FluentHealth : IFluentHealth
    {
        private readonly ISqlDatabaseVerifier _sqlDatabaseVerifier;
        private readonly IServiceBusQueueVerifier _serviceBusVerifier;
        private readonly IDictionary<string, (int Order, Func<Task<bool>> Verifier)> _verifications = new Dictionary<string, (int Order, Func<Task<bool>> Verifier)>();

        public FluentHealth(
            ISqlDatabaseVerifier sqlDatabaseVerifier,
            IServiceBusQueueVerifier serviceBusVerifier)
        {
            _sqlDatabaseVerifier = sqlDatabaseVerifier;
            _serviceBusVerifier = serviceBusVerifier;
        }

        public IFluentHealth AddSqlDatabase(string verficationKey, string connectionString)
        {
            Verify(verficationKey, () => _sqlDatabaseVerifier.VerifyAsync(connectionString));
            return this;
        }

        public IFluentHealth AddMessageBus(string verficationKey, string connectionString, string name)
        {
            Verify(verficationKey, () => _serviceBusVerifier.VerifyAsync(connectionString, name));
            return this;
        }

        public async Task<IReadOnlyCollection<(string Key, bool Result)>> RunAsync()
        {
            var results = new Dictionary<string, (int Order, bool Result)>();

            foreach (var verification in _verifications)
            {
                await RunVerifierAsync(results, verification.Key, verification.Value).ConfigureAwait(false);
            }

            return MapResults(results);
        }

        public async Task<IReadOnlyCollection<(string Key, bool Result)>> RunInParallelAsync()
        {
            var results = new ConcurrentDictionary<string, (int Order, bool Result)>();

            await Task.WhenAll(_verifications.Select(x => RunVerifierAsync(results, x.Key, x.Value))).ConfigureAwait(false);

            return MapResults(results);
        }

        private static IReadOnlyCollection<(string Key, bool Result)> MapResults(IDictionary<string, (int Order, bool Result)> results)
        {
            return results
                .OrderBy(x => x.Value.Order)
                .Select(x => (x.Key, x.Value.Result))
                .ToList();
        }

        private static async Task RunVerifierAsync(IDictionary<string, (int Order, bool Result)> results, string key, (int Order, Func<Task<bool>> Verifier) value)
        {
            var (order, verifier) = value;
            var result = await verifier().ConfigureAwait(false);
            results.TryAdd(key, (order, result));
        }

        private void Verify(string verficationKey, Func<Task<bool>> verifier)
        {
            var order = _verifications.Count;
            _verifications.Add(verficationKey, (order, verifier));
        }
    }
}

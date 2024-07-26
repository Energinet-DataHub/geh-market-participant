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

namespace Energinet.DataHub.MarketParticipant.Domain.Model;

public sealed class RevisionActivity
{
    private RevisionActivity(string activityName, string entityKey, string entityType)
    {
        ActivityName = activityName;
        EntityKey = entityKey;
        EntityType = entityType;
    }

    public string ActivityName { get; }
    public string EntityKey { get; }
    public string EntityType { get; }

    public static RevisionActivity Create<TModel, TKey>(TModel model, string activity, TKey key)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(activity);
        ArgumentNullException.ThrowIfNull(key);

        return new RevisionActivity(activity, $"{key}", typeof(TModel).Name);
    }
}

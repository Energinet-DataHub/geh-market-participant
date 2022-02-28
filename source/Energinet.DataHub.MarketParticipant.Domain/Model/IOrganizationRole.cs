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

namespace Energinet.DataHub.MarketParticipant.Domain.Model
{
    /// <summary>
    ///     Represents one of the role of an organization.
    /// </summary>
    public interface IOrganizationRole
    {
        /// <summary>
        ///     The id of the organization role.
        /// </summary>
        public Guid Id { get; }

        /// <summary>
        ///     The ebIX business role code for the current role.
        /// </summary>
        BusinessRoleCode Code { get; }

        /// <summary>
        ///     The status of the current role.
        /// </summary>
        RoleStatus Status { get; }

        /// <summary>
        /// The list of market roles (functions and permissions) supported by the current organization role.
        /// </summary>
        public ICollection<MarketRole> MarketRoles { get; }

        /// <summary>
        ///     The status of the current role.
        /// </summary>
        public ICollection<MeteringPointType> MeteringPointTypes { get; }

        /// <summary>
        ///     Activates the current role, the status changes to Active.
        ///     Only New and Inactive roles can be activated.
        /// </summary>
        void Activate();

        /// <summary>
        ///     Deactives the current role, the status changes to Inactive.
        ///     Only Active roles can be deactivated.
        /// </summary>
        void Deactivate();

        /// <summary>
        ///     Soft-deletes the current role, the status changes to Deleted.
        ///     The role becomes read-only after deletion.
        /// </summary>
        void Delete();
    }
}

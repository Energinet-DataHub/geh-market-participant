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
using System.Collections.ObjectModel;
using Energinet.DataHub.MarketParticipant.Domain.Model;
using Energinet.DataHub.MarketParticipant.Domain.Model.Roles;
using Xunit;
using Xunit.Categories;

namespace Energinet.DataHub.MarketParticipant.Tests.Model
{
    [UnitTest]
    public sealed class OrganizationRoleBaseTests
    {
        [Fact]
        public void Ctor_NewRole_HasStatusNew()
        {
            // Arrange + Act + Assert
            Assert.Equal(RoleStatus.New, new OrganizationRoleBaseTest().Status);
        }

        [Theory]
        [InlineData(RoleStatus.New, true)]
        [InlineData(RoleStatus.Active, true)]
        [InlineData(RoleStatus.Inactive, true)]
        [InlineData(RoleStatus.Deleted, false)]
        public void Activate_ChangesState_IfAllowed(RoleStatus initialStatus, bool isAllowed)
        {
            // Arrange
            var target = new OrganizationRoleBaseTest(initialStatus);

            // Act + Assert
            if (isAllowed)
            {
                target.Activate();
                Assert.Equal(RoleStatus.Active, target.Status);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => target.Activate());
            }
        }

        [Theory]
        [InlineData(RoleStatus.New, false)]
        [InlineData(RoleStatus.Active, true)]
        [InlineData(RoleStatus.Inactive, true)]
        [InlineData(RoleStatus.Deleted, false)]
        public void Deactivate_ChangesState_IfAllowed(RoleStatus initialStatus, bool isAllowed)
        {
            // Arrange
            var target = new OrganizationRoleBaseTest(initialStatus);

            // Act + Assert
            if (isAllowed)
            {
                target.Deactivate();
                Assert.Equal(RoleStatus.Inactive, target.Status);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => target.Deactivate());
            }
        }

        [Theory]
        [InlineData(RoleStatus.New, true)]
        [InlineData(RoleStatus.Active, true)]
        [InlineData(RoleStatus.Inactive, true)]
        [InlineData(RoleStatus.Deleted, true)]
        public void Delete_ChangesState_IfAllowed(RoleStatus initialStatus, bool isAllowed)
        {
            // Arrange
            var target = new OrganizationRoleBaseTest(initialStatus);

            // Act + Assert
            if (isAllowed)
            {
                target.Delete();
                Assert.Equal(RoleStatus.Deleted, target.Status);
            }
            else
            {
                Assert.Throws<InvalidOperationException>(() => target.Delete());
            }
        }

        private sealed class OrganizationRoleBaseTest : OrganizationRoleBase
        {
            public OrganizationRoleBaseTest()
            {
            }

            public OrganizationRoleBaseTest(RoleStatus initialStatus)
                : base(Guid.Empty, initialStatus, new Collection<MeteringPointType>())
            {
            }
        }
    }
}

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

namespace Energinet.DataHub.MarketParticipant.Infrastructure.Model.ActiveDirectory
{
    public static class ActiveDirectoryB2CRoles
    {
        public static string DdkId { get; set; } = null!;
        public static string DdmId { get; set; } = null!;
        public static string DdqId { get; set; } = null!;
        public static string EzId { get; set; } = null!;
        public static string MdrId { get; set; } = null!;
        public static string StsId { get; set; } = null!;
    }
}

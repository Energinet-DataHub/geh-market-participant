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

namespace Energinet.DataHub.MarketParticipant.Common.Configuration;

#pragma warning disable CA1724
public static class Settings
#pragma warning restore CA1724
{
    public static Setting<string> SqlDbConnectionString { get; }
        = new("SQL_MP_DB_CONNECTION_STRING");

    public static Setting<string> ServiceBusTopicConnectionString { get; }
        = new("SERVICE_BUS_CONNECTION_STRING");
    public static Setting<string> ServiceBusTopicName { get; }
        = new("SBT_MARKET_PARTICIPANT_CHANGED_NAME");

    public static Setting<string> B2CBackendObjectId { get; }
        = new("AZURE_B2C_BACKEND_OBJECT_ID");
    public static Setting<string> B2CBackendServicePrincipalNameObjectId { get; }
        = new("AZURE_B2C_BACKEND_SPN_OBJECT_ID");
    public static Setting<string> B2CBackendId { get; }
        = new("AZURE_B2C_BACKEND_ID");
    public static Setting<string> B2CTenant { get; }
        = new("AZURE_B2C_TENANT");
    public static Setting<string> B2CServicePrincipalId { get; }
        = new("AZURE_B2C_SPN_ID");
    public static Setting<string> B2CServicePrincipalSecret { get; }
        = new("AZURE_B2C_SPN_SECRET");

    public static Setting<string> ExternalOpenIdUrl { get; }
        = new("EXTERNAL_OPEN_ID_URL");
    public static Setting<string> InternalOpenIdUrl { get; }
        = new("INTERNAL_OPEN_ID_URL");
    public static Setting<string> BackendBffAppId { get; }
        = new("BACKEND_BFF_APP_ID");

    public static Setting<bool> AllowAllTokens { get; }
        = new("ALLOW_ALL_TOKENS", false);

    public static Setting<Uri> TokenKeyVault { get; }
        = new("TOKEN_KEY_VAULT");
    public static Setting<string> TokenKeyName { get; }
        = new("TOKEN_KEY_NAME");

    public static Setting<string> SendGridApiKey { get; }
        = new("SEND_GRID_APIKEY");

    public static Setting<string> UserInviteFromEmail { get; }
        = new("USER_INVITE_FROM_EMAIL");
    public static Setting<string> UserInviteBccEmail { get; }
        = new("USER_INVITE_BCC_EMAIL");
    public static Setting<string> UserInviteFlow { get; }
        = new("USER_INVITE_FLOW");
}

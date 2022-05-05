# Copyright 2020 Energinet DataHub A/S
#
# Licensed under the Apache License, Version 2.0 (the "License2");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#     http://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.
module "func_entrypoint_marketparticipant" {
  source                                              = "git::https://github.com/Energinet-DataHub/geh-terraform-modules.git//azure/function-app?ref=5.12.0"

  name                                                = "organization"
  project_name                                        = var.domain_name_short
  environment_short                                   = var.environment_short
  environment_instance                                = var.environment_instance
  resource_group_name                                 = azurerm_resource_group.this.name
  location                                            = azurerm_resource_group.this.location
  app_service_plan_id                                 = data.azurerm_key_vault_secret.plan_shared_id.value
  application_insights_instrumentation_key            = data.azurerm_key_vault_secret.appi_shared_instrumentation_key.value
  log_analytics_workspace_id                          = data.azurerm_key_vault_secret.log_shared_id.value
  always_on                                           = true
  health_check_path                                   = "/api/monitor/ready"
  health_check_alert_action_group_id                  = data.azurerm_key_vault_secret.primary_action_group_id.value
  health_check_alert_enabled                          = var.enable_health_check_alerts
  app_settings                                        = {
    # Region: Default Values
    WEBSITE_ENABLE_SYNC_UPDATE_SITE                   = true
    WEBSITE_RUN_FROM_PACKAGE                          = 1
    WEBSITES_ENABLE_APP_SERVICE_STORAGE               = true
    FUNCTIONS_WORKER_RUNTIME                          = "dotnet-isolated"
    # Endregion
    SQL_MP_DB_CONNECTION_STRING        		            = local.MS_MARKET_PARTICIPANT_CONNECTION_STRING
    SERVICE_BUS_CONNECTION_STRING                     = data.azurerm_key_vault_secret.sb_domain_relay_send_connection_string.value
    SERVICE_BUS_HEALTH_CHECK_CONNECTION_STRING        = "${data.azurerm_key_vault_secret.sb_domain_relay_manage_connection_string.value}"
    SBT_MARKET_PARTICIPANT_CHANGED_NAME               = data.azurerm_key_vault_secret.sbt-market-participant-changed-name.value
  }

  tags                                                = azurerm_resource_group.this.tags
}

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
variable subscription_id {
  type        = string
  description = "(Required) Subscription that the infrastructure code is deployed into."
}

variable resource_group_name {
  type        = string
  description = "(Required) Resource Group that the infrastructure code is deployed into."
}

variable environment_short {
  type          = string
  description   = "(Required) 1 character name of the enviroment that the infrastructure code is deployed into."
}

variable environment_instance {
  type          = string
  description   = "(Required) Enviroment instance that the infrastructure code is deployed into."
}

variable domain_name_short {
  type          = string
  description   = "(Required) Shortest possible edition of the domain name."
}

variable shared_resources_keyvault_name {
  type          = string
  description   = "(Required) Name of the KeyVault, that contains the shared secrets"
}

variable shared_resources_resource_group_name {
  type          = string
  description   = "(Required) Name of the Resource Group, that contains the shared resources."
}

variable b2c_tenant {
  type          = string
  description   = "(Required) The URL for the B2C Tenant."
}

variable b2c_spn_id {
  type          = string
  description   = "(Required) The app id for the service principal with global admin rights in the B2C Tenant."
}

variable b2c_spn_secret {
  type          = string
  description   = "(Required) The secret for the service principal with global admin rights in the B2C Tenant."
}

variable b2c_backend_spn_object_id {
  type          = string
  description   = "(Required) The object id for the backend app service principal in the B2C Tenant."
}

variable b2c_backend_id {
  type          = string
  description   = "(Required) The app id for the backend app in the B2C Tenant."
}
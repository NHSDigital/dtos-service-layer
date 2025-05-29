module "functionapp" {
  for_each = local.function_app_map

  source = "../../../dtos-devops-templates/infrastructure/modules/function-app"

  function_app_name   = "${module.regions_config[each.value.region].names.function-app}-${lower(each.value.name_suffix)}"
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  acr_login_server                       = "https://ghcr.io/nhsdigital"
  ai_connstring                          = data.azurerm_application_insights.ai.connection_string
  always_on                              = var.function_apps.always_on
  app_service_logs_disk_quota_mb         = var.function_apps.app_service_logs_disk_quota_mb
  app_service_logs_retention_period_days = var.function_apps.app_service_logs_retention_period_days
  app_settings                           = each.value.app_settings
  asp_id                                 = module.app-service-plan["${each.value.app_service_plan_key}-${each.value.region}"].app_service_plan_id
  cont_registry_use_mi                   = var.function_apps.cont_registry_use_mi
  # azuread_group_ids                                    = each.value.azuread_group_ids
  function_app_slots                                   = var.function_app_slots
  health_check_path                                    = var.function_apps.health_check_path
  image_name                                           = "${var.function_apps.docker_img_prefix}-${lower(each.value.name_suffix)}"
  image_tag                                            = var.function_apps.docker_env_tag
  ip_restriction_default_action                        = var.function_apps.ip_restriction_default_action
  ip_restrictions                                      = each.value.ip_restrictions
  log_analytics_workspace_id                           = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_function_app_enabled_logs = local.monitor_diagnostic_setting_function_app_enabled_logs
  monitor_diagnostic_setting_function_app_metrics      = local.monitor_diagnostic_setting_function_app_metrics

  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids                 = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-app_services"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region].names.subnet}-pep"].id
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  public_network_access_enabled = length(keys(each.value.ip_restrictions)) > 0 ? true : var.features.public_network_access_enabled
  rbac_role_assignments         = each.value.rbac_role_assignments
  storage_account_access_key    = var.function_apps.storage_uses_managed_identity == true ? null : module.storage["fnapp-${each.value.region}"].storage_account_primary_access_key
  storage_account_name          = module.storage["fnapp-${each.value.region}"].storage_account_name
  storage_uses_managed_identity = var.function_apps.storage_uses_managed_identity
  vnet_integration_subnet_id    = module.subnets["${module.regions_config[each.value.region].names.subnet}-apps"].id
  worker_32bit                  = var.function_apps.worker_32bit

  tags = var.tags
}


/* -------------------------------------------------------------------------------------------------
  Local variables used to create the Environment Variables for the Function Apps
-------------------------------------------------------------------------------------------------- */

locals {
  primary_region = [for k, v in var.regions : k if v.is_primary_region][0]

  app_settings_common = {
    REMOTE_DEBUGGING_ENABLED            = var.function_apps.remote_debugging_enabled
    WEBSITES_ENABLE_APP_SERVICE_STORAGE = var.function_apps.enable_appsrv_storage
    WEBSITE_PULL_IMAGE_OVER_VNET        = "false"
    FUNCTIONS_WORKER_RUNTIME            = "dotnet-isolated"
  }

  # There are multiple Function Apps and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  function_app_config_object_list = flatten([
    for region in keys(var.regions) : [
      for function, config in var.function_apps.function_app_config : merge(
        {
          region   = region   # 1st iterator
          function = function # 2nd iterator
        },
        config, # the rest of the key/value pairs for a specific function
        {
          ip_restriction = config.ip_restrictions

          app_settings = merge(
            local.app_settings_common,
            config.env_vars.static,
            {
              for k, v in config.env_vars.from_key_vault : k => "@Microsoft.KeyVault(SecretUri=${module.key_vault[region].key_vault_url}secrets/${v})"
            },
            {
              for k, v in config.env_vars.local_urls : k => format(v, module.regions_config[region].names["function-app"]) # Function App and Web App have the same naming prefix
            },
            length(config.db_connection_string) > 0 ? {
              (config.db_connection_string) = "Server=${module.regions_config[region].names.sql-server}.database.windows.net; Authentication=Active Directory Managed Identity; Database=${var.sqlserver.dbs.parman.db_name_suffix}"
            } : {}
          )

          # azuread_group_ids = flatten([
          #   length(config.db_connection_string) > 0 ? [data.azuread_group.sql_admin_group.object_id] : [],
          # ])

          # These RBAC assignments are for the Function Apps only
          rbac_role_assignments = flatten([
            var.key_vault != {} && length(config.env_vars.from_key_vault) > 0 ? [
              for role in local.rbac_roles_key_vault : {
                role_definition_name = role
                scope                = module.key_vault[region].key_vault_id
              }
            ] : [],
            [
              for account in keys(var.storage_accounts) : [
                for role in local.rbac_roles_storage : {
                  role_definition_name = role
                  scope                = module.storage["${account}-${region}"].storage_account_id
                }
              ]
            ],
            [
              for role in local.rbac_roles_database : {
                role_definition_name = role
                scope                = module.azure_sql_server[region].sql_server_id
              }
            ]
          ])
        }
      )
    ]
  ])

  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  function_app_map = {
    for object in local.function_app_config_object_list : "${object.function}-${object.region}" => object
  }
}

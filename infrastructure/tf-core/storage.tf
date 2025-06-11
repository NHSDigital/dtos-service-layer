module "storage" {
  for_each = local.storage_accounts_map

  source = "../../../dtos-devops-templates/infrastructure/modules/storage"

  name                = substr("${module.regions_config[each.value.region].names.storage-account}${lower(each.value.name_suffix)}", 0, 24)
  resource_group_name = azurerm_resource_group.core[each.value.region].name
  location            = each.value.region

  containers = each.value.containers

  log_analytics_workspace_id                              = data.terraform_remote_state.audit.outputs.log_analytics_workspace_id[local.primary_region]
  monitor_diagnostic_setting_storage_account_enabled_logs = local.monitor_diagnostic_setting_storage_account_enabled_logs
  monitor_diagnostic_setting_storage_account_metrics      = local.monitor_diagnostic_setting_storage_account_metrics

  account_replication_type      = each.value.replication_type
  account_tier                  = each.value.account_tier
  public_network_access_enabled = each.value.public_network_access_enabled

  rbac_roles = local.rbac_roles_storage

  # Private Endpoint Configuration if enabled
  private_endpoint_properties = var.features.private_endpoints_enabled ? {
    private_dns_zone_ids_blob            = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-storage_blob"].id]
    private_dns_zone_ids_queue           = [data.terraform_remote_state.hub.outputs.private_dns_zones["${each.value.region}-storage_queue"].id]
    private_endpoint_enabled             = var.features.private_endpoints_enabled
    private_endpoint_subnet_id           = module.subnets["${module.regions_config[each.value.region].names.subnet}-pep"].id
    private_endpoint_resource_group_name = azurerm_resource_group.rg_private_endpoints[each.value.region].name
    private_service_connection_is_manual = var.features.private_service_connection_is_manual
  } : null

  queues = each.value.queues

  tags = var.tags
}

locals {
  # There are multiple Storage Accounts and possibly multiple regions.
  # We cannot nest for loops inside a map, so first iterate all permutations of both as a list of objects...
  storage_accounts_object_list = flatten([
    for region in keys(var.regions) : [
      for storage_account, config in var.storage_accounts : merge(
        {
          region          = region          # 1st iterator
          storage_account = storage_account # 2nd iterator
        },
        config # the rest of the key/value pairs for a specific storage_account
      )
    ]
  ])
  # ...then project the list of objects into a map with unique keys (combining the iterators), for consumption by a for_each meta argument
  storage_accounts_map = {
    for object in local.storage_accounts_object_list : "${object.storage_account}-${object.region}" => object
  }
}

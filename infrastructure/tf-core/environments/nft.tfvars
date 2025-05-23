application           = "svclyr"
application_full_name = "service-layer"
environment           = "NFT"

features = {
  acr_enabled                          = false
  api_management_enabled               = false
  event_grid_enabled                   = false
  private_endpoints_enabled            = true
  private_service_connection_is_manual = false
  public_network_access_enabled        = false
}

tags = {
  Project = "Service-Layer"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.136.0.0/16"
    connect_peering   = true
    subnets = {
      apps = {
        cidr_newbits               = 8
        cidr_offset                = 2
        delegation_name            = "Microsoft.Web/serverFarms"
        service_delegation_name    = "Microsoft.Web/serverFarms"
        service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      }
      pep = {
        cidr_newbits = 8
        cidr_offset  = 1
      }
      sql = {
        cidr_newbits = 8
        cidr_offset  = 3
      }
      webapps = {
        cidr_newbits               = 8
        cidr_offset                = 4
        delegation_name            = "Microsoft.Web/serverFarms"
        service_delegation_name    = "Microsoft.Web/serverFarms"
        service_delegation_actions = ["Microsoft.Network/virtualNetworks/subnets/action"]
      }
      pep-dmz = {
        cidr_newbits = 8
        cidr_offset  = 5
      }
    }
  }
}

routes = {
  uksouth = {
    firewall_policy_priority = 100
    application_rules        = []
    nat_rules                = []
    network_rules = [
      {
        name                  = "AllowSvclyrToAudit"
        priority              = 800
        action                = "Allow"
        rule_name             = "SvclyrToAudit"
        source_addresses      = ["10.136.0.0/16"]
        destination_addresses = ["10.137.0.0/16"]
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      },
      {
        name                  = "AllowAuditToSvclyr"
        priority              = 810
        action                = "Allow"
        rule_name             = "AuditToSvclyr"
        source_addresses      = ["10.137.0.0/16"]
        destination_addresses = ["10.136.0.0/16"]
        protocols             = ["TCP", "UDP"]
        destination_ports     = ["443"]
      }
    ]
    route_table_routes_to_audit = [
      {
        name                   = "SvclyrToAudit"
        address_prefix         = "10.137.0.0/16"
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
    route_table_routes_from_audit = [
      {
        name                   = "AuditToSvclyr"
        address_prefix         = "10.136.0.0/16"
        next_hop_type          = "VirtualAppliance"
        next_hop_in_ip_address = "" # will be populated with the Firewall Private IP address
      }
    ]
  }
}

app_service_plan = {
  os_type                  = "Linux"
  sku_name                 = "P2v3"
  vnet_integration_enabled = true

  autoscale = {
    scaling_rule = {
      metric = "MemoryPercentage"

      capacity_min = "1"
      capacity_max = "5"
      capacity_def = "1"

      time_grain       = "PT1M"
      statistic        = "Average"
      time_window      = "PT10M"
      time_aggregation = "Average"

      inc_operator        = "GreaterThan"
      inc_threshold       = 70
      inc_scale_direction = "Increase"
      inc_scale_type      = "ChangeCount"
      inc_scale_value     = 1
      inc_scale_cooldown  = "PT5M"

      dec_operator        = "LessThan"
      dec_threshold       = 25
      dec_scale_direction = "Decrease"
      dec_scale_type      = "ChangeCount"
      dec_scale_value     = 1
      dec_scale_cooldown  = "PT5M"
    }
  }

  instances = {
    Default = {}
    # BIAnalyticsDataService       = {}
    # BIAnalyticsService           = {}
    # DemographicsService          = {}
    # EpisodeDataService           = {}
    # EpisodeIntegrationService    = {}
    # EpisodeManagementService     = {}
    # MeshIntegrationService       = {}
    # ParticipantManagementService = {}
    # ReferenceDataService         = {}
  }
}

diagnostic_settings = {
  metric_enabled = true
}

function_apps = {
  app_service_logs_disk_quota_mb         = 35
  app_service_logs_retention_period_days = 7
  always_on                              = true
  docker_env_tag                         = "nft"
  docker_img_prefix                      = "service-layer"
  enable_appsrv_storage                  = "false"
  ftps_state                             = "Disabled"
  https_only                             = true
  remote_debugging_enabled               = false
  storage_uses_managed_identity          = null
  worker_32bit                           = false
  ip_restriction_default_action          = "Deny"

  function_app_config = {



  }
}

function_app_slots = []

key_vault = {
  disk_encryption   = true
  soft_del_ret_days = 7
  purge_prot        = true
  sku_name          = "standard"
}

sqlserver = {
  sql_uai_name                         = "dtos-service-layer-sql-adm"
  sql_admin_group_name                 = "sqlsvr_svclyr_nft_uks_admin"
  ad_auth_only                         = true
  auditing_policy_retention_in_days    = 30
  security_alert_policy_retention_days = 30

  server = {
    sqlversion                    = "12.0"
    tlsversion                    = 1.2
    azure_services_access_enabled = true
  }

  # parman database
  dbs = {
    parman = {
      db_name_suffix = "service_layer_database"
      collation      = "SQL_Latin1_General_CP1_CI_AS"
      licence_type   = "LicenseIncluded"
      max_gb         = 5
      read_scale     = false
      sku            = "S0"
    }
  }

  fw_rules = {}
}

storage_accounts = {
  fnapp = {
    name_suffix                   = "fnappstor"
    account_tier                  = "Standard"
    replication_type              = "LRS"
    public_network_access_enabled = false
    containers                    = {}
  }
  # webapp = {
  #   name_suffix                             = "webappstor"
  #   account_tier                            = "Standard"
  #   replication_type                        = "LRS"
  #   public_network_access_enabled           = true
  #   blob_properties_delete_retention_policy = 7
  #   blob_properties_versioning_enabled      = false
  #   containers = {
  #     webapp = {
  #       container_name = "webapp"
  #     }
  #   }
  # }
}

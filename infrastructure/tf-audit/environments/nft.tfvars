application           = "svclyr"
application_full_name = "service-layer"
environment           = "NFT"

features = {
  private_endpoints_enabled              = true
  private_service_connection_is_manual   = false
  log_analytics_data_export_rule_enabled = false
  public_network_access_enabled          = false
}

tags = {
  Project = "Service-Layer"
}

regions = {
  uksouth = {
    is_primary_region = true
    address_space     = "10.137.0.0/16"
    connect_peering   = true
    subnets = {
      pep = {
        cidr_newbits = 8
        cidr_offset  = 1
      }
    }
  }
}

app_insights = {
  appinsights_type = "web"
}

law = {
  law_sku            = "PerGB2018"
  retention_days     = 30
  export_enabled     = false
  export_table_names = ["Alert"]
}

storage_accounts = {
  sqllogs = {
    name_suffix                   = "sqllogs"
    account_tier                  = "Standard"
    replication_type              = "LRS"
    public_network_access_enabled = false
    containers = {
      vulnerability-assessment = {
        container_name        = "vulnerability-assessment"
        container_access_type = "private"
      }
    }
  }
}

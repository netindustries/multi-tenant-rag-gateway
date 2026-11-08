data "azurerm_client_config" "current" {}

# 1. CORE RESOURCE GROUP
resource "azurerm_resource_group" "app_rg" {
  name     = "free-tier-app-rg"
  location = "centralus" # This instantly clears the "Total VMs: 0" capacity denial
}

resource "azurerm_resource_group" "flex_compute_rg" {
  name     = "flex-compute-rg"
  location = "centralus"
}

# 2. DATA BUCKET
resource "azurerm_storage_account" "store" {
  name                     = "freesegregatedstore2026"
  resource_group_name      = azurerm_resource_group.app_rg.name
  location                 = azurerm_resource_group.app_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

resource "azurerm_storage_container" "containers" {
  for_each              = local.final_client_map
  name                  = "document-silo-${each.key}"
  storage_account_id    = azurerm_storage_account.store.id
  container_access_type = "private"
}

# 3. SERVERLESS STORAGE RUNTIME CONTEXT
resource "azurerm_storage_account" "func_storage" {
  name                     = "funcstoragerag2026"
  resource_group_name      = azurerm_resource_group.app_rg.name
  location                 = azurerm_resource_group.app_rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
}

# 4. KEY VAULT TRUST STORE
resource "azurerm_key_vault" "vault" {
  name                = "free-vault-clean2026"
  location            = azurerm_resource_group.app_rg.location
  resource_group_name = azurerm_resource_group.app_rg.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"

  access_policy {
    tenant_id = data.azurerm_client_config.current.tenant_id
    object_id = data.azurerm_client_config.current.object_id 
    certificate_permissions = ["Create", "Get", "List", "Update", "Delete", "Purge"]
  }
}

# 5. TRUST CONFIGURATION CERTIFICATES
resource "azurerm_key_vault_certificate" "client_certs" {
  for_each     = local.final_client_map
  name         = "${each.key}-auth-cert"
  key_vault_id = azurerm_key_vault.vault.id

  certificate_policy {
    issuer_parameters { name = "Self" }
    key_properties {
      exportable = true
      key_size   = 2048
      key_type   = "RSA"
      reuse_key  = false
    }
    secret_properties { content_type = "application/x-pkcs12" }
    x509_certificate_properties {
      extended_key_usage = ["1.3.6.1.5.5.7.3.2"]
      key_usage          = ["digitalSignature", "keyEncipherment"]
      
      # FIX: Reference cert_safe_name to ensure un-broken X.500 DN generation constraints pass validation
      subject = "CN=${each.value.cert_safe_name},O=${each.value.cert_safe_name},OU=${each.value.data_tier},L=${each.value.residency_scope}"
      
      validity_in_months = 12
    }
  }
}

# 6. ENTRA ID APP REGISTRATIONS
resource "azuread_application" "client_apps" {
  for_each     = local.final_client_map
  display_name = "${each.value.full_name} Identity Profile"
}

resource "azuread_service_principal" "client_sps" {
  for_each  = local.final_client_map
  client_id = azuread_application.client_apps[each.key].client_id
}

# 7. THE DIRECT FEDERATED ASSOCIATION INTERACTION
resource "azuread_application_certificate" "direct_link" {
  for_each       = local.final_client_map
  application_id = azuread_application.client_apps[each.key].id
  type           = "AsymmetricX509Cert"
  value          = azurerm_key_vault_certificate.client_certs[each.key].certificate_data_base64
}

# 8. MULTI-SERVICE AGNOSTIC MODEL STORAGE ENGINES
resource "azurerm_search_service" "rag_search" {
  name                = "free-rag-search-2026"
  resource_group_name = azurerm_resource_group.app_rg.name
  location            = azurerm_resource_group.app_rg.location
  sku                 = "free" 
}

resource "azurerm_cognitive_account" "ai_language" {
  name                = "free-rag-language-2026"
  location            = azurerm_resource_group.app_rg.location
  resource_group_name = azurerm_resource_group.app_rg.name
  kind                = "TextAnalytics"
  sku_name            = "F0" 
}

# ============================================================================
# 9. REFACTORED: FLEX CONSUMPTION SERVICE PLAN CONFIGURATION LAYER
# ============================================================================
resource "azurerm_service_plan" "func_plan" {
  name                = "rag-serverless-plan"
  resource_group_name = azurerm_resource_group.flex_compute_rg.name
  location            = azurerm_resource_group.flex_compute_rg.location
  os_type             = "Linux"
  sku_name            = "FC1" 
}

resource "azurerm_storage_container" "app_package" {
  name                  = "app-package"
  storage_account_id    = azurerm_storage_account.func_storage.id
  container_access_type = "private"
}

# ============================================================================
# 10. REFACTORED: SERVERLESS FLEX RUNTIME WORKER ENGINE
# ============================================================================
resource "azurerm_function_app_flex_consumption" "api_gateway_func" {
  name                = "free-client-api-gateway"
  resource_group_name = azurerm_resource_group.flex_compute_rg.name
  location            = azurerm_resource_group.flex_compute_rg.location
  service_plan_id     = azurerm_service_plan.func_plan.id

  # Native Runtime Declarations
  runtime_name    = "dotnet-isolated"
  runtime_version = "10.0"

  # Exact Flex Platform Storage Constraints 
  storage_container_type      = "blobContainer"
  storage_authentication_type = "StorageAccountConnectionString"
  
  # Concatenates the exact Blob Endpoint with your specific Deployment Container
  storage_container_endpoint = "${azurerm_storage_account.func_storage.primary_blob_endpoint}${azurerm_storage_container.app_package.name}"
  storage_access_key  = azurerm_storage_account.func_storage.primary_access_key

  # Security Configurations
  client_certificate_enabled = true
  client_certificate_mode = "Required"
  identity { 
    type = "SystemAssigned" 
  }

  site_config {}

  app_settings = {
    # System storage attributes are derived above; assign backend services below
    GatewayConfiguration__AzureKeyVaultUri      = azurerm_key_vault.vault.vault_uri
    GatewayConfiguration__AzureTenantId         = data.azurerm_client_config.current.tenant_id
    GatewayConfiguration__AzureSearchEndpoint   = "https://${azurerm_search_service.rag_search.name}.search.windows.net"
    GatewayConfiguration__AzureLanguageEndpoint = azurerm_cognitive_account.ai_language.endpoint
  }
}


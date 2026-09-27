data "azuread_service_principal" "msgraph" {
  display_name = "Microsoft Graph"
}

# 1. CONTAINER DATA LAYER ISOLATION BOUNDARIES
resource "azurerm_role_assignment" "container_isolation" {
  for_each             = local.final_client_map
  scope                = "${azurerm_storage_account.store.id}/blobServices/default/containers/${azurerm_storage_container.containers[each.key].name}"
  role_definition_name = "Storage Blob Data Contributor"
  principal_id         = azuread_service_principal.client_sps[each.key].object_id
}

# 2. SERVICE GRAPH DIRECTORY READ ACCESS RULES
resource "azuread_app_role_assignment" "graph_read_permissions" {
  app_role_id         = data.azuread_service_principal.msgraph.app_role_ids["Application.Read.All"]
  principal_object_id = azurerm_function_app_flex_consumption.api_gateway_func.identity[0].principal_id
  resource_object_id  = data.azuread_service_principal.msgraph.object_id
}

# SYSTEM OUTPUT ENVELOPES
output "function_app_gateway_url" {
  value = "https://${azurerm_function_app_flex_consumption.api_gateway_func.default_hostname}"
  description = "The public, mTLS-secured gateway URL."
}

output "key_vault_uri" {
  value       = azurerm_key_vault.vault.vault_uri
  description = "The core secure Key Vault URI locator."
}

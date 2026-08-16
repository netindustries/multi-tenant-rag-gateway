variable "client_config_json" {
  type    = string
  default = "{\"client_setups\":[]}"
}

locals {
  raw_array = jsondecode(var.client_config_json).client_setups

  # UNIFIED MULTI-PROPERTY SANITIZATION ENGINE
  # Appending [0] converts the single-element tuple collection into a raw text string
  final_client_map = {
    for item in local.raw_array : 
    lower(replace(replace(keys(item)[0], " ", "-"), "/[^a-zA-Z0-9-]/", "")) => {
      full_name       = keys(item)[0]
      cert_safe_name  = replace(keys(item)[0], ",", "")
      data_tier       = item[keys(item)[0]].data_tier
      residency_scope = item[keys(item)[0]].residency_scope
    }
  }
}

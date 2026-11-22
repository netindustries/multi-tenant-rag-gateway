terraform {
  required_providers {
	azurerm = { source = "hashicorp/azurerm", version = "~> 4.0" }
	azuread = { source = "hashicorp/azuread", version = "~> 2.0" }
  }
  backend "azurerm" {}
}

provider "azurerm" {
  features {
	key_vault {
	  purge_soft_delete_on_destroy    = true
	  recover_soft_deleted_key_vaults = false
	}
	cognitive_account {
	  purge_soft_delete_on_destroy = true
	}
  }
}

provider "azuread" {}

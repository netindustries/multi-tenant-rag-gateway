# Terraform Backend Configuration Reference

This document explains how the Terraform backend state storage is provisioned and referenced.

## Backend Infrastructure (Created by `bootstrap.sh`)

| Component | Value | Notes |
|-----------|-------|-------|
| **Resource Group** | `free-tfstate-rg` | Houses the storage account |
| **Storage Account** | `freetfstate{random}` | Globally unique name (see output below) |
| **Container** | `tfstate-container` | Stores the state file |
| **State File Key** | `prod.terraform.tfstate` | Defined in `azure-pipelines.yml` |

## Terraform Backend Initialization

The backend is initialized **at pipeline runtime** (not stored in the repo). During `terraform init`, the Azure Pipelines task passes:

```bash
-backend-config="resource_group_name=free-tfstate-rg"
-backend-config="storage_account_name={OUTPUT_FROM_BOOTSTRAP}"
-backend-config="container_name=tfstate-container"
-backend-config="key=prod.terraform.tfstate"
```

## Pipeline Variables (`azure-pipelines.yml`)

These **must match** the bootstrap values:

```yaml
variables:
  bk-rg: 'free-tfstate-rg'
  bk-storage: 'freetfstate_paste_your_unique_string_here'  # ← OUTPUT FROM bootstrap.sh
  bk-container: 'tfstate-container'                        # ← MUST MATCH bootstrap.sh
  bk-key: 'prod.terraform.tfstate'
```

## Flow Diagram

```
1. bootstrap.sh (manual, one-time)
   ├── Creates Resource Group: free-tfstate-rg
   ├── Creates Storage Account: freetfstate{random}
   ├── Creates Container: tfstate-container
   ├── Creates Service Principal
   └── OUTPUT: Storage account name to paste in azure-pipelines.yml

2. azure-pipelines.yml (terraform init)
   ├── Reads bk-storage variable (from bootstrap output)
   ├── Reads bk-container variable (static: tfstate-container)
   ├── Calls: terraform init -backend-config="storage_account_name=$(bk-storage)" ...
   └── Backend ready for terraform apply/destroy

3. terraform destroy + cleanup.sh
   ├── terraform destroy removes app infrastructure
   └── cleanup.sh removes: Resource Group, Storage Account, Container
	   (Service Principal remains for next bootstrap)
```

## Validation Checklist

Before running the pipeline, ensure:

- [ ] `bootstrap.sh` has been executed in your local shell
- [ ] The output shows: `Storage account: freetfstate{XXXXXX}`
- [ ] You pasted that value into `azure-pipelines.yml` as `bk-storage`
- [ ] `bk-container` is still `tfstate-container` (unchanged)
- [ ] `clean-tfstate-rg` exists in Azure (verify: `az group list`)
- [ ] Service Principal exists (verify: `az ad sp list --display-name "DevOps Service Principal Name"`)

## Troubleshooting

### Pipeline fails: "storage account not found"
- Ensure `bk-storage` variable matches the actual storage account name
- Verify: `az storage account show --name {bk-storage} --resource-group free-tfstate-rg`

### Pipeline fails: "container not found"
- Verify container name is exactly `tfstate-container`
- Check: `az storage container list --account-name {bk-storage}`

### Need to re-bootstrap after destroy
- Run `cleanup.sh` (removes storage backend)
- Run `bootstrap.sh` again (creates new storage backend)
- Update `azure-pipelines.yml` with new `bk-storage` value

#!/bin/bash
set -e 

if [ $# -eq 0 ] || [ -z "${1// }" ]; then
    echo "Error: Missing required argument."
    echo "Usage: $0 <devops-service-principal-name>"
    echo "Example: $0 my-tenant-devops-spn"
    exit 1
fi

LOCATION="northcentralus"
RESOURCE_GROUP_NAME="free-tfstate-rg"
STORAGE_ACCOUNT_PREFIX="freetfstate"
CONTAINER_NAME="tfstate-container"
SERVICE_PRINCIPAL_NAME="$1"

echo "======================================================================="
echo "🚀 STARTING LOCAL CLOUD INITIALIZATION FOR TERRAFORM STATE & DEVOPS"
echo "======================================================================="

if ! az account show &>/dev/null; then
	echo "❌ Active session not detected. Initiating 'az login'..."
	az login
fi

SUBSCRIPTION_ID=$(az account show --query id --output tsv)
TENANT_ID=$(az account show --query tenantId --output tsv)

# IDEMPOTENCY CHECK: If resource group exists, reuse existing storage account
if [ "$(az group exists -g $RESOURCE_GROUP_NAME -o tsv)" == "true" ]; then
	echo "⚠️  Resource Group already exists. Checking for existing storage account..."
	EXISTING_STORAGE=$(az storage account list --resource-group $RESOURCE_GROUP_NAME --query "[0].name" --output tsv 2>/dev/null || echo "")

	if [ ! -z "$EXISTING_STORAGE" ]; then
		echo "✅ Reusing existing storage account: $EXISTING_STORAGE"
		STORAGE_ACCOUNT_NAME=$EXISTING_STORAGE
	else
		echo "❌ Resource group exists but no storage account found."
		echo "   If you previously ran 'terraform destroy', delete the resource group:"
		echo "   az group delete --name $RESOURCE_GROUP_NAME --yes"
		echo "   Then re-run this script."
		exit 1
	fi
else
	# Generate unique storage account name (Azure storage names must be globally unique and 3-24 chars)
	STORAGE_ACCOUNT_NAME="${STORAGE_ACCOUNT_PREFIX}$(openssl rand -hex 3)"
	echo "📦 Creating Resource Group: $RESOURCE_GROUP_NAME..."
	az group create --name $RESOURCE_GROUP_NAME --location $LOCATION --output none
fi

# Only create storage account if it doesn't exist
if ! az storage account show --name $STORAGE_ACCOUNT_NAME --resource-group $RESOURCE_GROUP_NAME &>/dev/null 2>&1; then
	echo "💾 Creating Storage Account: $STORAGE_ACCOUNT_NAME (SKU: Standard_LRS)..."
	az storage account create \
		--resource-group $RESOURCE_GROUP_NAME \
		--name $STORAGE_ACCOUNT_NAME \
		--sku Standard_LRS \
		--encryption-services blob \
		--output none
else
	echo "✅ Storage account already exists: $STORAGE_ACCOUNT_NAME"
fi

# Only create container if it doesn't exist
if ! az storage container exists \
	--name $CONTAINER_NAME \
	--account-name $STORAGE_ACCOUNT_NAME \
	--query exists --output tsv 2>/dev/null | grep -q '^True$'; then
	echo "📄 Creating Storage Container: $CONTAINER_NAME..."
	az storage container create \
		--name $CONTAINER_NAME \
		--account-name $STORAGE_ACCOUNT_NAME \
		--output none
else
	echo "✅ Storage container already exists: $CONTAINER_NAME"
fi

# Check if service principal already exists
echo "🛡️ Checking for existing Service Principal: $SERVICE_PRINCIPAL_NAME..."
EXISTING_SP=$(az ad sp list --display-name $SERVICE_PRINCIPAL_NAME --query "[0].appId" --output tsv 2>/dev/null || echo "")

if [ ! -z "$EXISTING_SP" ]; then
	echo "✅ Service Principal already exists. Retrieving details..."
	CLIENT_ID=$EXISTING_SP
	OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)
	echo "⚠️  NOTE: Client secret is not retrievable for existing service principals."
	echo "   If you need a new credential, delete the old SP and re-run this script:"
	echo "   az ad sp delete --id $CLIENT_ID"
	CLIENT_SECRET="[RETRIEVE_FROM_PREVIOUS_BOOTSTRAP_OUTPUT_OR_DEVOPS_VAULT]"
else
	echo "📝 Creating new Service Principal: $SERVICE_PRINCIPAL_NAME..."
	SP_JSON=$(az ad sp create-for-rbac \
		--name $SERVICE_PRINCIPAL_NAME \
		--role "Contributor" \
		--scopes "/subscriptions/$SUBSCRIPTION_ID" \
		--output json)
	CLIENT_ID=$(echo $SP_JSON | jq -r '.appId')
	CLIENT_SECRET=$(echo $SP_JSON | jq -r '.password')
	OBJECT_ID=$(az ad sp show --id $CLIENT_ID --query id --output tsv)
fi

GRAPH_APP_ID="00000003-0000-0000-c000-000000000000"

PERM_DIRECTORY_RW="7b02c8e1-aa56-42d4-aa3d-6b58988a8d11"     # Directory.ReadWrite.All
PERM_APPROLE_ASSIGN_RW="2b221361-4859-4ae8-87d1-51655102a3d3" # AppRoleAssignment.ReadWrite.All
PERM_SITES_RW="205e70c5-ac9b-45c7-ab3d-abdc7455d31d"          # Sites.ReadWrite.All

echo "Adding your exact verified Microsoft Graph API Permissions..."
az ad app permission add \
    --id "${CLIENT_ID}" \
    --api "${GRAPH_APP_ID}" \
    --api-permissions "${PERM_DIRECTORY_RW}=Role" "${PERM_APPROLE_ASSIGN_RW}=Role" "${PERM_SITES_RW}=Role"

echo "Granting Tenant Admin Consent for all three roles..."
GRAPH_SP_OBJECT_ID=$(az ad sp show --id "${GRAPH_APP_ID}" --query id -o tsv)

for ROLE_ID in "${PERM_DIRECTORY_RW}" "${PERM_APPROLE_ASSIGN_RW}" "${PERM_SITES_RW}"; do
    az rest --method POST \
        --uri "https://microsoft.com{SP_OBJECT_ID}/appRoleAssignedTo" \
        --headers "Content-Type=application/json" \
        --body "{\"principalId\": \"${OBJECT_ID}\", \"resourceId\": \"${GRAPH_SP_OBJECT_ID}\", \"appRoleId\": \"${ROLE_ID}\"}" \
        --output none || echo "Permission already granted or skipped."
done

echo "======================================================================="
echo "✅ INITIALIZATION COMPLETE! USE THESE EXACT VALUES FOR DEVOPS"
echo "======================================================================="
echo ""
echo "🔧 TERRAFORM PIPELINE CONFIGURATION ARTIFACT:"
echo "-----------------------------------------------------------------------"
echo "bk-storage (Paste into your azure-pipelines.yml variable):"
echo "  '$STORAGE_ACCOUNT_NAME'"
echo ""
echo "🔐 DEVOPS SERVICE CONNECTION VALUES (Copy-Paste manually into DevOps UI):"
echo "-----------------------------------------------------------------------"
echo "Connection Name:             azure-service-connection"
echo "Scope Level:                 Subscription"
echo "Subscription ID:             $SUBSCRIPTION_ID"
echo "Tenant ID:                   $TENANT_ID"
echo "Service Principal Client ID: $CLIENT_ID"
echo "Service Principal Secret:    $CLIENT_SECRET"
echo ""
echo "🆔 AZURE DEVOPS VARIABLE GROUP ENTRA REFERENCE OBJECT ID:"
echo "-----------------------------------------------------------------------"
echo "YOUR_SERVICE_PRINCIPAL_OBJECT_ID: $OBJECT_ID"
echo "======================================================================="

#!/bin/bash
set -e

if [ $# -eq 0 ] || [ -z "${1// }" ]; then
    echo "Error: Missing required argument."
    echo "Usage: $0 <devops-service-principal-name>"
    echo "Example: $0 my-tenant-devops-spn"
    exit 1
fi

RESOURCE_GROUP_NAME="free-tfstate-rg"
SERVICE_PRINCIPAL_NAME="$1"

echo "======================================================================="
echo "🗑️  DESTROYING BOOTSTRAP INFRASTRUCTURE (EXCEPT SERVICE PRINCIPAL)"
echo "======================================================================="

# Step 1: Delete the Resource Group (cascades to storage account and container)
echo "🔍 Checking Resource Group: $RESOURCE_GROUP_NAME..."
if az group exists -g $RESOURCE_GROUP_NAME &>/dev/null; then
	echo "🗑️  Deleting Resource Group: $RESOURCE_GROUP_NAME..."
	echo "   (This will also cascade-delete the storage account and container)"
	az group delete --name $RESOURCE_GROUP_NAME --yes --no-wait
	echo "✅ Resource Group deletion initiated (checking status...)"

	# Wait for deletion to complete (optional, removes race conditions)
	echo "⏳ Waiting for Resource Group deletion to complete..."
	az group wait --name $RESOURCE_GROUP_NAME --deleted --timeout 300 2>/dev/null || true
	echo "✅ Resource Group and storage backend successfully deleted"
else
	echo "⚠️  Resource Group not found (already deleted?)"
fi

# Step 2: Service Principal REMAINS (reused for subsequent bootstrap/deployments)
echo ""
echo "ℹ️  Service Principal '$SERVICE_PRINCIPAL_NAME' is NOT deleted."
echo "   It will be reused on the next bootstrap.sh run for future deployments."
echo ""

echo "======================================================================="
echo "✅ CLEANUP COMPLETE"
echo "======================================================================="
echo ""
echo "Next steps:"
echo "  1. Run bootstrap.sh again to recreate storage backend for new deployment"
echo "  2. Or manually manage the infrastructure elsewhere"
echo ""

# 1. DEFINE TARGET IDENTITY PARAMETERS
# Replace with the exact Client ID (App ID) printed by your bootstrap script
DEVOPS_SP_CLIENT_ID="xxxxxxxxxxxxxxxxxxxx"

# 2. Add the Microsoft Graph AppRoleAssignment.ReadWrite.All permission
# 00000003-0000-0000-c000-000000000000 is the static global ID for Microsoft Graph
# 72a07017-35d0-42c4-b15e-e4a3643387aa is the static ID for AppRoleAssignment.ReadWrite.All
az ad app permission add \
  --id "$DEVOPS_SP_CLIENT_ID" \
  --api 00000003-0000-0000-c000-000000000000 \
  --api-permissions 72a07017-35d0-42c4-b15e-e4a3643387aa=Role

# 3. Grant Admin Consent instantly for your tenant using your Global Admin session
 az ad app permission grant --id "$DEVOPS_SP_CLIENT_ID" --api "00000003-0000-0000-c000-000000000000" --scope "AppRoleAssignment.ReadWrite.All"
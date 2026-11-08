# 1. IMPORT COGNITIVE TEXT ANALYTICS ACCOUNT
import {
  to = azurerm_cognitive_account.ai_language
  id = "/subscriptions/e4b51ad2-7a88-42dc-a673-0c4ba3523d3c/resourceGroups/free-tier-app-rg/providers/Microsoft.CognitiveServices/accounts/free-rag-language-2026"
}

# 2. IMPORT AZURE AI SEARCH SERVICE
import {
  to = azurerm_search_service.rag_search
  id = "/subscriptions/e4b51ad2-7a88-42dc-a673-0c4ba3523d3c/resourceGroups/free-tier-app-rg/providers/Microsoft.Search/searchServices/free-rag-search-2026"
}

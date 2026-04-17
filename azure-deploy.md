# Azure App Service Free Tier Deploy
# 1. Create Azure account: https://azure.microsoft.com/free
# 2. Create App Service (Free tier: F1)
# 3. Install Azure CLI: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli
# 4. Login and deploy:

# Login to Azure
az login

# Create resource group
az group create --name WebMktRG --location southeastasia

# Create App Service Plan (Free tier)
az appservice plan create --name webmkt-plan --resource-group WebMktRG --sku FREE --is-linux

# Create Web App
az webapp create --resource-group WebMktRG --plan webmkt-plan --name webmkt-2026 --runtime "DOTNETCORE|8.0"

# Deploy from local
az webapp up --name webmkt-2026 --resource-group WebMktRG --sku FREE

# Or deploy from GitHub (recommended)
# Connect to GitHub in Azure Portal > Deployment Center
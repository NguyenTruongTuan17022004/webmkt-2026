# Render Deploy Guide
# 1. Push code to GitHub
# 2. Go to render.com and sign up
# 3. Create new Web Service
# 4. Connect GitHub repo
# 5. Set build command: dotnet publish -c Release -o out
# 6. Set start command: dotnet out/WebMkt.dll
# 7. Deploy!

# Environment Variables (if needed):
# ASPNETCORE_ENVIRONMENT=Production
# ASPNETCORE_URLS=http://+:10000
# PowerShell script to push to GitHub
# Replace YOUR_USERNAME and YOUR_REPO_NAME

$githubUsername = "YOUR_USERNAME"
$repoName = "webmkt-2026"

# Add remote if not exists
git remote add origin "https://github.com/$githubUsername/$repoName.git"

# Push to GitHub
git push -u origin main

Write-Host "Code pushed to GitHub successfully!"
Write-Host "Now go to Railway and deploy from this repo."
# What does this do?

Simple Web App that redirects urls to a new target.  A JSON Map of source Paths and URL Targets are read from Azure Blob Storage.  This app is designed to be behind a reverse proxy that is setting the X-Forwarded-Host header.  The app then matches the appropriate Redirect rules.

# What is required?

1. Azure Blob Storage account container to host the Redirect.json files
2. A HTTP Reverse Proxy that sets the X-Forwarded-Host header
3. A service (Azure App Services) that can host a Dotnet 8 Web Application

# Coming Soon

More detailed setup documentation. 
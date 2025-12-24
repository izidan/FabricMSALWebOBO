# MSAL Web Application with OBO Token Support

This ASP.NET Core web application demonstrates Microsoft authentication using MSAL (Microsoft Authentication Library) and On-Behalf-Of (OBO) token generation.

## Features

- **Microsoft Identity Authentication**: Secure user login using Microsoft accounts
- **On-Behalf-Of Token Generation**: Generate OBO tokens for different applications using current user credentials
- **Token Visualization**: View current user tokens and generated OBO tokens
- **Echo Chat Service**: Simple chat interface that echoes user messages for demonstration purposes
- **Responsive Web UI**: Bootstrap-based user interface with authentication-aware navigation

## Prerequisites

- .NET 8.0 SDK
- Azure AD application registration

## Configuration

### 1. Azure AD App Registration

1. Go to [Azure Portal](https://portal.azure.com)
2. Navigate to **Azure Active Directory** > **App registrations**
3. Click **New registration**
4. Configure:
   - **Name**: MSAL Web App
   - **Supported account types**: Accounts in this organizational directory only
   - **Redirect URI**: Web - `https://localhost:5001/signin-oidc`
5. Note down the **Application (client) ID** and **Directory (tenant) ID**
6. Under **Certificates & secrets**, create a new client secret

### 2. Application Configuration

Update the `appsettings.json` file with your Azure AD details:

```json
{
  "AzureAd": {
    "Instance": "https://login.microsoftonline.com/",
    "Domain": "yourdomain.onmicrosoft.com",
    "TenantId": "your-tenant-id",
    "ClientId": "your-client-id",
    "ClientSecret": "your-client-secret",
    "CallbackPath": "/signin-oidc",
    "SignedOutCallbackPath": "/signout-callback-oidc"
  },
  "OboToken": {
    "SourceScope": "api://your-client-id/envision_scope",
    "TargetScope": "https://api.fabric.microsoft.com/.default",
    "ProjectEndpoint": "https://api.fabric.microsoft.com/v1/workspaces/your-workspace-id/aiskills/your-fabric-data-agent-id/aiassistant/openai/"
  }
}
```

### 3. API Permissions Setup

For OBO token generation to work, ensure your Azure AD app registration has:

1. **API Permissions**:

   - `Azure Machine Learning Services` > `user_impersonation`
   - `Azure Service Management` > `user_impersonation`
   - `Microsoft Cognitive Services` > `user_impersonation`
   - `Microsoft Graph` > `Directory.Read.All`
   - `Microsoft Graph` > `email`
   - `Microsoft Graph` > `offline_access`
   - `Microsoft Graph` > `openid`
   - `Microsoft Graph` > `profile`
   - `Microsoft Graph` > `User.Read`
   - `Power BI Service` > `Code.AccessFabric.All`
   - `Power BI Service` > `DataAgent.Execute.All`
   - `Power BI Service` > `DataAgent.Read.All`
   - `Power BI Service` > `Dataset.Read.All`
   - `Power BI Service` > `Workspace.Read.All`

2. **Expose an API** (if your app will be the target for OBO):

   - Define scopes that other applications can request
   - Add authorized client applications

3. **Admin Consent**: Grant admin consent for all configured permissions

## Project Structure

```
├── Pages/
│   ├── Chat.cshtml              # Simple echo chat interface
│   ├── Chat.cshtml.cs           # Chat page model for user interactions
│   ├── OboToken.cshtml          # OBO token generation interface
│   ├── OboToken.cshtml.cs       # OBO token page model
│   ├── Token.cshtml             # Token viewing interface
│   ├── Index.cshtml             # Home page with authentication status
│   └── Shared/
│       └── _Layout.cshtml       # Main layout with navigation
├── Services/
│   └── FabricDataAgent .cs      # Service for calling Fabric Data Agents through the chat completion API
│   └── OboTokenService.cs       # Service for OBO token generation
├── Program.cs                   # Application startup and configuration
├── appsettings.json            # Configuration settings
└── MSALWebApp.csproj           # Project file with dependencies
```

## Key Components

### OboTokenService

The `OboTokenService` handles the On-Behalf-Of flow:

- **Token Exchange**: Takes a user's access token and exchanges it for an OBO token
- **Target Application Support**: Configurable to work with different target applications
- **Error Handling**: Comprehensive error logging and exception handling

### Authentication Flow

1. User clicks "Sign In" and is redirected to Microsoft login
2. After successful authentication, user is redirected back to the application
3. Users can view their current tokens and generate OBO tokens for other applications
4. The OBO token can be used to call APIs on behalf of the authenticated user

## Running the Application

1. **Install dependencies**:

   ```bash
   dotnet restore
   ```

2. **Build the project**:

   ```bash
   dotnet build
   ```

3. **Run the application**:

   ```bash
   dotnet run
   ```

4. **Access the application**:
   - Navigate to `https://localhost:5001` or `http://localhost:5000`
   - Click "Sign In" to authenticate with Microsoft
   - Go to "OBO Token" to generate on-behalf-of tokens
   - Use "View Tokens" to see current user tokens
   - Go to "Echo Chat" to test the chat functionality

## Development Notes

- The application includes token acquisition and caching capabilities
- All pages except the home page require authentication
- OBO tokens are generated using the Microsoft Authentication Library (MSAL)
- The application demonstrates both current user token display and OBO token generation

## Security Considerations

- Store client secrets securely (use Azure Key Vault in production)
- Configure appropriate redirect URIs for your deployment environment
- Implement proper error handling and logging for production use
- Ensure proper API permissions and admin consent for OBO scenarios

## Troubleshooting

### Common Issues

1. **Authentication fails**: Check Azure AD app registration and configuration values
2. **OBO token generation fails**:
   - Verify target application configuration in `appsettings.json`
   - Check API permissions and admin consent
   - Ensure target application exists and is properly configured
   - Verify the scope format (should be `api://client-id/.default` or specific scopes)

### Logs

The application logs authentication events and API calls. Check the console output or configure structured logging for detailed troubleshooting.

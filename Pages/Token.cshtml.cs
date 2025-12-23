using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using MSALWebApp.Services;

namespace MSALWebApp.Pages
{
    [Authorize]
    public class TokenModel : PageModel
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly ILogger<TokenModel> _logger;
        private readonly OboTokenOptions _oboOptions;

        public TokenModel(
            ITokenAcquisition tokenAcquisition, 
            ILogger<TokenModel> logger,
            IOptions<OboTokenOptions> oboOptions)
        {
            _tokenAcquisition = tokenAcquisition;
            _logger = logger;
            _oboOptions = oboOptions.Value;
        }

        public string? AccessToken { get; set; }
        public string? ErrorMessage { get; set; }
        public string? TokenScope { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                _logger.LogInformation($"Getting access token for user: {User.Identity?.Name}");

                // First try to get token for our own API (source scope for OBO)
                if (!string.IsNullOrEmpty(_oboOptions.SourceScope))
                {
                    try
                    {
                        AccessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { 
                            _oboOptions.SourceScope
                        });
                        TokenScope = $"Own API - {_oboOptions.SourceScope}";
                        _logger.LogInformation("Successfully acquired access token for own API scope: {Scope}", _oboOptions.SourceScope);
                        return;
                    }
                    catch (Exception apiEx)
                    {
                        _logger.LogWarning(apiEx, "Failed to get token for own API scope: {Scope}, trying Microsoft Graph fallback", _oboOptions.SourceScope);
                    }
                }

                // Fallback to Microsoft Graph (for backwards compatibility)
                try
                {
                    AccessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { 
                        "https://graph.microsoft.com/User.Read"
                    });
                    TokenScope = "Microsoft Graph - User.Read (Fallback)";
                    _logger.LogInformation("Successfully acquired access token for Microsoft Graph fallback");
                }
                catch (Exception graphEx)
                {
                    _logger.LogWarning(graphEx, "Failed to get Microsoft Graph token, trying default scope");
                    
                    // Try a more basic scope
                    try
                    {
                        AccessToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] { 
                            "https://graph.microsoft.com/.default"
                        });
                        TokenScope = "Microsoft Graph - Default (Fallback)";
                        _logger.LogInformation("Successfully acquired access token for Microsoft Graph default scope");
                    }
                    catch (Exception defaultEx)
                    {
                        _logger.LogError(defaultEx, "Failed to acquire any access token");
                        ErrorMessage = $"Unable to acquire access token for any scope. " +
                                     $"Own API error: Token not available for {_oboOptions.SourceScope}. " +
                                     $"Graph error: {graphEx.Message}. " +
                                     $"Default scope error: {defaultEx.Message}. " +
                                     $"Make sure the required API permissions are granted in your Azure AD app registration.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in OnGetAsync");
                ErrorMessage = $"Unexpected error: {ex.Message}";
            }
        }
    }
}
using Azure.Identity;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace MSALWebApp.Services
{
    public class OboTokenOptions
    {
        public string SourceScope { get; set; } = string.Empty;
        public string TargetScope { get; set; } = string.Empty;
        public string ProjectEndpoint { get; set; } = string.Empty;
        public string AgentId { get; set; } = string.Empty;
    }

    public interface IOboTokenService
    {
        Task<string> GetAccessTokenOnBehalfOfUserAsync(string userAccessToken);
    }

    public class OboTokenService : IOboTokenService
    {
        private readonly OboTokenOptions _oboOptions;
        private readonly MicrosoftIdentityOptions _azureAdOptions;
        private readonly ILogger<OboTokenService> _logger;

        public OboTokenService(
            IOptions<OboTokenOptions> oboOptions,
            IOptions<MicrosoftIdentityOptions> azureAdOptions,
            ILogger<OboTokenService> logger)
        {
            _oboOptions = oboOptions.Value;
            _azureAdOptions = azureAdOptions.Value;
            _logger = logger;
        }

        public async Task<string> GetAccessTokenOnBehalfOfUserAsync(string userAccessToken)
        {
            try
            {
                var confidentialClient = ConfidentialClientApplicationBuilder.Create(_azureAdOptions.ClientId)
                    .WithClientSecret(_azureAdOptions.ClientSecret)
                    .WithAuthority(new Uri($"{_azureAdOptions.Instance}{_azureAdOptions.TenantId}"))
                    .Build();

                var userAssertion = new UserAssertion(userAccessToken);

                var result = await confidentialClient.AcquireTokenOnBehalfOf(
                        [_oboOptions.TargetScope],
                        //[_oboOptions.TargetScope, "https://api.fabric.microsoft.com/.default", "https://analysis.windows.net/powerbi/api/.default"],
                        userAssertion)
                    .ExecuteAsync();

                _logger.LogInformation("Successfully acquired OBO token for scope: {Scope}", _oboOptions.TargetScope);
                return result.AccessToken;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire OBO token for scope: {Scope}", _oboOptions.TargetScope);
                throw;
            }
        }
    }
}
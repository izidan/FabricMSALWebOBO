using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using MSALWebApp.Services;

namespace MSALWebApp.Pages
{
    [Authorize]
    public class SetupModel : PageModel
    {
        private readonly OboTokenOptions _oboOptions;
        private readonly MicrosoftIdentityOptions _azureAdOptions;
        private readonly ILogger<SetupModel> _logger;

        public SetupModel(
            IOptions<OboTokenOptions> oboOptions,
            IOptions<MicrosoftIdentityOptions> azureAdOptions,
            ILogger<SetupModel> logger)
        {
            _oboOptions = oboOptions.Value;
            _azureAdOptions = azureAdOptions.Value;
            _logger = logger;
        }

        public string ClientId => _azureAdOptions.ClientId ?? "Not configured";
        public string TenantId => _azureAdOptions.TenantId ?? "Not configured";
        public string SourceScope => _oboOptions.SourceScope ?? "Not configured";
        public string TargetScope => _oboOptions.TargetScope ?? "Not configured";
        public string ProjectEndpoint => _oboOptions.ProjectEndpoint ?? "Not configured";
        public string AgentId => _oboOptions.AgentId ?? "Not configured";
        public bool HasClientSecret => !string.IsNullOrEmpty(_azureAdOptions.ClientSecret);

        public void OnGet()
        {
            _logger.LogInformation("Setup page loaded");
        }
    }
}
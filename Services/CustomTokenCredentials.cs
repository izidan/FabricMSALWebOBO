using Azure.Core;

namespace MSALWebApp.Services
{
    public class CustomTokenCredential : TokenCredential
    {
        private readonly string _token;
        private readonly HttpClient _httpClient;

        public CustomTokenCredential(string token)
        {
            _token = token;
            _httpClient = new HttpClient();
        }

        public override AccessToken GetToken(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new AccessToken(_token, DateTimeOffset.UtcNow.AddHours(1)); // Set appropriate expiry
        }

        public override ValueTask<AccessToken> GetTokenAsync(TokenRequestContext requestContext, CancellationToken cancellationToken)
        {
            return new ValueTask<AccessToken>(GetToken(requestContext, cancellationToken));
        }
    }
}

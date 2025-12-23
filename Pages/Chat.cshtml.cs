using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Authorization;

namespace MSALWebApp.Pages
{
    [Authorize]
    public class ChatModel : PageModel
    {
        private readonly ILogger<ChatModel> _logger;

        public ChatModel(ILogger<ChatModel> logger)
        {
            _logger = logger;
        }

        public string? AgentResponse { get; set; }
        public string? ErrorMessage { get; set; }

        public void OnGet()
        {
            // Page load - no action needed
        }

        public async Task<IActionResult> OnPostAsync(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                ErrorMessage = "Please enter a message.";
                return Page();
            }

            try
            {
                _logger.LogInformation($"User {User.Identity?.Name} sending message");
                
                // Simple echo response since external service is removed
                AgentResponse = $@"{{
    ""id"": ""echo-response-{DateTime.Now.Ticks}"",
    ""object"": ""chat.completion"",
    ""created"": {DateTimeOffset.UtcNow.ToUnixTimeSeconds()},
    ""model"": ""echo-service"",
    ""choices"": [
        {{
            ""index"": 0,
            ""message"": {{
                ""role"": ""assistant"",
                ""content"": ""Echo: {message}""
            }},
            ""finish_reason"": ""stop""
        }}
    ],
    ""usage"": {{
        ""prompt_tokens"": {message.Length},
        ""completion_tokens"": {message.Length + 6},
        ""total_tokens"": {(message.Length * 2) + 6}
    }}
}}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing request");
                ErrorMessage = $"An error occurred: {ex.Message}";
            }

            return Page();
        }
    }
}
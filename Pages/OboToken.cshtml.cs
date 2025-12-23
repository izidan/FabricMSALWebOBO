using Azure;
//using Azure.AI.Agents.Persistent;
//using Azure.AI.Projects;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Options;
using Microsoft.Identity.Web;
using MSALWebApp.Services;
using Azure.AI.OpenAI.Assistants;

namespace MSALWebApp.Pages
{
    [Authorize]
    public class OboTokenModel : PageModel
    {
        private readonly ITokenAcquisition _tokenAcquisition;
        private readonly IOboTokenService _oboTokenService;
        private readonly ILogger<OboTokenModel> _logger;
        private readonly OboTokenOptions _oboOptions;

        public OboTokenModel(
            ITokenAcquisition tokenAcquisition,
            IOboTokenService oboTokenService,
            ILogger<OboTokenModel> logger,
            IOptions<OboTokenOptions> oboOptions)
        {
            _tokenAcquisition = tokenAcquisition;
            _oboTokenService = oboTokenService;
            _logger = logger;
            _oboOptions = oboOptions.Value;
        }

        public string? CurrentUserToken { get; set; }
        public string? OboToken { get; set; }
        public string? ErrorMessage { get; set; }

        [BindProperty]
        public string UserInput { get; set; } = string.Empty;

        public string? AgentResponse { get; set; }

        public async Task OnGetAsync()
        {
            try
            {
                // Get the current user's access token with the correct scope for OBO
                CurrentUserToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] {
                    _oboOptions.SourceScope
                });

                _logger.LogInformation("Successfully acquired current user token for scope: {Scope}", _oboOptions.SourceScope);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current user token for scope: {Scope}", _oboOptions.SourceScope);
                ErrorMessage = $"Failed to get current user token: {ex.Message}";
            }
        }

        public async Task<IActionResult> OnPostGenerateOboTokenAsync()
        {
            try
            {
                // First get the current user token with the correct scope for OBO
                var currentUserToken = await _tokenAcquisition.GetAccessTokenForUserAsync(new[] {
                    _oboOptions.SourceScope
                });

                CurrentUserToken = currentUserToken;
                _logger.LogInformation("Successfully acquired source token for scope: {Scope}", _oboOptions.SourceScope);

                // Generate OBO token for the target application
                OboToken = await _oboTokenService.GetAccessTokenOnBehalfOfUserAsync(currentUserToken);

                // Use the user input if provided, otherwise use default message
                var inputMessage = string.IsNullOrWhiteSpace(UserInput)
                    ? "Give me number of buildings per tenant."
                    : UserInput;

                AgentResponse = await RunAgentWithOBOAuth(inputMessage, OboToken).ConfigureAwait(false);
                _logger.LogInformation("Successfully generated OBO token for target scope: {Scope}", _oboOptions.TargetScope);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to generate OBO token");
                ErrorMessage = $"Failed to generate OBO token: {ex.Message}";
            }

            return Page();
        }

        public async Task<string> RunAgentWithOBOAuth(string userInput, string oboToken = "")
        {
            if (string.IsNullOrEmpty(_oboOptions.ProjectEndpoint))
            {
                throw new InvalidOperationException("ProjectEndpoint is not configured in appsettings.json");
            }

            var projectEndpoint = new Uri(_oboOptions.ProjectEndpoint);

            try
            {
                Console.WriteLine("Attempting OBO flow with user token");

                if (string.IsNullOrEmpty(oboToken))
                {
                    throw new InvalidOperationException("Failed to obtain OBO token");
                }

                var agent = new FabricDataAgentClient(_oboOptions.ProjectEndpoint, oboToken);
                return await agent.AskAsync(userInput);

                // Create AI Assistants client with OBO token
                //AssistantsClient client = new(projectEndpoint, new CustomTokenCredential(oboToken));

                // Create AI Project client with OBO token
                //AIProjectClient client = new(projectEndpoint, new CustomTokenCredential(oboToken));
                //return await ExecuteAgentConversation(client, userInput);

            }
            catch (Exception ex)
            {
                Console.WriteLine($"General Error in RunAgentConversation: {ex.Message}");
                return ex.Message;
                //return $"I encountered an issue processing your request. Please try again later.";
            }
        }
        /*
            private async Task<string> ExecuteAgentConversation(AssistantsClient client, string userInput)
            {
                var response = "";

                //Response<Assistant> assistantResponse = await client.CreateAssistantAsync(new AssistantCreationOptions("not used"));
                Response<Assistant> assistantResponse = await client.GetAssistantAsync(_oboOptions.AgentId);
                Assistant assistant = assistantResponse.Value;
                Console.WriteLine($"Created assistant, ID: {assistant.Id}");

                Response<AssistantThread> threadResponse = await client.CreateThreadAsync();
                AssistantThread thread = threadResponse.Value;
                Console.WriteLine($"Created thread, ID: {thread.Id}");

                Response<ThreadMessage> messageResponse = await client.CreateMessageAsync(
                    thread.Id,
                    MessageRole.User,
                    userInput);
                ThreadMessage message = messageResponse.Value;
                Console.WriteLine($"Created message, ID: {message.Id}");

                Response<ThreadRun> runResponse = await client.CreateRunAsync(
                    thread.Id,
                    new CreateRunOptions(assistant.Id));
                ThreadRun run = runResponse.Value;
                Console.WriteLine($"Created run, ID: {run.Id}");

                // Poll until the run reaches a terminal status
                do
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(500));
                    runResponse = await client.GetRunAsync(thread.Id, run.Id);
                }
                while (run.Status == RunStatus.Queued
                  || run.Status == RunStatus.InProgress);


                if (run.Status != RunStatus.Completed)
                {
                    throw new InvalidOperationException($"Run failed or was canceled: {run.LastError?.Message}");
                }

                Response<PageableList<ThreadMessage>> afterRunMessagesResponse = await client.GetMessagesAsync(thread.Id);
                IReadOnlyList<ThreadMessage> messages = afterRunMessagesResponse.Value.Data;

                // Display messages
                foreach (ThreadMessage threadMessage in messages)
                {
                    Console.Write($"{threadMessage.CreatedAt:yyyy-MM-dd HH:mm:ss} - {threadMessage.Role,10}: ");
                    foreach (MessageContent contentItem in threadMessage.ContentItems)
                    {
                        if (contentItem is MessageTextContent textItem)
                        {
                            if (threadMessage.Role == MessageRole.Assistant)
                            {
                                response += textItem.Text + "\n";
                            }
                            Console.Write(textItem.Text);
                        }
                        else if (contentItem is MessageImageFileContent imageFileItem)
                        {
                            if (threadMessage.Role == MessageRole.Assistant)
                            {
                                response += $"<image from ID: {imageFileItem.FileId}\n";
                            }
                            Console.Write($"<image from ID: {imageFileItem.FileId}");
                        }
                    }
                }
                return response;
            }
            */
    }
}
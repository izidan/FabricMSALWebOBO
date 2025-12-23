//Here is the main FabricDataAgent code.   
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public class FabricDataAgentClient
{
    private readonly HttpClient _httpClient;
    private readonly string _apiVersion = "2024-05-01-preview";
    public FabricDataAgentClient(string agentUrl, string bearerToken)
    {

        _httpClient = new HttpClient();
        _httpClient.BaseAddress = new Uri(agentUrl);
        _httpClient.Timeout = TimeSpan.FromMinutes(5);
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FabricAgent/1.0");
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);
    }

    public async Task<string> AskAsync(string question)
    {
        // Create assistant
        var assistant = await CreateAssistantAsync();
        Console.WriteLine($"Created assistant: {assistant.Id}");

        var thread = await CreateThreadAsync();
        Console.WriteLine($"Created thread: {thread.Id}");

        // Create message on thread
        var message = await CreateMessageAsync(thread.Id, "user", question);
        Console.WriteLine($"Created message: {message.Id}");

        // Create run
        var run = await CreateRunAsync(thread.Id, assistant.Id);
        Console.WriteLine($"Created run: {run.Id}");

        // Wait for run to complete
        while (run.Status == "queued" || run.Status == "in_progress")
        {
            run = await RetrieveRunAsync(thread.Id, run.Id);
            Console.WriteLine($"Run status: {run.Status}");
            await Task.Delay(2000); // Wait 2 seconds
        }

        // Print messages
        var messages = await ListMessagesAsync(thread.Id, "desc");
        //PrettyPrint(messages);

        await DeleteThreadAsync(thread.Id);
        return messages.FirstOrDefault().Content.FirstOrDefault().Text.Value;

    }

    public async Task<Assistant> CreateAssistantAsync()
    {
        var requestBody = new { model = "not used" };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"assistants?api-version={_apiVersion}", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var assistant = JsonSerializer.Deserialize<Assistant>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return assistant;
    }

    public async Task<Thread> CreateThreadAsync()
    {
        var response = await _httpClient.PostAsync($"threads?api-version={_apiVersion}",
            new StringContent("{}", Encoding.UTF8, "application/json"));
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var thread = JsonSerializer.Deserialize<Thread>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return thread;
    }

    public async Task<Message> CreateMessageAsync(string threadId, string role, string content)
    {
        var requestBody = new { role, content };
        var json = JsonSerializer.Serialize(requestBody);
        var httpContent = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"threads/{threadId}/messages?api-version={_apiVersion}", httpContent);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var messageResponse = JsonSerializer.Deserialize<MessageResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return new Message { Id = messageResponse.Id, Role = role, Content = content };
    }

    public async Task<Run> CreateRunAsync(string threadId, string assistantId)
    {
        var requestBody = new { assistant_id = assistantId };
        var json = JsonSerializer.Serialize(requestBody);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync($"threads/{threadId}/runs?api-version={_apiVersion}", content);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var run = JsonSerializer.Deserialize<Run>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return run;
    }

    public async Task<Run> RetrieveRunAsync(string threadId, string runId)
    {
        var response = await _httpClient.GetAsync($"threads/{threadId}/runs/{runId}?api-version={_apiVersion}");
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var run = JsonSerializer.Deserialize<Run>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return run;
    }

    public async Task<List<MessageResponse>> ListMessagesAsync(string threadId, string order = "desc")
    {
        var response = await _httpClient.GetAsync($"threads/{threadId}/messages?order={order}&api-version={_apiVersion}");
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        var messagesResponse = JsonSerializer.Deserialize<MessagesListResponse>(responseContent, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        return messagesResponse.Data;
    }

    public async Task DeleteThreadAsync(string threadId)
    {
        try
        {
            var response = await _httpClient.DeleteAsync($"threads/{threadId}?api-version={_apiVersion}");
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error deleting thread: {threadId}", ex.Message);
        }
    }
}

// Data models
public class Assistant
{
    public string Id { get; set; }
}
public class Thread
{
    public string Id { get; set; }
}
public class Message
{
    public string Id { get; set; }
    public string Role { get; set; }
    public string Content { get; set; }
}

public class Run
{
    public string Id { get; set; }
    public string Status { get; set; }
}


public class MessageResponse
{
    public string Id { get; set; }
    public string Role { get; set; }
    public List<ContentBlock> Content { get; set; }
}
public class ContentBlock
{
    public string Type { get; set; }
    public TextContent Text { get; set; }
}
public class TextContent
{
    public string Value { get; set; }
}

public class MessagesListResponse
{
    public List<MessageResponse> Data { get; set; }
}

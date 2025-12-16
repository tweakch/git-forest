using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Llm;

/// <summary>
/// Minimal OpenAI-compatible chat client using the /chat/completions endpoint.
/// Works with OpenAI and OpenAI-compatible servers (e.g. Ollama with an OpenAI API shim).
/// </summary>
public sealed class OpenAiCompatibleAgentChatClient : IAgentChatClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;
    private readonly string _apiKeyEnvVar;
    private readonly string _defaultModel;
    private readonly double _defaultTemperature;

    public OpenAiCompatibleAgentChatClient(
        HttpClient httpClient,
        string baseUrl,
        string apiKeyEnvVar,
        string defaultModel,
        double defaultTemperature)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = string.IsNullOrWhiteSpace(baseUrl) ? throw new ArgumentException("Base URL must be provided.", nameof(baseUrl)) : baseUrl.Trim().TrimEnd('/');
        _apiKeyEnvVar = string.IsNullOrWhiteSpace(apiKeyEnvVar) ? "OPENAI_API_KEY" : apiKeyEnvVar.Trim();
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "gpt-4o-mini" : defaultModel.Trim();
        _defaultTemperature = defaultTemperature;
    }

    public async Task<AgentChatResponse> ChatAsync(AgentChatRequest request, CancellationToken cancellationToken = default)
    {
        if (request is null) throw new ArgumentNullException(nameof(request));

        var model = string.IsNullOrWhiteSpace(request.Model) ? _defaultModel : request.Model.Trim();
        var temperature = request.Temperature ?? _defaultTemperature;

        using var message = new HttpRequestMessage(HttpMethod.Post, $"{_baseUrl}/chat/completions");
        var apiKey = Environment.GetEnvironmentVariable(_apiKeyEnvVar) ?? string.Empty;
        if (!string.IsNullOrWhiteSpace(apiKey))
        {
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
        }

        var payload = new
        {
            model,
            temperature,
            messages = new[]
            {
                new { role = "system", content = request.SystemPrompt ?? string.Empty },
                new { role = "user", content = request.UserPrompt ?? string.Empty }
            }
        };

        message.Content = new StringContent(
            JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException($"LLM request failed ({(int)response.StatusCode}): {body}");
        }

        // Try to extract choices[0].message.content
        try
        {
            using var doc = JsonDocument.Parse(body);
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var choices) &&
                choices.ValueKind == JsonValueKind.Array &&
                choices.GetArrayLength() > 0)
            {
                var first = choices[0];
                if (first.TryGetProperty("message", out var msg) &&
                    msg.TryGetProperty("content", out var content))
                {
                    var text = content.GetString() ?? string.Empty;
                    return new AgentChatResponse(RawContent: text, Json: null);
                }
            }
        }
        catch
        {
            // fall back to returning the raw body
        }

        return new AgentChatResponse(RawContent: body, Json: null);
    }
}


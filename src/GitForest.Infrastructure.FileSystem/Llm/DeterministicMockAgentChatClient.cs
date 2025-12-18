using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GitForest.Core.Services;

namespace GitForest.Infrastructure.FileSystem.Llm;

/// <summary>
/// Offline deterministic mock implementation for agent chat calls.
/// Produces stable output based on the request contents so tests can run without network access.
/// </summary>
public sealed class DeterministicMockAgentChatClient : IAgentChatClient
{
    private readonly string _defaultModel;
    private readonly double _defaultTemperature;

    public DeterministicMockAgentChatClient(string defaultModel, double defaultTemperature)
    {
        _defaultModel = string.IsNullOrWhiteSpace(defaultModel) ? "mock" : defaultModel.Trim();
        _defaultTemperature = defaultTemperature;
    }

    public Task<AgentChatResponse> ChatAsync(
        AgentChatRequest request,
        CancellationToken cancellationToken = default
    )
    {
        _ = cancellationToken;
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        // Deterministically hash key request fields.
        var fingerprint =
            $"{request.AgentId}\n{request.SystemPrompt}\n{request.UserPrompt}\n{request.Model ?? _defaultModel}\n{request.Temperature ?? _defaultTemperature}";
        var bytes = Encoding.UTF8.GetBytes(fingerprint);
        var hash = SHA256.HashData(bytes);
        var hex = Convert.ToHexString(hash).ToLowerInvariant();
        var shortId = hex.Length >= 8 ? hex[..8] : hex;

        // Default mock response is an empty set, but includes metadata for debugging.
        var payload = new
        {
            desiredPlants = Array.Empty<object>(),
            summary = $"mock:{request.AgentId}:{shortId}",
            metadata = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["provider"] = "mock",
                ["model"] = request.Model ?? _defaultModel,
                ["temperature"] = (request.Temperature ?? _defaultTemperature).ToString(
                    "0.###",
                    System.Globalization.CultureInfo.InvariantCulture
                ),
                ["fingerprint"] = shortId,
            },
        };

        var json = JsonSerializer.Serialize(
            payload,
            new JsonSerializerOptions(JsonSerializerDefaults.Web)
        );
        return Task.FromResult(new AgentChatResponse(RawContent: json, Json: json));
    }
}


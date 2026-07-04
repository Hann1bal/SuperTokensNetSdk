using System.Text.Json.Serialization;

namespace SuperTokensSDK.Net.Mcp;

/// <summary>
/// Definition of an MCP tool exposed by the SuperTokens gateway.
/// </summary>
public class McpToolDefinition
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("description")]
    public string Description { get; set; } = "";

    [JsonPropertyName("inputSchema")]
    public Dictionary<string, object> InputSchema { get; set; } = new();
}

/// <summary>
/// An MCP tool call request.
/// </summary>
public class McpToolRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "";

    [JsonPropertyName("arguments")]
    public Dictionary<string, object>? Arguments { get; set; }
}

/// <summary>
/// Result of an MCP tool call.
/// </summary>
public class McpToolResult
{
    [JsonPropertyName("content")]
    public List<McpToolContent> Content { get; set; } = new();

    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}

public class McpToolContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";

    [JsonPropertyName("text")]
    public string Text { get; set; } = "";
}

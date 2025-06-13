using System;

public class AppSettings
{
    public string Secret { get; set; } = string.Empty;
    public string GeminiApiKey { get; set; } = string.Empty;
    public string GeminiApiEndpoint { get; set; } = string.Empty;
    public bool EnableAISearch { get; set; } = true; // Default to true for backward compatibility
} 
using System.ClientModel;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using OpenAI;
using OpenAI.Chat;

namespace AiResxTranslator.Services;

public class TranslationService
{
    private readonly string _apiKey;
    private readonly string _model;

    public TranslationService(string apiKey, string model)
    {
        _apiKey = apiKey;
        _model = model;
    }

    public async Task<Dictionary<string, string>> TranslateBatchAsync(
        Dictionary<string, string> keysAndValues,
        string targetCultureCode,
        CancellationToken cancellationToken = default)
    {
        if (keysAndValues.Count == 0)
            return [];

        string languageName;
        try
        {
            var culture = CultureInfo.GetCultureInfo(targetCultureCode);
            languageName = culture.EnglishName;
        }
        catch
        {
            languageName = targetCultureCode;
        }

        var client = new ChatClient(_model, new ApiKeyCredential(_apiKey));

        var jsonEntries = JsonSerializer.Serialize(keysAndValues);

        var systemPrompt = """
            You are a professional translator for software localization. 
            You translate .resx resource string values from English to other languages.
            You must preserve any format placeholders like {0}, {1}, etc.
            You must preserve any escape sequences.
            You must not translate or modify the keys, only the values.
            Return ONLY a valid JSON object with the same keys and translated values.
            Do not include any explanation or markdown formatting.
            """;

        var userPrompt = $"""
            Translate the following resource strings to {languageName} ({targetCultureCode}).
            Return a JSON object with the same keys and the translated values.

            {jsonEntries}
            """;

        var messages = new List<ChatMessage>
        {
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userPrompt)
        };

        var options = new ChatCompletionOptions
        {
            Temperature = 0.3f
        };

        var response = await client.CompleteChatAsync(messages, options, cancellationToken);
        var content = response.Value.Content[0].Text.Trim();

        // Strip markdown code fences if present
        if (content.StartsWith("```"))
        {
            var firstNewLine = content.IndexOf('\n');
            if (firstNewLine > 0)
                content = content[(firstNewLine + 1)..];
            if (content.EndsWith("```"))
                content = content[..^3];
            content = content.Trim();
        }

        var result = JsonSerializer.Deserialize<Dictionary<string, string>>(content);
        return result ?? [];
    }
}

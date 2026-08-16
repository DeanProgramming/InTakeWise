using OpenAI.Chat;

namespace InTakeWise.Services;

public interface IOpenAiChatClientProvider
{
    ChatClient GetClient(AiOperation operation);
}

public sealed class OpenAiChatClientProvider : IOpenAiChatClientProvider
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<OpenAiChatClientProvider> _logger;
    private readonly Lazy<ChatClient> _logClient;
    private readonly Lazy<ChatClient> _shoppingClient;
    private readonly Lazy<ChatClient> _receiptClient;

    public OpenAiChatClientProvider(IConfiguration configuration, ILogger<OpenAiChatClientProvider> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _logClient = CreateLazyClient("OpenAI:LoggingModel", "gpt-5.1", "log analysis");

        _shoppingClient = CreateLazyClient("OpenAI:ShoppingModel", "gpt-5.1", "shopping-plan generation");

        _receiptClient = CreateLazyClient("OpenAI:ReceiptModel", configuration["OpenAI:LoggingModel"] ?? "gpt-5.1", "receipt analysis");
    }

    public ChatClient GetClient(AiOperation operation) =>
        operation switch
        {
            AiOperation.MealAnalysis or AiOperation.WorkoutAnalysis => _logClient.Value,
            AiOperation.ShoppingPlan => _shoppingClient.Value,
            AiOperation.ReceiptAnalysis => _receiptClient.Value,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    private Lazy<ChatClient> CreateLazyClient(string modelConfigurationKey, string fallbackModel, string purpose)
    {
        return new Lazy<ChatClient>(
            () =>
            {
                var apiKey =_configuration["OpenAI:ApiKey"] ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");

                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogError("OpenAI is not configured for {Purpose}.", purpose);

                    throw new AiServiceUnavailableException("AI generation is temporarily unavailable.");
                }

                var model = _configuration[modelConfigurationKey] ?? fallbackModel;

                return new ChatClient(model: model, apiKey: apiKey);
            },
            LazyThreadSafetyMode.ExecutionAndPublication);
    }
}
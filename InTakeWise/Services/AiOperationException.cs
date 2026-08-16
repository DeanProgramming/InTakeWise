using System.Security.Cryptography;
using System.Text;

namespace InTakeWise.Services;

public abstract class AiOperationException : InvalidOperationException
{
    protected AiOperationException(string userMessage) : base(userMessage)
    {
        UserMessage = userMessage;
    }

    protected AiOperationException(string userMessage, Exception innerException) : base(userMessage, innerException)
    {
        UserMessage = userMessage;
    }

    public string UserMessage { get; }
}

public sealed class AiInputValidationException : AiOperationException
{
    public AiInputValidationException(string userMessage) : base(userMessage)
    {
    }
}

public sealed class AiRequestLimitException : AiOperationException
{
    public AiRequestLimitException(string userMessage, TimeSpan retryAfter, bool dailyQuotaExceeded) : base(userMessage)
    {
        RetryAfter = retryAfter;
        DailyQuotaExceeded = dailyQuotaExceeded;
    }

    public TimeSpan RetryAfter { get; }
    public bool DailyQuotaExceeded { get; }
}

public sealed class AiRequestTimeoutException : AiOperationException
{
    public AiRequestTimeoutException(string userMessage, Exception innerException) : base(userMessage, innerException)
    {
    }
}

public sealed class AiResponseValidationException : AiOperationException
{
    public AiResponseValidationException(string userMessage, Exception innerException) : base(userMessage, innerException)
    {
    }
}

public sealed class AiServiceUnavailableException : AiOperationException
{
    public AiServiceUnavailableException(string userMessage) : base(userMessage)
    {
    }

    public AiServiceUnavailableException(string userMessage, Exception innerException) : base(userMessage, innerException)
    {
    }
}

internal static class AiLogSanitizer
{
    public static string UserReference(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return "anonymous";
        }

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(userId));
        return Convert.ToHexString(hash.AsSpan(0, 6));
    }
}
namespace InTakeWise.Services;

public enum AiOperation
{
    MealAnalysis,
    WorkoutAnalysis,
    ShoppingPlan,
    ReceiptAnalysis
}

public sealed class AiSafetyOptions
{
    public const string SectionName = "AiSafety";

    public int LogAnalysisTimeoutSeconds { get; set; } = 25;
    public int ShoppingPlanTimeoutSeconds { get; set; } = 45;
    public int ReceiptAnalysisTimeoutSeconds { get; set; } = 30;

    public AiOperationLimitOptions MealAnalysis { get; set; } = new()
    {
        BurstPermitLimit = 4,
        BurstWindowSeconds = 60,
        DailyPermitLimit = 25
    };

    public AiOperationLimitOptions WorkoutAnalysis { get; set; } = new()
    {
        BurstPermitLimit = 4,
        BurstWindowSeconds = 60,
        DailyPermitLimit = 20
    };

    public AiOperationLimitOptions ShoppingPlan { get; set; } = new()
    {
        BurstPermitLimit = 2,
        BurstWindowSeconds = 600,
        DailyPermitLimit = 4
    };

    public AiOperationLimitOptions ReceiptAnalysis { get; set; } = new()
    {
        BurstPermitLimit = 5,
        BurstWindowSeconds = 600,
        DailyPermitLimit = 10
    };

    public TimeSpan GetTimeout(AiOperation operation) =>
        TimeSpan.FromSeconds(operation switch
        {
            AiOperation.MealAnalysis or AiOperation.WorkoutAnalysis => LogAnalysisTimeoutSeconds,
            AiOperation.ShoppingPlan => ShoppingPlanTimeoutSeconds,
            AiOperation.ReceiptAnalysis => ReceiptAnalysisTimeoutSeconds,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        });

    public AiOperationLimitOptions GetLimits(AiOperation operation) =>
        operation switch
        {
            AiOperation.MealAnalysis => MealAnalysis,
            AiOperation.WorkoutAnalysis => WorkoutAnalysis,
            AiOperation.ShoppingPlan => ShoppingPlan,
            AiOperation.ReceiptAnalysis => ReceiptAnalysis,
            _ => throw new ArgumentOutOfRangeException(nameof(operation))
        };

    public bool IsValid()
    {
        return LogAnalysisTimeoutSeconds is >= 1 and <= 300
            && ShoppingPlanTimeoutSeconds is >= 1 and <= 300
            && ReceiptAnalysisTimeoutSeconds is >= 1 and <= 300
            && MealAnalysis?.IsValid() == true
            && WorkoutAnalysis?.IsValid() == true
            && ShoppingPlan?.IsValid() == true
            && ReceiptAnalysis?.IsValid() == true;
    }
}

public sealed class AiOperationLimitOptions
{
    public int BurstPermitLimit { get; set; }
    public int BurstWindowSeconds { get; set; }
    public int DailyPermitLimit { get; set; }

    public bool IsValid() =>
        BurstPermitLimit is >= 1 and <= 1_000
        && BurstWindowSeconds is >= 1 and <= 86_400
        && DailyPermitLimit is >= 1 and <= 10_000;
}

internal static class AiOperationExtensions
{
    public static string GetDisplayName(this AiOperation operation) =>
        operation switch
        {
            AiOperation.MealAnalysis => "meal analysis",
            AiOperation.WorkoutAnalysis => "workout analysis",
            AiOperation.ShoppingPlan => "shopping-plan generation",
            AiOperation.ReceiptAnalysis => "receipt analysis",
            _ => "AI generation"
        };
}
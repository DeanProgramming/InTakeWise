namespace InTakeWise.Services;

public interface IReceiptImageAnalyzer
{
    Task<ReceiptImageAnalysis> AnalyzeAsync(
        string userId,
        byte[] imageBytes,
        string mediaType,
        CancellationToken cancellationToken = default);
}

public sealed record ReceiptImageAnalysis(
    bool IsReceipt,
    bool IsReadable,
    IReadOnlyList<ReceiptExtractedItem> Items);

public sealed record ReceiptExtractedItem(
    string Name,
    decimal Quantity,
    string Unit);

public sealed class ReceiptImageAnalysisException : InvalidOperationException
{
    public ReceiptImageAnalysisException(string message)
        : base(message)
    {
    }

    public ReceiptImageAnalysisException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

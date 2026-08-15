using Microsoft.AspNetCore.Http;

namespace InTakeWise.Services;

public static class ReceiptImageUpload
{
    public const long MaxImageBytes = 10 * 1024 * 1024;
    public const long MaxRequestBytes = MaxImageBytes + (256 * 1024);

    public static async Task<ReceiptImageUploadResult> ReadAsync(
        IFormFile? file,
        CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return ReceiptImageUploadResult.Invalid(
                "Choose a receipt photo before starting the analysis.");
        }

        if (file.Length > MaxImageBytes)
        {
            return ReceiptImageUploadResult.Invalid(
                "The receipt photo must be 10 MB or smaller.");
        }

        await using var input = file.OpenReadStream();
        using var buffer = new MemoryStream((int)file.Length);

        await input.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length == 0)
        {
            return ReceiptImageUploadResult.Invalid(
                "The selected receipt photo is empty.");
        }

        if (buffer.Length > MaxImageBytes)
        {
            return ReceiptImageUploadResult.Invalid(
                "The receipt photo must be 10 MB or smaller.");
        }

        var bytes = buffer.ToArray();
        var mediaType = DetectMediaType(bytes);

        if (mediaType is null)
        {
            return ReceiptImageUploadResult.Invalid(
                "Use a JPG, PNG, or WebP receipt photo.");
        }

        return ReceiptImageUploadResult.Valid(bytes, mediaType);
    }

    private static string? DetectMediaType(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length >= 3 &&
            bytes[0] == 0xFF &&
            bytes[1] == 0xD8 &&
            bytes[2] == 0xFF)
        {
            return "image/jpeg";
        }

        if (bytes.Length >= 8 &&
            bytes[0] == 0x89 &&
            bytes[1] == 0x50 &&
            bytes[2] == 0x4E &&
            bytes[3] == 0x47 &&
            bytes[4] == 0x0D &&
            bytes[5] == 0x0A &&
            bytes[6] == 0x1A &&
            bytes[7] == 0x0A)
        {
            return "image/png";
        }

        if (bytes.Length >= 12 &&
            bytes[0] == 0x52 &&
            bytes[1] == 0x49 &&
            bytes[2] == 0x46 &&
            bytes[3] == 0x46 &&
            bytes[8] == 0x57 &&
            bytes[9] == 0x45 &&
            bytes[10] == 0x42 &&
            bytes[11] == 0x50)
        {
            return "image/webp";
        }

        return null;
    }
}

public sealed record ReceiptImageUploadResult(
    byte[]? Bytes,
    string? MediaType,
    string? Error)
{
    public bool IsValid =>
        Bytes is { Length: > 0 } &&
        !string.IsNullOrWhiteSpace(MediaType) &&
        string.IsNullOrWhiteSpace(Error);

    public static ReceiptImageUploadResult Valid(byte[] bytes, string mediaType)
        => new(bytes, mediaType, null);

    public static ReceiptImageUploadResult Invalid(string error)
        => new(null, null, error);
}

using InTakeWise.Services;
using Microsoft.AspNetCore.Http;

namespace InTakeWise.Tests.Unit;

public sealed class ReceiptImageUploadTests
{
    public static TheoryData<byte[], string> SupportedImages => new()
    {
        { [0xFF, 0xD8, 0xFF, 0x00], "image/jpeg" },
        { [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A], "image/png" },
        { [0x52, 0x49, 0x46, 0x46, 0x01, 0x02, 0x03, 0x04, 0x57, 0x45, 0x42, 0x50], "image/webp" }
    };

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ReadAsync_RejectsMissingOrEmptyFile(bool missing)
    {
        var result = await ReceiptImageUpload.ReadAsync(
            missing ? null : File([]));

        Assert.False(result.IsValid);
        Assert.Contains("Choose a receipt photo", result.Error);
    }

    [Theory]
    [MemberData(nameof(SupportedImages))]
    public async Task ReadAsync_DetectsSupportedFormatFromMagicBytes(
        byte[] bytes,
        string expectedMediaType)
    {
        var result = await ReceiptImageUpload.ReadAsync(File(bytes));

        Assert.True(result.IsValid);
        Assert.Equal(expectedMediaType, result.MediaType);
        Assert.Equal(bytes, result.Bytes);
    }

    [Theory]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38 })]
    [InlineData(new byte[] { 0xFF, 0xD8 })]
    public async Task ReadAsync_RejectsUnsupportedOrIncompleteSignature(byte[] bytes)
    {
        var result = await ReceiptImageUpload.ReadAsync(File(bytes));

        Assert.False(result.IsValid);
        Assert.Contains("JPG, PNG, or WebP", result.Error);
    }

    [Fact]
    public async Task ReadAsync_RejectsDeclaredFileAboveLimitWithoutReadingStream()
    {
        var file = new TestFormFile(
            declaredLength: ReceiptImageUpload.MaxImageBytes + 1);

        var result = await ReceiptImageUpload.ReadAsync(file);

        Assert.False(result.IsValid);
        Assert.Contains("10 MB or smaller", result.Error);
    }

    private static FormFile File(
        byte[] bytes,
        string contentType = "application/octet-stream")
    {
        var stream = new MemoryStream(bytes);
        return new FormFile(stream, 0, bytes.Length, "receiptPhoto", "receipt.bin")
        {
            Headers = new HeaderDictionary(),
            ContentType = contentType
        };
    }

    private sealed class TestFormFile(long declaredLength) : IFormFile
    {
        public string ContentType => "application/octet-stream";
        public string ContentDisposition => "";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length => declaredLength;
        public string Name => "receiptPhoto";
        public string FileName => "receipt.bin";

        public void CopyTo(Stream target) =>
            throw new InvalidOperationException("The stream should not be opened.");

        public Task CopyToAsync(
            Stream target,
            CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("The stream should not be opened.");

        public Stream OpenReadStream() =>
            throw new InvalidOperationException("The stream should not be opened.");
    }
}

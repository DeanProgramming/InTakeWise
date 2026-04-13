namespace InTakeWise.Services
{
    public static class FoodItemNameNormalizer
    {
        public static string NormalizeName(string? value)
        {
            return (value ?? "").Trim().ToLowerInvariant();
        }

        public static string CleanDisplayName(string? value)
        {
            return (value ?? "").Trim();
        }
    }
}
using InTakeWise.ViewModels;

namespace InTakeWise.Services
{
    public sealed class PantryUnitService : IPantryUnitService
    {
        private sealed record UnitInfo(string NormalizedUnit, string Family, decimal FactorToBase);

        public PantryMergeResult MergeRowsByFoodName(IEnumerable<PantryItemInputViewModel> rows)
        {
            var result = new PantryMergeResult();

            var validRows = rows
                .Where(x => !string.IsNullOrWhiteSpace(x.Name))
                .ToList();

            foreach (var group in validRows.GroupBy(
                x => x.Name.Trim(),
                StringComparer.OrdinalIgnoreCase))
            {
                UnitInfo? firstUnit = null;
                decimal totalBaseQuantity = 0m;
                bool incompatible = false;

                foreach (var row in group)
                {
                    var parsedUnit = ParseUnit(row.Unit);
                    var qty = row.Quantity ?? 0m;

                    if (firstUnit == null)
                    {
                        firstUnit = parsedUnit;
                    }
                    else if (!string.Equals(firstUnit.Family, parsedUnit.Family, StringComparison.OrdinalIgnoreCase))
                    {
                        result.Errors.Add(
                            $"'{group.Key}' has incompatible units ('{firstUnit.NormalizedUnit}' and '{parsedUnit.NormalizedUnit}').");
                        incompatible = true;
                        break;
                    }

                    totalBaseQuantity += qty * parsedUnit.FactorToBase;
                }

                if (incompatible || firstUnit == null)
                    continue;

                var converted = ConvertFromBase(totalBaseQuantity, firstUnit.Family, firstUnit.NormalizedUnit);

                result.Rows.Add(new PantryItemInputViewModel
                {
                    Id = group.Where(x => x.Id > 0).Select(x => x.Id).FirstOrDefault(),
                    FoodItemId = group.Where(x => x.FoodItemId > 0).Select(x => x.FoodItemId).FirstOrDefault(),
                    Name = group.First().Name.Trim(),
                    Quantity = converted.Quantity,
                    Unit = converted.Unit
                });
            }

            return result;
        }

        public PantryQuantityMergeResult MergeWithExisting(
            string itemName,
            decimal incomingQuantity,
            string incomingUnit,
            IEnumerable<PantryQuantityItem> existingItems)
        {
            var incomingParsed = ParseUnit(incomingUnit);
            decimal totalBase = incomingQuantity * incomingParsed.FactorToBase;

            foreach (var existing in existingItems)
            {
                var existingParsed = ParseUnit(existing.Unit);

                if (!string.Equals(existingParsed.Family, incomingParsed.Family, StringComparison.OrdinalIgnoreCase))
                {
                    return new PantryQuantityMergeResult
                    {
                        Errors = new List<string>
                        {
                            $"Cannot combine pantry item '{itemName}' because units are incompatible ('{existing.Unit}' and '{incomingUnit}')."
                        }
                    };
                }

                totalBase += existing.Quantity * existingParsed.FactorToBase;
            }

            var converted = ConvertFromBase(totalBase, incomingParsed.Family, incomingParsed.NormalizedUnit);

            return new PantryQuantityMergeResult
            {
                Quantity = converted.Quantity,
                Unit = converted.Unit
            };
        }

        private static UnitInfo ParseUnit(string? rawUnit)
        {
            var unit = (rawUnit ?? "").Trim().ToLowerInvariant();

            return unit switch
            {
                "mg" or "milligram" or "milligrams"
                    => new UnitInfo("mg", "mass", 0.001m),

                "g" or "gram" or "grams"
                    => new UnitInfo("g", "mass", 1m),

                "kg" or "kilogram" or "kilograms"
                    => new UnitInfo("kg", "mass", 1000m),

                "ml" or "millilitre" or "millilitres" or "milliliter" or "milliliters"
                    => new UnitInfo("ml", "volume", 1m),

                "l" or "litre" or "litres" or "liter" or "liters"
                    => new UnitInfo("l", "volume", 1000m),

                "pc" or "pcs" or "piece" or "pieces" or "item" or "items" or "unit" or "units"
                    => new UnitInfo("items", "count:items", 1m),

                "tin" or "tins"
                    => new UnitInfo("tins", "count:tins", 1m),

                "can" or "cans"
                    => new UnitInfo("cans", "count:cans", 1m),

                "pack" or "packs"
                    => new UnitInfo("packs", "count:packs", 1m),

                "bottle" or "bottles"
                    => new UnitInfo("bottles", "count:bottles", 1m),

                _ => new UnitInfo(unit, $"custom:{unit}", 1m)
            };
        }

        private static (decimal Quantity, string Unit) ConvertFromBase(decimal totalBase, string family, string fallbackUnit)
        {
            if (family == "mass")
            {
                if (totalBase >= 1000m)
                    return (decimal.Round(totalBase / 1000m, 3), "kg");

                if (totalBase >= 1m)
                    return (decimal.Round(totalBase, 3), "g");

                return (decimal.Round(totalBase * 1000m, 3), "mg");
            }

            if (family == "volume")
            {
                if (totalBase >= 1000m)
                    return (decimal.Round(totalBase / 1000m, 3), "l");

                return (decimal.Round(totalBase, 3), "ml");
            }

            return (decimal.Round(totalBase, 3), fallbackUnit);
        }
    }
}
namespace Api.Models;

public static class GameScoring
{
    public static ScoreResult Calculate(IReadOnlyCollection<SubmitGameItemRequest> items, IReadOnlyCollection<Product> products, int budgetColones)
    {
        var productById = products.ToDictionary(product => product.Id);
        var expandedItems = items
            .Select(item => new { Product = productById[item.ProductId], Quantity = Math.Max(1, item.Quantity) })
            .ToList();

        var spent = expandedItems.Sum(item => item.Product.PriceColones * item.Quantity);
        var healthTotal = expandedItems.Sum(item => item.Product.Health * item.Quantity);
        var chemicalTotal = expandedItems.Sum(item => item.Product.Chemicals * item.Quantity);
        var totalServings = expandedItems.Sum(item => item.Quantity);
        var averageHealth = totalServings == 0 ? 0 : (int)Math.Round(healthTotal / (decimal)totalServings);
        var averageChemicals = totalServings == 0 ? 0 : (int)Math.Round(chemicalTotal / (decimal)totalServings);
        var remaining = budgetColones - spent;

        var categoryCounts = expandedItems
            .GroupBy(item => item.Product.Category)
            .ToDictionary(group => group.Key, group => group.Sum(item => item.Quantity));

        var balance = CalculateBalance(categoryCounts, totalServings);
        var fullness = Math.Min(20, totalServings * 3);
        var score = Math.Clamp((averageHealth * 8) - (averageChemicals * 5) + balance + fullness, 0, 100);
        var coins = score switch
        {
            >= 85 => 1500,
            >= 70 => 1000,
            >= 55 => 600,
            >= 40 => 300,
            _ => 0
        };

        return new ScoreResult(
            spent <= budgetColones,
            score,
            averageHealth,
            averageChemicals,
            balance,
            spent,
            remaining,
            coins,
            MessageFor(score, averageChemicals, categoryCounts, totalServings));
    }

    private static int CalculateBalance(IReadOnlyDictionary<FoodCategory, int> counts, int totalServings)
    {
        if (totalServings == 0)
        {
            return 0;
        }

        decimal Portion(FoodCategory category) => counts.GetValueOrDefault(category) / (decimal)totalServings;

        var fruitAndVeg = Portion(FoodCategory.Fruta) + Portion(FoodCategory.Verdura);
        var cereal = Portion(FoodCategory.Cereal);
        var protein = Portion(FoodCategory.Proteina);
        var processed = Portion(FoodCategory.Procesado);
        var waterBonus = counts.ContainsKey(FoodCategory.Agua) ? 5 : 0;

        var closeness = 0;
        closeness += Math.Max(0, 12 - (int)(Math.Abs(fruitAndVeg - 0.50m) * 40));
        closeness += Math.Max(0, 8 - (int)(Math.Abs(cereal - 0.25m) * 40));
        closeness += Math.Max(0, 8 - (int)(Math.Abs(protein - 0.25m) * 40));
        closeness -= (int)(processed * 25);

        return Math.Clamp(closeness + waterBonus, -20, 35);
    }

    private static string MessageFor(int score, int averageChemicals, IReadOnlyDictionary<FoodCategory, int> counts, int totalServings)
    {
        var processedShare = totalServings == 0 ? 0 : counts.GetValueOrDefault(FoodCategory.Procesado) / (decimal)totalServings;

        if (counts.GetValueOrDefault(FoodCategory.Procesado) > counts.GetValueOrDefault(FoodCategory.Verdura) + counts.GetValueOrDefault(FoodCategory.Fruta))
        {
            return "Fue barato, pero tiene demasiados productos de paquete.";
        }

        if (averageChemicals >= 5)
        {
            return "El plato llena, pero trae muchos quimicos o procesamiento.";
        }

        if (!counts.ContainsKey(FoodCategory.Proteina))
        {
            return "Buen intento, pero falta una fuente de proteina.";
        }

        if (score >= 85 && processedShare <= 0.15m)
        {
            return "Plato excelente: balanceado, saludable y con pocos ultraprocesados.";
        }

        return score >= 60 ? "Buen plato, todavia se puede mejorar el balance." : "El plato necesita mas alimentos frescos y menos procesados.";
    }
}

public sealed record ScoreResult(
    bool CanAfford,
    int Score,
    int HealthTotal,
    int ChemicalTotal,
    int BalanceScore,
    int SpentColones,
    int RemainingColones,
    int CoinsEarnedColones,
    string Message);

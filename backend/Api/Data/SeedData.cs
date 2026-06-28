using Api.Models;
using Microsoft.EntityFrameworkCore;

namespace Api.Data;

public static class SeedData
{
    public static async Task EnsureSeededAsync(AppDbContext db)
    {
        if (await db.Products.AnyAsync())
        {
            return;
        }

        db.Products.AddRange(
            new Product { Name = "Cebolla organica", Category = FoodCategory.Verdura, Kind = ProductKind.Organico, PriceColones = 650, Health = 9, Chemicals = 1, Emoji = "CE" },
            new Product { Name = "Cebolla de supermercado", Category = FoodCategory.Verdura, Kind = ProductKind.Supermercado, PriceColones = 350, Health = 7, Chemicals = 3, Emoji = "CE" },
            new Product { Name = "Zanahoria organica", Category = FoodCategory.Verdura, Kind = ProductKind.Organico, PriceColones = 500, Health = 9, Chemicals = 1, Emoji = "ZA" },
            new Product { Name = "Lechuga", Category = FoodCategory.Verdura, Kind = ProductKind.Supermercado, PriceColones = 450, Health = 8, Chemicals = 2, Emoji = "LE" },
            new Product { Name = "Banano", Category = FoodCategory.Fruta, Kind = ProductKind.Supermercado, PriceColones = 200, Health = 8, Chemicals = 2, Emoji = "BA" },
            new Product { Name = "Manzana importada", Category = FoodCategory.Fruta, Kind = ProductKind.Supermercado, PriceColones = 700, Health = 7, Chemicals = 4, Emoji = "MA" },
            new Product { Name = "Arroz blanco", Category = FoodCategory.Cereal, Kind = ProductKind.Supermercado, PriceColones = 400, Health = 5, Chemicals = 2, Emoji = "AR" },
            new Product { Name = "Arroz integral", Category = FoodCategory.Cereal, Kind = ProductKind.Organico, PriceColones = 900, Health = 8, Chemicals = 1, Emoji = "AI" },
            new Product { Name = "Tortilla de maiz", Category = FoodCategory.Cereal, Kind = ProductKind.Supermercado, PriceColones = 300, Health = 7, Chemicals = 2, Emoji = "TO" },
            new Product { Name = "Frijoles", Category = FoodCategory.Proteina, Kind = ProductKind.Supermercado, PriceColones = 650, Health = 9, Chemicals = 1, Emoji = "FR" },
            new Product { Name = "Huevo", Category = FoodCategory.Proteina, Kind = ProductKind.Supermercado, PriceColones = 250, Health = 7, Chemicals = 2, Emoji = "HU" },
            new Product { Name = "Pollo", Category = FoodCategory.Proteina, Kind = ProductKind.Supermercado, PriceColones = 1200, Health = 7, Chemicals = 3, Emoji = "PO" },
            new Product { Name = "Atun en lata", Category = FoodCategory.Proteina, Kind = ProductKind.Paquete, PriceColones = 1100, Health = 5, Chemicals = 5, Emoji = "AT" },
            new Product { Name = "Galletas rellenas", Category = FoodCategory.Procesado, Kind = ProductKind.Paquete, PriceColones = 450, Health = 1, Chemicals = 9, Emoji = "GA" },
            new Product { Name = "Refresco gaseoso", Category = FoodCategory.Procesado, Kind = ProductKind.Paquete, PriceColones = 700, Health = 1, Chemicals = 9, Emoji = "RE" },
            new Product { Name = "Sopa instantanea", Category = FoodCategory.Procesado, Kind = ProductKind.Paquete, PriceColones = 500, Health = 2, Chemicals = 8, Emoji = "SO" },
            new Product { Name = "Agua", Category = FoodCategory.Agua, Kind = ProductKind.Supermercado, PriceColones = 300, Health = 10, Chemicals = 0, Emoji = "AG" }
        );

        await db.SaveChangesAsync();
    }
}

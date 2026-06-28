namespace Api.Models;

public sealed class Room
{
    public int Id { get; set; }
    public required string Code { get; set; }
    public required string Name { get; set; }
    public int InitialBudgetColones { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Player> Players { get; set; } = [];
}

public sealed class Player
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public int RoomId { get; set; }
    public Room Room { get; set; } = null!;
    public int CoinsColones { get; set; }
    public int CurrentBudgetColones { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<Game> Games { get; set; } = [];
}

public sealed class Product
{
    public int Id { get; set; }
    public required string Name { get; set; }
    public FoodCategory Category { get; set; }
    public ProductKind Kind { get; set; }
    public int PriceColones { get; set; }
    public int Health { get; set; }
    public int Chemicals { get; set; }
    public string Emoji { get; set; } = "AL";
    public bool IsActive { get; set; } = true;
}

public sealed class Game
{
    public int Id { get; set; }
    public int PlayerId { get; set; }
    public Player Player { get; set; } = null!;
    public int Round { get; set; }
    public int Score { get; set; }
    public int HealthTotal { get; set; }
    public int ChemicalTotal { get; set; }
    public int BalanceScore { get; set; }
    public int SpentColones { get; set; }
    public int RemainingColones { get; set; }
    public int CoinsEarnedColones { get; set; }
    public required string Message { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public List<GameItem> Items { get; set; } = [];
}

public sealed class GameItem
{
    public int Id { get; set; }
    public int GameId { get; set; }
    public Game Game { get; set; } = null!;
    public int ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public int Quantity { get; set; }
}

public enum FoodCategory
{
    Verdura,
    Fruta,
    Cereal,
    Proteina,
    Procesado,
    Agua
}

public enum ProductKind
{
    Organico,
    Supermercado,
    Paquete
}

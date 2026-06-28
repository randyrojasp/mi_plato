namespace Api.Models;

public sealed record CreateRoomRequest(string Name, int InitialBudgetColones, string? Code);
public sealed record CreatePlayerRequest(string Name, string RoomCode);
public sealed record SubmitGameRequest(int PlayerId, List<SubmitGameItemRequest> Items);
public sealed record SubmitGameItemRequest(int ProductId, int Quantity);

public sealed record RoomDto(int Id, string Code, string Name, int InitialBudgetColones, bool IsActive)
{
    public static RoomDto From(Room room) => new(room.Id, room.Code, room.Name, room.InitialBudgetColones, room.IsActive);
}

public sealed record PlayerDto(int Id, string Name, string RoomCode, int CoinsColones, int CurrentBudgetColones)
{
    public static PlayerDto From(Player player, string roomCode) => new(player.Id, player.Name, roomCode, player.CoinsColones, player.CurrentBudgetColones);
}

public sealed record ProductDto(int Id, string Name, string Category, string Kind, int PriceColones, int Health, int Chemicals, string Emoji)
{
    public static ProductDto From(Product product) => new(
        product.Id,
        product.Name,
        product.Category.ToString(),
        product.Kind.ToString(),
        product.PriceColones,
        product.Health,
        product.Chemicals,
        product.Emoji);
}

public sealed record GameResultDto(
    int GameId,
    int Round,
    int Score,
    int HealthTotal,
    int ChemicalTotal,
    int BalanceScore,
    int SpentColones,
    int RemainingColones,
    int CoinsEarnedColones,
    int NewBudgetColones,
    string Message)
{
    public static GameResultDto From(Game game, Player player) => new(
        game.Id,
        game.Round,
        game.Score,
        game.HealthTotal,
        game.ChemicalTotal,
        game.BalanceScore,
        game.SpentColones,
        game.RemainingColones,
        game.CoinsEarnedColones,
        player.CurrentBudgetColones,
        game.Message);
}

public sealed record RankingDto(int PlayerId, string Name, int CoinsColones, int LastScore, int Rounds, string LastMessage);

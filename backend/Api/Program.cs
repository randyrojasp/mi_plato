using Api.Data;
using Api.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy => policy
        .AllowAnyHeader()
        .AllowAnyMethod()
        .AllowAnyOrigin());
});

var connectionString = builder.Configuration.GetConnectionString("Default") ?? "Data Source=Data/plato.db";
Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "Data"));
builder.Services.AddDbContext<AppDbContext>(options => options.UseSqlite(connectionString));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SeedData.EnsureSeededAsync(db);
}

app.MapGet("/api/health", () => Results.Ok(new { status = "ok" }));

app.MapPost("/api/rooms", async (CreateRoomRequest request, AppDbContext db) =>
{
    var code = string.IsNullOrWhiteSpace(request.Code)
        ? RoomCode.Create()
        : request.Code.Trim().ToUpperInvariant();

    if (await db.Rooms.AnyAsync(room => room.Code == code))
    {
        return Results.Conflict(new { message = "Ya existe una sala con ese codigo." });
    }

    var room = new Room
    {
        Code = code,
        Name = string.IsNullOrWhiteSpace(request.Name) ? "Clase" : request.Name.Trim(),
        InitialBudgetColones = request.InitialBudgetColones <= 0 ? 5000 : request.InitialBudgetColones,
        IsActive = true
    };

    db.Rooms.Add(room);
    await db.SaveChangesAsync();

    return Results.Created($"/api/rooms/{room.Code}", RoomDto.From(room));
});

app.MapGet("/api/rooms/{code}", async (string code, AppDbContext db) =>
{
    var room = await db.Rooms.FirstOrDefaultAsync(room => room.Code == code.Trim().ToUpperInvariant());
    return room is null ? Results.NotFound() : Results.Ok(RoomDto.From(room));
});

app.MapPost("/api/rooms/{code}/reset", async (string code, AppDbContext db) =>
{
    var room = await db.Rooms
        .Include(room => room.Players)
        .ThenInclude(player => player.Games)
        .FirstOrDefaultAsync(room => room.Code == code.Trim().ToUpperInvariant());

    if (room is null)
    {
        return Results.NotFound();
    }

    db.Games.RemoveRange(room.Players.SelectMany(player => player.Games));
    db.Players.RemoveRange(room.Players);
    await db.SaveChangesAsync();

    return Results.NoContent();
});

app.MapGet("/api/products", async (AppDbContext db) =>
{
    var products = await db.Products
        .Where(product => product.IsActive)
        .OrderBy(product => product.Category)
        .ThenBy(product => product.PriceColones)
        .Select(product => ProductDto.From(product))
        .ToListAsync();

    return Results.Ok(products);
});

app.MapPost("/api/players", async (CreatePlayerRequest request, AppDbContext db) =>
{
    var roomCode = request.RoomCode.Trim().ToUpperInvariant();
    var room = await db.Rooms.FirstOrDefaultAsync(room => room.Code == roomCode && room.IsActive);
    if (room is null)
    {
        return Results.NotFound(new { message = "Sala no encontrada o inactiva." });
    }

    var name = request.Name.Trim();
    if (name.Length < 2)
    {
        return Results.BadRequest(new { message = "El nombre debe tener al menos 2 caracteres." });
    }

    var player = new Player
    {
        Name = name,
        RoomId = room.Id,
        CoinsColones = 0,
        CurrentBudgetColones = room.InitialBudgetColones
    };

    db.Players.Add(player);
    await db.SaveChangesAsync();

    return Results.Created($"/api/players/{player.Id}", PlayerDto.From(player, room.Code));
});

app.MapGet("/api/players/{id:int}", async (int id, AppDbContext db) =>
{
    var player = await db.Players.Include(player => player.Room).FirstOrDefaultAsync(player => player.Id == id);
    return player is null ? Results.NotFound() : Results.Ok(PlayerDto.From(player, player.Room.Code));
});

app.MapPost("/api/games/submit", async (SubmitGameRequest request, AppDbContext db) =>
{
    var player = await db.Players
        .Include(player => player.Room)
        .Include(player => player.Games)
        .FirstOrDefaultAsync(player => player.Id == request.PlayerId);

    if (player is null)
    {
        return Results.NotFound(new { message = "Jugador no encontrado." });
    }

    if (request.Items.Count == 0)
    {
        return Results.BadRequest(new { message = "Agrega al menos un alimento al plato." });
    }

    var requestedIds = request.Items.Select(item => item.ProductId).Distinct().ToList();
    var products = await db.Products.Where(product => requestedIds.Contains(product.Id)).ToListAsync();
    if (products.Count != requestedIds.Count)
    {
        return Results.BadRequest(new { message = "Uno o mas productos no existen." });
    }

    var result = GameScoring.Calculate(request.Items, products, player.Room.InitialBudgetColones);
    if (!result.CanAfford)
    {
        return Results.BadRequest(new { message = "El plato supera el presupuesto disponible." });
    }

    var game = new Game
    {
        PlayerId = player.Id,
        Round = player.Games.Count + 1,
        Score = result.Score,
        HealthTotal = result.HealthTotal,
        ChemicalTotal = result.ChemicalTotal,
        BalanceScore = result.BalanceScore,
        SpentColones = result.SpentColones,
        RemainingColones = result.RemainingColones,
        CoinsEarnedColones = result.CoinsEarnedColones,
        Message = result.Message,
        Items = request.Items.Select(item => new GameItem
        {
            ProductId = item.ProductId,
            Quantity = Math.Max(1, item.Quantity)
        }).ToList()
    };

    player.CoinsColones += result.CoinsEarnedColones;
    player.CurrentBudgetColones = player.Room.InitialBudgetColones;

    db.Games.Add(game);
    await db.SaveChangesAsync();

    return Results.Ok(GameResultDto.From(game, player));
});

app.MapGet("/api/rooms/{code}/ranking", async (string code, AppDbContext db) =>
{
    var roomCode = code.Trim().ToUpperInvariant();
    var ranking = await db.Players
        .Where(player => player.Room.Code == roomCode)
        .Select(player => new
        {
            player.Id,
            player.Name,
            player.CoinsColones,
            LastScore = player.Games.OrderByDescending(game => game.CreatedAt).Select(game => game.Score).FirstOrDefault(),
            Rounds = player.Games.Count,
            LastMessage = player.Games.OrderByDescending(game => game.CreatedAt).Select(game => game.Message).FirstOrDefault() ?? "Sin rondas"
        })
        .OrderByDescending(player => player.LastScore)
        .ThenByDescending(player => player.CoinsColones)
        .Select(player => new RankingDto(
            player.Id,
            player.Name,
            player.CoinsColones,
            player.LastScore,
            player.Rounds,
            player.LastMessage))
        .ToListAsync();

    return Results.Ok(ranking);
});

app.Run();

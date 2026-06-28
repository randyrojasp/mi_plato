namespace Api.Models;

public static class RoomCode
{
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    public static string Create()
    {
        return new string(Enumerable.Range(0, 6)
            .Select(_ => Alphabet[Random.Shared.Next(Alphabet.Length)])
            .ToArray());
    }
}

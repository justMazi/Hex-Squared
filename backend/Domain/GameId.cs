namespace Domain;

public class GameId
{
    private readonly string _id;
    private const int MaxLength = 11;

    public GameId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            throw new ArgumentException("GameID cannot be null or whitespace.");

        if (id.Length > MaxLength)
            throw new ArgumentException($"GameID cannot exceed {MaxLength} characters.");

        _id = id;
    }
    public override string ToString() => _id;

    public override bool Equals(object obj) => obj is GameId other && _id == other._id;

    public override int GetHashCode() => _id.GetHashCode();

    public static bool operator ==(GameId left, GameId right) => Equals(left, right);

    public static bool operator !=(GameId left, GameId right) => !Equals(left, right);
}

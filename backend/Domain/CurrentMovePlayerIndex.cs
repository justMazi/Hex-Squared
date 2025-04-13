namespace Domain;

public record CurrentMovePlayerIndex(int Value = 1)
{
    public CurrentMovePlayerIndex Increment()
    {
        // Return a new instance with the incremented index
        return new CurrentMovePlayerIndex((Value % 3) + 1);
    }

    public static implicit operator int(CurrentMovePlayerIndex index) => index.Value;
}
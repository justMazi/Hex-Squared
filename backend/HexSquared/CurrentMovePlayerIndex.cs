namespace HexSquared;

public struct CurrentMovePlayerIndex
{
    private int _value;

    public CurrentMovePlayerIndex(int initialValue = 1)
    {
        if (initialValue < 1 || initialValue > 3)
            throw new ArgumentOutOfRangeException(nameof(initialValue), "Initial value must be between 1 and 3.");

        this._value = initialValue;
    }

    public void Increment()
    {
        // Cycle through 1, 2, 3 using modulo
        _value = (_value % 3) + 1;
    }

    public static implicit operator int(CurrentMovePlayerIndex index)
    {
        return index._value;
    }
}
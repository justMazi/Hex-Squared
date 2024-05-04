namespace HexSquared;

public static class GameHelpers
{
    public static List<Hex> GenerateInnerHexagonCoordinates(int radius = 10)
    {
        List<Hex> coordinates = new List<Hex>();
        var index = 0;
        for (int r = -radius; r <= radius; r++)
        {
            int r1 = Math.Max(-radius, -r - radius);
            int r2 = Math.Min(radius, -r + radius);

            for (int q = r1; q <= r2; q++)
            {
                int s = -r - q;
                coordinates.Add(new Hex(r, s, q, index++));
            }
        }

        return coordinates;
    }
}
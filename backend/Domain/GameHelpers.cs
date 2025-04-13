namespace Domain;

public static class GameHelpers
{

    public static List<Hex> GenerateInnerHexagonCoordinates(int radius = 10)
    {
        radius++;
        List<Hex> coordinates = new();
        var index = 0;

        for (var r = -radius; r <= radius; r++)
        {
            var r1 = Math.Max(-radius, -r - radius);
            var r2 = Math.Min(radius, -r + radius);

            for (var q = r1; q <= r2; q++)
            {
                var s = -r - q;
                var owner = 0; // Default owner for inner tiles

                // Determine edge ownership
                var isRedEdge = Math.Abs(q) == radius ;
                var isGreenEdge = Math.Abs(r) == radius  ;
                var isBlueEdge = Math.Abs(s) == radius;

                // Check if the hex is a corner hex (on two edges)
                var isCornerHex = (isRedEdge && isGreenEdge) || 
                                  (isGreenEdge && isBlueEdge) || 
                                  (isBlueEdge && isRedEdge);

                // Assign ownership based on edge, but exclude corners
                if (!isCornerHex)
                {
                    if (isRedEdge)
                        owner = 1; // Red player
                    else if (isGreenEdge)
                        owner = 2; // Green player
                    else if (isBlueEdge)
                        owner = 3; // Blue player;
                }

                // Create hex with calculated ownership and add to list
                coordinates.Add(new Hex(r, s, q, index++, owner));
            }
        }

        return coordinates;
    }
}
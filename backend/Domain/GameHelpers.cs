namespace Domain;

public static class GameHelpers
{

    public static List<Hex> GenerateInnerHexagonCoordinates(int radius = 10)
    {
        radius++;
        List<Hex> coordinates = new();
        var index = 0;

        for (int r = -radius; r <= radius; r++)
        {
            int r1 = Math.Max(-radius, -r - radius);
            int r2 = Math.Min(radius, -r + radius);

            for (int q = r1; q <= r2; q++)
            {
                int s = -r - q;
                int owner = 0; // Default owner for inner tiles

                // Determine edge ownership
                bool isRedEdge = Math.Abs(q) == radius ;
                bool isGreenEdge = Math.Abs(r) == radius  ;
                bool isBlueEdge = Math.Abs(s) == radius;

                // Check if the hex is a corner hex (on two edges)
                bool isCornerHex = (isRedEdge && isGreenEdge) || 
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
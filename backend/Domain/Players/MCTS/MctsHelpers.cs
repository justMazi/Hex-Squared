namespace Domain.Players.MCTS;

public static class MctsHelpers
{
    public static class HexRotation
    {
        /// <summary>
        /// Rotates hex coordinates (R, S, Q) based on the given rotation step (0, 1, 2).
        /// </summary>
        public static List<Hex> RotateHexes(IReadOnlyList<Hex> hexes, int rotation)
        {
            return hexes.Select(hex =>
            {
                if (rotation == 0)
                {
                    return hex;
                }

                if(rotation == 1){
                    
                    var (newR, newS, newQ)= RotateHexCoords(hex.R, hex.S, hex.Q, 1);
                    return new Hex(newR, newS, newQ, hex.Index, hex.Owner);

                }
                if (rotation == 2)
                {
                    var (newR1, newS1, newQ1)= RotateHexCoords(hex.R, hex.S, hex.Q, 1);
                    var (newR2, newS2, newQ2)= RotateHexCoords(newR1, newS1, newQ1, 1);
                    return new Hex(newR2, newS2, newQ2, hex.Index, hex.Owner);
                }

                throw new Exception("Invalid rotation value");
            }).ToList();
        }
        
        public static (int, int, int) RotateHexCoords(int r, int s, int q, int rotation)
        {
            return (-s, -q, -r);
        }

    }


}
using System.Diagnostics;

namespace Domain.Players.MCTS;

public class MctsNode
{
    public readonly byte[,] Board = new byte[7, 7];
    public int TotalReward = 0;
    public int TotalVisits = 0;
    public readonly List<MctsNode> Children = new();
    public readonly MctsNode? Parent = null;
    public readonly (int i, int j)? CoordinatesOfMove;
    public bool IsTerminal { get; set; }
    public byte PlayerValue { get; set; }
    public MctsNode(byte[,] board, MctsNode? parent, byte playerNum, (int i, int j)? coordinatesOfMove = null)
    {
        Board = board;
        Parent = parent;
        PlayerValue = playerNum;
        CoordinatesOfMove = coordinatesOfMove;
    }
}




public class MctsPlayer(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        Console.WriteLine("==================ZACATEEEK KOLAAAA=================");

        
        var playableIndexes = new HashSet<int>(game.PlayableHexagons.Select(hex => hex.Index));

        var hexes = game.Hexagons.Select(h =>
        {
            if (!playableIndexes.Contains(h.Index) && h.Owner == 0)
            {
                return h.SetPlayer(5);
            }

            return h;
        }).ToList();
        
        Console.WriteLine("INIT");
        game.PrintRaw2DArray(game.To2DArray(hexes));
        
        int rotation = game.CurrentMovePlayerIndex.Value switch
        {
            1 => 0,  // No rotation needed
            2 => 1,  // 120° Clockwise
            3 => 2,  // 240° Clockwise
            _ => throw new Exception("Invalid player")
        };
        

        
        
        var rotatedHexes = HexRotation.RotateHexes(hexes, rotation);

        Console.WriteLine("INDICES ROTATE");
        var indices = game.To2DArrayIndices(rotatedHexes);
        game.PrintRaw2DArray(indices);
        
        var d2_Rotate = game.To2DArray(rotatedHexes);
        Console.WriteLine("D2 ROTATE");
        game.PrintRaw2DArray(d2_Rotate);
        var root = new MctsNode(d2_Rotate, null, (byte)game.CurrentMovePlayerIndex.Value);

        
        var clock = new Stopwatch();
        clock.Start();

        const int iterations = 2_000;
        
        MCTS(root, iterations, rotation);

        clock.Stop();
        
        
        /*
        // POKUD VSECHNY CHILDREN JSOU STEJNE NA PICU TAK TO PAK MA TENDENCI VYBIRAT PRVNI CHILDREN, TAKZE NEJNIZSI VOLNY INDEX => TAM BY TO MELO BYT RANDOM + RIDIT MCTS NEURONKOU
        
        
        Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");
        Console.WriteLine($"Run {iterations} iterations");
        Console.WriteLine();
        */
        // PrintMaxDepth(res);
        Console.WriteLine($"Total visits / total rewards  = {root.TotalReward}/{root.TotalVisits}");
        var final = root.Children.MaxBy(child => child.TotalVisits);

        Console.WriteLine("AFTER SELECT");
        game.PrintRaw2DArray(final.Board);
        Console.WriteLine($"selected move is: i: {final.CoordinatesOfMove.Value.i}, j: {final.CoordinatesOfMove.Value.j}");

        var (col, row) = final.CoordinatesOfMove.Value;
        
        // Convert 2D indices back to axial coordinates
        int r = row - game.Radius - 1; // Reverse the positive shift
        int q = col - game.Radius - 1; // Reverse the positive shift
        int s = -r - q;           // Ensure axial coordinate constraint R + Q + S = 0

        var index = indices[col, row];
        var selectedHex = rotatedHexes.FirstOrDefault(h => 
            h.R == r && h.Q == q && h.S == s);
        
        Console.WriteLine($"index is {index}");

        if (selectedHex is null)
        {
            throw new ApplicationException("SNAZIM SE VYBRAT NECO CO NENI V PLAYABLE HEXAGONS");
        }
        
        return Task.FromResult((int)index);
    }


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

        /// <summary>
        /// Reverses the hex coordinate rotation to recover the original position.
        /// </summary>
        public static (int, int, int) RotateHexCoordsBack(int r, int s, int q, int rotation)
        {
            return rotation switch
            {
                1 => (-q, -r, -s), // Reverse of 120° (i.e., 240°)
                2 => (-s, -q, -r), // Reverse of 240° (i.e., 120°)
                _ => (r, s, q)  // No rotation
            };
        }
    }
    public static (int, int, int) ReverseRotateHexCoords(int r, int s, int q, int rotation)
    {
        return rotation switch
        {
            1 => (-q, -r, -s), // Correct Reverse of 120° (i.e., 240°)
            2 => (-s, -q, -r), // Correct Reverse of 240° (i.e., 120°)
            _ => (r, s, q)  // No rotation
        };
    }

        static (int i, int j) FindNewlyAddedSpace(byte[,] initialBoard, byte[,] finalBoard)
        {
            (int i, int j)? res = null;
            for (int i = 0; i < initialBoard.GetLength(0); i++)
            {
                for (int j = 0; j < initialBoard.GetLength(1); j++)
                {
                    if (initialBoard[i, j] != finalBoard[i, j])
                    {
                        if (res is not null)
                        {
                            throw new Exception("TADY TO NEMELO NAJIT DALSI ZMENU");
                        }

                        res = (i, j);
                    }
                }
            }

            return ((int i, int j))res;

            throw new Exception("there is no diference in the boards");
        }

        
        
        static private int GetMaxDepth(MctsNode node)
        {
            if (node == null) return 0; // Base case: No node, depth is 0
            if (node.Children == null || node.Children.Count == 0) return 1; // Leaf node, depth is 1

            int maxDepth = 0;

            // Traverse all children and calculate their depths
            foreach (var child in node.Children)
            {
                maxDepth = Math.Max(maxDepth, GetMaxDepth(child));
            }

            return maxDepth + 1; // Add 1 to include the current node in the depth
        }

        static public void PrintMaxDepth(MctsNode root)
        {
            int maxDepth = GetMaxDepth(root);
            Console.WriteLine($"Maximum depth from the root: {maxDepth}");
        }

        
        public static MctsNode MCTS(MctsNode root, int iterations, int rotation)
        {
            var whoShouldWin = root.PlayerValue;
            
            Expand(root);
            root.Children.ForEach(n =>
            {
                int reward = Simulate(n, whoShouldWin, rotation);
                Backpropagate(n, reward);
            });
            
            for (int i = 0; i < iterations; i++)
            {
                // Selection
                var node = Select(root);

                // Expansion
                if (!node.IsTerminal)
                    Expand(node);

                // Simulation
                int reward = Simulate(node, whoShouldWin, rotation);

                // Backpropagation
                Backpropagate(node, reward);
            }

            return root;
        }

        private static MctsNode Select(MctsNode node)
        {
            // Implement UCT or another selection policy
            while (node.Children.Count > 0)
            {
                node = BestChild(node);
            }
            return node;
        }

        private static void Expand(MctsNode node)
        {
            // Generate child nodes based on possible moves
            List<MctsNode> children = GenerateChildren(node);
            node.Children.AddRange(children);
        }

        private static int Simulate(MctsNode node, byte whoShouldWin, int rotation)
        {
            var simulationBoard = (byte[,])node.Board.Clone();
            var currentPlayer = node.PlayerValue;

            int rows = simulationBoard.GetLength(0);
            int cols = simulationBoard.GetLength(1);

            var untakenPositions = new List<(int, int)>();
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (simulationBoard[i, j] == 0)
                    {
                        untakenPositions.Add((i, j));
                    }
                }
            }

            // Shuffle the list to randomize move order
            var random = new Random();
            for (int i = untakenPositions.Count - 1; i > 0; i--)
            {
                int swapIndex = random.Next(i + 1);
                (untakenPositions[i], untakenPositions[swapIndex]) = (untakenPositions[swapIndex], untakenPositions[i]);
            }

            // Alternate players and simulate moves
            foreach (var position in untakenPositions)
            {
                (int row, int col) = position;
                simulationBoard[row, col] = currentPlayer;
                currentPlayer = (byte)((currentPlayer % 3) + 1);
            }

            PathFinder pathFinder = new PathFinder();

            
            // Check if any other player has a winning path
            foreach (int player in new[] { 1, 2, 3 })
            {
                if (player != whoShouldWin && pathFinder.HasPath(simulationBoard, player, whoShouldWin-1))
                {
                    return -1; // Another player wins
                }
            }
            
            // Check if the desired player has a winning path
            if (pathFinder.HasPath(simulationBoard, whoShouldWin, whoShouldWin-1))
            {
                return 1; // Desired player wins
            }
            

            // If no one wins, it's a draw
            return 0;
        }
        
        public void PrintRaw2DArray(byte[,] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                for (int j = 0; j < array.GetLength(1); j++)
                {
                    // Use fixed-width formatting to align numbers
                    Console.Write($"{(array[i, j] == 255 ? -1 : array[i, j]),3} ");
                }
                Console.WriteLine(); // New line after each row
            }
        }
        
        private static void Backpropagate(MctsNode node, int reward)
        {
            while (node != null)
            {
                node.TotalVisits++;
                node.TotalReward += reward;
                node = node.Parent;
            }
        }

        private static MctsNode BestChild(MctsNode node)
        {
            double maxUcb = double.MinValue;
            MctsNode bestChild = null;

            var c = 1.0; // Exploration constant
            double ucb;

            foreach (var child in node.Children)
            {
                if (child.TotalVisits == 0)
                {
                    // Assign a very high value to unexplored nodes to prioritize them
                    ucb = double.MaxValue;
                }
                else
                {
                    // Calculate UCB for explored nodes
                    ucb = (child.TotalReward / (double)child.TotalVisits) +
                          c * Math.Sqrt(2 * Math.Log(node.TotalVisits) / child.TotalVisits);
                }

                // Update the best child based on the UCB value
                if (ucb > maxUcb)
                {
                    maxUcb = ucb;
                    bestChild = child;
                }
            }

            if (bestChild == null)
            {
                throw new InvalidOperationException("No valid child found. This indicates a problem in the tree structure.");
            }

            return bestChild;
        }


        private static List<MctsNode> GenerateChildren(MctsNode node)
        {
            var children = new List<MctsNode>();

            int rows = node.Board.GetLength(0);
            int cols = node.Board.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (node.Board[i, j] == 0) // Check if the space is unoccupied
                    {
                        var newBoard = (byte[,])node.Board.Clone();
                        newBoard[i, j] = node.PlayerValue;
                        
                        var newPlayerVal = (byte)((node.PlayerValue % 3) + 1);
                        children.Add(new MctsNode(newBoard, node, newPlayerVal, (i,j)));
                    }
                }
            }

            return children;
        }
}

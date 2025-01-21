using System.Diagnostics;

namespace Domain.Players.MCTS;

public class MctsNode
{
    public readonly byte[,] Board = new byte[7, 7];
    public int TotalReward = 0;
    public int TotalVisits = 0;
    public readonly List<MctsNode> Children = new();
    public readonly MctsNode? Parent = null;
    public bool IsTerminal { get; set; }
    public byte PlayerValue { get; set; }

    public void IncreasePlayerValue()
    {
        PlayerValue = (byte)((PlayerValue % 3) + 1);
    }

    public MctsNode(byte[,] board, MctsNode? parent, byte playerNum)
    {
        Board = board;
        Parent = parent;
        PlayerValue = playerNum;
    }
}




public class MctsPlayer(int playerNum) : AiPlayer(playerNum)
{
    public override Task<int> CalculateBestMoveAsync(Game game, CancellationToken cancellationToken)
    {
        var playableIndexes = new HashSet<int>(game.PlayableHexagons.Select(hex => hex.Index));

        var hexes = game.Hexagons.Select(h =>
        {
            if (!playableIndexes.Contains(h.Index) && h.Owner == 0)
            {
                return h.SetPlayer(5);
            }

            return h;
        }).ToList();
        
        
        
        var root = new MctsNode(game.To2DArray(hexes), null, (byte)game.CurrentMovePlayerIndex.Value);

        game.PrintRaw2DArray(root.Board);
        
        var clock = new Stopwatch();
        clock.Start();

        const int iterations = 50000;
        
        var res  = MCTS(root, iterations);

        clock.Stop();
        Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");
        Console.WriteLine($"Run {iterations} iterations");
        Console.WriteLine($"Total visits / total rewards  = {res.TotalReward}/{res.TotalVisits}");
        Console.WriteLine();
        PrintMaxDepth(res);
        var final = root.Children.MaxBy(child => child.TotalVisits);

        var (col, row) = FindNewlyAddedSpace(root.Board, final.Board);
        
        // Convert 2D indices back to axial coordinates
        int r = row - game.Radius - 1; // Reverse the positive shift
        int q = col - game.Radius - 1; // Reverse the positive shift
        int s = -r - q;           // Ensure axial coordinate constraint R + Q + S = 0

        var a = (r, s, q);
        
        game.PrintRaw2DArray(final.Board);

        Console.WriteLine();
        
        var selectedHex = game.PlayableHexagons.FirstOrDefault(h => h.Q == r && h.R == q && h.S == s);

        if (selectedHex is null)
        {
            throw new ApplicationException("SNAZIM SE VYBRAT NECO CO NENI V PLAYABLE HEXAGONS");
        }
        
        return Task.FromResult(selectedHex.Index);
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

        
        private MctsNode MCTS(MctsNode root, int iterations)
        {
            var whoShouldWin = root.PlayerValue;
            
            Expand(root);
            root.Children.ForEach(n =>
            {
                int reward = Simulate(n, whoShouldWin);
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
                int reward = Simulate(node, whoShouldWin);

                // Backpropagation
                Backpropagate(node, reward);
            }

            return root;
        }

        private MctsNode Select(MctsNode node)
        {
            // Implement UCT or another selection policy
            while (node.Children.Count > 0)
            {
                node = BestChild(node);
            }
            return node;
        }

        private void Expand(MctsNode node)
        {
            // Generate child nodes based on possible moves
            List<MctsNode> children = GenerateChildren(node);
            node.Children.AddRange(children);
        }

        private int Simulate(MctsNode node, byte whoShouldWin)
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

            var haspath = pathFinder.HasPath(simulationBoard, whoShouldWin);
            
            // if ( haspath) Console.WriteLine("NALEZENA CESTA");

            // PrintRaw2DArray(simulationBoard);
            // Console.WriteLine("==========================");
            return  haspath ? 1 : 0;
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
        
        private void Backpropagate(MctsNode node, int reward)
        {
            while (node != null)
            {
                node.TotalVisits++;
                node.TotalReward += reward;
                node = node.Parent;
            }
        }

        private MctsNode BestChild(MctsNode node)
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


        private List<MctsNode> GenerateChildren(MctsNode node)
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
                        children.Add(new MctsNode(newBoard, node, newPlayerVal));
                    }
                }
            }

            return children;
        }
}

using System.Diagnostics;
using Domain;
using Domain.Players;

namespace MonteCarloConsole;


    public struct PlayerNum
    {
        public PlayerNum()
        {
        }

        public byte Value { get; private set; } = 1;

        public void Increase()
        {
            Value = (byte)((Value % 3) + 1);
        }
    }

    public class MctsNode
    {
        public readonly byte[,] Board = new byte[7, 7];
        public int TotalReward = 0;
        public int TotalVisits = 0;
        public readonly List<MctsNode> Children = new();
        public readonly MctsNode? Parent = null;
        public bool IsTerminal { get; set; }
        public PlayerNum PlayerValue { get; set; }

        public MctsNode(byte[,] board, MctsNode? parent, PlayerNum playerNum)
        {
            Board = board;
            Parent = parent;
            PlayerValue = playerNum;
            PlayerValue.Increase();
        }
    }



    class Program
    {
        static void Main(string[] args)
        {
            var radius = 5;
            var game = new Game(new GameId("absd"), radius, typeof(PathFinderHeuristic));
            
            var root = new MctsNode(game.To2DArray(), null, new PlayerNum());

            var clock = new Stopwatch();
            clock.Start();

            const int iterations = 10000;

            
            Program program = new Program();
            var res  = program.MCTS(root, iterations, 0);

            clock.Stop();
            Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");
            Console.WriteLine($"Ran {iterations} iterations");
            Console.WriteLine($"Total visits / total rewards  = {res.TotalReward}/{res.TotalVisits}");
            Console.WriteLine();
            PrintMaxDepth(res);
            var final = root.Children.MaxBy(child => child.TotalVisits);
            game.PrintRaw2DArray(final.Board);

            var coords = FindNewlyAddedSpace(root.Board, final.Board);
        }
        
        
        static (int i, int j) FindNewlyAddedSpace(byte[,] initialBoard, byte[,] finalBoard)
        {
            for (int i = 0; i < initialBoard.GetLength(0); i++)
            {
                for (int j = 0; j < initialBoard.GetLength(1); j++)
                {
                    if (initialBoard[i, j] != finalBoard[i, j])
                    {
                        return (i, j);
                    }
                }
            }

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

        
        private MctsNode MCTS(MctsNode root, int iterations, int rotation)
        {
            var whoShouldWin = root.PlayerValue.Value;
            
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

        private int Simulate(MctsNode node, byte whoShouldWin, int rotation)
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
                simulationBoard[row, col] = currentPlayer.Value;
                currentPlayer.Increase();
            }

            PathFinder pathFinder = new PathFinder();

            var haspath = pathFinder.HasPath(simulationBoard, whoShouldWin, rotation);
            
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
                        newBoard[i, j] = node.PlayerValue.Value;
                        children.Add(new MctsNode(newBoard, node, node.PlayerValue));
                    }
                }
            }

            return children;
        }

    }


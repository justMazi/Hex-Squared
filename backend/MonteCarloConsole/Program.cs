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
            var game = new Game(new GameId("absd"), radius-4, typeof(PathFinderHeuristic));
            
            int size = 2 * radius + 1;
            byte[,] grid = new byte[size, size];

            foreach (var hex in game.Hexagons)
            {
                // Map axial (Q, R) to array indices (i, j)
                int i = hex.Q + radius;
                int j = hex.R + radius;

                // Set the array cell based on the Owner property
                // 0 = empty, other values indicate ownership
                grid[i, j] = (byte)hex.Owner;
            }
            
            
            
            for (int i = 0; i < grid.GetLength(0); i++) // Iterate through rows
            {
                for (int j = 0; j < grid.GetLength(1); j++) // Iterate through columns
                {
                    Console.Write(grid[i, j] + "\t"); // Print each element separated by a tab
                }
                Console.WriteLine(); // Move to the next line after each row
            }
            
            
            var root = new MctsNode(grid, null, new PlayerNum());

            var clock = new Stopwatch();
            clock.Start();

            Program program = new Program();
            program.MCTS(root);

            clock.Stop();
            Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");
        }

        
        private void MCTS(MctsNode root)
        {
            
            Expand(root);
            root.Children.ForEach(n =>
            {
                int reward = Simulate(n);
                Backpropagate(n, reward);
            });
            
            const int iterations = 1000;
            for (int i = 0; i < iterations; i++)
            {
                // Selection
                var node = Select(root);

                // Expansion
                if (!node.IsTerminal)
                    Expand(node);

                // Simulation
                int reward = Simulate(node);

                // Backpropagation
                Backpropagate(node, reward);
            }
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

        private int Simulate(MctsNode node)
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

            return HasPath(simulationBoard, node.PlayerValue.Value) ? 1 : 0;
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
            MctsNode bestChild = default;

            foreach (var child in node.Children)
            {
                double ucb = (child.TotalReward / (double)child.TotalVisits) +
                             Math.Sqrt(2 * Math.Log(node.TotalVisits) / child.TotalVisits);
                if (ucb > maxUcb)
                {
                    maxUcb = ucb;
                    bestChild = child;
                }
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

        private bool HasPath(byte[,] board, byte playerValue)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);

            var visited = new bool[rows, cols];
            var startPositions = new List<(int, int)>();
            var targetPositions = new HashSet<(int, int)>();

            if (playerValue == 1) // Player 1: Connect left-to-right
            {
                for (int i = 0; i < rows; i++)
                {
                    if (board[i, 0] == playerValue)
                        startPositions.Add((i, 0));
                    if (board[i, cols - 1] == playerValue)
                        targetPositions.Add((i, cols - 1));
                }
            }
            else if (playerValue == 2) // Player 2: Connect top-to-bottom
            {
                for (int j = 0; j < cols; j++)
                {
                    if (board[0, j] == playerValue)
                        startPositions.Add((0, j));
                    if (board[rows - 1, j] == playerValue)
                        targetPositions.Add((rows - 1, j));
                }
            }
            else
            {
                return false;
            }

            foreach (var start in startPositions)
            {
                if (DFS(board, start, visited, targetPositions, playerValue))
                    return true;
            }

            return false;
        }

        private bool DFS(byte[,] board, (int, int) position, bool[,] visited, HashSet<(int, int)> targets, byte playerValue)
        {
            int rows = board.GetLength(0);
            int cols = board.GetLength(1);
            var (row, col) = position;

            if (row < 0 || row >= rows || col < 0 || col >= cols) return false;
            if (visited[row, col]) return false;
            if (board[row, col] != playerValue) return false;
            if (targets.Contains(position)) return true;

            visited[row, col] = true;

            var neighbors = GetHexNeighborsOffset(row, col, rows, cols);

            foreach (var (neighborRow, neighborCol) in neighbors)
            {
                if (DFS(board, (neighborRow, neighborCol), visited, targets, playerValue))
                    return true;
            }

            return false;
        }

        private List<(int, int)> GetHexNeighborsOffset(int row, int col, int maxRows, int maxCols)
        {
            var evenRowDirections = new (int, int)[]
            {
                (-1, 0), (-1, 1), (0, -1), (0, 1), (1, 0), (1, 1)
            };

            var oddRowDirections = new (int, int)[]
            {
                (-1, -1), (-1, 0), (0, -1), (0, 1), (1, -1), (1, 0)
            };

            var directions = row % 2 == 0 ? evenRowDirections : oddRowDirections;
            var neighbors = new List<(int, int)>();

            foreach (var (dr, dc) in directions)
            {
                int newRow = row + dr;
                int newCol = col + dc;

                if (newRow >= 0 && newRow < maxRows && newCol >= 0 && newCol < maxCols)
                {
                    neighbors.Add((newRow, newCol));
                }
            }

            return neighbors;
        }
    }


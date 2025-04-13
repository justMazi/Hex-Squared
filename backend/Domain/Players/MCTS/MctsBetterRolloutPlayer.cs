namespace Domain.Players.MCTS;


public class MctsBetterRolloutPlayer(int playerNum) : AiPlayer(playerNum)
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
        
        var rotation = game.CurrentMovePlayerIndex.Value - 1;


        
        var rotatedHexes = MctsHelpers.HexRotation.RotateHexes(hexes, rotation);
        var indices = game.To2DArrayIndices(rotatedHexes);
        
        var d2Rotate = game.To2DArray(rotatedHexes);
        var root = new MctsNode(d2Rotate, null, (byte)game.CurrentMovePlayerIndex.Value);

        const int iterations = 200;
        Mcts(root, iterations, rotation);
        
        var final = root.Children.MaxBy(child => child.TotalVisits);
        var (col, row) = final.CoordinatesOfMove.Value;

        // Console.WriteLine($"Player {playerNum} has {final.TotalVisits} visits and {final.TotalReward} rewards");
        
        var index = indices[col, row];
        
        return Task.FromResult((int)index);
    }


    private static void Mcts(MctsNode root, int iterations, int rotation)
    {
        var whoShouldWin = root.PlayerValue;
        
        Expand(root);
        root.Children.ForEach(n =>
        {
            var reward = Simulate(n, whoShouldWin, rotation);
            Backpropagate(n, reward);
        });
        
        for (var i = 0; i < iterations; i++)
        {
            // Selection
            var node = Select(root);

            // Expansion
            if (!node.IsTerminal)
                Expand(node);

            // Simulation
            var reward = Simulate(node, whoShouldWin, rotation);

            // Backpropagation
            Backpropagate(node, reward);
        }
    }

    private static MctsNode Select(MctsNode node)
    {
        while (node.Children.Count > 0)
        {
            node = BestChild(node);
        }
        return node;
    }

    private static void Expand(MctsNode node)
    {
        // Generate child nodes based on possible moves
        var children = GenerateChildren(node);
        node.Children.AddRange(children);
    }
    
    
    
    private static int Simulate(MctsNode node, byte whoShouldWin, int rotation)
    {
        var simulationBoard = (byte[,])node.Board.Clone();
        
        
        var rows = simulationBoard.GetLength(0);
        var cols = simulationBoard.GetLength(1);
        var pathFinder = new PathFinder();
    
        byte currentPlayer = 1; // Start with player 1
        var random = new Random();

        while (true) // Keep playing until there's a winner
        {
            var path = pathFinder.FindPathWithUntakenCells(simulationBoard, currentPlayer, rotation);



            if (path.Count > 0)
            { 
                var randomIndex = Random.Shared.Next(0, path.Count);

                var middleUntakenCell = path[randomIndex];
                
                var (row, col) = middleUntakenCell;
                // Use the middle untaken cell, e.g., place a piece there
                simulationBoard[row, col] = currentPlayer;            }
            else
            {
                // Play randomly if the middle hex is occupied or unavailable
                var emptyHexes = new List<(int Row, int Col)>();

                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < cols; c++)
                    {
                        if (simulationBoard[r, c] == 0)
                        {
                            emptyHexes.Add((r, c));
                        }
                    }
                }

                if (emptyHexes.Count > 0)
                {
                    var (randRow, randCol) = emptyHexes[random.Next(emptyHexes.Count)];
                    simulationBoard[randRow, randCol] = currentPlayer;
                }
                else
                {
                    break;
                }
            }
            
            
            currentPlayer = (byte)((currentPlayer % 3) + 1);
        }
        
// PrintRaw2DArray(simulationBoard);
        
        
        // Check if any other player has a winning path
        foreach (var player in new[] { 1, 2, 3 })
        {
            if (player != whoShouldWin && pathFinder.HasPath(simulationBoard, player, whoShouldWin-1))
            {
                return -1; // Some other player wins
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


    public static void PrintRaw2DArray(byte[,] array)
    {
        for (var i = 0; i < array.GetLength(0); i++)
        {
            for (var j = 0; j < array.GetLength(1); j++)
            {
                // Use fixed-width formatting to align numbers
                Console.Write($"{(array[i, j] == 255 ? -1 : array[i, j]),3} ");
            }
            Console.WriteLine(); // New line after each row
        }
        Console.WriteLine();

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
        var maxUcb = double.MinValue;
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

        var rows = node.Board.GetLength(0);
        var cols = node.Board.GetLength(1);
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
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

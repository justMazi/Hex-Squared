public class PathFinder
{
    private readonly int[,] _directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }, {-1,1}, {1,-1} }; // Up, Down, Left, Right, Diagonals
    private int _rows, _cols;

    public bool HasPath(byte[,] board, int playerIamTryingToConnect, int rotation)
    {
        _rows = board.GetLength(0);
        _cols = board.GetLength(1);
        var visited = new bool[_rows, _cols];

        var player = GetMappedValue(rotation, playerIamTryingToConnect);
        
        // Initialize starting and ending points based on the player
        var startCell = GetStartingCells(_rows, player);
        var endCell = GetEndingCells(_rows, player);
        
        // Get all cells reachable from start that belong to the player
        var startSet = GetReachableCells(board, startCell.Item1, startCell.Item2, playerIamTryingToConnect);
        
        // Get all cells reachable from end that belong to the player
        var endSet = GetReachableCells(board, endCell.Item1, endCell.Item2, playerIamTryingToConnect);
        
        // Reset visited array for the main path finding
        visited = new bool[_rows, _cols];
        
        // Check if there's a path between any cell in startSet and any cell in endSet
        return IterativeDfsWithSets(board, startSet, endSet, playerIamTryingToConnect, visited);
    }

    // New method that returns the actual path considering untaken cells
    public List<(int, int)> FindPathWithUntakenCells(byte[,] board, int playerIamTryingToConnect, int rotation)
    {
        _rows = board.GetLength(0);
        _cols = board.GetLength(1);
        var visited = new bool[_rows, _cols];

        var plaaayer = GetMappedValue(rotation, playerIamTryingToConnect);
        
        // Initialize starting and ending points based on the player
        var startCell = GetStartingCells(_rows, plaaayer);
        var endCell = GetEndingCells(_rows, plaaayer);
        
        var startValue = board[startCell.Item1, startCell.Item2];
        var endValue = board[endCell.Item1, endCell.Item2];
        
        if (startValue != playerIamTryingToConnect || endValue != playerIamTryingToConnect)
        {
            throw new ApplicationException("SPATNE ZACATECNI HODNOTA");
        }
        
        // Get all cells reachable from start that belong to the player
        var startSet = GetReachableCells(board, startCell.Item1, startCell.Item2, playerIamTryingToConnect);
        
        // Get all cells reachable from end that belong to the player
        var endSet = GetReachableCells(board, endCell.Item1, endCell.Item2, playerIamTryingToConnect);
        
        // Reset visited array for the main path finding
        visited = new bool[_rows, _cols];
        
        return FindPathIterativeDfsWithSets(board, startSet, endSet, playerIamTryingToConnect, visited);
    }
    
    // New method to get all reachable cells that belong to the player
    private HashSet<(int, int)> GetReachableCells(byte[,] board, int startRow, int startCol, int player)
    {
        var reachableCells = new HashSet<(int, int)>();
        var visited = new bool[_rows, _cols];
        var queue = new Queue<(int, int)>();
        
        // Start BFS from the initial cell
        queue.Enqueue((startRow, startCol));
        visited[startRow, startCol] = true;
        
        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();
            
            // Add cell to reachable set
            reachableCells.Add((row, col));
            
            // Check all adjacent cells
            for (var i = 0; i < 6; i++)
            {
                var newRow = row + _directions[i, 0];
                var newCol = col + _directions[i, 1];
                
                if (newRow >= 0 && newRow < _rows && newCol >= 0 && newCol < _cols && 
                    !visited[newRow, newCol] && board[newRow, newCol] == player)
                {
                    queue.Enqueue((newRow, newCol));
                    visited[newRow, newCol] = true;
                }
            }
        }
        
        return reachableCells;
    }

    private int GetMappedValue(int rotation, int playerNumber)
    {
        // Define the mapping using a dictionary
        var mapping = new Dictionary<(int, int), int>
        {
            { (2, 1), 1 }, { (1, 1), 2 }, { (0, 1), 0 },
            { (0, 2), 1 }, { (1, 2), 0 }, { (2, 2), 2 },
            { (0, 3), 2 }, { (1, 3), 1 }, { (2, 3), 0 }
        };

        // Return the mapped value or throw an error if inputs are invalid
        if (mapping.TryGetValue((rotation, playerNumber), out var result))
        {
            return result;
        }
    
        throw new ArgumentException($"Invalid rotation ({rotation}) or playerNumber ({playerNumber})");
    }
    
    public (int, int) GetStartingCells(int squareSize, int player)
    {
        return player switch
        {
            0 => (squareSize-2, 0),
            1 => (squareSize-1, 1),
            2 => (squareSize-2, ((squareSize-1)/2)+1),
            _ => throw new Exception("toto se nemelo stat")
        };
    }

    public (int, int) GetEndingCells(int squareSize, int player)
    {
        return player switch
        {
            0 => (1, squareSize-1),
            1 => (0, squareSize-2),
            2 => (1, ((squareSize-1)/2)-1),
            _ => throw new Exception("toto se nemelo stat")
        };
    }
    
    // Modified DFS to check if there's a path between any start cell and any end cell
    private bool IterativeDfsWithSets(byte[,] board, HashSet<(int, int)> startSet, HashSet<(int, int)> endSet, 
                                     int player, bool[,] visited)
    {
        var stack = new Stack<(int, int)>();
        
        // Start from all cells in the start set
        foreach (var startCell in startSet)
        {
            stack.Push(startCell);
        }

        while (stack.Count > 0)
        {
            var (row, col) = stack.Pop();

            // Skip invalid or visited cells
            if (row < 0 || row >= _rows || col < 0 || col >= _cols || visited[row, col])
                continue;

            // Skip cells that are not player's or untaken (0)
            if (board[row, col] != player && board[row, col] != 0)
                continue;

            // Mark the cell as visited
            visited[row, col] = true;

            // Check if this cell is in the end set
            if (endSet.Contains((row, col)))
                return true;
            
            // Explore neighbors
            for (var i = 0; i < 6; i++)
            {
                var newRow = row + _directions[i, 0];
                var newCol = col + _directions[i, 1];
                stack.Push((newRow, newCol));
            }
        }

        return false;
    }

    // Helper method for FindPathWithUntakenCells that returns the actual path
    private List<(int, int)> FindPathIterativeDfsWithSets(byte[,] board, HashSet<(int, int)> startSet, 
                                                         HashSet<(int, int)> endSet, int player, bool[,] visited)
    {
        var queue = new Queue<(int, int)>();
        var parent = new Dictionary<(int, int), (int, int)>();
        
        // Start BFS from all cells in the start set
        foreach (var startCell in startSet)
        {
            queue.Enqueue(startCell);
            parent[startCell] = (-1, -1); // Mark start cells with a special parent
        }

        while (queue.Count > 0)
        {
            var (row, col) = queue.Dequeue();

            // Skip invalid or visited cells
            if (row < 0 || row >= _rows || col < 0 || col >= _cols || visited[row, col])
                continue;

            // Skip cells that are not player's or untaken (0)
            if (board[row, col] != player && board[row, col] != 0)
                continue;

            // Mark the cell as visited
            visited[row, col] = true;

            // Check if we've reached any cell in the end set
            if (endSet.Contains((row, col)))
            {
                // Reconstruct the path
                return ReconstructPath(parent, (row, col));
            }
            
            // Explore neighbors
            for (var i = 0; i < 6; i++)
            {
                var newRow = row + _directions[i, 0];
                var newCol = col + _directions[i, 1];
                
                if (newRow >= 0 && newRow < _rows && newCol >= 0 && newCol < _cols && 
                    !visited[newRow, newCol] && !parent.ContainsKey((newRow, newCol)))
                {
                    queue.Enqueue((newRow, newCol));
                    parent[(newRow, newCol)] = (row, col);
                }
            }
        }

        return new List<(int, int)>(); // Return empty list if no path is found
    }

    // Helper method to reconstruct the path from end to start
    private List<(int, int)> ReconstructPath(Dictionary<(int, int), (int, int)> parent, (int, int) end)
    {
        var path = new List<(int, int)>();
        var current = end;
        
        while (current != (-1, -1))
        {
            path.Add(current);
            current = parent[current];
        }
        
        path.Reverse(); // Reverse to get path from start to end
        return path;
    }
}
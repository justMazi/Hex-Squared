using System;
using System.Collections.Generic;

public class PathFinder
{
    private readonly int[,] _directions = { { -1, 0 }, { 1, 0 }, { 0, -1 }, { 0, 1 }, {-1,1}, {1,-1} }; // Up, Down, Left, Right
    private int _rows, _cols;

    public bool HasPath(byte[,] board, int player)
    {
        _rows = board.GetLength(0);
        _cols = board.GetLength(1);
        var visited = new bool[_rows, _cols];

        // Initialize starting and ending points based on the player
        var startCells = GetStartingCells(player);
        var endCells = GetEndingCells(player);

        return IterativeDfs(board, startCells.Item1, startCells.Item2, endCells, player, visited);
    }

    private (int, int) GetStartingCells(int player)
    {
        return player switch
        {
            1 => (9, 0),
            2 => (10, 1),
            3 => (1, 4),
            _ => throw new Exception("toto se nemelo stat")
        };
    }

    private (int, int) GetEndingCells(int player)
    {
        return player switch
        {
            1 => (1, 10),
            2 => (0, 6),
            3 => (9, 6),
            _ => throw new Exception("toto se nemelo stat")
        };
    }

    private bool IterativeDfs(byte[,] board, int startRow, int startCol, (int, int) endCells, int player, bool[,] visited)
    {
        var stack = new Stack<(int, int)>();
        stack.Push((startRow, startCol));

        while (stack.Count > 0)
        {
            var (row, col) = stack.Pop();

            // Skip invalid or visited cells
            if (row < 0 || row >= _rows || col < 0 || col >= _cols || visited[row, col] || board[row, col] != player)
                continue;

            // Mark the cell as visited
            visited[row, col] = true;

            // Check if the end condition is met
            if (row == endCells.Item1 && col == endCells.Item2)
                return true;
            
            for (int i = 0; i < 6; i++)
            {
                int newRow = row + _directions[i, 0];
                int newCol = col + _directions[i, 1];
                stack.Push((newRow, newCol));
            }
        }

        return false;
    }
}

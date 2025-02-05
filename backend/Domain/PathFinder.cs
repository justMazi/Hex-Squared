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
        var startCells = GetStartingCells(_rows, player);
        var endCells = GetEndingCells(_rows, player);

        if (board[startCells.Item1, startCells.Item2] != board[endCells.Item1, endCells.Item2])
        {
            throw new ApplicationException("KONEC A ZACATEK NENI STEJNA HODNOTA");
        }
        
        
        return IterativeDfs(board, startCells.Item1, startCells.Item2, endCells, player, visited);
    }

    private (int, int) GetStartingCells(int squareSize, int player)
    {
        return player switch
        {
            1 => (squareSize-2, 0),
            2 => (squareSize-1, 1),
            3 => (1, ((squareSize-1)/2)-1),
            _ => throw new Exception("toto se nemelo stat")
        };
    }

    private (int, int) GetEndingCells(int squareSize, int player)
    {
        return player switch
        {
            1 => (1, squareSize-1),
            2 => (0, squareSize-2),
            3 => (squareSize-2, ((squareSize-1)/2)+1),
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

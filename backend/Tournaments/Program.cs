using System.Diagnostics;
using Domain;
using Domain.Players;
using Domain.Players.MCTS;
using Infrastructure.Repositories;

namespace Tournaments;

class Program
{
    static async Task Main(string[] args)
    {
        var radius = 7;
        const int numberOfGames = 10; 
        
        
        var gameRepository = new GameRepository();
        var cancellationToken = new CancellationToken();

        var clock = new Stopwatch();
        clock.Start();

        var playerTypes = new[]
        {
            typeof(MctsPlayer),
            typeof(RandomPlayer),
            typeof(RandomPlayer) 
        };

        // Results accumulator
        var gameResultsLock = new object(); // Lock for thread-safe access to shared list

        var finishedGames = Enumerable.Range(0, numberOfGames).Select(gameNum =>
        {
            var players = playerTypes
                .Select((type, index) => Activator.CreateInstance(type, index + 1) as Player)
                .ToList();

            var game = gameRepository.CreateNewGame(new GameId(gameNum.ToString()), radius, typeof(MctsPlayer));

            foreach (var player in players)
            {
                game = game.PickColor(player, player.PlayerNum).Match(
                    Some: updatedGame =>
                    {
                        gameRepository.SaveGame(updatedGame);
                        return updatedGame;
                    },
                    None: () => throw new Exception("Couldn't pick the color")
                );
            }

            while (game.GameState != GameState.Finished)
            {
                var currentPlayer = game.Players[game.CurrentMovePlayerIndex - 1];

                if (currentPlayer is not AiPlayer player)
                    throw new Exception("Tournaments allow only AI players");

                // Asynchronous method in a synchronous context with .GetAwaiter().GetResult()
                var bestMoveIndex = player.CalculateBestMoveAsync(game, cancellationToken).GetAwaiter().GetResult();

                var hexagon = game.Hexagons.FirstOrDefault(h => h.Index == bestMoveIndex);
                if (hexagon == null)
                    throw new Exception($"Invalid move index {bestMoveIndex} for player {currentPlayer.PlayerNum}");

                game = game.Move(player, hexagon).Match(
                    Some: updatedGame =>
                    {
                        gameRepository.SaveGame(updatedGame);

                        if (currentPlayer is MctsPlayer)
                        {
                            updatedGame.PrintRaw2DArray(updatedGame.To2DArray());
                            Console.WriteLine("==================================================");
                        }
                        return updatedGame;
                        
                    },
                    None: () => throw new Exception("Failed to make a move")
                );
            }

            return game;

        }).ToList();

        clock.Stop();
        Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");

        var gamesResults = finishedGames.Select(g => g.Players.Select(p => p.NumberOfWins).ToList()).ToList();

        // Calculate aggregated results
        var aggregatedResults = gamesResults
            .Aggregate(
                Enumerable.Repeat(0, gamesResults.First().Count()).ToList(),
                (acc, gameResult) => acc.Zip(gameResult, (a, b) => a + b).ToList()
            );

        // Calculate the total number of games and draws
        var totalGames = finishedGames.Count();
        var totalDraws = totalGames - aggregatedResults.Sum(); // Assuming 1 win per game if no draws

        // Display consolidated results
        Console.WriteLine("Tournament Results:");
        Console.WriteLine("-------------------");

        for (int i = 0; i < aggregatedResults.Count; i++)
        {
            var winCount = aggregatedResults[i];
            var winPercentage = (double)winCount / totalGames * 100;

            Console.WriteLine($"Player {i + 1}: {winCount} wins ({winPercentage:F2}%) ({playerTypes[i].Name}) ");
        }

        var drawPercentage = (double)totalDraws / totalGames * 100;
        Console.WriteLine($"Draws: {totalDraws} games ({drawPercentage:F2}%)");

  
    }
}
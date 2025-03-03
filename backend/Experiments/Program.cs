using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Domain;
using Domain.Players;
using Domain.Players.MCTS;
using Infrastructure.Repositories;

namespace Experiments;

class Program
{
    static async Task Main(string[] args)
    {
        
        // Discover all available AI players dynamically
        var availablePlayers = Assembly.GetAssembly(typeof(AiPlayer))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(AiPlayer).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, t => t);

        Type[] playerTypes;

        if (args.Length == 3)
        {
            try
            {
                playerTypes = args.Select(name =>
                {
                    if (!availablePlayers.TryGetValue(name, out var type))
                    {
                        throw new ArgumentException($"Unknown player type: {name}");
                    }
                    return type;
                }).ToArray();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return;
            }
        }
        else
        {
            // Default player setup if no arguments are given
            playerTypes = new[]
            {
                typeof(NeuralNetworkPlayer),
                typeof(RandomPlayer),
                typeof(RandomPlayer),
            };
        }

        Console.WriteLine("Using players:");
        foreach (var type in playerTypes)
        {
            Console.WriteLine($"- {type.Name}");
        }

        var radius = 6;
        const int numberOfGames = 1;

        var gameRepository = new GameRepository();
        var cancellationToken = new CancellationToken();

        var clock = new Stopwatch();
        clock.Start();

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
                    throw new Exception($"Experiments allow only {nameof(AiPlayer)} players");

                var bestMoveIndex = player.CalculateBestMoveAsync(game, cancellationToken).GetAwaiter().GetResult();

                var hexagon = game.Hexagons.FirstOrDefault(h => h.Index == bestMoveIndex);
                if (hexagon == null)
                    throw new Exception($"Invalid move index {bestMoveIndex} for player {currentPlayer.PlayerNum}");

                game = game.Move(player, hexagon).Match(
                    Some: updatedGame =>
                    {
                        gameRepository.SaveGame(updatedGame);
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

        for (var i = 0; i < aggregatedResults.Count; i++)
        {
            var winCount = aggregatedResults[i];
            var winPercentage = (double)winCount / totalGames * 100;

            Console.WriteLine($"Player {i + 1}: {winCount} wins ({winPercentage:F2}%) ({playerTypes[i].Name}) ");
        }

        var drawPercentage = (double)totalDraws / totalGames * 100;
        Console.WriteLine($"Draws: {totalDraws} games ({drawPercentage:F2}%)");
    }
}

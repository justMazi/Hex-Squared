using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Domain;
using Domain.Players;
using Domain.Players.MCTS;
using Infrastructure.Repositories;

namespace Experiments;

class Program
{
    static async Task Main(string[] args)
    {
        var availablePlayers = Assembly.GetAssembly(typeof(AiPlayer))
            .GetTypes()
            .Where(t => t.IsClass && !t.IsAbstract && typeof(AiPlayer).IsAssignableFrom(t))
            .ToDictionary(t => t.Name, t => t);

        Type[] playerTypes = availablePlayers.Values.ToArray();

        var playerCombinations = GetCombinations(playerTypes, 3);
        
        
        // small = 4, mid = 6, big = 10
        var radius = 10;
        
        const int numberOfGames = 2;
        var gameRepository = new GameRepository();
        var cancellationToken = new CancellationToken();
        var clock = new Stopwatch();
        clock.Start();

        Dictionary<string, int> totalWins = playerTypes.ToDictionary(t => t.Name, t => 0);
        int totalDraws = 0;
        string resultsFile = "tournament_results.json";
        var tournamentResults = new List<object>();

        
        foreach (var combination in playerCombinations)
        {
            Console.WriteLine($"Running games for combination: {string.Join(", ", combination.Select(p => p.Name))}");
            Dictionary<string, int> matchWins = new Dictionary<string, int>();
            int matchDraws = 0;

            foreach (var player in combination.Distinct())
            {
                matchWins[player.Name] = 0;
            }

            for (int gameNum = 0; gameNum < numberOfGames; gameNum++)
            {
                var players = combination
                    .Select((type, index) => Activator.CreateInstance(type, index + 1) as Player)
                    .ToList();

                var id = Guid.NewGuid().ToString().Substring(0, 10);
                var game = gameRepository.CreateNewGame(new GameId(id), radius, typeof(MctsPlayer));

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

                Console.WriteLine($"Game {gameNum + 1} finished");
                
                var gameWinners = game.Players.Where(p => p.NumberOfWins > 0).ToList();
                if (gameWinners.Count == 0)
                {
                    totalDraws++;
                    matchDraws++;
                }
                else
                {
                    foreach (var winner in gameWinners)
                    {
                        totalWins[winner.GetType().Name]++;
                        matchWins[winner.GetType().Name]++;
                    }
                }
            }

            Console.WriteLine($"Results for combination: {string.Join(", ", combination.Select(p => p.Name))}");
            foreach (var (playerName, wins) in matchWins)
            {
                Console.WriteLine($"{playerName}: {wins} wins");
            }
            Console.WriteLine($"Draws: {matchDraws}");

            tournamentResults.Add(new {
                Combination = combination.Select(p => p.Name).ToArray(),
                MatchWins = matchWins,
                Draws = matchDraws
            });
        }

        clock.Stop();

        var finalResults = new
        {
            TotalElapsedTimeMs = clock.ElapsedMilliseconds,
            TotalWins = totalWins,
            TotalDraws = totalDraws,
            MatchResults = tournamentResults
        };

        await File.WriteAllTextAsync(resultsFile, JsonSerializer.Serialize(finalResults, new JsonSerializerOptions { WriteIndented = true }), cancellationToken);

        Console.WriteLine($"Elapsed Time: {clock.ElapsedMilliseconds} ms");
        Console.WriteLine("Overall Tournament Results:");
        Console.WriteLine("---------------------------");
        foreach (var (playerName, winCount) in totalWins)
        {
            Console.WriteLine($"{playerName}: {winCount} total wins");
        }
        Console.WriteLine($"Total Draws: {totalDraws}");
    }

    static IEnumerable<IEnumerable<T>> GetCombinations<T>(T[] array, int size)
    {
        return GetCombinationsRecursive(array, size, 0);
    }

    static IEnumerable<IEnumerable<T>> GetCombinationsRecursive<T>(T[] array, int size, int start)
    {
        if (size == 0)
        {
            yield return Enumerable.Empty<T>();
            yield break;
        }

        for (int i = start; i <= array.Length - size; i++)
        {
            foreach (var combination in GetCombinationsRecursive(array, size - 1, i + 1))
            {
                yield return new[] { array[i] }.Concat(combination);
            }
        }
    }
}

using System.Collections.Concurrent;
using Application;
using Application.IRepositories;
using Domain;
using Infrastructure.Exceptions;
using Serilog;

namespace Infrastructure.Repositories;

public class GameRepository : IGameRepository
{
    private ConcurrentDictionary<GameId, Game?> Games { get; set; } = new();

    public void SaveGame(Game game)
    {
        Games.TryUpdate(game.Id, game, Games.GetValueOrDefault(game.Id));
    }

    public void DeleteGame(Game existingGame)
    {
        Games.TryRemove(existingGame.Id, out var _);
    }

    public Game? GetById(GameId gameId)
    {
        return Games.GetValueOrDefault(gameId);
    }

    public Game CreateNewGame(GameId gameId)
    {
        var game =  new Game(gameId);
        if (!Games.TryAdd(gameId, game))
        {
            throw new CantAddGameException(gameId);
        }
        Log.Information("Creating new game with id {gameId}", gameId);
        return game;
    }
    
    public void RestartGame(GameId gameId)
    {
        var game = new Game(gameId);
        Games.TryGetValue(gameId, out var oldValue);
        Games.TryUpdate(gameId, game, oldValue);
    }
}
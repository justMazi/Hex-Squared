using Application.IRepositories;
using Application.Services.Interfaces;
using Domain;
using Framework;
using LanguageExt;

namespace Application.Services.Implementations;

public class GameService(IGameRepository gameRepository) : IGameService
{
    private IGameRepository GameRepository { get; } = gameRepository;

    public Game GetOrCreate(GameId gameId, int radius = 10)
    {
        var game = GameRepository.GetById(gameId).ToOption();
        return game.Match(
            Some: existingGame => existingGame,
            None: () => GameRepository.CreateNewGame(gameId, radius)
        );
    }

    public Option<Game> Get(GameId gameId)
    {
        return GameRepository.GetById(gameId).ToOption();
    }

    public void SaveGame(Game game)
    {
        GameRepository.SaveGame(game);
    }



}
using Application.IRepositories;
using Application.Services.Interfaces;
using Domain;
using Domain.Players.MCTS;
using Framework;
using LanguageExt;

namespace Application.Services.Implementations;

public class GameService(IGameRepository gameRepository) : IGameService
{
    private IGameRepository GameRepository { get; } = gameRepository;

    public Game GetOrCreate(GameId gameId, int? radius = null, Type? aiType = null)
    {
        aiType ??= typeof(MctsPlayer);
        var game = GameRepository.GetById(gameId).ToOption();
        return game.Match(
            Some: existingGame => existingGame,
            None: () => GameRepository.CreateNewGame(gameId, radius, aiType)
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
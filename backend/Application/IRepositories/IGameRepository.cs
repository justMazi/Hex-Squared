using Domain;

namespace Application.IRepositories;

public interface IGameRepository
{
    public Game? GetById(GameId id);

    public Game CreateNewGame(GameId gameId, int? radius, Type? aiType);
    public List<Game?> GetAllInProgressGamesWithAi();
    public List<Game?> GetAllGames();

    public bool SaveGame(Game game);

    void DeleteGame(Game existingGame);
}
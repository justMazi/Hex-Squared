using Domain;

namespace Application.IRepositories;

public interface IGameRepository
{
    public Game? GetById(GameId id);

    public Game CreateNewGame(GameId gameId);
    public List<Game?> GetAllInProgressGamesWithAi();

    public bool SaveGame(Game game);

    void DeleteGame(Game existingGame);
}
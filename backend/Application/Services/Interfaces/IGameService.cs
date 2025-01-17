using Domain;
using LanguageExt;

namespace Application.Services.Interfaces;

public interface IGameService
{
    public Game GetOrCreate(GameId gameId, int radius = 10);
    public Option<Game> Get(GameId gameId);
}
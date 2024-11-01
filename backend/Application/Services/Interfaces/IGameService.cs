using Domain;
using LanguageExt;

namespace Application.Services.Interfaces;

public interface IGameService
{
    public Game GetOrCreate(GameId gameId);
    public Option<Game> Get(GameId gameId);
}
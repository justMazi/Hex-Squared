using Domain;
using LanguageExt;

namespace Application.Services.Interfaces;

public interface IGameService
{
    public Game GetOrCreate(GameId gameId, int? radius = null, Type? aiType = null);
    public Option<Game> Get(GameId gameId);
}
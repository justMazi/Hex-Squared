using Domain;

namespace Infrastructure.Exceptions;

public class CantAddGameException : Exception
{
    public CantAddGameException(GameId gameId)
    {
        throw new NotImplementedException();
    }
}
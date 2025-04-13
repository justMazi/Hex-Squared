using LanguageExt;

namespace Framework;

public static class OptionExtensions
{
    public static Option<T> ToOption<T>(this T? input) where T : class
    {
        return input is not null ? Option<T>.Some(input) : Option<T>.None;
    }
}
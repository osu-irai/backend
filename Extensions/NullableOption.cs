using OneOf.Monads;
using osuRequestor.Models;

namespace osuRequestor.Extensions;

public static class NullableOption
{
    public static Option<T> IntoOption<T>(this T? value)
    {
        return value is null ? Option<T>.None() : Option<T>.Some(value);
    }

    public static async Task<Option<T>> IntoOptionAsync<T>(this Task<T?> value)
    {
        var awaited = await value;
        return (awaited is null ? Option<T>.None() : Option<T>.Some(awaited));
    }
}

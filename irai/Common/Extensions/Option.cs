using ChiffadicErrors;

namespace irai.Common.Extensions;

public static class NullableOption
{

    public static async Task<Option<T>> IntoOptionAsync<T>(this Task<T?> value) where T : notnull
    {
        var awaited = await value;
        return awaited is null ? new None() : new Some<T>(awaited);
    }
}

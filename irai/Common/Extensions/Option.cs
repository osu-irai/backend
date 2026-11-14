namespace osuRequestor.Common.Extensions;

public struct Some<T>
{
    public T Value { get; }

    public Some(T value)
    {
        this.Value = value;
    }
}

public struct None { };

public union Option<T>(Some<T>, None) where T : notnull
{
    public T Unwrap()
    {
        return this switch
        {
            None none    => throw new NullReferenceException(),
            Some<T> some => some.Value,
            _            => throw new NullReferenceException()
        };
    }
};

public static class NullableOption
{
    public static Option<T> IntoOption<T>(this T? value)
    {
        return value is null ? new None() : new Some<T>(value);
    }

    public static async Task<Option<T>> IntoOptionAsync<T>(this Task<T?> value)
    {
        var awaited = await value;
        return awaited is null ? new None() : new Some<T>(awaited);
    }
}
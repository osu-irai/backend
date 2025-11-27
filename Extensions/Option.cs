using OneOf;

namespace osuRequestor.Extensions;

public struct Some<T>
{
    public T Value { get; }

    public Some(T value)
    {
        Value = value;
    }
}

public struct None
{
};

[GenerateOneOf]
public partial class Option<T> : OneOfBase<None, Some<T>>
{
    public static implicit operator Option<T>(T _)
    {
        return Some(_);
    }

    public static Option<T> None()
    {
        return new None();
    }

    public static Option<T> Some(T value)
    {
        return new Some<T>(value);
    }

    public bool IsNone()
    {
        return this.IsT0;
    }

    public bool IsSome()
    {
        return this.IsT1;
    }

    /// <summary>
    ///     Returns the current value. Will throw <c>NullReferenceException</c> if current option state is None.
    /// </summary>
    public new T Value()
    {
        return IsSome() ? this.AsT1.Value : throw new NullReferenceException();
    }
}
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
    public static implicit operator Option<T>(T _) => Some(_); 
    public static Option<T> None() => new None();
    public static Option<T> Some(T value) => new Some<T>(value);    
    public bool IsNone() => this.IsT0;
    public bool IsSome() => this.IsT1;

    /// <summary>
    /// Returns the current value. Will throw <c>NullReferenceException</c> if current option state is None.
    /// </summary>
    public new T Value() => IsSome() ? this.AsT1.Value : throw new NullReferenceException();
 
}
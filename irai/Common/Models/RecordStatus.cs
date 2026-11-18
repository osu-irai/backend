namespace irai.Common.Models;

public record Created<T>(T Value);
public record Updated<T>(T Value);

public union RecordStatus<T, TU>(Created<T>, Updated<TU>);
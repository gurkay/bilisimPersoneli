namespace BilisimPersoneli.Application.Common;

public class ServiceResult
{
    public bool Succeeded { get; init; }
    public string? Error { get; init; }

    public static ServiceResult Success() => new() { Succeeded = true };
    public static ServiceResult Fail(string error) => new() { Succeeded = false, Error = error };
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; init; }

    public static ServiceResult<T> Success(T data) => new() { Succeeded = true, Data = data };
    public new static ServiceResult<T> Fail(string error) => new() { Succeeded = false, Error = error };
}

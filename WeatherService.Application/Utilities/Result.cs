namespace WeatherService.Application.Utilities;

/// <summary>
/// Represents the result of an operation with success or failure status.
/// </summary>
public record Result
{
    /// <summary>
    /// Indicates whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Indicates whether the operation failed.
    /// </summary>
    public bool IsFailure => !IsSuccess;

    /// <summary>
    /// Collection of error messages if the operation failed.
    /// </summary>
    public IReadOnlyCollection<string> Errors { get; }

    /// <summary>
    /// Creates a new Result instance.
    /// </summary>
    protected Result(bool isSuccess, IEnumerable<string>? errors)
    {
        if (isSuccess && errors != null && errors.Any())
            throw new InvalidOperationException("A successful result cannot contain errors.");

        if (!isSuccess && (errors == null || !errors.Any()))
            throw new InvalidOperationException("A failure result must contain at least one error.");

        IsSuccess = isSuccess;
        Errors = errors?.ToList().AsReadOnly() ?? new List<string>().AsReadOnly();
    }

    /// <summary>
    /// Creates a success result.
    /// </summary>
    public static Result Success() => new(true, null);

    /// <summary>
    /// Creates a failure result with the specified errors.
    /// </summary>
    public static Result Failure(params string[] errors) => new(false, errors);

    /// <summary>
    /// Creates a failure result with the specified errors.
    /// </summary>
    public static Result Failure(IEnumerable<string> errors) => new(false, errors);
}

/// <summary>
/// Represents the result of an operation with success or failure status and a value on success.
/// </summary>
public record Result<T> : Result
{
    /// <summary>
    /// The value returned by the operation if successful.
    /// </summary>
    public T Value { get; }

    private Result(T? value, bool isSuccess, IEnumerable<string>? errors)
        : base(isSuccess, errors)
    {
        if (isSuccess && value is null)
            throw new InvalidOperationException("A successful result with value cannot have a null value.");

        Value = value!;
    }

    /// <summary>
    /// Creates a success result with the specified value.
    /// </summary>
    public static Result<T> Success(T value) => new(value, true, null);

    /// <summary>
    /// Creates a failure result with the specified errors.
    /// </summary>
    public new static Result<T> Failure(params string[] errors) => new(default, false, errors);

    /// <summary>
    /// Creates a failure result with the specified errors.
    /// </summary>
    public new static Result<T> Failure(IEnumerable<string> errors) => new(default, false, errors);
}


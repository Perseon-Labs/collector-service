namespace CollectorService.Models
{
    public class Result<T>
    {
        private readonly T? _value;
        private readonly string? _errorMessage; // Just a string for the error

        public bool IsSuccess { get; }

        // Access the value if successful
        public T Value => IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access Value when IsSuccess is false. Check IsFailure first.");

        // Access the error message if failed
        public string? ErrorMessage => _errorMessage;

        // Private constructor for a successful result
        private Result(T value)
        {
            IsSuccess = true;
            _value = value;
            _errorMessage = null; // No error message on success
        }

        // Private constructor for a failed result
        private Result(string errorMessage)
        {
            IsSuccess = false;
            _value = default; // Default value for T on failure
            _errorMessage = errorMessage ?? throw new ArgumentNullException(nameof(errorMessage),
                "Error message cannot be null for a failed result.");
        }

        // Factory method for a successful result
        public static Result<T> Success(T value) => new(value);

        // Factory method for a failed result
        public static Result<T> Failure(string errorMessage) => new(errorMessage);

        // Implicit conversion from T (for success)
        public static implicit operator Result<T>(T value) => Success(value);

        // Implicit conversion from string (for failure)
        public static implicit operator Result<T>(string errorMessage) => Failure(errorMessage);
    }
}

namespace DoanPhi.Application.Common.Exceptions;

public class ValidationException : Exception
{
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public ValidationException(string message) : base(message)
    {
        Errors = new Dictionary<string, string[]> { { "General", [message] } };
    }

    public ValidationException(Dictionary<string, string[]> errors) 
        : base("Dữ liệu khai báo không hợp lệ.")
    {
        Errors = errors;
    }
}

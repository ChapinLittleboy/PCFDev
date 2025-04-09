namespace FileProcessingLib.Excel;

public class ProcessResult
{
    public bool IsSuccess { get; }
    public string Message { get; }

    private ProcessResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static ProcessResult Success(string message = null)
    {
        return new ProcessResult(true, message);
    }

    public static ProcessResult Failure(string message)
    {
        return new ProcessResult(false, message);
    }
}
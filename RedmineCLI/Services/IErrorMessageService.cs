namespace RedmineCLI.Services;

public interface IErrorMessageService
{
    string GetUserFriendlyMessage(Exception exception);
    string? GetSuggestion(Exception exception);
}
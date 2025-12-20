namespace MinimalEndpoints.AdvancedSample.Services;

public interface IAuthenticationService
{
    bool ValidateToken(string token);
}

public class SimpleAuthenticationService : IAuthenticationService
{
    // Simple token validation for demo purposes
    public bool ValidateToken(string token)
    {
        return !string.IsNullOrEmpty(token) && token.StartsWith("Bearer");
    }
}


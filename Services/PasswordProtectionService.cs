using Microsoft.AspNetCore.DataProtection;

namespace EMPTRACK.Services;

public interface IPasswordProtectionService
{
    string Protect(string password);
    string? Unprotect(string? protectedPassword);
}

public sealed class PasswordProtectionService : IPasswordProtectionService
{
    private readonly IDataProtector _protector;

    public PasswordProtectionService(IDataProtectionProvider provider)
    {
        _protector = provider.CreateProtector("EMPTRACK.UserAccount.Password.v1");
    }

    public string Protect(string password) => _protector.Protect(password);

    public string? Unprotect(string? protectedPassword)
    {
        if (string.IsNullOrWhiteSpace(protectedPassword))
            return null;

        try
        {
            return _protector.Unprotect(protectedPassword);
        }
        catch (Exception)
        {
            // Never expose protection/decryption details to the UI.
            return null;
        }
    }
}

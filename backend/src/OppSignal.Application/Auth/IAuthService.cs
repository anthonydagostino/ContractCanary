namespace OppSignal.Application.Auth;

public interface IAuthService
{
    Task RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<AuthTokens> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthTokens> RefreshAsync(string refreshToken, CancellationToken ct = default);
    Task LogoutAsync(string refreshToken, CancellationToken ct = default);

    Task ConfirmEmailAsync(ConfirmEmailRequest request, CancellationToken ct = default);
    Task ResendVerificationAsync(string email, CancellationToken ct = default);
    Task ForgotPasswordAsync(string email, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);

    Task<MeDto> GetMeAsync(Guid userId, CancellationToken ct = default);
    Task UpdateAccountAsync(Guid userId, UpdateAccountRequest request, CancellationToken ct = default);

    /// <summary>Change the caller's password (verifying the current one) and re-issue tokens;
    /// all other sessions are revoked.</summary>
    Task<AuthTokens> ChangePasswordAsync(Guid userId, ChangePasswordRequest request, CancellationToken ct = default);

    /// <summary>Export all of the caller's personal data (right to access / portability).</summary>
    Task<AccountExport> ExportDataAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Permanently delete the caller's account and all personal data (right to erasure),
    /// after verifying their password. Cancels any Stripe subscription first.</summary>
    Task DeleteAccountAsync(Guid userId, string password, CancellationToken ct = default);
}

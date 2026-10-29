namespace DoanPhi.Domain.Entities;

public class AdminUser
{
    public Guid Id { get; private set; }
    public string Username { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FullName { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    public DateTime? LastLogin { get; private set; }

    public AdminUser(
        Guid id,
        string username,
        string passwordHash,
        string fullName,
        DateTime createdAt,
        DateTime? lastLogin = null)
    {
        Id = id;
        Username = username.Trim().ToLowerInvariant();
        PasswordHash = passwordHash;
        FullName = fullName.Trim();
        CreatedAt = createdAt;
        LastLogin = lastLogin;
    }

    public static AdminUser Create(string username, string passwordHash, string fullName)
    {
        return new AdminUser(
            Guid.NewGuid(),
            username,
            passwordHash,
            fullName,
            DateTime.UtcNow
        );
    }

    public void UpdatePassword(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
        {
            throw new ArgumentException("Password hash cannot be empty.", nameof(newPasswordHash));
        }
        PasswordHash = newPasswordHash;
    }

    public void RecordLogin()
    {
        LastLogin = DateTime.UtcNow;
    }
}

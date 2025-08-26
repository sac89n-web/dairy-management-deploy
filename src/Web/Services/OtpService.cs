using Dapper;
using Npgsql;

namespace Dairy.Web.Services;

public class OtpService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public OtpService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task<string> GenerateOtpAsync(string mobile)
    {
        var otp = "2025"; // Fixed OTP for demo
        var expiresAt = DateTime.UtcNow.AddMinutes(5);
        
        var connectionString = _httpContextAccessor.HttpContext?.Session.GetString("ConnectionString");
        if (string.IsNullOrEmpty(connectionString))
            throw new InvalidOperationException("Database not connected");

        using var connection = new NpgsqlConnection(connectionString);
        
        await connection.ExecuteAsync(
            "INSERT INTO user_otp (mobile, otp, expires_at) VALUES (@mobile, @otp, @expiresAt)",
            new { mobile, otp, expiresAt });

        return otp;
    }

    public async Task<bool> ValidateOtpAsync(string mobile, string otp)
    {
        var connectionString = _httpContextAccessor.HttpContext?.Session.GetString("ConnectionString");
        if (string.IsNullOrEmpty(connectionString))
            return false;

        using var connection = new NpgsqlConnection(connectionString);
        
        var validOtp = await connection.QueryFirstOrDefaultAsync<dynamic>(
            @"SELECT id FROM user_otp 
              WHERE mobile = @mobile AND otp = @otp 
              AND expires_at > @now AND is_used = false",
            new { mobile, otp, now = DateTime.UtcNow });

        if (validOtp != null)
        {
            await connection.ExecuteAsync(
                "UPDATE user_otp SET is_used = true WHERE id = @id",
                new { id = validOtp.id });
            return true;
        }

        return false;
    }

    public async Task<dynamic?> GetUserByMobileAsync(string mobile)
    {
        var connectionString = _httpContextAccessor.HttpContext?.Session.GetString("ConnectionString");
        if (string.IsNullOrEmpty(connectionString))
            return null;

        using var connection = new NpgsqlConnection(connectionString);
        
        return await connection.QueryFirstOrDefaultAsync<dynamic>(
            "SELECT * FROM user_master WHERE mobile = @mobile AND is_active = true",
            new { mobile });
    }
}
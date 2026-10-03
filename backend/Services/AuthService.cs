using backend.Data;
using backend.Dtos;
using backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace backend.Services;

public class AuthService
{
    private readonly AppDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public AuthService(AppDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Response)> SignupAsync(SignupDto dto)
    {
        var username = dto.Username.Trim();
        var email = dto.Email.Trim().ToLowerInvariant();

        if (string.IsNullOrWhiteSpace(username))
            return (false, "Username is required.", null);

        if (string.IsNullOrWhiteSpace(email))
            return (false, "Email is required.", null);

        if (string.IsNullOrWhiteSpace(dto.Password))
            return (false, "Password is required.", null);

        if (await _context.Users.AnyAsync(u => u.Username == username))
            return (false, "Username is already taken.", null);

        if (await _context.Users.AnyAsync(u => u.Email == email))
            return (false, "Email is already registered.", null);

        var user = new User
        {
            Username = username,
            Email = email
        };

        user.PasswordHash = _passwordHasher.HashPassword(
            user,
            dto.Password
        );

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return (
            true,
            null,
            new AuthResponseDto
            {
                Username = user.Username
            }
        );
    }

    public async Task<(bool Success, string? Error, AuthResponseDto? Response)> LoginAsync(LoginDto dto)
    {
        var username = dto.Username.Trim();

        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Username == username);

        if (user is null)
            return (false, "Invalid username or password.", null);

        var result = _passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            dto.Password
        );

        if (result == PasswordVerificationResult.Failed)
            return (false, "Invalid username or password.", null);

        return (
            true,
            null,
            new AuthResponseDto
            {
                Username = user.Username
            }
        );
    }
}
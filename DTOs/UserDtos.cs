using System.ComponentModel.DataAnnotations;

namespace ProjectsDonetskWaterHope.DTOs
{
    public record RegisterUserDto(
        [Required] string FirstName,
        [Required] string LastName,
        [Required, EmailAddress] string Email,
        [Required, MinLength(6)] string Password,
        string? PhoneNumber
    );

    // «Ã≤Õ¿ “”“: ¬ı≥‰ Ú≥Î¸ÍË Á‡ AccountNumber
    public record LoginDto(
        [Required] string AccountNumber,
        [Required] string Password
    );

    // «Ã≤Õ¿ “”“: ƒÓ‰‡ÎË AccountNumber Û ‚Ë‚≥‰
    public record UserDto(
        int UserId,
        string AccountNumber,
        string FirstName,
        string LastName,
        string Email,
        string? PhoneNumber,
        string Role
    );

    public record AuthResponseDto(
        string Token,
        UserDto User
    );

    public record UpdateUserDto(
        string? FirstName,
        string? LastName,
        string? PhoneNumber,
        string? Email,
        string? Role
    );
}
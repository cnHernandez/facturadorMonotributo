using System.ComponentModel.DataAnnotations;

namespace Back.DTOs
{
    public class LoginDTO
    {
        [Required]
        [StringLength(100)]
        public string UserName { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Password { get; set; } = null!;
    }

    public class LoginResponseDTO
    {
        public string Token { get; set; } = null!;
        public DateTime ExpirationUtc { get; set; }
        public string UserName { get; set; } = null!;
        public string Role { get; set; } = null!;
    }

    public class ChangePasswordDTO
    {
        [Required]
        [StringLength(200)]
        public string CurrentPassword { get; set; } = null!;

        [Required]
        [MinLength(8)]
        [StringLength(200)]
        public string NewPassword { get; set; } = null!;
    }
}

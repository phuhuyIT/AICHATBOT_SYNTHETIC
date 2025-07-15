using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO.Auth
{
    public class ResendEmailConfirmationDTO
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email format")]
        public string Email { get; set; }
    }
}


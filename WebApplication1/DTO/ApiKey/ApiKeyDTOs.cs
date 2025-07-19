using System.ComponentModel.DataAnnotations;

namespace WebApplication1.DTO.ApiKey
{
    public class ApiKeyCreateDTO
    {
        [Required]
        [StringLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
        public string ApiKey { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters")]
        public string ServiceName { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Setting cannot exceed 1000 characters")]
        public string? Setting { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Usage cannot exceed 500 characters")]
        public string Usage { get; set; } = null!;

        [Required]
        public Guid ChatbotModelId { get; set; }

        public bool IsActive { get; set; } = true;

        public string? UserId { get; set; }
    }

    public class ApiKeyUpdateDTO
    {
        [Required]
        public Guid ApiKeyId { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
        public string ApiKey { get; set; } = null!;

        [Required]
        [StringLength(100, ErrorMessage = "Service name cannot exceed 100 characters")]
        public string ServiceName { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "Setting cannot exceed 1000 characters")]
        public string? Setting { get; set; }

        [Required]
        [StringLength(500, ErrorMessage = "Usage cannot exceed 500 characters")]
        public string Usage { get; set; } = null!;

        [Required]
        public Guid ChatbotModelId { get; set; }

        public bool IsActive { get; set; } = true;

        public string? UserId { get; set; }
    }

    public class ApiKeyResponseDTO
    {
        public Guid ApiKeyId { get; set; }
        public string? UserId { get; set; }
        public string ApiKey { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public string? Setting { get; set; }
        public string Usage { get; set; } = null!;
        public Guid ChatbotModelId { get; set; }
        public bool IsActive { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public string? ChatbotModelName { get; set; }
        public string? UserName { get; set; }
    }

    public class ApiKeyValidationDTO
    {
        [Required]
        [StringLength(500, ErrorMessage = "API key cannot exceed 500 characters")]
        public string ApiKey { get; set; } = null!;
    }

    public class ApiKeyListDTO
    {
        public Guid ApiKeyId { get; set; }
        public string ApiKey { get; set; } = null!;
        public string ServiceName { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ChatbotModelName { get; set; }
        public string? Usage { get; set; }
    }

    public class BulkApiKeyCreateDTO
    {
        [Required]
        public Guid ChatbotModelId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one API key is required")]
        public List<ApiKeyCreateDTO> ApiKeys { get; set; } = new();
    }

    public class BulkApiKeyUpdateDTO
    {
        [Required]
        public Guid ChatbotModelId { get; set; }

        [Required]
        [MinLength(1, ErrorMessage = "At least one API key is required")]
        public List<ApiKeyUpdateDTO> ApiKeys { get; set; } = new();
    }
}

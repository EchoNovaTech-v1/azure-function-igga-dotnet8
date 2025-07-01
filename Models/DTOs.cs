using System.ComponentModel.DataAnnotations;

namespace AzureFunctionIgga.Models.DTOs;

public record UserDto
{
    public int Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string? PhoneNumber { get; init; }
    public string Role { get; init; } = string.Empty;
    public bool IsActive { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public string? ProfileImageUrl { get; init; }
}

public record CreateUserDto
{
    [Required]
    [MaxLength(100)]
    public string FirstName { get; init; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string LastName { get; init; } = string.Empty;

    [Required]
    [EmailAddress]
    [MaxLength(255)]
    public string Email { get; init; } = string.Empty;

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [Required]
    [MaxLength(50)]
    public string Role { get; init; } = "User";

    [MaxLength(500)]
    public string? ProfileImageUrl { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

public record UpdateUserDto
{
    [MaxLength(100)]
    public string? FirstName { get; init; }

    [MaxLength(100)]
    public string? LastName { get; init; }

    [EmailAddress]
    [MaxLength(255)]
    public string? Email { get; init; }

    [MaxLength(20)]
    public string? PhoneNumber { get; init; }

    [MaxLength(50)]
    public string? Role { get; init; }

    public bool? IsActive { get; init; }

    [MaxLength(500)]
    public string? ProfileImageUrl { get; init; }

    [MaxLength(1000)]
    public string? Notes { get; init; }
}

public record DataRecordDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string Category { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public decimal Value { get; init; }
    public DateTime RecordDate { get; init; }
    public DateTime CreatedAt { get; init; }
    public int UserId { get; init; }
    public string? DocumentUrl { get; init; }
    public UserDto? User { get; init; }
}

public record CreateDataRecordDto
{
    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [MaxLength(2000)]
    public string? Description { get; init; }

    [Required]
    [MaxLength(100)]
    public string Category { get; init; } = string.Empty;

    [Required]
    public decimal Value { get; init; }

    [Required]
    public DateTime RecordDate { get; init; }

    [Required]
    public int UserId { get; init; }

    [MaxLength(500)]
    public string? DocumentUrl { get; init; }

    public Dictionary<string, object>? Metadata { get; init; }
}

public record UpdateDataRecordDto
{
    [MaxLength(255)]
    public string? Title { get; init; }

    [MaxLength(2000)]
    public string? Description { get; init; }

    [MaxLength(100)]
    public string? Category { get; init; }

    [MaxLength(50)]
    public string? Status { get; init; }

    public decimal? Value { get; init; }

    public DateTime? RecordDate { get; init; }

    [MaxLength(500)]
    public string? DocumentUrl { get; init; }

    public Dictionary<string, object>? Metadata { get; init; }
}

public record ProcessingJobDto
{
    public int Id { get; init; }
    public string JobName { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
    public string? ErrorMessage { get; init; }
    public int UserId { get; init; }
    public int Progress { get; init; }
    public Dictionary<string, object>? Parameters { get; init; }
    public Dictionary<string, object>? Result { get; init; }
}

public record CreateProcessingJobDto
{
    [Required]
    [MaxLength(255)]
    public string JobName { get; init; } = string.Empty;

    [Required]
    public int UserId { get; init; }

    public Dictionary<string, object>? Parameters { get; init; }
}

public record NotificationDto
{
    public int Id { get; init; }
    public string Title { get; init; } = string.Empty;
    public string Message { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public bool IsRead { get; init; }
    public DateTime CreatedAt { get; init; }
    public DateTime? ReadAt { get; init; }
    public int UserId { get; init; }
    public string? ActionUrl { get; init; }
    public DateTime? ExpiresAt { get; init; }
}

public record CreateNotificationDto
{
    [Required]
    [MaxLength(255)]
    public string Title { get; init; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; init; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Type { get; init; } = "Info";

    [Required]
    public int UserId { get; init; }

    [MaxLength(500)]
    public string? ActionUrl { get; init; }

    public DateTime? ExpiresAt { get; init; }
}

public record ApiResponse<T>
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public T? Data { get; init; }
    public Dictionary<string, string[]>? Errors { get; init; }
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

public record PagedResult<T>
{
    public IEnumerable<T> Items { get; init; } = Enumerable.Empty<T>();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;
}

public record PaginationQuery
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public string? SearchTerm { get; init; }
    public string? SortBy { get; init; }
    public bool SortDescending { get; init; } = false;
}
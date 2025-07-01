using AzureFunctionIgga.Models;
using AzureFunctionIgga.Models.DTOs;

namespace AzureFunctionIgga.Services.Interfaces;

public interface IUserService
{
    Task<ApiResponse<UserDto>> GetUserByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<UserDto>>> GetUsersAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> CreateUserAsync(CreateUserDto createUserDto, CancellationToken cancellationToken = default);
    Task<ApiResponse<UserDto>> UpdateUserAsync(int id, UpdateUserDto updateUserDto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteUserAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ActivateUserAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeactivateUserAsync(int id, CancellationToken cancellationToken = default);
}

public interface IDataProcessingService
{
    Task<ApiResponse<DataRecordDto>> GetDataRecordByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<DataRecordDto>>> GetDataRecordsAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<DataRecordDto>>> GetDataRecordsByUserAsync(int userId, PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<DataRecordDto>> CreateDataRecordAsync(CreateDataRecordDto createDataRecordDto, CancellationToken cancellationToken = default);
    Task<ApiResponse<DataRecordDto>> UpdateDataRecordAsync(int id, UpdateDataRecordDto updateDataRecordDto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteDataRecordAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProcessingJobDto>> ProcessDataAsync(int dataRecordId, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GenerateReportAsync(string reportType, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
}

public interface INotificationService
{
    Task<ApiResponse<NotificationDto>> GetNotificationByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<NotificationDto>>> GetNotificationsByUserAsync(int userId, PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<NotificationDto>> CreateNotificationAsync(CreateNotificationDto createNotificationDto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> MarkAsReadAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> MarkAllAsReadAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteNotificationAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<int>> GetUnreadCountAsync(int userId, CancellationToken cancellationToken = default);
    Task SendRealTimeNotificationAsync(int userId, string title, string message, string type = "Info");
}

public interface IReportService
{
    Task<ApiResponse<byte[]>> GeneratePdfReportAsync(string reportType, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<ApiResponse<byte[]>> GenerateExcelReportAsync(string reportType, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GenerateChartDataAsync(string chartType, Dictionary<string, object> parameters, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetDashboardDataAsync(int userId, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetAnalyticsDataAsync(string analyticsType, DateTime fromDate, DateTime toDate, CancellationToken cancellationToken = default);
}

public interface IAuthenticationService
{
    Task<ApiResponse<object>> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> RefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ValidateTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> RevokeTokenAsync(string token, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ResetPasswordAsync(string email, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> ChangePasswordAsync(int userId, string currentPassword, string newPassword, CancellationToken cancellationToken = default);
}

public interface IFileStorageService
{
    Task<ApiResponse<string>> UploadFileAsync(Stream fileStream, string fileName, string contentType, CancellationToken cancellationToken = default);
    Task<ApiResponse<Stream>> DownloadFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> DeleteFileAsync(string fileUrl, CancellationToken cancellationToken = default);
    Task<ApiResponse<string>> GetFileUrlAsync(string fileName, TimeSpan expiry, CancellationToken cancellationToken = default);
    Task<ApiResponse<object>> GetFileMetadataAsync(string fileUrl, CancellationToken cancellationToken = default);
}

public interface IEmailService
{
    Task<ApiResponse<bool>> SendEmailAsync(string to, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> SendEmailWithAttachmentAsync(string to, string subject, string body, IEnumerable<EmailAttachment> attachments, bool isHtml = true, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> SendBulkEmailAsync(IEnumerable<string> recipients, string subject, string body, bool isHtml = true, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> SendTemplatedEmailAsync(string to, string templateId, Dictionary<string, object> templateData, CancellationToken cancellationToken = default);
}

public interface IAuditService
{
    Task<ApiResponse<PagedResult<object>>> GetAuditLogsAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<object>>> GetAuditLogsByEntityAsync(string entityType, int entityId, PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<object>>> GetAuditLogsByUserAsync(int userId, PaginationQuery query, CancellationToken cancellationToken = default);
    Task LogActionAsync(string action, string entityType, int entityId, int userId, object? oldValues = null, object? newValues = null, string? ipAddress = null, string? userAgent = null);
}

public interface IProcessingJobService
{
    Task<ApiResponse<ProcessingJobDto>> GetJobByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<ProcessingJobDto>>> GetJobsAsync(PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<PagedResult<ProcessingJobDto>>> GetJobsByUserAsync(int userId, PaginationQuery query, CancellationToken cancellationToken = default);
    Task<ApiResponse<ProcessingJobDto>> CreateJobAsync(CreateProcessingJobDto createJobDto, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> UpdateJobStatusAsync(int id, string status, int progress = 0, string? errorMessage = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CompleteJobAsync(int id, Dictionary<string, object>? result = null, CancellationToken cancellationToken = default);
    Task<ApiResponse<bool>> CancelJobAsync(int id, CancellationToken cancellationToken = default);
    Task ProcessPendingJobsAsync(CancellationToken cancellationToken = default);
}

// DTOs auxiliares
public record EmailAttachment
{
    public string FileName { get; init; } = string.Empty;
    public byte[] Content { get; init; } = Array.Empty<byte>();
    public string ContentType { get; init; } = "application/octet-stream";
}

public record AuthenticationResult
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public UserDto User { get; init; } = null!;
}

public record FileUploadResult
{
    public string FileUrl { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public long FileSize { get; init; }
    public string ContentType { get; init; } = string.Empty;
    public DateTime UploadedAt { get; init; } = DateTime.UtcNow;
}
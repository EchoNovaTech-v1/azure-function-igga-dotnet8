using AutoMapper;
using AzureFunctionIgga.Models;
using AzureFunctionIgga.Models.DTOs;

namespace AzureFunctionIgga.Mappings;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        ConfigureUserMappings();
        ConfigureDataRecordMappings();
        ConfigureProcessingJobMappings();
        ConfigureNotificationMappings();
        ConfigureAuditLogMappings();
    }

    private void ConfigureUserMappings()
    {
        // User -> UserDto
        CreateMap<User, UserDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.LastLoginAt, opt => opt.MapFrom(src => src.LastLoginAt))
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImageUrl));

        // CreateUserDto -> User
        CreateMap<CreateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.MapFrom(src => src.FirstName))
            .ForMember(dest => dest.LastName, opt => opt.MapFrom(src => src.LastName))
            .ForMember(dest => dest.Email, opt => opt.MapFrom(src => src.Email))
            .ForMember(dest => dest.PhoneNumber, opt => opt.MapFrom(src => src.PhoneNumber))
            .ForMember(dest => dest.Role, opt => opt.MapFrom(src => src.Role))
            .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.MapFrom(src => src.ProfileImageUrl))
            .ForMember(dest => dest.Notes, opt => opt.MapFrom(src => src.Notes))
            .ForMember(dest => dest.DataRecords, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());

        // UpdateUserDto -> User (para actualizaciones parciales)
        CreateMap<UpdateUserDto, User>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.FirstName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.FirstName)))
            .ForMember(dest => dest.LastName, opt => opt.Condition(src => !string.IsNullOrEmpty(src.LastName)))
            .ForMember(dest => dest.Email, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Email)))
            .ForMember(dest => dest.PhoneNumber, opt => opt.Condition(src => !string.IsNullOrEmpty(src.PhoneNumber)))
            .ForMember(dest => dest.Role, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Role)))
            .ForMember(dest => dest.IsActive, opt => opt.Condition(src => src.IsActive.HasValue))
            .ForMember(dest => dest.ProfileImageUrl, opt => opt.Condition(src => !string.IsNullOrEmpty(src.ProfileImageUrl)))
            .ForMember(dest => dest.Notes, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Notes)))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.LastLoginAt, opt => opt.Ignore())
            .ForMember(dest => dest.DataRecords, opt => opt.Ignore())
            .ForMember(dest => dest.AuditLogs, opt => opt.Ignore());
    }

    private void ConfigureDataRecordMappings()
    {
        // DataRecord -> DataRecordDto
        CreateMap<DataRecord, DataRecordDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.DocumentUrl, opt => opt.MapFrom(src => src.DocumentUrl))
            .ForMember(dest => dest.User, opt => opt.MapFrom(src => src.User));

        // CreateDataRecordDto -> DataRecord
        CreateMap<CreateDataRecordDto, DataRecord>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Description, opt => opt.MapFrom(src => src.Description))
            .ForMember(dest => dest.Category, opt => opt.MapFrom(src => src.Category))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Pending"))
            .ForMember(dest => dest.Value, opt => opt.MapFrom(src => src.Value))
            .ForMember(dest => dest.RecordDate, opt => opt.MapFrom(src => src.RecordDate))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.DocumentUrl, opt => opt.MapFrom(src => src.DocumentUrl))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => 
                src.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(src.Metadata) : null))
            .ForMember(dest => dest.User, opt => opt.Ignore());

        // UpdateDataRecordDto -> DataRecord
        CreateMap<UpdateDataRecordDto, DataRecord>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Title)))
            .ForMember(dest => dest.Description, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Description)))
            .ForMember(dest => dest.Category, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Category)))
            .ForMember(dest => dest.Status, opt => opt.Condition(src => !string.IsNullOrEmpty(src.Status)))
            .ForMember(dest => dest.Value, opt => opt.Condition(src => src.Value.HasValue))
            .ForMember(dest => dest.RecordDate, opt => opt.Condition(src => src.RecordDate.HasValue))
            .ForMember(dest => dest.DocumentUrl, opt => opt.Condition(src => !string.IsNullOrEmpty(src.DocumentUrl)))
            .ForMember(dest => dest.Metadata, opt => opt.MapFrom(src => 
                src.Metadata != null ? System.Text.Json.JsonSerializer.Serialize(src.Metadata) : null))
            .ForMember(dest => dest.UpdatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
    }

    private void ConfigureProcessingJobMappings()
    {
        // ProcessingJob -> ProcessingJobDto
        CreateMap<ProcessingJob, ProcessingJobDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.JobName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => src.Status))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.StartedAt, opt => opt.MapFrom(src => src.StartedAt))
            .ForMember(dest => dest.CompletedAt, opt => opt.MapFrom(src => src.CompletedAt))
            .ForMember(dest => dest.ErrorMessage, opt => opt.MapFrom(src => src.ErrorMessage))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Progress, opt => opt.MapFrom(src => src.Progress))
            .ForMember(dest => dest.Parameters, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Parameters) ? 
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(src.Parameters) : 
                null))
            .ForMember(dest => dest.Result, opt => opt.MapFrom(src => 
                !string.IsNullOrEmpty(src.Result) ? 
                System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(src.Result) : 
                null));

        // CreateProcessingJobDto -> ProcessingJob
        CreateMap<CreateProcessingJobDto, ProcessingJob>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.JobName, opt => opt.MapFrom(src => src.JobName))
            .ForMember(dest => dest.Status, opt => opt.MapFrom(src => "Queued"))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.StartedAt, opt => opt.Ignore())
            .ForMember(dest => dest.CompletedAt, opt => opt.Ignore())
            .ForMember(dest => dest.ErrorMessage, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.Progress, opt => opt.MapFrom(src => 0))
            .ForMember(dest => dest.Parameters, opt => opt.MapFrom(src => 
                src.Parameters != null ? System.Text.Json.JsonSerializer.Serialize(src.Parameters) : null))
            .ForMember(dest => dest.Result, opt => opt.Ignore())
            .ForMember(dest => dest.User, opt => opt.Ignore());
    }

    private void ConfigureNotificationMappings()
    {
        // Notification -> NotificationDto
        CreateMap<Notification, NotificationDto>()
            .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.Id))
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => src.IsRead))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => src.CreatedAt))
            .ForMember(dest => dest.ReadAt, opt => opt.MapFrom(src => src.ReadAt))
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.ActionUrl, opt => opt.MapFrom(src => src.ActionUrl))
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt));

        // CreateNotificationDto -> Notification
        CreateMap<CreateNotificationDto, Notification>()
            .ForMember(dest => dest.Id, opt => opt.Ignore())
            .ForMember(dest => dest.Title, opt => opt.MapFrom(src => src.Title))
            .ForMember(dest => dest.Message, opt => opt.MapFrom(src => src.Message))
            .ForMember(dest => dest.Type, opt => opt.MapFrom(src => src.Type))
            .ForMember(dest => dest.IsRead, opt => opt.MapFrom(src => false))
            .ForMember(dest => dest.CreatedAt, opt => opt.MapFrom(src => DateTime.UtcNow))
            .ForMember(dest => dest.ReadAt, opt => opt.Ignore())
            .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.UserId))
            .ForMember(dest => dest.ActionUrl, opt => opt.MapFrom(src => src.ActionUrl))
            .ForMember(dest => dest.ExpiresAt, opt => opt.MapFrom(src => src.ExpiresAt))
            .ForMember(dest => dest.User, opt => opt.Ignore());
    }

    private void ConfigureAuditLogMappings()
    {
        // AuditLog -> Object (para DTOs de auditoría)
        CreateMap<AuditLog, object>()
            .ConvertUsing(src => new
            {
                Id = src.Id,
                Action = src.Action,
                EntityType = src.EntityType,
                EntityId = src.EntityId,
                Timestamp = src.Timestamp,
                UserId = src.UserId,
                OldValues = !string.IsNullOrEmpty(src.OldValues) ? 
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(src.OldValues) : 
                    null,
                NewValues = !string.IsNullOrEmpty(src.NewValues) ? 
                    System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(src.NewValues) : 
                    null,
                IpAddress = src.IpAddress,
                UserAgent = src.UserAgent,
                User = src.User != null ? new { src.User.FirstName, src.User.LastName, src.User.Email } : null
            });
    }
}
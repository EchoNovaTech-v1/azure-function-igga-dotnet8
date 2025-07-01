using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using AzureFunctionIgga.Models;
using System.Text.Json;

namespace AzureFunctionIgga.Data;

public class IggaDbContext : DbContext
{
    public IggaDbContext(DbContextOptions<IggaDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<DataRecord> DataRecords { get; set; }
    public DbSet<AuditLog> AuditLogs { get; set; }
    public DbSet<ProcessingJob> ProcessingJobs { get; set; }
    public DbSet<Notification> Notifications { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configuración global
        ConfigureGlobalSettings(modelBuilder);

        // Configuración de entidades
        ConfigureUser(modelBuilder);
        ConfigureDataRecord(modelBuilder);
        ConfigureAuditLog(modelBuilder);
        ConfigureProcessingJob(modelBuilder);
        ConfigureNotification(modelBuilder);

        // Configuración de índices
        ConfigureIndexes(modelBuilder);

        // Datos semilla
        SeedData(modelBuilder);
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (!optionsBuilder.IsConfigured)
        {
            // Configuración adicional si es necesario
            optionsBuilder.EnableDetailedErrors();
            optionsBuilder.EnableSensitiveDataLogging(false);
        }
    }

    private static void ConfigureGlobalSettings(ModelBuilder modelBuilder)
    {
        // Configuración para conversores JSON
        var jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        var jsonConverter = new ValueConverter<Dictionary<string, object>?, string>(
            v => v == null ? null : JsonSerializer.Serialize(v, jsonOptions),
            v => v == null ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(v, jsonOptions));

        // Aplicar conversores JSON a propiedades específicas
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (var property in entityType.GetProperties())
            {
                if (property.ClrType == typeof(Dictionary<string, object>) || 
                    property.ClrType == typeof(Dictionary<string, object>?))
                {
                    property.SetValueConverter(jsonConverter);
                }
            }
        }
    }

    private static void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            
            entity.Property(e => e.Email)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.FirstName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.LastName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Role)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("User");

            entity.Property(e => e.IsActive)
                .IsRequired()
                .HasDefaultValue(true);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.PhoneNumber)
                .HasMaxLength(20);

            entity.Property(e => e.ProfileImageUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Notes)
                .HasMaxLength(1000);

            // Configuración de relaciones
            entity.HasMany(e => e.DataRecords)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(e => e.AuditLogs)
                .WithOne(e => e.User)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }

    private static void ConfigureDataRecord(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DataRecord>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Description)
                .HasMaxLength(2000);

            entity.Property(e => e.Category)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Pending");

            entity.Property(e => e.Value)
                .IsRequired()
                .HasPrecision(18, 2);

            entity.Property(e => e.RecordDate)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.DocumentUrl)
                .HasMaxLength(500);

            entity.Property(e => e.Metadata)
                .HasColumnType("nvarchar(max)");
        });
    }

    private static void ConfigureAuditLog(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Action)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.EntityType)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(e => e.Timestamp)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.IpAddress)
                .HasMaxLength(45);

            entity.Property(e => e.UserAgent)
                .HasMaxLength(500);

            entity.Property(e => e.OldValues)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.NewValues)
                .HasColumnType("nvarchar(max)");
        });
    }

    private static void ConfigureProcessingJob(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ProcessingJob>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.JobName)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Status)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Queued");

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.ErrorMessage)
                .HasMaxLength(2000);

            entity.Property(e => e.Progress)
                .HasDefaultValue(0);

            entity.Property(e => e.Parameters)
                .HasColumnType("nvarchar(max)");

            entity.Property(e => e.Result)
                .HasColumnType("nvarchar(max)");

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureNotification(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Notification>(entity =>
        {
            entity.HasKey(e => e.Id);

            entity.Property(e => e.Title)
                .IsRequired()
                .HasMaxLength(255);

            entity.Property(e => e.Message)
                .IsRequired()
                .HasMaxLength(2000);

            entity.Property(e => e.Type)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("Info");

            entity.Property(e => e.IsRead)
                .IsRequired()
                .HasDefaultValue(false);

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(e => e.ActionUrl)
                .HasMaxLength(500);

            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private static void ConfigureIndexes(ModelBuilder modelBuilder)
    {
        // Índices para User
        modelBuilder.Entity<User>()
            .HasIndex(e => e.Email)
            .IsUnique()
            .HasDatabaseName("IX_Users_Email");

        modelBuilder.Entity<User>()
            .HasIndex(e => new { e.IsActive, e.Role })
            .HasDatabaseName("IX_Users_IsActive_Role");

        // Índices para DataRecord
        modelBuilder.Entity<DataRecord>()
            .HasIndex(e => new { e.UserId, e.Category, e.Status })
            .HasDatabaseName("IX_DataRecords_UserId_Category_Status");

        modelBuilder.Entity<DataRecord>()
            .HasIndex(e => e.RecordDate)
            .HasDatabaseName("IX_DataRecords_RecordDate");

        // Índices para AuditLog
        modelBuilder.Entity<AuditLog>()
            .HasIndex(e => new { e.EntityType, e.EntityId, e.Timestamp })
            .HasDatabaseName("IX_AuditLogs_EntityType_EntityId_Timestamp");

        // Índices para ProcessingJob
        modelBuilder.Entity<ProcessingJob>()
            .HasIndex(e => new { e.Status, e.CreatedAt })
            .HasDatabaseName("IX_ProcessingJobs_Status_CreatedAt");

        // Índices para Notification
        modelBuilder.Entity<Notification>()
            .HasIndex(e => new { e.UserId, e.IsRead, e.CreatedAt })
            .HasDatabaseName("IX_Notifications_UserId_IsRead_CreatedAt");
    }

    private static void SeedData(ModelBuilder modelBuilder)
    {
        // Usuario administrador por defecto
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = 1,
                FirstName = "Administrador",
                LastName = "Sistema",
                Email = "admin@igga.com",
                Role = "Administrator",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            }
        );
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Auditoría automática
        var auditEntries = OnBeforeSaveChanges();
        var result = await base.SaveChangesAsync(cancellationToken);
        await OnAfterSaveChanges(auditEntries);
        return result;
    }

    private List<AuditEntry> OnBeforeSaveChanges()
    {
        ChangeTracker.DetectChanges();
        var auditEntries = new List<AuditEntry>();

        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.Entity is AuditLog || entry.State == EntityState.Detached || 
                entry.State == EntityState.Unchanged)
                continue;

            var auditEntry = new AuditEntry(entry)
            {
                TableName = entry.Entity.GetType().Name,
                Action = entry.State.ToString(),
                Timestamp = DateTime.UtcNow
            };

            auditEntries.Add(auditEntry);
            
            foreach (var property in entry.Properties)
            {
                if (property.IsTemporary)
                {
                    auditEntry.TemporaryProperties.Add(property);
                    continue;
                }

                string propertyName = property.Metadata.Name;
                if (property.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[propertyName] = property.CurrentValue;
                    continue;
                }

                switch (entry.State)
                {
                    case EntityState.Added:
                        auditEntry.NewValues[propertyName] = property.CurrentValue;
                        break;
                    case EntityState.Deleted:
                        auditEntry.OldValues[propertyName] = property.OriginalValue;
                        break;
                    case EntityState.Modified:
                        if (property.IsModified)
                        {
                            auditEntry.OldValues[propertyName] = property.OriginalValue;
                            auditEntry.NewValues[propertyName] = property.CurrentValue;
                        }
                        break;
                }
            }
        }

        foreach (var auditEntry in auditEntries.Where(e => !e.HasTemporaryProperties))
        {
            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        return auditEntries.Where(e => e.HasTemporaryProperties).ToList();
    }

    private Task OnAfterSaveChanges(List<AuditEntry> auditEntries)
    {
        if (auditEntries == null || auditEntries.Count == 0)
            return Task.CompletedTask;

        foreach (var auditEntry in auditEntries)
        {
            foreach (var prop in auditEntry.TemporaryProperties)
            {
                if (prop.Metadata.IsPrimaryKey())
                {
                    auditEntry.KeyValues[prop.Metadata.Name] = prop.CurrentValue;
                }
                else
                {
                    auditEntry.NewValues[prop.Metadata.Name] = prop.CurrentValue;
                }
            }

            AuditLogs.Add(auditEntry.ToAuditLog());
        }

        return SaveChangesAsync();
    }
}

public class AuditEntry
{
    public AuditEntry(EntityEntry entry)
    {
        Entry = entry;
    }

    public EntityEntry Entry { get; }
    public string UserId { get; set; } = "System";
    public string TableName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object?> KeyValues { get; } = new();
    public Dictionary<string, object?> OldValues { get; } = new();
    public Dictionary<string, object?> NewValues { get; } = new();
    public List<PropertyEntry> TemporaryProperties { get; } = new();

    public bool HasTemporaryProperties => TemporaryProperties.Any();

    public AuditLog ToAuditLog()
    {
        var audit = new AuditLog
        {
            Action = Action,
            EntityType = TableName,
            EntityId = KeyValues.FirstOrDefault().Value?.ToString() ?? "0",
            Timestamp = Timestamp,
            UserId = int.TryParse(UserId, out var uid) ? uid : 1,
            OldValues = OldValues.Count == 0 ? null : JsonSerializer.Serialize(OldValues),
            NewValues = NewValues.Count == 0 ? null : JsonSerializer.Serialize(NewValues)
        };

        return audit;
    }
}
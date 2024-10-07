using System;

namespace Cultiv.Hangfire;

public class HangfireSettings
{
    public Server Server { get; set; }
    public StorageOptions StorageOptions { get; set; } = new StorageOptions();
}

public class Server
{
    public bool? Disabled { get; set; }
}

public class StorageOptions
{
    public bool PrepareSchemaIfNecessary { get; set; } = true;
    public bool EnableHeavyMigrations { get; set; } = true;
    public TimeSpan CommandBatchMaxTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan SlidingInvisibilityTimeout { get; set; } = TimeSpan.FromMinutes(5);
    public TimeSpan QueuePollInterval { get; set; } = TimeSpan.Zero;
    public bool UseRecommendedIsolationLevel { get; set; } = true;
    public bool DisableGlobalLocks { get; set; } = true;
}
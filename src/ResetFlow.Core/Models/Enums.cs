namespace ResetFlow.Core.Models;

public enum AutomationLevel
{
    Automatic,
    Assisted,
    ManualChecklist,
    Excluded
}

public enum RollbackCapability
{
    None,
    Partial,
    Full
}

public enum FeatureStatus
{
    Pending,
    Success,
    Failed,
    Skipped,
    ManualRequired,
    RolledBack
}

public enum DeviceType
{
    Unknown,
    Desktop,
    Laptop
}

public enum FeatureStep
{
    Precheck,
    Backup,
    Apply,
    Verify,
    Rollback
}

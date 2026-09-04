namespace MiniBank.Domain.AuditAggregate;

public enum AuditAction
{
    Create = 0,
    Update = 1,
    Delete = 2,
    Verify = 3,
    Block = 4,
    Approve = 5,
    Reject = 6,
    Login = 7,
    Logout = 8
}

namespace MiniBank.Domain.AccountAggregate;

public enum AccountStatus
{
    Active = 0,
    Frozen = 1,
    Closed = 2,
    PendingApproval = 3
}

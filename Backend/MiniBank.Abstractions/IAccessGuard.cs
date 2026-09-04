namespace MiniBank.Abstractions;

/// <summary>
/// Generalized ownership guard for Documents, KYC, Risk, etc.
/// Admins bypass all ownership checks.
/// </summary>
public interface IAccessGuard
{
    /// <summary>Throws ForbiddenException if the document is not owned by the current user (admins bypass).</summary>
    Task EnsureDocumentOwnershipAsync(Guid documentId, CancellationToken cancellationToken = default);

    /// <summary>Throws ForbiddenException if the KYC verification is not owned by the current user (admins bypass).</summary>
    Task EnsureKycOwnershipAsync(Guid kycId, CancellationToken cancellationToken = default);

    /// <summary>Throws ForbiddenException if the risk record does not belong to the current user (admins bypass).</summary>
    Task EnsureRiskOwnershipAsync(Guid customerId, CancellationToken cancellationToken = default);

    /// <summary>Throws ForbiddenException if the customer record does not belong to the current user (admins bypass).</summary>
    Task EnsureCustomerOwnershipAsync(Guid customerId, CancellationToken cancellationToken = default);
}

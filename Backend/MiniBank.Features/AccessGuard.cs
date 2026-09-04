using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks.Exceptions;

namespace MiniBank.Features;

/// <summary>
/// Ownership guard using Dapper queries. Admins bypass all checks.
/// </summary>
internal sealed class AccessGuard(ISqlConnectionFactory connectionFactory, ICurrentUserContext currentUser) : IAccessGuard
{
    public async Task EnsureDocumentOwnershipAsync(Guid documentId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
            return;

        using var connection = connectionFactory.CreateOpenConnection();

        var ownerId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "SELECT customer_id FROM documents WHERE document_id = @Id",
                new { Id = documentId },
                cancellationToken: cancellationToken));

        if (ownerId is null)
            throw new NotFoundException("document", documentId);

        if (ownerId.Value != currentUser.UserId)
            throw new ForbiddenException("document", "Document does not belong to the current user.");
    }

    public async Task EnsureKycOwnershipAsync(Guid kycId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
            return;

        using var connection = connectionFactory.CreateOpenConnection();

        var ownerId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "SELECT customer_id FROM kyc_verifications WHERE kyc_id = @Id",
                new { Id = kycId },
                cancellationToken: cancellationToken));

        if (ownerId is null)
            throw new NotFoundException("kyc", kycId);

        if (ownerId.Value != currentUser.UserId)
            throw new ForbiddenException("kyc", "KYC verification does not belong to the current user.");
    }

    public async Task EnsureRiskOwnershipAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
            return;

        using var connection = connectionFactory.CreateOpenConnection();

        var ownerId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "SELECT customer_id FROM customer_risks WHERE customer_id = @Id",
                new { Id = customerId },
                cancellationToken: cancellationToken));

        if (ownerId is null)
            throw new NotFoundException("risk", customerId);

        if (ownerId.Value != currentUser.UserId)
            throw new ForbiddenException("risk", "Risk record does not belong to the current user.");
    }

    public async Task EnsureCustomerOwnershipAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        if (currentUser.IsAdmin)
            return;

        using var connection = connectionFactory.CreateOpenConnection();

        var ownerId = await connection.QuerySingleOrDefaultAsync<Guid?>(
            new CommandDefinition(
                "SELECT customer_id FROM customers WHERE customer_id = @Id",
                new { Id = customerId },
                cancellationToken: cancellationToken));

        if (ownerId is null)
            throw new NotFoundException("customer", customerId);

        if (ownerId.Value != currentUser.UserId)
            throw new ForbiddenException("customer", "Customer does not belong to the current user.");
    }
}

using Dapper;
using MiniBank.Abstractions;
using MiniBank.Domain.AccountAggregate.ValueObjects;
using MiniBank.Domain.BeneficiaryAggregate;
using MiniBank.Domain.BeneficiaryAggregate.ValueObjects;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate.ValueObjects;
using MiniBank.Features.Beneficiaries.ListBeneficiaries;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Beneficiaries.AddBeneficiary;

internal sealed class AddBeneficiaryHandler(
    IBeneficiaryRepository beneficiaries,
    ISqlConnectionFactory connectionFactory,
    ICurrentUserContext currentUser,
    IUnitOfWork unitOfWork) : ICommandHandler<AddBeneficiaryCommand, BeneficiaryDto>
{
    private const string HolderSql = """
        SELECT c.full_name AS FullName
        FROM   accounts a
        JOIN   customers c ON c.customer_id = a.customer_id
        WHERE  UPPER(a.account_number) = UPPER(@AccountNumber)
        """;

    public async Task<BeneficiaryDto> HandleAsync(AddBeneficiaryCommand command, CancellationToken cancellationToken = default)
    {
        var normalized = AccountNumber.Normalize(command.AccountNumber);
        var number = new AccountNumber(normalized); // throws 400 on bad format (validator covers API path)
        var owner = new CustomerId(currentUser.UserId);

        if (await beneficiaries.GetByOwnerAndNumberAsync(owner.Value, normalized, cancellationToken) is not null)
            throw new DomainConflictException(nameof(command.AccountNumber), "This account is already in your saved beneficiaries.");

        using var connection = connectionFactory.CreateOpenConnection();
        var holder = await connection.QuerySingleOrDefaultAsync<string>(
            new CommandDefinition(HolderSql, new { AccountNumber = normalized }, cancellationToken: cancellationToken));

        if (holder is null)
            throw new NotFoundException("account", command.AccountNumber);

        var displayName = string.IsNullOrWhiteSpace(command.Nickname) ? holder : command.Nickname!.Trim();
        var beneficiary = Beneficiary.Create(owner, number, displayName);

        await beneficiaries.AddAsync(beneficiary, cancellationToken);
        try
        {
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch (UniqueConstraintViolationException)
        {
            throw new DomainConflictException(nameof(command.AccountNumber), "This account is already in your saved beneficiaries.");
        }

        return new BeneficiaryDto(beneficiary.Id.Value, beneficiary.AccountNumber.Value, beneficiary.HolderName, beneficiary.CreatedAt);
    }
}

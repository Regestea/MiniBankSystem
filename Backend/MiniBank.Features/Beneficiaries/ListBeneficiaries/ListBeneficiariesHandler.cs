using MiniBank.Abstractions;
using MiniBank.Domain.BeneficiaryAggregate;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Beneficiaries.ListBeneficiaries;

internal sealed class ListBeneficiariesHandler(
    IBeneficiaryRepository beneficiaries,
    ICurrentUserContext currentUser) : IQueryHandler<ListBeneficiariesQuery, IReadOnlyList<BeneficiaryDto>>
{
    public async Task<IReadOnlyList<BeneficiaryDto>> HandleAsync(ListBeneficiariesQuery query, CancellationToken cancellationToken = default)
    {
        var list = await beneficiaries.ListByOwnerAsync(currentUser.UserId, cancellationToken);
        return list.Select(x => new BeneficiaryDto(x.Id.Value, x.AccountNumber.Value, x.HolderName, x.CreatedAt)).ToList();
    }
}

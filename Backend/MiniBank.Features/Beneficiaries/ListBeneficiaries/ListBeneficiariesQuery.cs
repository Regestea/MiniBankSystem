using FluentValidation;
using MiniBank.Features.Messaging;

namespace MiniBank.Features.Beneficiaries.ListBeneficiaries;

public sealed record ListBeneficiariesQuery : IQuery<IReadOnlyList<BeneficiaryDto>>;

public sealed record BeneficiaryDto(
    Guid BeneficiaryId,
    string AccountNumber,
    string HolderName,
    DateTimeOffset CreatedAt);

public sealed class ListBeneficiariesValidator : AbstractValidator<ListBeneficiariesQuery>
{
    public ListBeneficiariesValidator() { }
}

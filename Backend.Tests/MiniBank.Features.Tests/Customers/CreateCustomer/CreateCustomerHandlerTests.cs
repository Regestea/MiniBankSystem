using FluentAssertions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Customers.CreateCustomer;
using NSubstitute;

namespace MiniBank.Features.Tests.Customers.CreateCustomer;

public sealed class CreateCustomerHandlerTests
{
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private CreateCustomerHandler CreateHandler() => new(_customers, _uow);

    [Fact]
    public async Task HandleAsync_ValidCommand_CreatesCustomer_And_Persists()
    {
        var handler = CreateHandler();
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);

        var cmd = new CreateCustomerCommand("Amir Hossein", "amir@test.com", "09123456789");

        var response = await handler.HandleAsync(cmd);

        response.CustomerId.Should().NotBe(Guid.Empty);
        response.Status.Should().Be("Pending");
        response.FullName.Should().Be("Amir Hossein");
        await _customers.Received(1).AddAsync(Arg.Is<Customer>(c => c.Email == "amir@test.com"), Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflict_And_DoesNotPersist()
    {
        var handler = CreateHandler();
        _customers.EmailExistsAsync("dup@test.com", Arg.Any<CancellationToken>()).Returns(true);

        var cmd = new CreateCustomerCommand("John Doe", "dup@test.com", "09123456789");

        var act = async () => await handler.HandleAsync(cmd);

        await act.Should().ThrowAsync<DomainConflictException>()
            .Where(ex => ex.Details.ToString()!.Contains("already registered"));
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
        await _uow.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("", "john@x.com", "09123456789")]
    [InlineData("John", "invalid-email", "09123456789")]
    [InlineData("John", "john@x.com", "123")]
    [InlineData("John", "john@x.com", "abcdefghij")]
    public void Validator_Should_Reject_Invalid_Input(string fullName, string email, string phone)
    {
        var validator = new CreateCustomerValidator();
        var result = validator.Validate(new CreateCustomerCommand(fullName, email, phone));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validator_Should_Accept_Valid_Command()
    {
        var validator = new CreateCustomerValidator();
        var result = validator.Validate(new CreateCustomerCommand("Valid Name", "valid@test.com", "09123456789"));
        result.IsValid.Should().BeTrue();
    }
}

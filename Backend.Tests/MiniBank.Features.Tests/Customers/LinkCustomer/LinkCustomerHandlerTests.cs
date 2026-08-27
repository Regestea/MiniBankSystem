using FluentAssertions;
using MiniBank.Abstractions;
using MiniBank.Domain.BuildingBlocks;
using MiniBank.Domain.BuildingBlocks.Exceptions;
using MiniBank.Domain.CustomerAggregate;
using MiniBank.Features.Customers.LinkCustomer;
using NSubstitute;

namespace MiniBank.Features.Tests.Customers.LinkCustomer;

public sealed class LinkCustomerHandlerTests
{
    private readonly IAppUserDirectory _users = Substitute.For<IAppUserDirectory>();
    private readonly ICustomerRepository _customers = Substitute.For<ICustomerRepository>();
    private readonly IUnitOfWork _uow = Substitute.For<IUnitOfWork>();

    private LinkCustomerHandler CreateHandler() => new(_users, _customers, _uow);

    [Fact]
    public async Task HandleAsync_Valid_CreatesCustomer_AttachesAndEnsuresRole()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserSnapshot(userId, "user@test.com", null));
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.TryAttachCustomerAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(true);

        var handler = CreateHandler();
        var response = await handler.HandleAsync(new LinkCustomerCommand(userId, "John Doe", "09123456789"));

        response.Email.Should().Be("user@test.com");
        await _customers.Received(1).AddAsync(Arg.Is<Customer>(c => c.FullName == "John Doe"), Arg.Any<CancellationToken>());
        await _users.Received(1).EnsureUserRoleAsync(userId, Arg.Any<CancellationToken>());
        await _uow.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_UserNotFound_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns((UserSnapshot?)null);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new LinkCustomerCommand(userId, "John", "09123456789"));

        await act.Should().ThrowAsync<NotFoundException>()
            .Where(ex => ex.Field == "user");
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_AlreadyLinked_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserSnapshot(userId, "user@test.com", Guid.NewGuid()));
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new LinkCustomerCommand(userId, "John", "09123456789"));

        await act.Should().ThrowAsync<DomainConflictException>()
            .Where(ex => ex.Details.ToString()!.Contains("already linked"));
    }

    [Fact]
    public async Task HandleAsync_NoEmail_ThrowsValidation()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserSnapshot(userId, "   ", null));
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new LinkCustomerCommand(userId, "John", "09123456789"));

        await act.Should().ThrowAsync<DomainValidationException>();
    }

    [Fact]
    public async Task HandleAsync_DuplicateEmail_ThrowsConflict()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserSnapshot(userId, "dup@test.com", null));
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(true);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new LinkCustomerCommand(userId, "John", "09123456789"));

        await act.Should().ThrowAsync<DomainConflictException>();
        await _customers.DidNotReceive().AddAsync(Arg.Any<Customer>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task HandleAsync_TryAttachFails_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(new UserSnapshot(userId, "user@test.com", null));
        _customers.EmailExistsAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(false);
        _users.TryAttachCustomerAsync(userId, Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(false);
        var handler = CreateHandler();

        var act = async () => await handler.HandleAsync(new LinkCustomerCommand(userId, "John", "09123456789"));

        await act.Should().ThrowAsync<NotFoundException>();
    }
}

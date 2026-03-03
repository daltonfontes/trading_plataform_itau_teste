using Application.Interfaces;
using Application.Services;
using Domain.Entities;
using Domain.Enums;
using Domain.Events.Base;
using Domain.Interfaces;
using Moq;
using Xunit;

namespace UnitTests.Services;

public class CustomerServiceTests
{
    private readonly Mock<ICustomerRepository> _customerRepo = new();
    private readonly Mock<ICustomerCustodyRepository> _custodyRepo = new();
    private readonly Mock<IAssetPriceRepository> _priceRepo = new();
    private readonly Mock<IEventStore> _eventStore = new();
    private readonly CustomerService _sut;

    public CustomerServiceTests()
    {
        _sut = new CustomerService(
            _customerRepo.Object,
            _custodyRepo.Object,
            _priceRepo.Object,
            _eventStore.Object);
    }

    // --- Adesão ---

    [Fact]
    public async Task EnrollAsync_ValidData_CreatesCustomer()
    {
        _customerRepo.Setup(r => r.GetByCpfAsync("12345678901")).ReturnsAsync((Customer?)null);
        _customerRepo.Setup(r => r.AddAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

        var result = await _sut.EnrollAsync("João Silva", "12345678901", "joao@email.com", 3000m);

        Assert.Equal("João Silva", result.Nome);
        Assert.True(result.Ativo);
        Assert.StartsWith("FLH-", result.NumeroConta);
        _customerRepo.Verify(r => r.AddAsync(It.IsAny<Customer>()), Times.Once);
    }

    [Fact]
    public async Task EnrollAsync_DuplicateCpf_ThrowsException()
    {
        _customerRepo.Setup(r => r.GetByCpfAsync("12345678901"))
            .ReturnsAsync(new Customer { CPF = "12345678901" });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EnrollAsync("Outro", "12345678901", "x@x.com", 500m));

        Assert.Contains("CLIENTE_CPF_DUPLICADO", ex.Message);
    }

    [Fact]
    public async Task EnrollAsync_MonthlyValueBelowMinimum_ThrowsException()
    {
        _customerRepo.Setup(r => r.GetByCpfAsync(It.IsAny<string>())).ReturnsAsync((Customer?)null);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _sut.EnrollAsync("João", "99988877766", "x@x.com", 50m));

        Assert.Contains("VALOR_MENSAL_INVALIDO", ex.Message);
    }

    // --- Saída ---

    [Fact]
    public async Task ExitAsync_ActiveCustomer_DeactivatesCorrectly()
    {
        var id = Guid.NewGuid();
        var customer = new Customer { Id = id, Name = "João", Status = CustomerStatus.Active };
        _customerRepo.Setup(r => r.GetByIdAsync(id)).ReturnsAsync(customer);
        _customerRepo.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

        var result = await _sut.ExitAsync(id);

        Assert.False(result.Ativo);
        _customerRepo.Verify(r => r.UpdateAsync(It.Is<Customer>(c => c.Status == CustomerStatus.Inactive)), Times.Once);
    }

    [Fact]
    public async Task ExitAsync_CustomerNotFound_ThrowsKeyNotFoundException()
    {
        _customerRepo.Setup(r => r.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((Customer?)null);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => _sut.ExitAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ExitAsync_AlreadyInactive_ThrowsInvalidOperationException()
    {
        var id = Guid.NewGuid();
        _customerRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Customer { Id = id, Status = CustomerStatus.Inactive });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.ExitAsync(id));
        Assert.Contains("CLIENTE_JA_INATIVO", ex.Message);
    }

    // --- Alterar valor mensal ---

    [Fact]
    public async Task UpdateMonthlyAsync_ValidValue_UpdatesCorrectly()
    {
        var id = Guid.NewGuid();
        _customerRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Customer { Id = id, MonthlyContribution = 3000m });
        _customerRepo.Setup(r => r.UpdateAsync(It.IsAny<Customer>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateMonthlyAsync(id, 6000m);

        Assert.Equal(3000m, result.ValorMensalAnterior);
        Assert.Equal(6000m, result.ValorMensalNovo);
    }

    [Fact]
    public async Task UpdateMonthlyAsync_BelowMinimum_ThrowsException()
    {
        var id = Guid.NewGuid();
        _customerRepo.Setup(r => r.GetByIdAsync(id))
            .ReturnsAsync(new Customer { Id = id, MonthlyContribution = 3000m });

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateMonthlyAsync(id, 50m));
    }
}

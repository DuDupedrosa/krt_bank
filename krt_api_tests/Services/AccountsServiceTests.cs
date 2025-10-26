using AutoMapper;
using FluentAssertions;
using krt_api.Application.Services;
using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Entities;
using krt_api.Core.Accounts.Interfaces;
using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Interfaces;
using krt_api.Core.Utils;
using krt_api.Core.Utils.Enums;
using Moq;
using RabbitMQ.Client;
using System.Net;

namespace krt_api_tests.Services
{
    public class AccountsServiceTests
    {
        private readonly Mock<IAccountsRepository> _accountsRepositoryMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<IAccountCacheService> _accountCacheServiceMock;
        private readonly Mock<IAccountProducer> _accountProducerMock;
        private readonly AccountsService _accountsService;
        private const string validateDataError = "Validaton data error";
        private const string alreadyRegisteredCpfError = "Another user is already registered with this CPF";
        private const string cpf = "36070315502";
        private const string johnDoe = "John Doe";
        private const string invalidCpfFormat = "11111111111";
        private const string accountNotFound = "Account not found";
        private const string accountCreatedEvent = "account_created";
        private const string accountUpdatedEvent = "account_updated";
        private const string accountDeletedEvent = "account_deleted";
        private const string accountExchangeName = "accounts_exchange";

        public AccountsServiceTests()
        {
            _accountsRepositoryMock = new Mock<IAccountsRepository>();
            _mapperMock = new Mock<IMapper>();
            _accountCacheServiceMock = new Mock<IAccountCacheService>();
            _accountProducerMock = new Mock<IAccountProducer>();

            _accountsService = new AccountsService(
                _accountsRepositoryMock.Object,
                _mapperMock.Object,
                _accountCacheServiceMock.Object,
                _accountProducerMock.Object
            );
        }

        [Fact]
        public async Task CreateAsync_InvalidDto_ReturnsBadRequest()
        {
            CreateAccountDto dto = new CreateAccountDto { CPF = invalidCpfFormat };
            ResponseModel result = await _accountsService.CreateAsync(dto);
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Be(validateDataError);
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task CreateAsync_CpfAlreadyRegister_ReturnsBadRequest()
        {
            Accounts existingAccount = GetCreatedAccount();
            CreateAccountDto dto = new() { CPF = existingAccount.CPF, Name = existingAccount.Name };

            _accountsRepositoryMock
                .Setup(x => x.GetByCPFAsync(dto.CPF))
                .ReturnsAsync(existingAccount);

            ResponseModel result = await _accountsService.CreateAsync(dto);

            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Be(alreadyRegisteredCpfError);

            _accountsRepositoryMock.Verify(x => x.GetByCPFAsync(dto.CPF), Times.Once);
        }

        [Fact]
        public async Task CreateAsync_ReturnsCreatedStatus()
        {
            Accounts newAccount = GetCreatedAccount();
            CreateAccountDto dto = new() { CPF = newAccount.CPF, Name = newAccount.Name };

            _accountsRepositoryMock
                .Setup(x => x.GetByCPFAsync(dto.CPF))
                .ReturnsAsync((Accounts?)null);
            _mapperMock
                .Setup(x => x.Map<Accounts>(dto))
                .Returns(newAccount);
            _accountsRepositoryMock
                .Setup(x => x.AddAsync(newAccount))
                .ReturnsAsync(newAccount);
            _accountCacheServiceMock
                .Setup(x => x.SaveAccountAsync(newAccount, It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);
            _accountProducerMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            ResponseModel result = await _accountsService.CreateAsync(dto);
            result.StatusCode.Should().Be(HttpStatusCode.Created);
            result.Content.Should().Be(newAccount);

            _accountsRepositoryMock .Verify(x => x.GetByCPFAsync(dto.CPF), Times.Once);
            _accountsRepositoryMock.Verify(x => x.AddAsync(newAccount), Times.Once);
            _mapperMock.Verify(x => x.Map<Accounts>(dto), Times.Once);
            _accountCacheServiceMock.Verify(x => x.SaveAccountAsync(newAccount, It.IsAny<TimeSpan?>()), Times.Once);
            _accountProducerMock.Verify(x => x.PublishAsync(
                accountCreatedEvent,
                accountExchangeName,
                ExchangeType.Direct,
                It.Is<Accounts>(a => a.CPF == newAccount.CPF)), Times.Once);
        }

        [Fact]
        public async Task UpdateAsync_InvalidDto_ReturnsBadRequest()
        {
            UpdateAccountDto dto = new() { CPF = invalidCpfFormat };
            ResponseModel result = await _accountsService.UpdateAsync(dto);
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Be(validateDataError);
            result.Errors.Should().NotBeEmpty();
        }

        [Fact]
        public async Task UpdateAsync_AccountNotExist_ReturnsNotFound()
        {
            Guid id = Guid.NewGuid();
            UpdateAccountDto dto = new() { CPF = cpf, Id = id, Name = johnDoe };

            _accountCacheServiceMock
               .Setup(x => x.GetAccountAsync(id))
               .ReturnsAsync((Accounts?)null);
            _accountsRepositoryMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((Accounts?)null);
         
            ResponseModel result = await _accountsService.UpdateAsync(dto);
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Message.Should().Be(accountNotFound);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }

        [Fact] 
        public async Task UpdateAsync_AccountIsInactive_ReturnsBadRequest()
        {
            Accounts account = GetCreatedAccount(false);
            UpdateAccountDto dto = new() { CPF = account.CPF, Id = account.Id, Name = account.Name };

            _accountCacheServiceMock
               .Setup(x => x.GetAccountAsync(dto.Id))
               .ReturnsAsync(account);

            ResponseModel result = await _accountsService.UpdateAsync(dto);
            result.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            result.Message.Should().Be("Only active accounts can be updated");

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(dto.Id), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_UserChangeCpf_AlreadyRegister_ReturnsBadRequest()
        {
            Accounts anotherUserAccount = GetCreatedAccount();
            Accounts userUpdateAccount = GetCreatedAccount();
            userUpdateAccount.CPF = "57561807708";
            UpdateAccountDto dto = new() { CPF = anotherUserAccount.CPF, Id = userUpdateAccount.Id, Name = userUpdateAccount.Name };

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(dto.Id))
                .ReturnsAsync(userUpdateAccount);
            _accountsRepositoryMock
                .Setup(x => x.AnotherUserRegisterWithSameCPF(dto.Id, dto.CPF))
                .ReturnsAsync(true);

            ResponseModel response = await _accountsService.UpdateAsync(dto);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Message.Should().Be(alreadyRegisteredCpfError);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(dto.Id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.AnotherUserRegisterWithSameCPF(dto.Id, dto.CPF), Times.Once);
        }
        [Fact]
        public async Task UpdateAsync_AccountUpdated_ReturnsSuccess()
        {
            Accounts createdAccount = GetCreatedAccount();
            UpdateAccountDto dto = new() { CPF = createdAccount.CPF, Id = createdAccount.Id, Name = "Jane Doe" };
            createdAccount.Name = dto.Name;
            createdAccount.UpdatedAt = DateTime.UtcNow;

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(dto.Id))
                .ReturnsAsync(createdAccount);
            _accountsRepositoryMock
                .Setup(x => x.UpdateAsync(createdAccount))
                .ReturnsAsync(createdAccount);
            _accountCacheServiceMock
                .Setup(x => x.SaveAccountAsync(createdAccount, It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);
            _accountProducerMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            ResponseModel response = await _accountsService.UpdateAsync(dto);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Should().Be(createdAccount);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(dto.Id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.UpdateAsync(createdAccount), Times.Once);
            _accountCacheServiceMock.Verify(x => x.SaveAccountAsync(createdAccount, It.IsAny<TimeSpan?>()), Times.Once);
            _accountProducerMock.Verify(x => x.PublishAsync(
                accountUpdatedEvent,
                accountExchangeName,
                ExchangeType.Direct,
                It.Is<Accounts>(a => a.CPF == createdAccount.CPF)), Times.Once);
        }
        [Fact] 
        public async Task GetAsync_AccountNotExist_ReturnsNotFound()
        {
            Guid id = Guid.NewGuid();

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(id))
                .ReturnsAsync((Accounts?)null);
            _accountsRepositoryMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((Accounts?)null);

            ResponseModel result = await _accountsService.GetAsync(id);
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Message.Should().Be(accountNotFound);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }

        [Fact]
        public async Task GetAsync_AccountExistInCache_ReturnsAccount()
        {
            Accounts account = GetCreatedAccount();

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(account.Id))
                .ReturnsAsync(account);

            ResponseModel result = await _accountsService.GetAsync(account.Id);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Should().Be(account);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(account.Id), Times.Once);
        }

        [Fact]
        public async Task GetAsync_NotExistInCache_ExistInDb_ReturnsSuccess()
        {
            Accounts account = GetCreatedAccount();

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(account.Id))
                .ReturnsAsync((Accounts?)null);
            _accountsRepositoryMock
                .Setup(x => x.GetByIdAsync(account.Id))
                .ReturnsAsync(account);
            _accountCacheServiceMock
                .Setup(x => x.SaveAccountAsync(account, It.IsAny<TimeSpan?>()))
                .Returns(Task.CompletedTask);

            ResponseModel result = await _accountsService.GetAsync(account.Id);
            result.StatusCode.Should().Be(HttpStatusCode.OK);
            result.Content.Should().Be(account);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(account.Id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.GetByIdAsync(account.Id), Times.Once);
            _accountCacheServiceMock.Verify(x => x.SaveAccountAsync(account, It.IsAny<TimeSpan?>()), Times.Once);
        }
        [Fact]
        public async Task DeleteAsync_AccountNotExist_ReturnsNotFound()
        {
            Guid id = Guid.NewGuid();

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(id))
                .ReturnsAsync((Accounts?)null);
            _accountsRepositoryMock
                .Setup(x => x.GetByIdAsync(id))
                .ReturnsAsync((Accounts?)null);

            ResponseModel result = await _accountsService.DeleteAsync(id);
            result.StatusCode.Should().Be(HttpStatusCode.NotFound);
            result.Message.Should().Be(accountNotFound);

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.GetByIdAsync(id), Times.Once);
        }
        [Fact]
        public async Task DeleteAsync_AccountIsInactive_ReturnsBadRequest()
        {
            Accounts account = GetCreatedAccount(false);

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(account.Id))
                .ReturnsAsync(account);

            ResponseModel response = await _accountsService.DeleteAsync(account.Id);
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Message.Should().Be("Only active accounts can be deleted");

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(account.Id), Times.Once);
        }
        [Fact]
        public async Task DeleteAsync_AccountDeleted_ReturnsSuccess()
        {

            Accounts account = GetCreatedAccount();
            Accounts accountAfterDelete = new()
            {
                Id = account.Id,
                Name = account.Name,
                CPF = account.CPF,
                Status = AccountStatus.INACTIVE,
                CreatedAt = account.CreatedAt,
                UpdatedAt = DateTime.UtcNow
            };

            _accountCacheServiceMock
                .Setup(x => x.GetAccountAsync(account.Id))
                .ReturnsAsync(account);
            _accountsRepositoryMock
                .Setup(x => x.UpdateAsync(accountAfterDelete))
                .ReturnsAsync(accountAfterDelete);
            _accountCacheServiceMock
                .Setup(x => x.RemoveAccountAsync(account.Id))
                .Returns(Task.CompletedTask);
            _accountProducerMock
                .Setup(x => x.PublishAsync(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<object>()))
                .Returns(Task.CompletedTask);

            ResponseModel response = await _accountsService.DeleteAsync(account.Id);
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Message.Should().Be("Account deleted successfully");

            _accountCacheServiceMock.Verify(x => x.GetAccountAsync(account.Id), Times.Once);
            _accountsRepositoryMock.Verify(x => x.UpdateAsync(
                It.Is<Accounts>(a =>
                    a.Id == account.Id &&
                    a.CPF == account.CPF &&
                    a.Name == account.Name &&
                    a.Status == AccountStatus.INACTIVE
                )), Times.Once);
            _accountCacheServiceMock.Verify(x => x.RemoveAccountAsync(account.Id), Times.Once);
            _accountProducerMock.Verify(x => x.PublishAsync(
                accountDeletedEvent,
                accountExchangeName,
                ExchangeType.Direct,
                It.Is<Accounts>(a => a.CPF == account.CPF)), Times.Once);
        }
        [Fact]
        public async Task GetAllAsync_NoAccountsRegister_ReturnsSuccess()
        {
            List<Accounts> accounts = new List<Accounts>();
            ListAllAccountsResponseDto listAllAccountsResponseDto = new()
            {
                Accounts = accounts,
                Paginate = new PaginateModel
                {
                    Page = 1,
                    PageCount = 0,
                    PageSize = 10,
                    TotalCount = 0
                }
            };

            _accountsRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<AccountStatus?>(), It.IsAny<OrderBy>(), It.IsAny<int>()))
                .ReturnsAsync(listAllAccountsResponseDto);

            ResponseModel response = await _accountsService.GetAllAsync(string.Empty, null, OrderBy.Descending, 1);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Should().BeEquivalentTo(listAllAccountsResponseDto);

            _accountsRepositoryMock.Verify(x => x.GetAllAsync(
                It.IsAny<string>(), 
                It.IsAny<AccountStatus?>(), 
                It.IsAny<OrderBy>(), 
                It.IsAny<int>()), 
                Times.Once
                );
        }
        [Fact]
        public async Task GetAllAsync_AccountsRegister_ReturnsSuccess()
        {
            List<Accounts> accounts = new List<Accounts>
            {
                GetCreatedAccount(),
                GetCreatedAccount(),
                GetCreatedAccount()
            };

            ListAllAccountsResponseDto listAllAccountsResponseDto = new()
            {
                Accounts = accounts,
                Paginate = new PaginateModel
                {
                    Page = 1,
                    PageCount = 1,
                    PageSize = 10,
                    TotalCount = 3
                }
            };

            _accountsRepositoryMock
                .Setup(x => x.GetAllAsync(It.IsAny<string>(), It.IsAny<AccountStatus?>(), It.IsAny<OrderBy>(), It.IsAny<int>()))
                .ReturnsAsync(listAllAccountsResponseDto);

            ResponseModel response = await _accountsService.GetAllAsync(string.Empty, null, OrderBy.Descending, 1);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Should().BeEquivalentTo(listAllAccountsResponseDto);

            _accountsRepositoryMock.Verify(x => x.GetAllAsync(
                It.IsAny<string>(),
                It.IsAny<AccountStatus?>(),
                It.IsAny<OrderBy>(),
                It.IsAny<int>()),
                Times.Once
                );
        }
        private static Accounts GetCreatedAccount(bool isActive = true)
        {
            return new Accounts
            {
                Id = Guid.NewGuid(),
                Name = johnDoe,
                CPF = cpf,
                Status = isActive ? AccountStatus.ACTIVE : AccountStatus.INACTIVE,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
        }
    }
}

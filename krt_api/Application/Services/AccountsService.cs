using AutoMapper;
using FluentValidation.Results;
using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Entities;
using krt_api.Core.Accounts.Interfaces;
using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Accounts.Validators;
using krt_api.Core.Interfaces;
using krt_api.Core.Utils;
using krt_api.Core.Utils.Enums;
using krt_api.Infrastructure.Messaging;
using RabbitMQ.Client;
using System.Net;

namespace krt_api.Application.Services
{
    public class AccountsService : IAccountsService
    {
        private readonly IAccountsRepository _accountsRepository;
        private readonly IMapper _mapper;
        private readonly IAccountCacheService _accountCacheService;
        private readonly IAccountProducer _accountProducer;

        private const string accountNotFound = "Account not found";
        private const string validationDataError = "Validaton data error";
        private const string accountCreatedEvent = "account_created";
        private const string accountUpdatedEvent = "account_updated";
        private const string accountDeletedEvent = "account_deleted";
        private const string accountExchangeName = "accounts_exchange";
        private const string alreadyRegisteredCpfError = "Another user is already registered with this CPF";

        public AccountsService(IAccountsRepository accountsRepository,
            IMapper mapper,
            IAccountCacheService accountCacheService,
            IAccountProducer accountProducer
            )
        {
            _accountsRepository = accountsRepository;
            _mapper = mapper;
            _accountCacheService = accountCacheService;
            _accountProducer = accountProducer;
        }

        public async Task<ResponseModel> CreateAsync(CreateAccountDto dto)
        {
            try
            {
                CreateAccountDtoValidator validator = new();
                ValidationResult validationResult = await validator.ValidateAsync(dto);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                    .Select(e => new ValidationError
                    {
                        Field = e.PropertyName,
                        Error = e.ErrorMessage
                    })
                    .ToList();

                    return new ResponseModel
                    {
                        Message = validationDataError,
                        StatusCode = HttpStatusCode.BadRequest,
                        Errors = errors
                    };
                }

                Accounts createdAccount = await _accountsRepository.GetByCPFAsync(dto.CPF);

                if (createdAccount != null)
                {
                    return new ResponseModel
                    {
                        Message = alreadyRegisteredCpfError,
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }

                Accounts newCreatedAccount = await _accountsRepository.AddAsync(_mapper.Map<Accounts>(dto));
                await _accountCacheService.SaveAccountAsync(newCreatedAccount);
                await _accountProducer.PublishAsync(accountCreatedEvent, accountExchangeName, ExchangeType.Direct, newCreatedAccount);

                return new ResponseModel
                {
                    Content = newCreatedAccount,
                    StatusCode = HttpStatusCode.Created
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    Message = $"AccountsService|CreateAsync|InternalServerError|Error:{ex.Message}",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
        public async Task<ResponseModel> UpdateAsync(UpdateAccountDto dto)
        {
            try
            {
                UpdateAccountDtoValidator validator = new();
                ValidationResult validationResult = await validator.ValidateAsync(dto);

                if (!validationResult.IsValid)
                {
                    var errors = validationResult.Errors
                    .Select(e => new ValidationError
                    {
                        Field = e.PropertyName,
                        Error = e.ErrorMessage
                    })
                    .ToList();

                    return new ResponseModel
                    {
                        Message = validationDataError,
                        StatusCode = HttpStatusCode.BadRequest,
                        Errors = errors
                    };
                }

                ResponseModel accountDataResponse = await GetAccountDataAsync(dto.Id, false);

                if (accountDataResponse.StatusCode != HttpStatusCode.OK)
                    return accountDataResponse;

                Accounts account = (Accounts)accountDataResponse.Content!;

                if (account.Status != AccountStatus.ACTIVE)
                {
                    return new ResponseModel
                    {
                        Message = "Only active accounts can be updated",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }

                if (dto.CPF != account.CPF && await _accountsRepository.AnotherUserRegisterWithSameCPF(dto.Id, dto.CPF))
                {
                    return new ResponseModel
                    {
                        Message = alreadyRegisteredCpfError,
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }

                account.UpdatedAt = DateTime.UtcNow;
                _mapper.Map(dto, account);

                Accounts updatedAccount = await _accountsRepository.UpdateAsync(account);
                await _accountCacheService.SaveAccountAsync(updatedAccount);
                await _accountProducer.PublishAsync(accountUpdatedEvent, accountExchangeName, ExchangeType.Direct, updatedAccount);

                return new ResponseModel
                {
                    Content = updatedAccount,
                    StatusCode = HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    Message = $"AccountsService|UpdateAsync|InternalServerError|Error:{ex.Message}",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
        public async Task<ResponseModel> GetAsync(Guid id)
        {
            try
            {
                ResponseModel accountDataResponse = await GetAccountDataAsync(id);

                if (accountDataResponse.StatusCode != HttpStatusCode.OK)
                    return accountDataResponse;

                return new ResponseModel
                {
                    Content = (Accounts)accountDataResponse.Content!,
                    StatusCode = HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    Message = $"AccountsService|GetAsync|InternalServerError|Error:{ex.Message}",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
        public async Task<ResponseModel> GetAllAsync(string? filter = null, AccountStatus? status = null, OrderBy orderBy = OrderBy.Descending, int page = 1)
        {
            try
            {
                ListAllAccountsResponseDto accounts = await _accountsRepository.GetAllAsync(filter, status, orderBy, page);

                return new ResponseModel
                {
                    Content = accounts,
                    StatusCode = HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    Message = $"AccountsService|GetAllAsync|InternalServerError|Error:{ex.Message}",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
        public async Task<ResponseModel> DeleteAsync(Guid id)
        {
            try
            {
                ResponseModel accountDataResponse = await GetAccountDataAsync(id, false);

                if (accountDataResponse.StatusCode != HttpStatusCode.OK)
                    return accountDataResponse;

                Accounts account = (Accounts)accountDataResponse.Content!;

                if (account.Status == AccountStatus.INACTIVE)
                {
                    return new ResponseModel
                    {
                        Message = "Only active accounts can be deleted",
                        StatusCode = HttpStatusCode.BadRequest
                    };
                }

                account.Status = AccountStatus.INACTIVE;
                account.UpdatedAt = DateTime.UtcNow;

                await _accountsRepository.UpdateAsync(account);
                await _accountCacheService.RemoveAccountAsync(id);
                await _accountProducer.PublishAsync(accountDeletedEvent, accountExchangeName, ExchangeType.Direct, account);

                return new ResponseModel
                {
                    Message = "Account deleted successfully",
                    StatusCode = HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    Message = $"AccountsService|DeleteAsync|InternalServerError|Error:{ex.Message}",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
        private async Task<ResponseModel> GetAccountDataAsync(Guid id, bool updateAccountCache = true)
        {
            try
            {
                Accounts? account = await _accountCacheService.GetAccountAsync(id);

                if (account == null)
                {
                    Accounts accountFromDb = await _accountsRepository.GetByIdAsync(id);

                    if (accountFromDb == null)
                    {
                        return new ResponseModel
                        {
                            Message = accountNotFound,
                            StatusCode = HttpStatusCode.NotFound
                        };
                    }

                    account = accountFromDb;

                    if (updateAccountCache)
                        await _accountCacheService.SaveAccountAsync(accountFromDb);
                }

                return new ResponseModel
                {
                    Content = account,
                    StatusCode = HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                return new ResponseModel
                {
                    Message = $"AccountsService|GetAccountDataAsync|InternalServerError|Error:{ex.Message}",
                    StatusCode = HttpStatusCode.InternalServerError
                };
            }
        }
    }
}

using krt_api.Core.Accounts.Dtos;
using krt_api.Core.Accounts.Entities;
using krt_api.Core.Accounts.Interfaces;
using krt_api.Core.Accounts.Utils.Enums;
using krt_api.Core.Utils;
using krt_api.Core.Utils.Enums;
using Microsoft.AspNetCore.Mvc;

namespace krt_api.Controllers
{

    [ApiController]
    [Route("[controller]")]
    public class AccountsController : ControllerBase
    {
        private readonly IAccountsService _accountsService;
        public AccountsController(IAccountsService accountsService)
        {
            _accountsService = accountsService;
        }

        /// <summary>
        /// Cria uma nova conta de um cliente
        /// </summary>
        /// <param name="dto">Dados para criação da conta.</param>
        /// <returns>Retorna o status e os dados da conta criada.</returns>
        [HttpPost("create")]
        [ProducesResponseType(typeof(Accounts), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Create([FromBody] CreateAccountDto dto)
        {
            ResponseModel response = await _accountsService.CreateAsync(dto);
            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Atualizar os dados da conta de um cliente
        /// </summary>
        /// <param name="dto">Dados para edição da conta.</param>
        /// <returns>Retorna o status e os dados da conta atualizada.</returns>
        [HttpPut("update")]
        [ProducesResponseType(typeof(Accounts), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Update([FromBody] UpdateAccountDto dto)
        {
            ResponseModel response = await _accountsService.UpdateAsync(dto);
            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Consultar os dados da conta de um cliente
        /// </summary>
        /// <param name="id">ID da conta do cliente.</param>
        /// <returns>Retorna o status e os dados da conta.</returns>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(Accounts), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Get([FromRoute] Guid id)
        {
            ResponseModel response = await _accountsService.GetAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// listagem de todas as contas de clientes (com filtros opcionais e paginado)
        /// </summary>
        /// <param name="filter">Pode ser o nome ou cpf do cliente</param>
        /// <param name="status">Status da conta</param>
        /// <param name="orderBy">Ordem da listagem (por padrão é dos registros mais novos para os mais antigos)</param>
        /// <param name="page">Página atual da consulta</param>
        /// <returns>Retorna o status e a listagem.</returns>
        [HttpGet("get-all")]
        [ProducesResponseType(typeof(List<ListAllAccountsResponseDto>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> GetAll([FromQuery] string? filter = null, 
            [FromQuery] AccountStatus? status = null, 
            [FromQuery] OrderBy orderBy = OrderBy.Descending,
            [FromQuery] int page = 1)
        {
            ResponseModel response = await _accountsService.GetAllAsync(filter, status, orderBy, page);
            return StatusCode((int)response.StatusCode, response);
        }

        /// <summary>
        /// Deletar a conta de um cliente (passa a conta do cliente para o status de inativa)
        /// </summary>
        /// <param name="id">ID da conta do cliente.</param>
        /// <returns>Retorna o status e uma mensagem de sucesso.</returns>
        [HttpDelete("{id}")]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ResponseModel), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> Delete([FromRoute] Guid id)
        {
            ResponseModel response = await _accountsService.DeleteAsync(id);
            return StatusCode((int)response.StatusCode, response);
        }

    }
}

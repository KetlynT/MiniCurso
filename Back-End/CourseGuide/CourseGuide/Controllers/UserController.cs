using CourseGuide.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using CourseGuide.Objects.DTOs.Entities;
using CourseGuide.Objects.Contracts;
using CourseGuide.Objects.Utilities;
using System.Dynamic;

namespace CourseGuide.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : Controller
    {
        private readonly IUserService _userService;
        private readonly Response _response;

        public UserController(IUserService userService)
        {
            _userService = userService;

            _response = new Response();
        }

        [HttpGet("GetAll")]
        public async Task<ActionResult<IEnumerable<UserDTO>>> GetAll()
        {
            try
            {
                var usersDTO = await _userService.GetAll();
                _response.SetSuccess();
                _response.Message = usersDTO.Any() ?
                    "Lista do(s) Usu�rio(s) obtida com sucesso." :
                    "Nenhum Usu�rio encontrado.";
                _response.Data = usersDTO;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel adquirir a lista do(s) Usu�rio(s)!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpGet("GetById/{id:int}")]
        public async Task<ActionResult<UserDTO>> GetById(int id)
        {
            try
            {
                var userDTO = await _userService.GetById(id);
                if (userDTO == null)
                {
                    _response.SetNotFound();
                    _response.Message = "Usu�rio n�o encontrado!";
                    _response.Data = userDTO;
                    return NotFound(_response);
                };

                _response.SetSuccess();
                _response.Message = "Usu�rio " + userDTO.NameUser + " obtido com sucesso.";
                _response.Data = userDTO;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel adquirir o Usu�rio informado!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPost("Create")]
        public async Task<ActionResult> Create([FromBody] UserDTO userDTO)
        {
            if (userDTO == null)
            {
                _response.SetInvalid();
                _response.Message = "Dado(s) inv�lido(s)!";
                _response.Data = userDTO;
                return BadRequest(_response);
            }
            userDTO.Id = 0;

            try
            {
                dynamic errors = new ExpandoObject();
                var hasErrors = false;

                CheckDatas(userDTO, ref errors, ref hasErrors);

                if (hasErrors)
                {
                    _response.SetConflict();
                    _response.Message = "Dado(s) com conflito!";
                    _response.Data = errors;
                    return BadRequest(_response);
                }

                var usersDTO = await _userService.GetAll();
                CheckDuplicates(usersDTO, userDTO, ref errors, ref hasErrors);

                if (hasErrors)
                {
                    _response.SetConflict();
                    _response.Message = "Dado(s) com conflito!";
                    _response.Data = errors;
                    return BadRequest(_response);
                }

                userDTO.PasswordUser = userDTO.PasswordUser.HashPassword();
                await _userService.Create(userDTO);

                _response.SetSuccess();
                _response.Message = "Usu�rio " + userDTO.NameUser + " cadastrado com sucesso.";
                _response.Data = userDTO;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel cadastrar o Usu�rio!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPost("Login")]
        public async Task<ActionResult> Login([FromBody] Login login)
        {
            if (login is null)
            {
                _response.SetInvalid();
                _response.Message = "Dado(s) inv�lido(s)!";
                _response.Data = login;
                return BadRequest(_response);
            }

            try
            {
                login.Password = login.Password.HashPassword();
                var userDTO = await _userService.Login(login);
                if (userDTO is null)
                {
                    _response.SetUnauthorized();
                    _response.Message = "Login inv�lido!";
                    _response.Data = new { errorLogin = "Login inv�lido!" };
                    return BadRequest(_response);
                }

                var token = new Token();
                token.GenerateToken(userDTO.EmailUser);

                _response.SetSuccess();
                _response.Message = "Login realizado com sucesso.";
                _response.Data = token;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel realizar o Login!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpPost("Validate")]
        public async Task<ActionResult> Validate([FromBody] Token token)
        {
            if (token is null)
            {
                _response.SetInvalid();
                _response.Message = "Dado inv�lido!";
                _response.Data = token;
                return BadRequest(_response);
            }

            try
            {
                var email = token.ExtractSubject();

                if (string.IsNullOrEmpty(email) || await _userService.GetByEmail(email) == null)
                {
                    _response.SetUnauthorized();
                    _response.Message = "Token inv�lido!";
                    _response.Data = new { errorToken = "Token inv�lido!" };
                    return BadRequest(_response);
                }
                else if (!token.ValidateToken())
                {
                    _response.SetUnauthorized();
                    _response.Message = "Token inv�lido!";
                    _response.Data = new { errorToken = "Token inv�lido!" };
                    return BadRequest(_response);
                }

                _response.SetSuccess();
                _response.Message = "Token validado com sucesso.";
                _response.Data = token;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel validar o Token!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }


        [HttpPut("Update")]
        public async Task<ActionResult> Update([FromBody] UserDTO userDTO)
        {
            if (userDTO == null)
            {
                _response.SetInvalid();
                _response.Message = "Dado(s) inv�lido(s)!";
                _response.Data = userDTO;
                return BadRequest(_response);
            }

            try
            {
                var existingUserDTO = await _userService.GetById(userDTO.Id);
                if (existingUserDTO == null)
                {
                    _response.SetNotFound();
                    _response.Message = "Dado(s) com conflito!";
                    _response.Data = new { errorId = "O Usu�rio informado n�o existe!" };
                    return NotFound(_response);
                }

                dynamic errors = new ExpandoObject();
                var hasErrors = false;

                CheckDatas(userDTO, ref errors, ref hasErrors);

                if (hasErrors)
                {
                    _response.SetConflict();
                    _response.Message = "Dado(s) com conflito!";
                    _response.Data = errors;
                    return BadRequest(_response);
                }

                var usersDTO = await _userService.GetAll();
                CheckDuplicates(usersDTO, userDTO, ref errors, ref hasErrors);

                if (hasErrors)
                {
                    _response.SetConflict();
                    _response.Message = "Dado(s) com conflito!";
                    _response.Data = errors;
                    return BadRequest(_response);
                }

                userDTO.PasswordUser = existingUserDTO.PasswordUser;
                await _userService.Update(userDTO);

                _response.SetSuccess();
                _response.Message = "Usu�rio " + userDTO.NameUser + " alterado com sucesso.";
                _response.Data = userDTO;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel alterar o Usu�rio!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        [HttpDelete("Delete/{id:int}")]
        public async Task<ActionResult<UserDTO>> Delete(int id)
        {
            try
            {
                var userDTO = await _userService.GetById(id);
                if (userDTO == null)
                {
                    _response.SetNotFound();
                    _response.Message = "Dado com conflito!";
                    _response.Data = new { errorId = "Usu�rio n�o encontrado!" };
                    return NotFound(_response);
                }

                await _userService.Delete(userDTO);

                _response.SetSuccess();
                _response.Message = "Usu�rio " + userDTO.NameUser + " exclu�do com sucesso.";
                _response.Data = userDTO;
                return Ok(_response);
            }
            catch (Exception ex)
            {
                _response.SetError();
                _response.Message = "N�o foi poss�vel excluir o Usu�rio!";
                _response.Data = new { ErrorMessage = ex.Message, StackTrace = ex.StackTrace ?? "No stack trace available!" };
                return StatusCode(StatusCodes.Status500InternalServerError, _response);
            }
        }

        private static void CheckDatas(UserDTO userDTO, ref dynamic errors, ref bool hasErrors)
        {
            if (!ValidatorUtilitie.CheckValidPhone(userDTO.PhoneUser))
            {
                errors.errorPhoneUser = "N�mero inv�lido!";
                hasErrors = true;
            }

            int status = ValidatorUtilitie.CheckValidEmail(userDTO.EmailUser);
            if (status == -1)
            {
                errors.errorEmailUser = "E-mail inv�lido!";
                hasErrors = true;
            }
            else if (status == -2)
            {
                errors.errorEmailUser = "Dom�nio inv�lido!";
                hasErrors = true;
            }
        }

        private static void CheckDuplicates(IEnumerable<UserDTO> usersDTO, UserDTO userDTO, ref dynamic errors, ref bool hasErrors)
        {
            foreach (var user in usersDTO)
            {
                if (userDTO.Id == user.Id)
                {
                    continue;
                }

                if (ValidatorUtilitie.CompareString(userDTO.EmailUser, user.EmailUser))
                {
                    errors.errorEmailUser = "O e-mail " + userDTO.EmailUser + " j� est� sendo utilizado!";
                    hasErrors = true;

                    break;
                }
            }
        }
    }
}
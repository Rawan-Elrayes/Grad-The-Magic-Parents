using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Interfaces;
using Microsoft.AspNetCore.Http;
using System.IO;
using System;
using Microsoft.EntityFrameworkCore;
using TheMagicParents.Enums;
using TheMagicParents.Models;
using TheMagicParents.Infrastructure.Repositories;
using Microsoft.AspNetCore.Identity;

using Microsoft.AspNetCore.WebUtilities;
using System.Text;
using TheMagicParents.Core.Responses;
using Microsoft.Extensions.DependencyInjection;

namespace TheMagicParents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IClientRepository clientRepository;
        private readonly IServiceProviderRepository serviceProviderRepository;
        private readonly IUserRepository userRepository;
        private readonly IAuthentication _authService;
        private readonly UserManager<User> _userManager;

        public AccountController(IClientRepository clientRepository, IUserRepository userRepository, IServiceProviderRepository serviceProviderRepository, IAuthentication authService, UserManager<User> userManager)
        {
            this.clientRepository = clientRepository;
            this.userRepository = userRepository;
            this.serviceProviderRepository = serviceProviderRepository;
            _authService = authService;
            _userManager = userManager;
        }

        [HttpGet("governments")]
        public async Task<IActionResult> GetGovernorate()
        {
            try
            {
                var governments = await userRepository.GetGovernorateAsync();
                return Ok(new Response<IEnumerable<Governorate>>
                {
                    Data = governments,
                    Status = 0,
                    Message = "Governorates fetched successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<IEnumerable<Governorate>>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("cities/{GovernorateId}")]
        public async Task<IActionResult> GetCitiesByGovernorate(int GovernorateId)
        {
            try
            {
                var cities = await userRepository.GetCitiesByGovernorateAsync(GovernorateId);
                return Ok(new Response<IEnumerable<City>>
                {
                    Data = cities,
                    Status = 0,
                    Message = "cities fetched successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<IEnumerable<City>>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("register/client")]
        public async Task<IActionResult> RegisterClient([FromForm] ClientRegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<ClientRegisterResponse>
                {
                    Message = "Invalid model state",
                    Status = 1,
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var client = await clientRepository.RegisterClientAsync(model);
                HttpContext.Session.SetString("UserId", client.Id.ToString());
                return Ok(new Response<ClientRegisterResponse>
                {
                    Message = "Client registered successfully",
                    Data = client,
                    Status = 0
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ClientRegisterResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ClientRegisterResponse>
                {
                    Message = "An error occurred while registering the client.",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("register/ServiceProvider")]
        public async Task<IActionResult> RegisterServiceProvider([FromForm] ServiceProviderRegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<ServiceProviderRegisterResponse>
                {
                    Message = "Invalid model state",
                    Status = 1,
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var ServiceProvider = await serviceProviderRepository.RegisterServiceProviderAsync(model);

                HttpContext.Session.SetString("UserId", ServiceProvider.Id.ToString());
                return Ok(new Response<ServiceProviderRegisterResponse>
                {
                    Message = "service provider registered successfully",
                    Data = ServiceProvider,
                    Status = 0
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ServiceProviderRegisterResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ServiceProviderRegisterResponse>
                {
                    Message = "An error occurred while registering the service provider.",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.LoginAsync(model);
            if (result.Data != null)  // Adding null check before accessing Data
            {
                HttpContext.Session.SetString("UserId", result.Data.ToString());
            }
            if (result.Status!=0)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(model);
            if (result.Status != 0)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(model);
            if (result.Status != 0)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _authService.LogoutAsync();
            return Ok(result);
        }

        //Forgot to handle change password and deletion through repository pattern 

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = HttpContext.Session.GetString("UserId");
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return BadRequest(new Response<string>
                {
                    Message = "User not found",
                    Status = 1
                });

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (!result.Succeeded)
                return BadRequest(new Response<string>
                {
                    Message = "Failed to change password",
                    Status = 1,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });

            return Ok(new Response<string>
            {
                Message = "Password changed successfully",
                Status = 0
            });
        }
    }
}
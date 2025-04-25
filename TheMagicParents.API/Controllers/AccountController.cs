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

        public AccountController(IClientRepository clientRepository, IUserRepository userRepository, IServiceProviderRepository serviceProviderRepository, IAuthentication authService)
        {
            this.clientRepository = clientRepository;
            this.userRepository = userRepository;
            this.serviceProviderRepository = serviceProviderRepository;
            _authService = authService;
        }

        [HttpGet("governments")]
        public async Task<IActionResult> GetGovernments()
        {
            var governments = await userRepository.GetGovernmentsAsync();
            return Ok(governments);
        }

        [HttpGet("cities/{governmentId}")]
        public async Task<IActionResult> GetCitiesByGovernment(int governmentId)
        {
            var cities = await userRepository.GetCitiesByGovernmentAsync(governmentId);
            return Ok(cities);
        }

        [HttpPost("register/client")]
        public async Task<IActionResult> RegisterClient([FromForm] ClientRegisterDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new Response<ClientRegisterResponse>
                {
                    Message = "Invalid model state",
                    Status = false,
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var client = await clientRepository.RegisterClientAsync(model);

                return Ok(new Response<ClientRegisterResponse>
                {
                    Message = "Client registered successfully",
                    Data = client,
                    Status = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ClientRegisterResponse>
                {
                    Message = ex.Message,
                    Status = false,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ClientRegisterResponse>
                {
                    Message = "An error occurred while registering the client.",
                    Status = false,
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
                    Status = false,
                    Errors = ModelState.Values.SelectMany(v => v.Errors)
                                        .Select(e => e.ErrorMessage).ToList()
                });
            }

            try
            {
                var ServiceProvider = await serviceProviderRepository.RegisterServiceProviderAsync(model);

                return Ok(new Response<ServiceProviderRegisterResponse>
                {
                    Message = "service provider registered successfully",
                    Data = ServiceProvider,
                    Status = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ServiceProviderRegisterResponse>
                {
                    Message = ex.Message,
                    Status = false,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ServiceProviderRegisterResponse>
                {
                    Message = "An error occurred while registering the service provider.",
                    Status = false,
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
            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ForgotPasswordAsync(model);
            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _authService.ResetPasswordAsync(model);
            if (!result.Status)
                return BadRequest(result);

            return Ok(result);
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            var result = await _authService.LogoutAsync();
            return Ok(result);
        }
    }
}
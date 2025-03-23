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

namespace TheMagicParents.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IClientRepository clientRepository;
        private readonly IUserRepository userRepository;

        public AccountController(IClientRepository clientRepository, IUserRepository userRepository)
        {
            this.clientRepository = clientRepository;
            this.userRepository = userRepository;
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
                return BadRequest(ModelState);
            }

            try
            {
                var client=await clientRepository.RegisterClientAsync(model);

                return Ok(new
                {
                    Message = "Client registered successfully",
                    UserNameId = client.UserNameId,
                    PersonalPhotoUrl = client.PersonalPhoto,
                    IdCardFrontPhotoUrl = client.IdCardFrontPhoto,
                    IdCardBackPhotoUrl = client.IdCardBackPhoto
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { Message = ex.Message });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    Message = "An error occurred while registering the client.",
                    Error = ex.Message
                });
            }
        }

        
    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Core.Responses;
using TheMagicParents.Models;

namespace TheMagicParents.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServiceProviderController : ControllerBase
    {
        private readonly IServiceProviderRepository serviceProviderRepository;

        public ServiceProviderController(IServiceProviderRepository serviceProviderRepository)
        {
            this.serviceProviderRepository = serviceProviderRepository;
        }

        [HttpPost("SaveAvailability")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> SaveAvailability([FromForm] AvailabilityDTO request)
        {
            var userId = HttpContext.Session.GetString("UserId");
            try
            {
                // Add new ones
                var newAvailabilities = await serviceProviderRepository.SaveAvailability(request, userId);

                return Ok(new Response<AvailabilityResponse>
                {
                    Message = "Availability added successfully>",
                    Data = newAvailabilities,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<AvailabilityResponse>
                {
                    Message = "An error occurred.",
                    Status = false,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("GetAvailabilitiesHoures/{date}")]
        [Authorize(Roles = "ServiceProvider")]
        public async Task<IActionResult> GetAvailabilitiesHoures(DateTime date)
        {
            var userId = HttpContext.Session.GetString("UserId");
            try
            {
                var availabilities = await serviceProviderRepository.GetAvailabilitiesHoures(date,userId);

                return Ok(new Response<AvailabilityResponse>
                {
                    Data = availabilities,
                    Status = true
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Message = ex.Message });
            }
        }

        [HttpGet("profileData")]
        public async Task<IActionResult> GetServiceProviderProfile()
        {
            var userId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(userId))
                return Unauthorized(new Response<string>
                {
                    Message = "User not authenticated",
                    Status = false
                });

            try
            {
                var profile = await serviceProviderRepository.GetProfileAsync(userId);
                return Ok(new Response<ProviderGetDataResponse>
                {
                    Data = profile,
                    Status = true,
                    Message = "Profile retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<ProviderGetDataResponse>
                {
                    Message = ex.Message,
                    Status = false,
                    Errors = new List<string> { ex.Message }
                });
            }
        }


        [HttpPut("update-profile")]
        public async Task<IActionResult> UpdateServiceProviderProfile([FromForm] ServiceProviderUpdateProfileDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                var result = await serviceProviderRepository.UpdateProfileAsync(userId, model);

                return Ok(new Response<ProviderGetDataResponse>
                {
                    Message = "Profile updated successfully",
                    Data = result,
                    Status = true
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ProviderGetDataResponse>
                {
                    Message = ex.Message,
                    Status = false,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ProviderGetDataResponse>
                {
                    Message = "An error occurred while updating the profile.",
                    Status = false,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

    }
}
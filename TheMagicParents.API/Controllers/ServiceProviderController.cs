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
    }
}
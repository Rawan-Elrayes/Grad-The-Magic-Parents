using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
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
                var newAvailabilities = await serviceProviderRepository.SaveAvailability(request, userId);

                return Ok(new Response<AvailabilityResponse>
                {
                    Message = "Availability added successfully>",
                    Data = newAvailabilities,
                    Status = 0
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<AvailabilityResponse>
                {
                    Message = "An error occurred.",
                    Status = 1,
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
                var availabilities = await serviceProviderRepository.GetAvailabilitiesHoures(date, userId);

                return Ok(new Response<AvailabilityResponse>
                {
                    Data = availabilities,
                    Status = 0
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
                    Status = 1
                });

            try
            {
                var profile = await serviceProviderRepository.GetProfileAsync(userId);
                return Ok(new Response<ProviderGetDataResponse>
                {
                    Data = profile,
                    Status = 0,
                    Message = "Profile retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<ProviderGetDataResponse>
                {
                    Message = ex.Message,
                    Status = 1
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
                    Status = 0
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ProviderGetDataResponse>
                {
                    Message = ex.Message,
                    Status = 1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ProviderGetDataResponse>
                {
                    Message = "An error occurred while updating the profile.",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("filter")]
        public async Task<IActionResult> FilterProviders([FromBody] ProviderFilterDTO filter, [FromQuery] int page = 1, [FromQuery] int pageSize = 8)
        {
            try
            {
                var result = await serviceProviderRepository.GetFilteredProvidersAsync(filter, page, pageSize);
                return Ok(new Response<PagedResult<FilteredProviderDTO>>
                {
                    Data = result,
                    Status = 0,
                    Message = "Providers retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<PagedResult<FilteredProviderDTO>>
                {
                    Message = "Failed to retrieve providers",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{bookingId}/confirm")]
        public async Task<IActionResult> ConfirmBooking(int bookingId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var providerId = HttpContext.Session.GetString("UserId");
                var result = await serviceProviderRepository.ConfirmBookingAsync(bookingId, providerId);

                return Ok(new Response<BookingConfirmationResponse>
                {
                    Message = "Booking confirmed successfully.",
                    Data = result,
                    Status = 0
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<BookingConfirmationResponse>
                {
                    Message = ex.Message,
                    Status = 1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<BookingConfirmationResponse>
                {
                    Message = "An error occurred while conferming the booking.",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("{bookingId}/reject")]
        public async Task<IActionResult> RejectBooking(int bookingId)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);
            try
            {
                var providerId = HttpContext.Session.GetString("UserId");
                var result = await serviceProviderRepository.RejectBookingAsync(bookingId, providerId);

                return Ok(new Response<BookingConfirmationResponse>
                {
                    Message = "Booking rejected successfully.",
                    Data = result,
                    Status = 0
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<BookingConfirmationResponse>
                {
                    Message = ex.Message,
                    Status = 1
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<BookingConfirmationResponse>
                {
                    Message = "An error occurred while rejecting the booking.",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

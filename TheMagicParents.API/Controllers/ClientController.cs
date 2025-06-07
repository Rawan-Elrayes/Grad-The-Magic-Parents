using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Core.Responses;
using TheMagicParents.Infrastructure.Repositories;

namespace TheMagicParents.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientController : ControllerBase
    {
        private readonly IClientRepository _clientRepository;

        public ClientController(IClientRepository clientRepository)
        {
            _clientRepository = clientRepository;
        }

        [HttpGet("profileData")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetClientProfile()
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
                var profile = await _clientRepository.GetProfileAsync(userId);
                return Ok(new Response<ClientGetDataResponse>
                {
                    Data = profile,
                    Status = 0,
                    Message = "Profile retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<ClientGetDataResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPut("update-profile")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> UpdateClientProfile([FromForm] ClientUpdateProfileDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new Response<string>
                    {
                        Message = "User not authenticated",
                        Status = 1
                    });

                var result = await _clientRepository.UpdateProfileAsync(userId, model);

                return Ok(new Response<ClientGetDataResponse>
                {
                    Message = "Profile updated successfully",
                    Data = result,
                    Status = 0
                });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new Response<ClientGetDataResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new Response<ClientGetDataResponse>
                {
                    Message = "An error occurred while updating the profile.",
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("GetSelectedServiceProvider")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetSelectedServiceProvider(string serviceProviderId)
        {
            try
            {
                var profile = await _clientRepository.GetSelctedProviderProfile(serviceProviderId);
                return Ok(new Response<GetSelectedProvider>
                {
                    Data = profile,
                    Status = 0,
                    Message = "Profile geted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<GetSelectedProvider>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpGet("GetSelectedServiceProviderAvailabilities")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> GetSelectedServiceProviderAvailabilities(string serviceProviderId)
        {
            try
            {
                var availabilities = await _clientRepository.GetSelectedProviderAvailableDaysOfWeek(serviceProviderId);
                return Ok(new Response<List<AvailabilityResponse>>
                {
                    Data = availabilities,
                    Status = 0,
                    Message = "Availabilities geted successfully"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<List<AvailabilityResponse>>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("SubmitBooking")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SubmitBooking(string serviceProviderId, [FromForm] BookingDTO bookingDTO)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new Response<string>
                    {
                        Message = "User not authenticated",
                        Status = 1
                    });

                var Book = await _clientRepository.CreateBookingAsync(bookingDTO, userId, serviceProviderId);
                return Ok(new Response<BookingResponse>
                {
                    Data = Book,
                    Status = 0,
                    Message = "Booking submitted succefully and service rovider will confirm your book soon."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<BookingResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        [HttpPost("SubmitReviewBooking")]
        [Authorize(Roles = "Client")]
        public async Task<IActionResult> SubmitReviewBooking([FromForm] ReviewDTO reviewDTO)
        {
            try
            {
                var userId = HttpContext.Session.GetString("UserId");
                if (string.IsNullOrEmpty(userId))
                    return Unauthorized(new Response<string>
                    {
                        Message = "User not authenticated",
                        Status = 1
                    });

                var review = await _clientRepository.SubmitReviewAsync(reviewDTO, userId);
                return Ok(new Response<ReviewSubmissionResponse>
                {
                    Data = review,
                    Status = 0,
                    Message = "Review submitted succefully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<ReviewSubmissionResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}
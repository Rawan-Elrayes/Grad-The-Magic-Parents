using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TheMagicParents.Core.DTOs;
using TheMagicParents.Core.EmailService;
using TheMagicParents.Core.Interfaces;
using TheMagicParents.Core.Responses;
using TheMagicParents.Enums;
using TheMagicParents.Infrastructure.Repositories;
using TheMagicParents.Models;

namespace TheMagicParents.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserRepository userRepository;
        private readonly UserManager<User> _userManager;

        public UserController(IUserRepository userRepository, UserManager<User> userManager)
        {
            this.userRepository = userRepository;
            _userManager = userManager;
        }

        [HttpPost("request-account-deletion")]
        public async Task<IActionResult> RequestAccountDeletion()
        {
            var userId = HttpContext.Session.GetString("UserId");
            var user = await _userManager.FindByIdAsync(userId);

            if (user == null)
                return BadRequest(new Response<string>
                {
                    Message = "User not found",
                    Status = 1
                });

            user.AccountState = StateType.PendingDeletion;
            var result = await _userManager.UpdateAsync(user);

            if (!result.Succeeded)
                return BadRequest(new Response<string>
                {
                    Message = "Failed to request account deletion",
                    Status = 1,
                    Errors = result.Errors.Select(e => e.Description).ToList()
                });

            return Ok(new Response<string>
            {
                Message = "Account deletion request submitted successfully",
                Status = 0
            });
        }

        [HttpPost("report-user")]
        public async Task<IActionResult> ReportUser([FromBody] SupportDTO model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var reporterId = HttpContext.Session.GetString("UserId");
            if (string.IsNullOrEmpty(reporterId))
                return Unauthorized(new Response<string>
                {
                    Message = "User not authenticated",
                    Status = 1
                });

            var result = await userRepository.SubmitReportAsync(reporterId, model.UserName, model.Comment);

            if (!result)
                return BadRequest(new Response<string>
                {
                    Message = "Failed to submit report. User not found or invalid report.",
                    Status = 1
                });

            return Ok(new Response<string>
            {
                Message = "Report submitted successfully",
                Status = 0
            });
        }

        [HttpGet("GetPendingBookings")]
        public async Task<IActionResult> GetPendingBookings()
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
                var Bookings = await userRepository.GetPendingBookingsAsync(userId);
                return Ok(new Response<List<BookingResponse>>
                {
                    Data = Bookings,
                    Status = 0,
                    Message = "Bookings fetched successfully."
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

        [HttpGet("GetConfirmedBookings")]
        public async Task<IActionResult> GetConfirmedBookings()
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
                var Bookings = await userRepository.GetProviderConfirmedBookingsAsync(userId);
                return Ok(new Response<List<BookingResponse>>
                {
                    Data = Bookings,
                    Status = 0,
                    Message = "Bookings fetched successfully."
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

        [HttpGet("GetPaidBookings")]
        public async Task<IActionResult> GetPaidBookingsAsync()
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
                var Bookings = await userRepository.GetPaidBookingsAsync(userId);
                return Ok(new Response<List<BookingResponse>>
                {
                    Data = Bookings,
                    Status = 0,
                    Message = "Bookings fetched successfully."
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

        [HttpGet("GetCancelledBookings")]
        public async Task<IActionResult> GetCancelledBookings()
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
                var Bookings = await userRepository.GetCancelledBookingsAsync(userId);
                return Ok(new Response<List<BookingResponse>>
                {
                    Data = Bookings,
                    Status = 0,
                    Message = "Bookings fetched successfully."
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

        [HttpGet("GetCompletedBookings")]
        public async Task<IActionResult> GetCompletedBookings()
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
                var Bookings = await userRepository.GetCompletedBookingsAsync(userId);
                return Ok(new Response<List<BookingResponse>>
                {
                    Data = Bookings,
                    Status = 0,
                    Message = "Bookings fetched successfully."
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

        [HttpGet("GetRejectedBookings")]
        public async Task<IActionResult> GetRejectedBookings()
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
                var Bookings = await userRepository.GetRejectedBookingsAsync(userId);
                return Ok(new Response<List<BookingResponse>>
                {
                    Data = Bookings,
                    Status = 0,
                    Message = "Bookings fetched successfully."
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

        [HttpPut("cancelBookings")]
        public async Task<IActionResult> CancelBooking(int bookingId)
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
                var CancelTran = await userRepository.CancelBookingAsync(bookingId, userId);
                return Ok(new Response<CancelBookingResponse>
                {
                    Data = CancelTran,
                    Status = 0,
                    Message = "Booking cancelled successfully."
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new Response<CancelBookingResponse>
                {
                    Message = ex.Message,
                    Status = 1,
                    Errors = new List<string> { ex.Message }
                });
            }
        }
    }
}

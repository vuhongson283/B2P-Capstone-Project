using Microsoft.AspNetCore.Mvc;
using Stripe;
using B2P_API.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace B2P_API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestStripeController : ControllerBase
    {
        private readonly SportBookingDbContext _context;
        private readonly IConfiguration _configuration;

        public TestStripeController(SportBookingDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("onboard")]
        public async Task<IActionResult> OnboardPartner([FromQuery] int userId)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                return NotFound("User not found.");
            }

            var stripeSecretKey = _configuration["Stripe:SecretKey"];
            if (string.IsNullOrEmpty(stripeSecretKey))
            {
                return StatusCode(500, "Stripe Secret Key is not configured.");
            }

            // --- BỔ SUNG: TẠO MỘT INSTANCE CỦA STRIPECLIENT ---
            var stripeClient = new StripeClient(stripeSecretKey);

            string stripeAccountId;
            if (string.IsNullOrEmpty(user.StripeAccountId))
            {
                var options = new AccountCreateOptions
                {
                    Type = "express",
                    Country = "US",
                    Email = user.Email,
                    Capabilities = new AccountCapabilitiesOptions
                    {
                        CardPayments = new AccountCapabilitiesCardPaymentsOptions { Requested = true },
                        Transfers = new AccountCapabilitiesTransfersOptions { Requested = true },
                    },
                };
                // TRUYỀN INSTANCE CỦA STRIPECLIENT VÀO CONSTRUCTOR
                var service = new AccountService(stripeClient); // ĐÃ SỬA ĐỔI
                var account = await service.CreateAsync(options);
                stripeAccountId = account.Id;

                user.StripeAccountId = stripeAccountId;
                await _context.SaveChangesAsync();
            }
            else
            {
                stripeAccountId = user.StripeAccountId;
            }

            var accountLinkOptions = new AccountLinkCreateOptions
            {
                Account = stripeAccountId,
                RefreshUrl = "http://localhost:5227/api/TestStripe/onboard-refresh",
                ReturnUrl = "http://localhost:5227/api/TestStripe/onboard-return",
                Type = "account_onboarding",
            };
            // TRUYỀN INSTANCE CỦA STRIPECLIENT VÀO CONSTRUCTOR
            var accountLinkService = new AccountLinkService(stripeClient); // ĐÃ SỬA ĐỔI
            var accountLink = await accountLinkService.CreateAsync(accountLinkOptions);

            return Ok(new { Url = accountLink.Url });
        }

        [HttpGet("onboard-return")]
        public IActionResult OnboardReturn()
        {
            return Ok("Onboarding complete! You can now close this window or return to your application.");
        }

        [HttpGet("onboard-refresh")]
        public IActionResult OnboardRefresh()
        {
            return BadRequest("Onboarding link expired or failed. Please try again from your application.");
        }
    }
}
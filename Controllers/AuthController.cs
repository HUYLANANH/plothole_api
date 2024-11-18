using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PotholeDetectionApi.Dto;
using PotholeDetectionApi.Service;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Web;

namespace PotholeDetectionApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IConfiguration _configuration;
        private readonly EmailService _emailService;

        public AuthController(UserManager<IdentityUser> userManager, IConfiguration configuration, 
            EmailService emailService)
        {
            _userManager = userManager;
            _configuration = configuration;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserDto registerDto)
        {
            var user = new IdentityUser { UserName = registerDto.Username, Email = registerDto.Email };
            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
                return Ok(new { Message = "User registered successfully" });

            return BadRequest(result.Errors);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserDto loginDto)
        {
            var user = await _userManager.FindByNameAsync(loginDto.Username);
            if (user != null && await _userManager.CheckPasswordAsync(user, loginDto.Password))
            {
                var token = GenerateJwtToken(user);
                return Ok(new { Token = token });
            }

            return Unauthorized("Invalid credentials");
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto forgotPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(forgotPasswordDto.Email);
            if (user == null)
            {
                return NotFound("User with this email does not exist");
            }

            if (_emailService == null)
            {
                return StatusCode(500, "Email service not available.");
            }

            try
            {
                _emailService.GenerateAndStoreOtp(forgotPasswordDto.Email);

                return Ok( "OTP đã được gửi đến email của bạn");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi gửi email: {ex.Message}" +$"{ex.StackTrace}");
            }
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpDto request)
        {
            // Kiểm tra tính hợp lệ của OTP
            bool isValid = await Task.Run(() => _emailService.VerifyOtp(request.Email, request.Otp));

            if (isValid)
            {
                return Ok(new { Message = "OTP verified successfully." });
            }

            return BadRequest("Invalid OTP or OTP has expired.");
        }

        [HttpGet("get-otp")]
        public IActionResult GetOtpList()
        {
            var otpList = _emailService.GetOtpStore(); // Gọi phương thức lấy danh sách OTP

            if (otpList == null || !otpList.Any())
            {
                return NotFound("No OTPs found.");
            }

            return Ok(otpList);
        }

        /*[HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto resetPasswordDto)
        {
            var user = await _userManager.FindByEmailAsync(resetPasswordDto.Email);
            if (user == null)
            {
                return NotFound("User with this email does not exist");
            }

            var result = await _userManager.ResetPasswordAsync(user, decodedToken, resetPasswordDto.NewPassword);
            if (result.Succeeded)
            {
                return Ok("Password has been reset successfully");
            }

            return BadRequest(result.Errors);
        }*/



        private string GenerateJwtToken(IdentityUser user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]);
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                new Claim(ClaimTypes.Name, user.UserName),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            }),
                Expires = DateTime.UtcNow.AddHours(10),
                Issuer = _configuration["Jwt:Issuer"],
                Audience = _configuration["Jwt:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };
            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }

    }
}

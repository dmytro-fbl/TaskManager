using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using TaskManager.API.Repositories;
using TaskManager.API.Utils;

namespace TaskManager.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly IUserRepository _userRepository;
        public AuthController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _userRepository.GetUserByEmail(request.Email);

           
            if ((user == null || user.PasswordHash == null || user.PasswordSalt == null))
            {
                return BadRequest("Неправильний email або пароль");
            }

            bool isPasswordValid = PasswordHasher.VerifyPasswordHash(request.Password, user.PasswordHash, user.PasswordSalt);
            if (!isPasswordValid)
            {
                return BadRequest("Неправильний email або пароль");
            } 

            return Ok("Успішний вхід");
        }
    }
}

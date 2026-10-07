using DAL;
using Microsoft.AspNetCore.Mvc;
using Model;

namespace EmetGuard.Controllers
{
    /// <summary>
    /// נקודות קצה לטבלת Users. כל פעולה מפעילה שאילתה אחת מ-UserDB
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly UserDB userDB;

        // UserDB מגיע אוטומטית מהרישום ב-Program.cs
        public UsersController(UserDB userDB)
        {
            this.userDB = userDB;
        }

        // GET api/users
        [HttpGet]
        public async Task<ActionResult<List<User>>> GetAll()
        {
            return Ok(await userDB.GetAllUsersAsync());
        }

        // GET api/users/5
        [HttpGet("{id:int}")]
        public async Task<ActionResult<User>> GetById(int id)
        {
            User? user = await userDB.GetUserByIdAsync(id);
            return user == null ? NotFound() : Ok(user);
        }

        // GET api/users/exists/admin
        [HttpGet("exists/{username}")]
        public async Task<ActionResult<bool>> UsernameExists(string username)
        {
            return Ok(await userDB.UsernameExistsAsync(username));
        }

        // GET api/users/email-exists/admin@example.com
        [HttpGet("email-exists/{email}")]
        public async Task<ActionResult<bool>> EmailExists(string email)
        {
            return Ok(await userDB.EmailExistsAsync(email));
        }

        // GET api/users/count-by-role
        [HttpGet("count-by-role")]
        public async Task<ActionResult<Dictionary<string, int>>> CountByRole()
        {
            return Ok(await userDB.CountUsersByRoleAsync());
        }

        // POST api/users?roleName=Student
        // גוף הבקשה: username, email, password
        [HttpPost]
        public async Task<ActionResult<User>> Add([FromBody] User user, [FromQuery] string roleName = "Student")
        {
            if (await userDB.UsernameExistsAsync(user.Username))
                return Conflict("Username already exists.");

            if (await userDB.EmailExistsAsync(user.Email))
                return Conflict("Email already exists.");

            try
            {
                await userDB.AddUserAsync(user, roleName);
            }
            catch (InvalidOperationException ex)
            {
                // תפקיד שלא קיים בטבלת Roles
                return BadRequest(ex.Message);
            }

            return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
        }

        // POST api/users/login
        // גוף הבקשה: username, password
        [HttpPost("login")]
        public async Task<ActionResult<User>> Login([FromBody] User credentials)
        {
            User? user = await userDB.CheckLoginAsync(credentials.Username, credentials.Password);
            return user == null ? Unauthorized() : Ok(user);
        }

        // PUT api/users/5/role?roleName=Teacher
        [HttpPut("{id:int}/role")]
        public async Task<IActionResult> UpdateRole(int id, [FromQuery] string roleName)
        {
            return await userDB.UpdateUserRoleAsync(id, roleName) ? NoContent() : NotFound();
        }

        // PUT api/users/5/password
        // גוף הבקשה: הסיסמה החדשה כמחרוזת JSON, למשל "NewPass123"
        [HttpPut("{id:int}/password")]
        public async Task<IActionResult> UpdatePassword(int id, [FromBody] string newPassword)
        {
            return await userDB.UpdatePasswordAsync(id, newPassword) ? NoContent() : NotFound();
        }

        // PUT api/users/5/email
        // גוף הבקשה: האימייל החדש כמחרוזת JSON
        [HttpPut("{id:int}/email")]
        public async Task<IActionResult> UpdateEmail(int id, [FromBody] string newEmail)
        {
            if (await userDB.EmailExistsAsync(newEmail))
                return Conflict("Email already exists.");

            return await userDB.UpdateEmailAsync(id, newEmail) ? NoContent() : NotFound();
        }

        // DELETE api/users/5
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            return await userDB.DeleteUserAsync(id) ? NoContent() : NotFound();
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SpaceUser.Interface;
using SpaceUser.Models.User;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace SpaceUser.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AccountController(IEmailSender emailSender, SignInManager<Models.User.SpaceUser> signInManager, UserManager<Models.User.SpaceUser> userManager, IConfiguration configuration) : ControllerBase
    {
        [HttpGet("Users")]
        public async Task<IActionResult> Users()
        {
            return Ok(await userManager.Users.ToListAsync());
        }

        [HttpPost("Login")]
        public async Task<IActionResult> Login([FromBody] Login model)
        {
            Models.User.SpaceUser user = await userManager.FindByEmailAsync(model.Email);
            if (user != null && await userManager.CheckPasswordAsync(user, model.Password))
            {
                IList<string> roles = await userManager.GetRolesAsync(user);
                List<Claim> claims =
                [
                    new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString() ),
                    new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
                    new Claim(JwtRegisteredClaimNames.Sub, user.UserName)
                ];

                foreach (var role in roles)
                    claims.Add(new Claim(ClaimTypes.Role, role));

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["JWT:Key"]));
                var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

                var token = new JwtSecurityToken
                    (
                        configuration["JWT:Issuer"],
                        configuration["JWT:Audience"],
                        claims,
                        expires: DateTime.UtcNow.AddMinutes(120),
                        signingCredentials: credentials
                    );
                return Ok(new JwtSecurityTokenHandler().WriteToken(token));
            }

            return Unauthorized();
        }

        [HttpPost("ForgotPassword")]
        public async Task<IActionResult> ForgotPassword(ForgotPassword model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user != null)
            {
                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var resetLink = Url.Action("ResetPassword", "Account", new { email = model.Email, token }, Request.Scheme);
                await emailSender.SendEmailAsync(model.Email, "Reestablecer Contraseña", $"Por Favor Reestablece tu Contraseña Haciendo Click en Este Enlace: <a href='{resetLink}'>Reestablecer Contraseña</a>");
                return Ok("Por Favor Revisa tu E-mail Para Cambiar la Contraseña.");
            }

            return NotFound("ERROR: No Existe un Usuario Registrado con ese E-mail.");
        }

        [HttpGet("ResetPassword")]
        public IActionResult ResetPassword(string email, string token)
        {
            if (email == null || token == null)
            {
                return BadRequest("ERROR: El E-mail y el Token no Pueden Estar Vacios.");
            }

            _ = new ResetPassword { Email = email, Token = token };

            return Ok("Por Favor Cambia tu Contraseña.");
        }

        [HttpPost("ResetPassword")]
        public async Task<IActionResult> ResetPassword(ResetPassword model)
        {
            var user = await userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                return NotFound("ERROR: No Existe un Usuario Registrado con ese E-mail.");
            }

            var result = await userManager.ResetPasswordAsync(user, model.Token, model.Password);
            if (result.Succeeded)
            {
                return Ok("Contraseña Cambiada Correctamente.");
            }

            return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
        }

        [HttpPost("Register")]
        public async Task<IActionResult> Register(Register model)
        {
            var profileImagePath = await SaveProfileImageAsync(model.ProfileImageFile, model.Email);

            var user = new Models.User.SpaceUser
            {
                Name = model.Name,
                Surname1 = model.Surname1,
                Surname2 = model.Surname2,
                UserName = model.Email,
                Email = model.Email,
                PhoneNumber = model.PhoneNumber,
                ProfileImage = profileImagePath,
                Bday = model.Bday
            };

            try
            {
                IdentityResult result = await userManager.CreateAsync(user, model.Password!);
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Basic");
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmedEmail", "Account", new { userId = user.Id, token }, Request.Scheme);

                    await emailSender.SendEmailAsync(user.Email, "Confirma tu Registro", $"Por Favor Confirma tu Cuenta Haciendo Click en Este Enlace: <a href='{confirmationLink}'>Confirmar Registro</a>");

                    return Ok("Confirma tu Registro.");
                }
            }
            catch (Exception)
            {
                return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
            }

            return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
        }

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null || userId == "" || token == "")
            {
                return BadRequest("ERROR: La Id y el Token no Pueden Estar Vacios.");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound("ERROR: No Existe un Usuario con esa ID.");
            }

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return Ok("Registro Confirmado.");
            }

            return BadRequest("ERROR: El E-mail de Confirmación no Está Registrado, ¿Estás Seguro que no Eliminaste tu Cuenta?.");
        }

        [HttpPost("Update")]
        public async Task<IActionResult> Update(Register model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            user.Name = model.Name;
            user.Surname1 = model.Surname1;
            user.Surname2 = model.Surname2;
            user.Email = model.Email;
            user.PhoneNumber = model.PhoneNumber;
            user.Bday = model.Bday;

            if (model.ProfileImageFile != null)
            {
                user.ProfileImage = await SaveProfileImageAsync(model.ProfileImageFile, model.Email);
            }

            var result = await userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                if (!string.IsNullOrEmpty(model.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordChangeResult = await userManager.ResetPasswordAsync(user, token, model.Password);
                    if (!passwordChangeResult.Succeeded)
                    {
                        foreach (var error in passwordChangeResult.Errors)
                        {
                            ModelState.AddModelError("", error.Description);
                        }

                        return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
                    }
                }

                await Logout();

                return Ok("Datos Actualizados.");
            }

            return BadRequest("ERROR: La Contraseña Tiene que Tener al Menos una Letra Mayúscula, una Minúscula, un Dígito, un Caracer Especial y 8 Caracteres de Longitud.");
        }

        [HttpPost("Logout")]
        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();

            return Ok("Loged Out.");
        }

        [HttpPost("Delete")]
        public async Task<IActionResult> Delete(Delete model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound("ERROR: Ese Usuario no Existe.");
            }

            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await Logout();

                return Ok("Usuario Eliminado.");
            }

            return BadRequest("ERROR: No se Pudo Eliminar el Usuario.");
        }

        private async Task<string> SaveProfileImageAsync(IFormFile? profileImageFile, string email)
        {
            if (profileImageFile is null)
            {
                return "/imgs/default-profile.jpg";
            }

            var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imgs/profile/" + email);
            Directory.CreateDirectory(uploadsFolder);
            var uniqueFileName = profileImageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await profileImageFile.CopyToAsync(fileStream);
            }

            return "/imgs/profile/" + email + "/" + uniqueFileName;
        }
    }
}
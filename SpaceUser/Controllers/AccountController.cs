using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpaceUser.Interface;
using SpaceUser.Models.User;

namespace SpaceUser.Controllers
{
    public class AccountController(IEmailSender emailSender, SignInManager<Models.User.SpaceUser> signInManager, UserManager<Models.User.SpaceUser> userManager) : Controller
    {
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(Login model)
        {
            if (ModelState.IsValid)
            {
                var result = await signInManager.PasswordSignInAsync(model.UserName!, model.Password!, model.RememberMe, false);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Home");
                }
                ModelState.AddModelError("", "Credenciales No Válidas, o Aun No has Activado Tu Cuenta.");
            }
            return View(model);
        }

        public IActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Register(Register model)
        {
            if (ModelState.IsValid)
            {
                string? profileImagePath = null;

                if (model.ProfileImageFile != null)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/imgs/profile/" + model.Email);
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = model.ProfileImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImageFile.CopyToAsync(fileStream);
                    }
                    profileImagePath = "/imgs/profile/" + model.Email + "/" + uniqueFileName;
                }

                profileImagePath ??= "/imgs/default-profile.jpg";

                Models.User.SpaceUser user = new()
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

                var result = await userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, "Basic");
                    var token = await userManager.GenerateEmailConfirmationTokenAsync(user);
                    var confirmationLink = Url.Action("ConfirmedEmail", "Account", new { userId = user.Id, token }, Request.Scheme);

                    await emailSender.SendEmailAsync(user.Email, "Confirma tu Registro", $"Por Favor Confirma tu Cuenta Haciendo Click en Este Enlace: <a href='{confirmationLink}'>Confirmar Registro</a>");

                    return RedirectToAction("Confirm", "Account");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
        }

        public IActionResult Confirm()
        {
            return View();
        }

        public async Task<IActionResult> ConfirmedEmail(string userId, string token)
        {
            if (userId == null || token == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var user = await userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound();
            }

            var result = await userManager.ConfirmEmailAsync(user, token);
            if (result.Succeeded)
            {
                return View();
            }

            return View("Error");
        }


        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        public async Task<IActionResult> Profile(string Id)
        {
            Models.User.SpaceUser profile = await userManager.FindByIdAsync(Id);
            if (profile == null)
            {
                return NotFound();
            }
            return View(profile);
        }
    }
}
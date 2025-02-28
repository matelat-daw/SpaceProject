using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Update.Internal;
using Microsoft.VisualStudio.Web.CodeGenerators.Mvc.Templates.Blazor;
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
        [ValidateAntiForgeryToken]
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Update(Register model)
        {
            if (ModelState.IsValid)
            {
                Models.User.SpaceUser user = await userManager.FindByIdAsync(model.Id);
                if (user == null)
                {
                    return NotFound();
                }

                user.Name = model.Name;
                user.Surname1 = model.Surname1;
                user.Surname2 = model.Surname2;
                user.Email = model.Email;
                user.PhoneNumber = model.PhoneNumber;
                user.Bday = model.Bday;

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
                    user.ProfileImage = "/imgs/profile/" + model.Email + "/" + uniqueFileName;
                }

                var result = await userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    await Logout();
                    return RedirectToAction("Login", "Account");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View("Profile", model);
        }


        public async Task<IActionResult> Logout()
        {
            await signInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }

        public IActionResult Profile()
        {
            return View();
        }

        [HttpGet]
        public async Task<IActionResult> Delete(string Id)
        {
            var user = await userManager.FindByIdAsync(Id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new Models.User.Delete()
            {
                Id = Id
            };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIt(Models.User.Delete model)
        {
            var user = await userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return NotFound();
            }
            var result = await userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                await Logout();
                return RedirectToAction("Login", "Account");
            }
            ModelState.AddModelError("", "Ha Ocurrido un Error al Intentar Eliminar el Perfil.");
            return View("Delete", model);
        }
    }
}
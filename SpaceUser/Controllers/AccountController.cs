using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SpaceUser.Models.User;

namespace SpaceUser.Controllers
{
    public class AccountController(SignInManager<Models.User.SpaceUser> signInManager, UserManager<Models.User.SpaceUser> userManager) : Controller
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
                ModelState.AddModelError("", "Credenciales No Válidas, Inténtalo de Nuevo.");
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
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "imgs/profile");
                    Directory.CreateDirectory(uploadsFolder);
                    var uniqueFileName = Guid.NewGuid().ToString() + "_" + model.ProfileImageFile.FileName;
                    var filePath = Path.Combine(uploadsFolder, uniqueFileName);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await model.ProfileImageFile.CopyToAsync(fileStream);
                    }
                    profileImagePath = "/imgs/profile/" + uniqueFileName;
                }

                Models.User.SpaceUser user = new()
                {
                    Name = model.Name,
                    Surname1 = model.Surname1,
                    Surname2 = model.Surname2,
                    UserName = model.Email,
                    Email = model.Email,
                    PhoneNumber = model.PhoneNumber,
                    ProfileImage = profileImagePath,
                    Bday = model.Bday,
                    Active = model.Active
                };

                var result = await userManager.CreateAsync(user, model.Password!);

                if (result.Succeeded)
                {
                    await signInManager.SignInAsync(user, false);
                    return RedirectToAction("Index", "Home");
                }
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }
            }
            return View(model);
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
using Dtos; // Asegúrate de que esto apunta al espacio de nombres correcto
using DTOs;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Security;
using System.ComponentModel.DataAnnotations;

public class AuthController : Controller
{
    private readonly IJwtTokenGenerator _jwtTokenGenerator;
    private readonly IAuthService _authService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthController(IJwtTokenGenerator jwtTokenGenerator, IAuthService authService, IHttpContextAccessor httpContextAccessor)
    {
        _jwtTokenGenerator = jwtTokenGenerator;
        _authService = authService;
        _httpContextAccessor = httpContextAccessor;
    }

    [HttpGet]
    public IActionResult Register()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterRequest registerDto)
    {
        if (!ModelState.IsValid)
        {
            return View(registerDto);
        }
        if (!new EmailAddressAttribute().IsValid(registerDto.Email))
        {
            ModelState.AddModelError("Email", "El correo electrónico no es válido.");
            return View(registerDto);
        }

        await _authService.RegisterAsync(registerDto);
        return RedirectToAction("Login");
    }

    [HttpGet]
    public IActionResult Login()
    {
        // Verificar si hay un token en la sesión
        if (_httpContextAccessor.HttpContext!.Session.GetString("Token") != null)
        {
            return RedirectToAction("Index", "Products"); // Redirige a Products si la sesión está activa
        }

        return View();
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginRequest loginDto)
    {
        if (!ModelState.IsValid)
        {
            return View(loginDto);
        }
        if (!new EmailAddressAttribute().IsValid(loginDto.Email))
        {
            ModelState.AddModelError("Email", "El correo electrónico no es válido.");
            return View(loginDto);
        }
        try
        {
            var user = await _authService.AuthenticateAsync(loginDto.Email, loginDto.Password);
            var token = _jwtTokenGenerator.GenerateJwtToken(user);

            if (_httpContextAccessor.HttpContext != null)
            {
                _httpContextAccessor.HttpContext.Session.SetString("Token", token);
            }
            else
            {
                ModelState.AddModelError(string.Empty, "No se pudo establecer la sesión.");
                return View(loginDto);
            }

            TempData["Success"] = "Inicio de sesión exitoso.";
            return RedirectToAction("Index", "Products");
        }
        catch (Exception ex)
        {
            ModelState.AddModelError(string.Empty, ex.Message);
            return View(loginDto);
        }
    }


    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        _httpContextAccessor.HttpContext?.Session.Remove("Token");
        return RedirectToAction("Login", "Auth");
    }

}

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Interfaces;
using Repositories;
using Services;
using Security;
using System.Globalization;
using Microsoft.AspNetCore.Localization;
using System.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configurar base de datos
builder.Services.AddDbContext<ApplicationDbContext>(options =>
        options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

//builder.Services.AddDbContext<ApplicationDbContext>(options =>
//    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Configurar localización (idioma por defecto: Español - México)
builder.Services.Configure<RequestLocalizationOptions>(options =>
{
    var supportedCultures = new[] { new CultureInfo("es-MX") };
    options.DefaultRequestCulture = new RequestCulture("es-MX");
    options.SupportedCultures = supportedCultures;
    options.SupportedUICultures = supportedCultures;
});

// Agregar soporte para controladores y vistas
builder.Services.AddControllersWithViews();

// Configurar sesiones
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Duración de la sesión
    options.Cookie.HttpOnly = true; // Mayor seguridad
    options.Cookie.IsEssential = true; // Necesario para el funcionamiento de la sesión
});

// Registrar IHttpContextAccessor para permitir el acceso a HttpContext en servicios y controladores
builder.Services.AddHttpContextAccessor();

// Configurar JWT
var jwtSettings = builder.Configuration.GetSection("JwtSettings");

string? secretKey = jwtSettings["SecretKey"];
string? issuer = jwtSettings["Issuer"];
string? audience = jwtSettings["Audience"];
string? expirationMinutes = jwtSettings["ExpirationMinutes"];

// Validar valores de JWT
if (string.IsNullOrEmpty(secretKey) || secretKey.Length < 32)
    throw new ArgumentException("JwtSettings:SecretKey es inválido o demasiado corto (mínimo 32 caracteres).");

if (string.IsNullOrEmpty(issuer))
    throw new ArgumentException("JwtSettings:Issuer no puede ser nulo o vacío.");

if (string.IsNullOrEmpty(audience))
    throw new ArgumentException("JwtSettings:Audience no puede ser nulo o vacío.");

if (!int.TryParse(expirationMinutes, out int expMinutes) || expMinutes <= 0)
    throw new ArgumentException("JwtSettings:ExpirationMinutes debe ser un número entero positivo.");

// Convertir la clave a bytes
var keyBytes = Encoding.UTF8.GetBytes(secretKey);

// Configurar autenticación JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = issuer,
        ValidAudience = audience,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes)
    };
});

// Configurar CORS (si la aplicación tiene un frontend separado)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.WithOrigins("http://localhost:4200") // Cambia esto si el frontend está en otro dominio
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Agregar servicios y repositorios
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();

// Registrar el generador de tokens con valores ya validados
builder.Services.AddScoped<IJwtTokenGenerator>(_ => new JwtTokenGenerator(
    secretKey!,
    issuer!,
    audience!,
    expMinutes
));

var app = builder.Build();

// Configurar manejo global de errores en producción
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseCors("AllowAll"); // Habilitar CORS

app.UseSession(); // Habilitar sesiones

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Auth}/{action=Login}/{id?}"
);

app.Run();

using DTOs;
using Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Models;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

public class ProductsController : Controller
{
    private readonly IProductService _productService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ProductsController(IProductService productService, IHttpContextAccessor httpContextAccessor)
    {
        _productService = productService;
        _httpContextAccessor = httpContextAccessor;
    }

    private bool UserIsAuthenticated()
    {
        var token = _httpContextAccessor.HttpContext!.Session.GetString("Token");
        return !string.IsNullOrEmpty(token);
    }

    [HttpGet]
    public async Task<IActionResult> Index(int pageNumber = 1, int pageSize = 5)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            var paginatedProducts = await _productService.GetPaginatedProductsAsync(pageNumber, pageSize);
            return View(paginatedProducts);
        }
        catch (Exception ex)
        {
            // Manejo de la excepción
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }

    [HttpGet]
    public IActionResult Create()
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Product product, IFormFile imageFile)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            if (imageFile != null && imageFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot/images");

                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileExtension = Path.GetExtension(imageFile.FileName);
                var fileName = $"{Path.GetFileNameWithoutExtension(imageFile.FileName)}_{Guid.NewGuid()}{fileExtension}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                product.ImageUrl = $"/images/{fileName}";
            }

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                foreach (var error in errors)
                {
                    Console.WriteLine(error);
                }
                return View(product);
            }

            await _productService.AddProductAsync(product);
            TempData["Success"] = "Producto creado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al crear el producto.");
            return View(product);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, Product product)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            if (id != product.Id) return BadRequest();

            if (!ModelState.IsValid) return View(product);

            await _productService.UpdateProductAsync(product);
            TempData["Success"] = "Producto actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            ModelState.AddModelError(string.Empty, "Ocurrió un error al actualizar el producto.");
            return View(product);
        }
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            return View(product);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            return View("Error");
        }
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, int quantity)
    {
        if (!UserIsAuthenticated()) return RedirectToAction("Login", "Auth");

        try
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null) return NotFound();

            if (quantity <= 0 || quantity > product.Stock)
            {
                TempData["Error"] = "Cantidad inválida.";
                return RedirectToAction(nameof(Index));
            }

            // Restar del stock en lugar de eliminar el producto
            product.Stock -= quantity;

            if (product.Stock == 0)
            {
                await _productService.DeleteProductAsync(id); // Si el stock llega a 0, se elimina el producto
            }
            else
            {
                await _productService.UpdateProductAsync(product); // Solo actualiza el stock
            }

            TempData["Success"] = $"Se eliminaron {quantity} unidades de {product.Name}.";
            return RedirectToAction(nameof(Index));
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            TempData["Error"] = "Ocurrió un error al eliminar el producto.";
            return RedirectToAction(nameof(Index));
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using SupermarketApp.Data;
using SupermarketApp.Models;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using Microsoft.AspNetCore.Localization;

namespace SupermarketApp.Controllers
{
    [Authorize]
    public class ProductsController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly List<string> _categories = new() { "Жемістер/Фрукты", "Көкөністер/Овощи", "Тәттілер/Сладости", "Ұн өнімдері/Мучное", "Сүт өнімдері/Молоко" };

        public ProductsController(ApplicationDbContext context) => _context = context;

        public async Task<IActionResult> Index(string category)
        {
            ViewBag.Categories = _categories;
            ViewBag.SelectedCategory = category;

            var products = _context.Products.AsQueryable();

            if (!string.IsNullOrEmpty(category))
            {
                products = products.Where(p => p.Category == category);
            }

            return View(await products.ToListAsync());
        }

        [Authorize(Roles = "Admin")]
        public IActionResult Create() 
        { 
            ViewBag.Categories = _categories; 
            return View(); 
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Create(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Add(product);
                await _context.SaveChangesAsync();
                return File(GenerateInvoicePdf(product), "application/pdf", $"Invoice_{product.Name}.pdf");
            }
            ViewBag.Categories = _categories;
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product == null) return NotFound();
            ViewBag.Categories = _categories;
            return View(product);
        }

        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Edit(Product product)
        {
            if (ModelState.IsValid)
            {
                _context.Update(product);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            ViewBag.Categories = _categories;
            return View(product);
        }

        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Delete(int id)
        {
            var product = await _context.Products.FindAsync(id);
            if (product != null) 
            { 
                _context.Products.Remove(product); 
                await _context.SaveChangesAsync(); 
            }
            return RedirectToAction(nameof(Index));
        }

        public IActionResult SetLanguage(string culture, string returnUrl)
        {
            Response.Cookies.Append(
                CookieRequestCultureProvider.DefaultCookieName, 
                CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)), 
                new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
            );
            return LocalRedirect(returnUrl);
        }

        private byte[] GenerateInvoicePdf(Product product)
        {
            return Document.Create(container => {
                container.Page(page => {
                    page.Margin(50);
                    page.Header().Text("ЖЕТКІЗУ ПАРАҒЫ / ЛИСТ ПОСТАВКИ").FontSize(20).SemiBold();
                    page.Content().PaddingVertical(20).Table(table => {
                        table.ColumnsDefinition(c => { 
                            c.RelativeColumn(2); 
                            c.RelativeColumn(2); 
                            c.RelativeColumn(1); 
                        });
                        table.Header(h => { 
                            h.Cell().BorderBottom(1).Text("Тауар / Товар"); 
                            h.Cell().BorderBottom(1).Text("Санаты / Категория"); 
                            h.Cell().BorderBottom(1).Text("Саны / Кол-во"); 
                        });
                        table.Cell().Padding(5).Text(product.Name); 
                        table.Cell().Padding(5).Text(product.Category); 
                        table.Cell().Padding(5).Text(product.Stock.ToString());
                    });
                    page.Footer().AlignCenter().Text(DateTime.Now.ToString("dd.MM.yyyy HH:mm"));
                });
            }).GeneratePdf();
        }
    }
}
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Quote.Application.Common.Interfaces;

namespace Quote.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class TradeCategoriesController : ControllerBase
{
    private readonly IApplicationDbContext _context;

    public TradeCategoriesController(IApplicationDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCategories()
    {
        var categories = await _context.TradeCategories
            .Where(c => c.IsActive)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.Description,
                c.Icon
            })
            .ToListAsync();

        return Ok(categories);
    }
}

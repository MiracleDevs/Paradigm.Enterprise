using InventoryCrud.Domain.Inventory.Entities;
using InventoryCrud.Providers.Inventory;
using Microsoft.AspNetCore.Mvc;

namespace InventoryCrud.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ProductsController : ControllerBase
{
    #region Fields

    private readonly IProductProvider _productProvider;

    #endregion

    #region Constructors

    public ProductsController(IProductProvider productProvider)
    {
        _productProvider = productProvider;
    }

    #endregion

    #region Public Methods

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductView>>> GetAll()
    {
        var products = await _productProvider.GetAllAsync();
        return Ok(products);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ProductView>> GetById(int id)
    {
        var product = await _productProvider.GetByIdAsync(id);
        
        if (product == null)
            return NotFound();
        
        return Ok(product);
    }

    [HttpGet("category/{category}")]
    public async Task<ActionResult<IEnumerable<ProductView>>> GetByCategory(string category)
    {
        var products = await _productProvider.GetByCategoryAsync(category);
        return Ok(products);
    }

    [HttpGet("available")]
    public async Task<ActionResult<IEnumerable<ProductView>>> GetAvailable()
    {
        var products = await _productProvider.GetAvailableProductsAsync();
        return Ok(products);
    }

    [HttpPost]
    public async Task<ActionResult<ProductView>> Create([FromBody]ProductView product)
    {
        var result = await _productProvider.AddAsync(product);

        if (result is null)
            return BadRequest();

        return Ok(result);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update([FromBody]ProductView product)
    {
        var result = await _productProvider.UpdateAsync(product);
        
        if (result is null)
            return NotFound();
       
        return NoContent();
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _productProvider.DeleteAsync(id);
        return NoContent();
    }

    #endregion
}

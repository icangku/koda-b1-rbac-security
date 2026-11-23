using EfCore.Application.Dtos;
using EfCore.Application.Security;
using EfCore.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EfCore.Controllers;

[ApiController]
[Route("api/v1/products")]
[Authorize(Policy = AppPolicies.CanManageProduct)]
public sealed class ProductController : ControllerBase
{
	private readonly IProductService _service;

	public ProductController(IProductService service)
	{
			_service = service;
	}

	// GET /api/v1/products
	[AllowAnonymous]
	[HttpGet]
	[ProducesResponseType(typeof(IReadOnlyList<ProductResponse>),StatusCodes.Status200OK)]
	public async Task<ActionResult<IReadOnlyList<ProductResponse>>> GetAll([FromQuery] int page = 1, [FromQuery] int size = 10, CancellationToken cancellationToken = default)
	{
		try
		{
			return Ok(await _service.GetAllAsync(page, size, cancellationToken));
		}
		catch(ArgumentException e)
		{
			return BadRequest(
				new ProblemDetails{
					Title = "Invalid Pagination",
					Detail = e.Message,
					Status = 400
				});
		}
	}

	[AllowAnonymous]
	[HttpGet("search")]
	[ProducesResponseType(typeof(PageResult<ProductSearchItemResponse>), StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(PageResult<ProductSearchItemResponse>), StatusCodes.Status404NotFound)]
	[ProducesResponseType(typeof(PageResult<ProductSearchItemResponse>), StatusCodes.Status400BadRequest)]
	public async Task<ActionResult<PageResult<ProductSearchItemResponse>>> Search([FromQuery] ProductSearchRequest  request, CancellationToken ct)
	{
		var response = await _service.Search(request, ct);

		return Ok(response);
	}

	// GET /api/v1/products/PR001
	[AllowAnonymous]
	[HttpGet("{productCode}")]
	[ProducesResponseType(typeof(ProductResponse),StatusCodes.Status200OK)][ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ProductResponse>> GetByCode([FromRoute] string productCode,CancellationToken cancellationToken)
	{
		var product = await _service.GetByCodeAsync(
			productCode,
			cancellationToken);

		if (product is null)
		{
			return Problem(
				statusCode: StatusCodes.Status404NotFound,
				title: "Product not found");
		}

		return Ok(product);
	}

	// POST /api/v1/products
	[HttpPost]
	[ProducesResponseType(typeof(ProductResponse),StatusCodes.Status201Created)]
	[ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status409Conflict)]
	public async Task<ActionResult<ProductResponse>> Create([FromBody] ProductRequest request,CancellationToken cancellationToken)
	{
		try
		{
			var created = await _service.CreateAsync(request, cancellationToken);

			if (created is null)
			{
				return Problem(
					statusCode: StatusCodes.Status409Conflict,
					title: "Product code already exists");
			}

			return CreatedAtAction(
					nameof(GetByCode),
					new { productCode = created.Code },
					created);
		}
		catch (ArgumentException ex)
		{
			return Problem(
				statusCode: StatusCodes.Status400BadRequest,
				title: "Invalid product data",
				detail: ex.Message);
		}
	}

	// PUT /api/v1/products/PR001
	[HttpPut("{productCode}")]
	[ProducesResponseType(typeof(ProductResponse),StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ProductResponse>> Update([FromRoute] string productCode,[FromBody] UpdateProductRequest request,CancellationToken cancellationToken)
	{
		try
		{
			var updated = await _service.UpdateAsync(
				productCode,
				request,
				cancellationToken);

			if (updated is null)
			{
				return Problem(
					statusCode: StatusCodes.Status404NotFound,
					title: "Product not found");
			}

			return Ok(updated);
		}
		catch (ArgumentException ex)
		{
			return Problem(
				statusCode: StatusCodes.Status400BadRequest,
				title: "Invalid product data",
				detail: ex.Message);
		}
	}

	// DELETE /api/v1/products/PR001
	[HttpDelete("{productCode}")]
	[ProducesResponseType(StatusCodes.Status204NoContent)]
	[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
	public async Task<IActionResult> Delete([FromRoute] string productCode, CancellationToken cancellationToken)
	{
		var deleted = await _service.DeleteAsync(
			productCode,
			cancellationToken);

		if (!deleted)
		{
			return Problem(
				statusCode: StatusCodes.Status404NotFound,
				title: "Product not found");
		}

		return NoContent();
	}

	// GET /api/v1/products/active/PR001
	[HttpGet("active/{productCode}")]
	[ProducesResponseType(typeof(ProductResponse),StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ProductResponse>> Active([FromRoute] string productCode,CancellationToken cancellationToken)
	{
		try
		{
			var updated = await _service.ActivateAsync(
				productCode,
				cancellationToken);

			if (!updated)
			{
				return Problem(
					statusCode: StatusCodes.Status404NotFound,
					title: "Product not found");
			}

			return Ok(updated);
		}
		catch (ArgumentException ex)
		{
			return Problem(
				statusCode: StatusCodes.Status400BadRequest,
				title: "Invalid product data",
				detail: ex.Message);
		}
	}


	// GET /api/v1/products/active/PR001
	[HttpGet("deactive/{productCode}")]
	[ProducesResponseType(typeof(ProductResponse),StatusCodes.Status200OK)]
	[ProducesResponseType(typeof(ValidationProblemDetails),StatusCodes.Status400BadRequest)]
	[ProducesResponseType(typeof(ProblemDetails),StatusCodes.Status404NotFound)]
	public async Task<ActionResult<ProductResponse>> Deactive([FromRoute] string productCode,CancellationToken cancellationToken)
	{
		try
		{
			var updated = await _service.DeactivateAsync(
				productCode,
				cancellationToken);

			if (!updated)
			{
				return Problem(
					statusCode: StatusCodes.Status404NotFound,
					title: "Product not found");
			}

			return Ok(updated);
		}
		catch (ArgumentException ex)
		{
			return Problem(
				statusCode: StatusCodes.Status400BadRequest,
				title: "Invalid product data",
				detail: ex.Message);
		}
	}

}

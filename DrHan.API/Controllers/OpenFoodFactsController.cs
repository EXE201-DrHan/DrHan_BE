using DrHan.Infrastructure.Services.OpenFoodService;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace DrHan.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OpenFoodFactsController : ControllerBase
    {
        private readonly OpenFoodFactsService _openFoodFactsService;

        public OpenFoodFactsController(OpenFoodFactsService openFoodFactsService)
        {
            _openFoodFactsService = openFoodFactsService;
        }

        [HttpPost("import")]
        public async Task<IActionResult> ImportData(IFormFile file, CancellationToken cancellationToken)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file was uploaded");
                }

                // Create a temporary file
                var tempPath = Path.GetTempFileName();

                try
                {
                    using (var stream = new FileStream(tempPath, FileMode.Create))
                    {
                        await file.CopyToAsync(stream, cancellationToken);
                    }

                    var importedCount = await _openFoodFactsService.ImportProductsFromCsvAsync(tempPath, cancellationToken);

                    return Ok(new { message = $"Successfully imported {importedCount} products" });
                }
                finally
                {
                    // Clean up the temporary file
                    if (System.IO.File.Exists(tempPath))
                    {
                        System.IO.File.Delete(tempPath);
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred during import: {ex.Message}");
            }
        }
    }
} 
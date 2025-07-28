using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Collections.Generic;
using System;
using System.Text.Json;

namespace StrongHelpOfficial.Controllers
{
    public class BenefitsAssistantController : Controller
    {
        private readonly ILogger<BenefitsAssistantController> _logger;
        private readonly IMemoryCache _memoryCache;
        private const string RequiredDocumentsCacheKey = "RequiredDocuments";

        public BenefitsAssistantController(
            ILogger<BenefitsAssistantController> logger,
            IMemoryCache memoryCache)
        {
            _logger = logger;
            _memoryCache = memoryCache;
        }

        public IActionResult LoanInformation()
        {
            // Get documents from cache or use defaults
            if (!_memoryCache.TryGetValue(RequiredDocumentsCacheKey, out List<string> documents))
            {
                // Default documents if not in cache
                documents = new List<string>
                {
                    "Latest 2 months payslip",
                    "Certificate of Employment",
                    "Valid government-issued ID"
                };

                // Store defaults in cache
                var cacheOptions = new MemoryCacheEntryOptions()
                    .SetPriority(CacheItemPriority.Normal)
                    .SetSlidingExpiration(TimeSpan.FromDays(30));

                _memoryCache.Set(RequiredDocumentsCacheKey, documents, cacheOptions);
            }

            ViewData["RequiredDocuments"] = documents;
            return View("BenefitsAssistantLoanInformation");
        }

        [HttpPost]
        public IActionResult UpdateLoanInformation([FromBody] LoanInformationUpdateModel model)
        {
            try
            {
                if (model.RequiredDocuments != null && model.RequiredDocuments.Any())
                {
                    // Update the cache with new document list
                    var cacheOptions = new MemoryCacheEntryOptions()
                        .SetPriority(CacheItemPriority.Normal)
                        .SetSlidingExpiration(TimeSpan.FromDays(30));

                    _memoryCache.Set(RequiredDocumentsCacheKey, model.RequiredDocuments, cacheOptions);
                    _logger.LogInformation("Loan information updated by user");

                    return Json(new { success = true, message = "Loan information updated successfully" });
                }

                return Json(new { success = false, message = "No documents provided" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating loan information");
                return Json(new { success = false, message = $"Failed to update loan information: {ex.Message}" });
            }
        }
    }

    public class LoanInformationUpdateModel
    {
        public List<string> RequiredDocuments { get; set; } = new List<string>();
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;

namespace StrongHelpOfficial.Controllers.Loaner
{
    [Area("Loaner")]
    public class DocumentController : Controller
    {
        private readonly IConfiguration _config;

        public DocumentController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public async Task<IActionResult> Preview(int id)
        {
            byte[] fileBytes = null;
            string fileName = null;

            using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
            {
                await conn.OpenAsync();
                var cmd = new SqlCommand(@"
            SELECT FileContent, LoanDocumentName
            FROM LoanDocument
            WHERE LoanDocumentID = @Id", conn);
                cmd.Parameters.AddWithValue("@Id", id);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        fileBytes = reader["FileContent"] as byte[];
                        fileName = reader["LoanDocumentName"] as string;
                    }
                }
            }

            if (fileBytes == null)
                return NotFound();

            string contentType = "application/octet-stream";
            if (fileName != null)
            {
                var ext = Path.GetExtension(fileName).ToLowerInvariant();
                if (ext == ".pdf") contentType = "application/pdf";
                else if (ext == ".jpg" || ext == ".jpeg") contentType = "image/jpeg";
                else if (ext == ".png") contentType = "image/png";
            }

            // Set Content-Disposition to inline for browser display
            Response.Headers["Content-Disposition"] = $"inline; filename=\"{fileName}\"";
            return File(fileBytes, contentType);
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using System.Data;
using System.Text.Json;

[Route("api/[controller]")]
[ApiController]
public class NotificationsController : ControllerBase
{
    private readonly IConfiguration _config;
    public NotificationsController(IConfiguration config) { _config = config; }

    // GET: api/Notifications/CoMakerRequests
    [HttpGet("CoMakerRequests")]
    public IActionResult GetCoMakerRequests()
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        if (userId == null) return Unauthorized();

        var results = new List<CoMakerRequestDto>();
        using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            var cmd = new SqlCommand(@"
                SELECT la.LoanID, la.UserID, u.FirstName, u.LastName
                FROM LoanApplication la
                JOIN [User] u ON la.UserID = u.UserID
                WHERE la.ApplicationStatus = 'Drafted' AND la.ComakerUserId = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId.Value);
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    results.Add(new CoMakerRequestDto
                    {
                        LoanId = reader.GetInt32(0),
                        ApplicantUserId = reader.GetInt32(1),
                        ApplicantName = reader.GetString(2) + " " + reader.GetString(3)
                    });
                }
            }
        }
        var json = JsonSerializer.Serialize(results, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        return Content(json, "application/json");
    }

    // POST: api/Notifications/CoMakerAction
    [HttpPost("CoMakerAction")]
    public IActionResult CoMakerAction([FromBody] CoMakerActionDto dto)
    {
        var userId = HttpContext.Session.GetInt32("UserID");
        if (userId == null) 
        {
            // Log: "UserID missing from session"
            return Unauthorized();
        }

        using (var conn = new SqlConnection(_config.GetConnectionString("DefaultConnection")))
        {
            conn.Open();
            var cmd = new SqlCommand(@"
                UPDATE LoanApplication
                SET ApplicationStatus = @Status, Remarks = @Remarks
                WHERE ComakerUserId = @UserID", conn);
            cmd.Parameters.AddWithValue("@UserID", userId.Value);

            if (dto.Action == "Approve")
            {
                cmd.Parameters.AddWithValue("@Status", "Submitted");
                cmd.Parameters.AddWithValue("@Remarks", DBNull.Value);
            }
            else if (dto.Action == "Reject")
            {
                cmd.Parameters.AddWithValue("@Status", "Rejected");
                cmd.Parameters.AddWithValue("@Remarks", "Rejected by Co-maker");
            }
            else
            {
                return BadRequest();
            }

            int affected = cmd.ExecuteNonQuery();
            if (affected == 0) 
            {
                // Log: $"No LoanApplication found for LoanID={dto.LoanID} and ComakerUserId={userId.Value}"
                return NotFound();
            }
            return affected > 0 ? Ok() : NotFound();
        }
    }
}

public class CoMakerActionDto
{
    public int LoanID { get; set; }
    public string Action { get; set; } // "Approve" or "Reject"
}

public class CoMakerRequestDto
{
    public int LoanId { get; set; }
    public int ApplicantUserId { get; set; }
    public string ApplicantName { get; set; }
}
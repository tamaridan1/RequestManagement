using Microsoft.AspNetCore.Mvc;
using RequestManagementBL;
using RequestManagementDTO;

namespace RequestManagementAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RequestsController : ControllerBase
    {
        private readonly IRequestService _service;
        private readonly IWebHostEnvironment _environment;

        public RequestsController(IRequestService service, IWebHostEnvironment environment)
        {
            _service = service;
            _environment = environment;
        }

        [HttpPost("reseed")]
        public async Task<IActionResult> Reseed(CancellationToken cancellationToken)
        {
            if (!_environment.IsDevelopment())
                return Forbid();

            await _service.ReseedAsync(cancellationToken);
            return Ok(new { message = "נתוני הבדיקה נוצרו מחדש בהצלחה (100,000 רשומות)." });
        }

        [HttpGet]
        public async Task<ActionResult<PagedResult<RequestDTO>>> GetRequests([FromQuery] RequestQueryParams queryParams,CancellationToken cancellationToken)
        {
            var result = await _service.GetRequestsAsync(queryParams, cancellationToken);
            return Ok(result);
        }

        [HttpGet("summary")]
        public async Task<ActionResult<RequestSummaryDTO>> GetSummary(CancellationToken cancellationToken)
        {
            var summary = await _service.GetSummaryAsync(cancellationToken);
            return Ok(summary);
        }

        [HttpPut("{id:int}/status")]
        public async Task<ActionResult<RequestDTO>> UpdateStatus(
            int id,
            [FromBody] UpdateStatusDTO dto,
            CancellationToken cancellationToken)
        {
            var updated = await _service.UpdateStatusAsync(id, dto, cancellationToken);
            return Ok(updated);
        }

        [HttpGet("{id:int}/history")]
        public async Task<ActionResult<List<StatusHistoryDTO>>> GetHistory(
            int id,
            CancellationToken cancellationToken)
        {
            var history = await _service.GetHistoryAsync(id, cancellationToken);
            return Ok(history);
        }

        [HttpPost("bulk-status")]
        public async Task<ActionResult<BulkUpdateResultDTO>> BulkUpdateStatus(
            [FromBody] BulkUpdateStatusDTO dto,
            CancellationToken cancellationToken)
        {
            var result = await _service.BulkUpdateStatusAsync(dto, cancellationToken);
            return Ok(result);
        }
    }
}

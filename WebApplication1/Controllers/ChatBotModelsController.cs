using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Service.Interface;
using WebApplication1.DTO.ChatbotModel;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatBotModelsController : ControllerBase
{
    private readonly IChatbotModelsService _chatbotService;
    private readonly ILogger<ChatBotModelsController> _logger;

    public ChatBotModelsController(IChatbotModelsService chatbotService, ILogger<ChatBotModelsController> logger)
    {
        _chatbotService = chatbotService;
        _logger = logger;
    }

    #region CRUD Operations

/* <<<<<<<<<<<<<<  ✨ Windsurf Command ⭐ >>>>>>>>>>>>>>>> */
    /// <summary>
    /// Get all AI models from the database.
    /// </summary>
    /// <returns>A JSON object containing the list of AI models, or an error message.</returns>
    /// <example>
    /// {
    ///     "success": true,
    ///     "data": [...],
    ///     "message": "All models retrieved successfully"
    /// }
    /// </example>
/* <<<<<<<<<<  175822bc-3ef1-4488-aeea-73bbb39c166f  >>>>>>>>>>> */
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllModels()
    {
        try
        {
            var result = await _chatbotService.GetAllAsync();
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAllModels");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetModelById(Guid id)
    {
        try
        {
            var result = await _chatbotService.GetByIdAsync(id);
            
            if (result.IsSuccess)
                return Ok(new { success = true, data = result.Data, message = result.Message });
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetModelById for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPost]
    [Authorize]
    public async Task<IActionResult> CreateModel([FromBody] ChatbotModelCreateDTO modelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
            }

            var result = await _chatbotService.CreateAsync(modelDto);
            
            if (result.IsSuccess && result.Data != null)
            {
                return CreatedAtAction(nameof(GetModelById), new { id = result.Data.Id }, 
                    new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in CreateModel");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateModel(Guid id, [FromBody] ChatbotModelUpdateDTO modelDto)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
            }

            if (id != modelDto.Id)
            {
                return BadRequest(new { success = false, message = "ID mismatch" });
            }

            var result = await _chatbotService.UpdateAsync(id, modelDto);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in UpdateModel for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpDelete("{id}")]
    [Authorize]
    public async Task<IActionResult> DeleteModel(Guid id)
    {
        try
        {
            var result = await _chatbotService.DeleteAsync(id);
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, message = result.Message });
            }
            
            return NotFound(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DeleteModel for ID {Id}", id);
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    #endregion

    #region Additional Operations

    [HttpGet("available")]
    public async Task<IActionResult> GetAvailableModels()
    {
        try
        {
            var result = await _chatbotService.GetAllAsync();
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetAvailableModels");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    [HttpGet("paid-user-models")]
    public async Task<IActionResult> GetPaidUserModels()
    {
        try
        {
            var result = await _chatbotService.GetPaidChatbotModel();
            
            if (result.IsSuccess)
            {
                return Ok(new { success = true, data = result.Data, message = result.Message });
            }
            
            return BadRequest(new { success = false, message = result.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in GetPaidUserModels");
            return StatusCode(500, new { success = false, message = "Internal server error" });
        }
    }

    #endregion
}
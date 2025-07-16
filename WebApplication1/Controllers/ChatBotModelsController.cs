using Microsoft.AspNetCore.Mvc;
using WebApplication1.Models;
using WebApplication1.Service.Interface;
using WebApplication1.DTO.ChatbotModel;

namespace WebApplication1.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ChatBotModelsController : ControllerBase
{
    private readonly IChatbotModelsService _chatbotService;

    public ChatBotModelsController(IChatbotModelsService chatbotService)
    {
        _chatbotService = chatbotService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var result = await _chatbotService.GetAllAsync();
        
        if (result.IsSuccess)
            return Ok(new { success = true, message = result.Message, data = result.Data });
        
        return BadRequest(new { success = false, message = result.Message });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id)
    {
        var result = await _chatbotService.GetByIdAsync(id);
        
        if (result.IsSuccess)
            return Ok(new { success = true, message = result.Message, data = result.Data });
        
        return NotFound(new { success = false, message = result.Message });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ChatbotModelCreateDTO model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid model data", errors = ModelState });

        var result = await _chatbotService.AddWithApiKeysAsync(model);
        
        if (result.IsSuccess)
            return CreatedAtAction(nameof(GetById), new { id = result.Data.Id }, 
                new { success = true, message = result.Message, data = result.Data });
        
        return BadRequest(new { success = false, message = result.Message });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(int id, [FromBody] ChatbotModelUpdateDTO model)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { success = false, message = "Invalid model data", errors = ModelState });

        if (id != model.Id)
            return BadRequest(new { success = false, message = "ID mismatch" });

        var result = await _chatbotService.UpdateWithApiKeysAsync(model);
        
        if (result.IsSuccess)
            return Ok(new { success = true, message = result.Message, data = result.Data });
        
        return BadRequest(new { success = false, message = result.Message });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _chatbotService.DeleteAsync(id);
        
        if (result.IsSuccess)
            return Ok(new { success = true, message = result.Message });
        
        return NotFound(new { success = false, message = result.Message });
    }
}
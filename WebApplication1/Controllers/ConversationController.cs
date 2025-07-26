using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.DTO.Conversation;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ConversationController : ControllerBase
    {
        private readonly IConversationService _conversationService;

        public ConversationController(IConversationService conversationService)
        {
            _conversationService = conversationService;
        }

        // GET api/conversation
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var result = await _conversationService.GetAllAsync();
            if (result.IsSuccess)
                return Ok(new { success = true, data = result.Data, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        // GET api/conversation/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(Guid id)
        {
            var result = await _conversationService.GetByIdAsync(id);
            if (result.IsSuccess)
                return Ok(new { success = true, data = result.Data, message = result.Message });
            return NotFound(new { success = false, message = result.Message });
        }

        // POST api/conversation
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] ConversationCreateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            var result = await _conversationService.CreateAsync(dto);
            if (result.IsSuccess)
                return CreatedAtAction(nameof(GetById), new { id = result.Data.ConversationId }, new { success = true, data = result.Data, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        // PUT api/conversation/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(Guid id, [FromBody] ConversationUpdateDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { success = false, message = "Invalid data", errors = ModelState });

            if (id != dto.ConversationId)
                return BadRequest(new { success = false, message = "ID mismatch" });

            var result = await _conversationService.UpdateAsync(id, dto);
            if (result.IsSuccess)
                return Ok(new { success = true, data = result.Data, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        [HttpDelete("thanos-delete/{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var result = await _conversationService.DeleteAsync(id);
            if (result.IsSuccess)
                return Ok(new { success = true, message = result.Message });
            return NotFound(new { success = false, message = result.Message });
        }

        #region Soft Delete Endpoints

        // DELETE api/conversation/{id}/soft
        [HttpDelete("{id}/soft")]
        public async Task<IActionResult> SoftDelete(Guid id, [FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { success = false, message = "UserId is required" });

            var result = await _conversationService.SoftDeleteConversationAsync(id, userId);
            if (result.IsSuccess)
                return Ok(new { success = true, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        // POST api/conversation/{id}/restore
        [HttpPost("{id}/restore")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Restore(Guid id, [FromQuery] string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return BadRequest(new { success = false, message = "UserId is required" });

            var result = await _conversationService.RestoreConversationAsync(id, userId);
            if (result.IsSuccess)
                return Ok(new { success = true, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        // GET api/conversation/deleted/{userId}
        [HttpGet("deleted/{userId}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> GetDeleted(string userId)
        {
            var result = await _conversationService.GetDeletedConversationsAsync(userId);
            if (result.IsSuccess)
                return Ok(new { success = true, data = result.Data, message = result.Message });
            return BadRequest(new { success = false, message = result.Message });
        }

        #endregion
    }
}
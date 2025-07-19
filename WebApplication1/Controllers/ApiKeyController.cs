using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTO.ApiKey;
using WebApplication1.Service.Interface;

namespace WebApplication1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ApiKeyController : ControllerBase
    {
        private readonly IApiKeyService _apiKeyService;
        private readonly ILogger<ApiKeyController> _logger;

        public ApiKeyController(IApiKeyService apiKeyService, ILogger<ApiKeyController> logger)
        {
            _apiKeyService = apiKeyService;
            _logger = logger;
        }

        #region CRUD Operations

        [HttpGet]
        public async Task<IActionResult> GetAllApiKeys()
        {
            try
            {
                var result = await _apiKeyService.GetAllAsync();
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAllApiKeys");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetApiKeyById(Guid id)
        {
            try
            {
                var result = await _apiKeyService.GetByIdAsync(id);
                
                if (result.IsSuccess)
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                
                return NotFound(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApiKeyById for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateApiKey([FromBody] ApiKeyCreateDTO apiKeyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var result = await _apiKeyService.CreateAsync(apiKeyDto);
                
                if (result.IsSuccess && result.Data != null)
                {
                    return CreatedAtAction(nameof(GetApiKeyById), new { id = result.Data.ApiKeyId }, 
                        new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApiKey");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateApiKey(Guid id, [FromBody] ApiKeyUpdateDTO apiKeyDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                if (id != apiKeyDto.ApiKeyId)
                {
                    return BadRequest(new { success = false, message = "ID mismatch" });
                }

                var result = await _apiKeyService.UpdateAsync(id, apiKeyDto);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApiKey for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteApiKey(Guid id)
        {
            try
            {
                var result = await _apiKeyService.DeleteAsync(id);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return NotFound(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteApiKey for ID {Id}", id);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region Model-Specific Operations

        [HttpGet("model/{modelId}")]
        public async Task<IActionResult> GetApiKeysByModelId(Guid modelId)
        {
            try
            {
                var result = await _apiKeyService.GetApiKeysByModelIdAsync(modelId);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApiKeysByModelId for ModelId {ModelId}", modelId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("model/{modelId}/create")]
        public async Task<IActionResult> CreateApiKeysForModel(Guid modelId, [FromBody] List<ApiKeyCreateDTO> apiKeyDtos)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var result = await _apiKeyService.CreateApiKeysAsync(modelId, apiKeyDtos);
                
                if (result.IsSuccess)
                {
                    return CreatedAtAction(nameof(GetApiKeysByModelId), new { modelId }, 
                        new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApiKeysForModel for ModelId {ModelId}", modelId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPost("model/{modelId}/bulk-create")]
        public async Task<IActionResult> CreateApiKeysForModelBulk(Guid modelId, [FromBody] BulkApiKeyCreateDTO bulkCreateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                if (modelId != bulkCreateDto.ChatbotModelId)
                {
                    return BadRequest(new { success = false, message = "Model ID mismatch" });
                }

                var result = await _apiKeyService.CreateApiKeysAsync(modelId, bulkCreateDto.ApiKeys);
                
                if (result.IsSuccess)
                {
                    return CreatedAtAction(nameof(GetApiKeysByModelId), new { modelId }, 
                        new { success = true, data = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateApiKeysForModelBulk for ModelId {ModelId}", modelId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPut("model/{modelId}/update")]
        public async Task<IActionResult> UpdateApiKeysForModel(Guid modelId, [FromBody] List<ApiKeyUpdateDTO> apiKeyDtos)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var result = await _apiKeyService.UpdateApiKeysAsync(modelId, apiKeyDtos);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApiKeysForModel for ModelId {ModelId}", modelId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpPut("model/{modelId}/bulk-update")]
        public async Task<IActionResult> UpdateApiKeysForModelBulk(Guid modelId, [FromBody] BulkApiKeyUpdateDTO bulkUpdateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                if (modelId != bulkUpdateDto.ChatbotModelId)
                {
                    return BadRequest(new { success = false, message = "Model ID mismatch" });
                }

                var result = await _apiKeyService.UpdateApiKeysAsync(modelId, bulkUpdateDto.ApiKeys);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in UpdateApiKeysForModelBulk for ModelId {ModelId}", modelId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpDelete("model/{modelId}")]
        public async Task<IActionResult> DeleteApiKeysByModelId(Guid modelId)
        {
            try
            {
                var result = await _apiKeyService.DeleteApiKeysByModelIdAsync(modelId);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in DeleteApiKeysByModelId for ModelId {ModelId}", modelId);
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion

        #region Validation Operations

        [HttpPost("validate")]
        public async Task<IActionResult> ValidateApiKey([FromBody] ApiKeyValidationDTO validationDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new { success = false, message = "Invalid model state", errors = ModelState });
                }

                var result = await _apiKeyService.ValidateApiKeyAsync(validationDto.ApiKey);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, isValid = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateApiKey");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("validate/{apiKeyValue}")]
        public async Task<IActionResult> ValidateApiKeyByParam(string apiKeyValue)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return BadRequest(new { success = false, message = "API key value is required" });
                }

                var result = await _apiKeyService.ValidateApiKeyAsync(apiKeyValue);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, isValid = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in ValidateApiKeyByParam");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("check-unique/{apiKeyValue}")]
        public async Task<IActionResult> CheckApiKeyUnique(string apiKeyValue, [FromQuery] Guid? excludeId = null)
        {
            try
            {
                if (string.IsNullOrEmpty(apiKeyValue))
                {
                    return BadRequest(new { success = false, message = "API key value is required" });
                }

                var result = await _apiKeyService.IsApiKeyUniqueAsync(apiKeyValue, excludeId);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, isUnique = result.Data, message = result.Message });
                }
                
                return BadRequest(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CheckApiKeyUnique");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        [HttpGet("model-key/{modelName}")]
        public async Task<IActionResult> GetApiKeyForModel(string modelName)
        {
            try
            {
                if (string.IsNullOrEmpty(modelName))
                {
                    return BadRequest(new { success = false, message = "Model name is required" });
                }

                var result = await _apiKeyService.GetApiKeyForModelAsync(modelName);
                
                if (result.IsSuccess)
                {
                    return Ok(new { success = true, data = result.Data, message = result.Message });
                }
                
                return NotFound(new { success = false, message = result.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetApiKeyForModel");
                return StatusCode(500, new { success = false, message = "Internal server error" });
            }
        }

        #endregion
    }
}

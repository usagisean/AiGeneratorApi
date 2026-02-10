using System.ComponentModel.DataAnnotations;

namespace AiGeneratorApi.Model;

public class GenerateRequest
{
    [Required]
    public string Prompt { get; set; }
    
    // 调用方只需要告诉我它想要什么模型，其他的别管
    public string? ModelName { get; set; }
}
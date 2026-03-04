namespace AiGeneratorApi.Model;

/// <summary>
/// 统一 API 响应包装器
/// 所有接口返回都通过此类包装，保持格式一致
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTimeOffset.Now.ToString("yyyy-MM-ddTHH:mm:sszzz");
    public T? Data { get; set; }

    /// <summary>
    /// 构建成功响应
    /// </summary>
    public static ApiResponse<T> Ok(T data, string message = "生成成功")
    {
        return new ApiResponse<T>
        {
            Success = true,
            Message = message,
            Data = data
        };
    }

    /// <summary>
    /// 构建失败响应
    /// </summary>
    public static ApiResponse<T> Fail(string message)
    {
        return new ApiResponse<T>
        {
            Success = false,
            Message = message,
            Data = default
        };
    }
}

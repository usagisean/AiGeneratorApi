namespace AiGeneratorApi.Model;

public class NewApiSettings
{
    public string BaseUrl { get; set; } = NewApiModelDefaults.DefaultBaseUrl;
    public string VipApiKey { get; set; } = string.Empty;
    public string FreeApiKey { get; set; } = string.Empty;
    public string DefaultModelId { get; set; } = NewApiModelDefaults.DefaultModelId;
    public List<string> Models { get; set; } = NewApiModelDefaults.KnownModels.ToList();
    public List<string> FallbackModels { get; set; } = NewApiModelDefaults.FallbackModels.ToList();
    public bool FetchRemoteModels { get; set; } = true;
    public int RequestTimeoutSeconds { get; set; } = 300;
    public int ModelListTimeoutSeconds { get; set; } = 10;
}

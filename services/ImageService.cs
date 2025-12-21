namespace DemoApi.Services;
using Supabase;

public class ImageService: IImageService {

    private readonly Client _supabaseClient;
    private readonly ILogger<ImageService> _logger;

    public ImageService(Client supabaseClient, ILogger<ImageService> logger)
    {
        _supabaseClient = supabaseClient;
        _logger = logger;
    }

    public async Task<string?> uploadImage(IFormFile file) {
        try {
            using var memoryStream = new MemoryStream();
            await file.CopyToAsync(memoryStream);
            var fileBytes = memoryStream.ToArray();
            var nowTimestamp = DateTime.Now;
            var fileName = nowTimestamp.ToString() + Path.GetExtension(file.FileName);
            var storage = _supabaseClient.Storage.From("products");
            await storage.Upload(fileBytes, fileName);
            return storage.GetPublicUrl(fileName);
        } catch(Exception exception) {
            _logger.LogWarning(exception, exception.ToString());
            return null;
        }
    }
}
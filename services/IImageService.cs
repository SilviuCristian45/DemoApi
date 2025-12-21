namespace DemoApi.Services;

public interface IImageService {
    public Task<string?> uploadImage(IFormFile file);
}
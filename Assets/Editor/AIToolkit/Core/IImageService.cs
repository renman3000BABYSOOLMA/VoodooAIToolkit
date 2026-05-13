using System.Threading.Tasks;

namespace AIToolkit.Core
{
    public enum ImageResolution { _512x512 = 512, _1024x1024 = 1024 }

    public enum ImageModel { GptImage2, GptImage15, GptImage1, GptImage1Mini }

    public static class ImageModelExtensions
    {
        public static string ToApiString(this ImageModel model) => model switch
        {
            ImageModel.GptImage2    => "gpt-image-2",
            ImageModel.GptImage15   => "gpt-image-1.5",
            ImageModel.GptImage1    => "gpt-image-1",
            ImageModel.GptImage1Mini => "gpt-image-1-mini",
            _                        => "gpt-image-2"
        };
    }

    public interface IImageService
    {
        Task<byte[]> GenerateImageAsync(string prompt, string styleHint, ImageResolution resolution);
    }
}

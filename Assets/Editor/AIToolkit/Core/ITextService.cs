using System.Threading.Tasks;

namespace AIToolkit.Core
{
    public enum ReviewType { General, Performance, CodeQuality, MobileOptimization }

    public enum TextModel { Gpt55, Gpt54Mini, Gpt54Nano }

    public static class TextModelExtensions
    {
        public static string ToApiString(this TextModel model) => model switch
        {
            TextModel.Gpt55     => "gpt-5.5",
            TextModel.Gpt54Mini => "gpt-5.4-mini",
            TextModel.Gpt54Nano => "gpt-5.4-nano",
            _                   => "gpt-5.4-mini"
        };
    }

    public interface ITextService
    {
        Task<string> ReviewCodeAsync(string code, ReviewType reviewType);
        Task<string> ChatAsync(string message);
    }
}

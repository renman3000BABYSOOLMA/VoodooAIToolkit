using System.Text;

namespace AIToolkit.Core
{
    // Minimal JSON string extractor — avoids third-party dependencies.
    // Handles escaped characters inside string values.
    public static class JsonHelper
    {
        public static string ExtractString(string json, string key)
        {
            string searchKey = $"\"{key}\"";
            int keyIndex = json.IndexOf(searchKey, System.StringComparison.Ordinal);
            if (keyIndex < 0) return null;

            int colonIndex = json.IndexOf(':', keyIndex + searchKey.Length);
            if (colonIndex < 0) return null;

            int openQuote = json.IndexOf('"', colonIndex + 1);
            if (openQuote < 0) return null;

            int pos = openQuote + 1;
            var sb = new StringBuilder();

            while (pos < json.Length)
            {
                char c = json[pos];

                if (c == '\\' && pos + 1 < json.Length)
                {
                    char next = json[pos + 1];
                    pos += 2;
                    switch (next)
                    {
                        case '"':  sb.Append('"');  break;
                        case '\\': sb.Append('\\'); break;
                        case 'n':  sb.Append('\n'); break;
                        case 'r':  sb.Append('\r'); break;
                        case 't':  sb.Append('\t'); break;
                        default:   sb.Append(next); break;
                    }
                    continue;
                }

                if (c == '"') break;
                sb.Append(c);
                pos++;
            }

            return sb.ToString();
        }

        public static string EscapeForJson(string s)
        {
            return s.Replace("\\", "\\\\")
                    .Replace("\"", "\\\"")
                    .Replace("\n", "\\n")
                    .Replace("\r", "")
                    .Replace("\t", "\\t");
        }
    }
}

// Tập tin: CafebookModel/Utils/SlugifyUtil.cs
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace CafebookModel.Utils
{
    public static class SlugifyUtil
    {
        // Hàm này sẽ chuyển "Cà Phê Sữa Đá" -> "ca-phe-sua-da"
        public static string GenerateSlug(this string phrase)
        {
            if (string.IsNullOrEmpty(phrase)) return "file";

            // 1. Chuyển đổi ký tự có dấu thành không dấu
            string str = phrase.ToLower().Trim();
            str = str.RemoveDiacritics();

            // 2. Thay thế khoảng trắng và ký tự đặc biệt
            str = Regex.Replace(str, @"[^a-z0-9\s-]", ""); // Xóa ký tự lạ
            str = Regex.Replace(str, @"\s+", "-"); // Thay khoảng trắng bằng -
            str = Regex.Replace(str, @"-+", "-"); // Gộp nhiều dấu -
            str = str.Trim('-');

            if (string.IsNullOrEmpty(str)) return "file";

            return str;
        }

        // Hàm helper để bỏ dấu tiếng Việt
        private static string RemoveDiacritics(this string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }
            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }
    }
}
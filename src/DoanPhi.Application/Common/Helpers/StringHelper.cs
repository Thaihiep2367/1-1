using System.Text.RegularExpressions;

namespace DoanPhi.Application.Common.Helpers;

public static class StringHelper
{
    public static string RemoveVietnameseTones(string? str)
    {
        if (string.IsNullOrWhiteSpace(str)) return string.Empty;

        string result = str;
        result = Regex.Replace(result, "[àáạảãâầấậẩẫăằắặẳẵ]", "a", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "[èéẹẻẽêềếệểễ]", "e", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "[ìíịỉĩ]", "i", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "[òóọỏõôồốộổỗơờớợởỡ]", "o", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "[ùúụủũưừứựửữ]", "u", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "[ỳýỵỷỹ]", "y", RegexOptions.IgnoreCase);
        result = Regex.Replace(result, "[đ]", "d", RegexOptions.IgnoreCase);

        result = Regex.Replace(result, "[ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ]", "A");
        result = Regex.Replace(result, "[ÈÉẸẺẼÊỀẾỆỂỄ]", "E");
        result = Regex.Replace(result, "[ÌÍỊỈĨ]", "I");
        result = Regex.Replace(result, "[ÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ]", "O");
        result = Regex.Replace(result, "[ÙÚỤỦŨƯỪỨỰỬỮ]", "U");
        result = Regex.Replace(result, "[ỲÝỴỶỸ]", "Y");
        result = Regex.Replace(result, "[Đ]", "D");

        result = Regex.Replace(result, "[^a-zA-Z0-9\\s-]", "");
        return result.Trim();
    }
}

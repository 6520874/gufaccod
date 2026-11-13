using System;
using System.Collections.Generic;
using System.Text;

namespace GufaCoding
{
    /// <summary>
    /// 极简 C 语法高亮，输出 uGUI 富文本（VS Code Dark+ 配色）。
    /// 只认关键字、预处理、字符串、数字、注释、函数名，够关卡里那几行代码用了。
    /// </summary>
    public static class CodeHighlighter
    {
        const string CType = "#569CD6";  // int char void ...
        const string CCtrl = "#C586C0";  // return if for ...
        const string CStr = "#CE9178";
        const string CNum = "#B5CEA8";
        const string CCmt = "#6A9955";
        const string CFunc = "#DCDCAA";
        const string CVar = "#9CDCFE";
        const string CPre = "#C586C0";
        const string CBlank = "#FFD54A";

        static readonly HashSet<string> Types = new HashSet<string>
        {
            "int", "char", "float", "double", "void", "long", "short", "unsigned", "signed",
            "const", "static", "struct", "extern", "sizeof", "NULL", "true", "false",
        };

        static readonly HashSet<string> Ctrl = new HashSet<string>
        {
            "return", "if", "else", "for", "while", "do", "switch", "case", "break", "continue", "goto", "default",
        };

        public static string Highlight(string line)
        {
            if (string.IsNullOrEmpty(line)) return line;
            var sb = new StringBuilder(line.Length * 3);
            int i = 0, n = line.Length;
            while (i < n)
            {
                char c = line[i];

                if (c == '/' && i + 1 < n && line[i + 1] == '/')
                {
                    Wrap(sb, line.Substring(i), CCmt);
                    break;
                }

                if (c == '#')
                {
                    int j = i;
                    while (j < n && !char.IsWhiteSpace(line[j])) j++;
                    Wrap(sb, line.Substring(i, j - i), CPre);
                    i = j;
                    continue;
                }

                if (c == '"')
                {
                    int j = i + 1;
                    while (j < n && line[j] != '"')
                    {
                        if (line[j] == '\\') j++;
                        j++;
                    }
                    j = Math.Min(n, j + 1);
                    Wrap(sb, line.Substring(i, j - i), CStr);
                    i = j;
                    continue;
                }

                if (c == '_' && i + 1 < n && line[i + 1] == '_')
                {
                    int j = i;
                    while (j < n && line[j] == '_') j++;
                    sb.Append("<b><color=").Append(CBlank).Append('>').Append(line, i, j - i).Append("</color></b>");
                    i = j;
                    continue;
                }

                if (IsAsciiDigit(c))
                {
                    int j = i;
                    while (j < n && (IsAsciiLetterOrDigit(line[j]) || line[j] == '.')) j++;
                    Wrap(sb, line.Substring(i, j - i), CNum);
                    i = j;
                    continue;
                }

                if (IsAsciiLetter(c) || c == '_')
                {
                    int j = i;
                    while (j < n && (IsAsciiLetterOrDigit(line[j]) || line[j] == '_')) j++;
                    string w = line.Substring(i, j - i);
                    int k = j;
                    while (k < n && line[k] == ' ') k++;
                    if (Ctrl.Contains(w)) Wrap(sb, w, CCtrl);
                    else if (Types.Contains(w)) Wrap(sb, w, CType);
                    else if (k < n && line[k] == '(') Wrap(sb, w, CFunc);
                    else Wrap(sb, w, CVar);
                    i = j;
                    continue;
                }

                sb.Append(c);
                i++;
            }
            return sb.ToString();
        }

        static void Wrap(StringBuilder sb, string s, string color)
        {
            sb.Append("<color=").Append(color).Append('>').Append(s).Append("</color>");
        }

        static bool IsAsciiDigit(char c) { return c >= '0' && c <= '9'; }
        static bool IsAsciiLetter(char c) { return (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z'); }
        static bool IsAsciiLetterOrDigit(char c) { return IsAsciiLetter(c) || IsAsciiDigit(c); }
    }
}

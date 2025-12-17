using System.Text.RegularExpressions;
using System;

namespace GraphicsCat
{
    public class RegexUtils
    {
        public static string RemoveComments(string input)
        {
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";

            string noComments = Regex.Replace
            (
                input + "\r\n", // Add \r\n to solve the problem that the last line comment may not have a newline

                blockComments + "|" + lineComments,

                me =>
                {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                        return me.Value.StartsWith("//") ? Environment.NewLine : "";
                    // Keep the literal strings
                    return me.Value;
                },

                RegexOptions.Singleline
            );

            return noComments;
        }
    }
}

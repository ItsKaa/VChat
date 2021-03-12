namespace VChat.Extensions
{
    public static class StringExtensions
    {
        public static string Between(this string source, string start, string end, out int position, int fromPosition = 0)
        {
            var sourceFromPosition = source.Substring(fromPosition);
            position = 0;
            int startPos = sourceFromPosition.IndexOf(start);
            if (startPos < 0)
                return string.Empty;
            startPos += start.Length;

            int endPos = sourceFromPosition.IndexOf(end, startPos);
            if (end.Length == 0)
                endPos = sourceFromPosition.Length;
            else if (endPos < 0)
                return string.Empty; // end not found

            position = startPos;
            var result = sourceFromPosition.Substring(startPos, endPos - startPos);
            return result;
        }

        public static string Between(this string source, string start, string end, int fromPosition = 0)
            => Between(source, start, end, out int _, fromPosition);
    }
}

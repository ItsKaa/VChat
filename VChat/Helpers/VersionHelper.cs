namespace VChat.Helpers
{
    public static class VersionHelper
    {
        /// <summary>
        /// Compares two version strings
        /// </summary>
        /// <returns>True if the other version is greater than the source version, otherwise false.</returns>
        public static bool IsNewerVersion(string sourceVersionString, string otherVersionString, bool isSourceBeta)
        {
            var sourceVersion = new System.Version(sourceVersionString);
            var otherVersion = new System.Version(otherVersionString);

            var result = otherVersion.CompareTo(sourceVersion);
            if (isSourceBeta)
            {
                return result >= 0;
            }
            else
            {
                return result > 0;
            }
        }
    }
}

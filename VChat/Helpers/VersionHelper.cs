namespace VChat.Helpers
{
    public static class VersionHelper
    {
        /// <summary>
        /// Compares two version strings
        /// </summary>
        /// <returns>True if the other version is greater than the source version, otherwise false.</returns>
        public static bool IsNewerVersion(string sourceVersionString, string otherVersionString)
        {
            var sourceVersion = new System.Version(sourceVersionString);
            var otherVersion = new System.Version(otherVersionString);
            return otherVersion.CompareTo(sourceVersion) > 0;
        }
    }
}

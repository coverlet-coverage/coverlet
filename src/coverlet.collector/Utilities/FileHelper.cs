using System.IO;
using Coverlet.Collector.Utilities.Interfaces;

namespace Coverlet.Collector.Utilities
{
    /// <inheritdoc />
    internal class FileHelper : IFileHelper
    {
        /// <inheritdoc />
        public bool Exists(string path)
        {
            return File.Exists(path);
        }

        /// <inheritdoc />
        public void WriteAllText(string path, string contents)
        {
            File.WriteAllText(path, contents);
        }
    }
}

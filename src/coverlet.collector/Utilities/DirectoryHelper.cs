using System.IO;
using Coverlet.Collector.Utilities.Interfaces;

namespace Coverlet.Collector.Utilities
{
    /// <inheritdoc />
    internal class DirectoryHelper : IDirectoryHelper
    {
        /// <inheritdoc />
        public bool Exists(string path)
        {
            return Directory.Exists(path);
        }

        /// <inheritdoc />
        public void CreateDirectory(string path)
        {
            Directory.CreateDirectory(path);
        }

        /// <inheritdoc />
        public void Delete(string path, bool recursive)
        {
            Directory.Delete(path, recursive);
        }
    }
}

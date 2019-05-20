namespace Coverlet.Collector.Utilities.Interfaces
{
    interface IDirectoryHelper
    {
        /// <summary>
        /// Determines whether the specified directory exists.
        /// </summary>
        /// <param name="path">The directory to check.</param>
        /// <returns>true if the caller has the required permissions and path contains the name of an existing directory; otherwise, false.
        /// This method also returns false if path is null, an invalid path, or a zero-length string.
        /// If the caller does not have sufficient permissions to read the specified file,
        /// no exception is thrown and the method returns false regardless of the existence of path.</returns>
        bool Exists(string path);

        /// <summary>
        /// Creates all directories and subdirectories in the specified path unless they already exist.
        /// </summary>
        /// <param name="directory">The directory to create.</param>
        void CreateDirectory(string directory);

        /// <summary>
        /// Deletes the specified directory and, if indicated, any subdirectories and files in the directory.
        /// </summary>
        /// <param name="path">The name of the directory to remove.</param>
        /// <param name="recursive">true to remove directories, subdirectories, and files in path; otherwise, false.</param>
        void Delete(string path, bool recursive);
    }
}

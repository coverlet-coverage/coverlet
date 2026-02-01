// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.IO;

namespace Coverlet.Core.Abstractions
{
  internal interface IFileSystem
  {
    void CreateDirectory(string path); // Add this method
    bool Exists(string path);

    void WriteAllText(string path, string contents);

    string ReadAllText(string path);

    Stream OpenRead(string path);

    void Copy(string sourceFileName, string destFileName, bool overwrite);

    void Delete(string path);

    Stream NewFileStream(string path, FileMode mode);

    Stream NewFileStream(string path, FileMode mode, FileAccess access);

    string[] ReadAllLines(string path);
  }
}

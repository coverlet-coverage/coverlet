#nullable enable

using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace Coverlet.Core.Standalone.Tests
{
    public abstract class BaseTest
    {
        protected string GetLibToInstrument()
        {
            string moduleToInstrument = "coverlet.core.standalone.sample.dll";
            string location = Path.GetFullPath(moduleToInstrument);
            string newPath = Path.Combine(Path.GetDirectoryName(location), Guid.NewGuid().ToString("N"), moduleToInstrument);
            Directory.CreateDirectory(Path.GetDirectoryName(newPath));
            File.Copy(location, newPath);
            File.Copy(Path.ChangeExtension(location, ".pdb"), Path.ChangeExtension(newPath, ".pdb"));
            return newPath;
        }
    }

    public class SampleWrapper
    {
        readonly dynamic? _object;
        readonly Assembly _asm;

        public SampleWrapper(string asmFile)
        {
            _asm = Assembly.Load(File.ReadAllBytes(asmFile));
            foreach (Type type in _asm.GetTypes())
            {
                if (type.Name == "Calculator")
                {
                    _object = Activator.CreateInstance(type);
                }
            }
        }

        public int Add(int a, int b)
        {
            if (_object is null)
            {
                throw new NullReferenceException("_object is null");
            }

            return _object.Add(a, b);
        }
    }
}

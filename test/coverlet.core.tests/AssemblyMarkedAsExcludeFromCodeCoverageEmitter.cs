using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace coverlet.core.tests
{
    internal static class AssemblyMarkedAsExcludeFromCodeCoverageEmitter
    {
        private static bool IsWindows => Path.DirectorySeparatorChar == '\\';
        public static (string dllPath, string pdbPath) EmitAssemblyToInstrument(string outputFolder,string attributeName)
        {
            var attributeClassSyntaxTree = CSharpSyntaxTree.ParseText("[System.AttributeUsage(System.AttributeTargets.Assembly)]public class " + attributeName + ":System.Attribute{}");
            var instrumentableClassSyntaxTree = CSharpSyntaxTree.ParseText($@"
[assembly:{attributeName}]
namespace coverlet.tests.projectsample.excludedbyattribute{{
public class SampleClass
{{
	public int SampleMethod()
	{{
		return new System.Random().Next();
	}}
}}

}}
");
            var compilation = CSharpCompilation.Create(attributeName, new List<SyntaxTree>
                {
                    attributeClassSyntaxTree,instrumentableClassSyntaxTree
                }).AddReferences(
                MetadataReference.CreateFromFile(typeof(Attribute).Assembly.Location)).
            WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, false));

            var dllPath = Path.Combine(outputFolder, $"{attributeName}.dll");
            var pdbPath = Path.Combine(outputFolder, $"{attributeName}.pdb");

            using (var outputStream = File.Create(dllPath))
            using (var pdbStream = File.Create(pdbPath))
            {
                var emitOptions = new EmitOptions(pdbFilePath: pdbPath);
                var emitResult = compilation.Emit(outputStream, pdbStream, options: IsWindows ? emitOptions : emitOptions.WithDebugInformationFormat(DebugInformationFormat.PortablePdb));
                if (!emitResult.Success)
                {
                    var message = "Failure to dynamically create dll";
                    foreach (var diagnostic in emitResult.Diagnostics)
                    {
                        message += Environment.NewLine;
                        message += diagnostic.GetMessage();
                    }
                    throw new Xunit.Sdk.XunitException(message);
                }
            }
            return (dllPath, pdbPath);
        }

    }
}

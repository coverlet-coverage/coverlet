using System;
using System.Diagnostics.CodeAnalysis;

[assembly: ExcludeFromCodeCoverage]

namespace coverlet.tests.projectsample.excludedbyattribute
{
    public class SampleClass
    {
        public int SampleMethod()
        {
            return new Random().Next();
        }
    }
}

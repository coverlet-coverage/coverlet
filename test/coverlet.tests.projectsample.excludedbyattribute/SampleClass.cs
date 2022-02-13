// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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

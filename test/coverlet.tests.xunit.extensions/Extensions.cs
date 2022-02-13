// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Linq;

using Xunit.Abstractions;
using Xunit.Sdk;

namespace Coverlet.Tests.Xunit.Extensions
{
    internal static class TestMethodExtensions
    {
        public static string EvaluateSkipConditions(this ITestMethod testMethod)
        {
            ITypeInfo testClass = testMethod.TestClass.Class;
            IAssemblyInfo assembly = testMethod.TestClass.TestCollection.TestAssembly.Assembly;
            System.Collections.Generic.IEnumerable<System.Attribute> conditionAttributes = testMethod.Method
                .GetCustomAttributes(typeof(ITestCondition))
                .Concat(testClass.GetCustomAttributes(typeof(ITestCondition)))
                .Concat(assembly.GetCustomAttributes(typeof(ITestCondition)))
                .OfType<ReflectionAttributeInfo>()
                .Select(attributeInfo => attributeInfo.Attribute);

            foreach (ITestCondition condition in conditionAttributes)
            {
                if (!condition.IsMet)
                {
                    return condition.SkipReason;
                }
            }

            return null;
        }
    }
}

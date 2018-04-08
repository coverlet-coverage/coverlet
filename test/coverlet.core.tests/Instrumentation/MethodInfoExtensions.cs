using System;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Coverlet.Core.Instrumentation.Tests
{
    public static class MethodInfoExtensions
    {
        public static string GetMethodNameInMonoCecilFormat(this MethodInfo method)
        {
            var parameters = method.GetParameters();
            StringBuilder sb = new StringBuilder();
            sb.Append(method.ReturnParameter.ParameterType.FullName);
            sb.Append(" ");
            sb.Append(method.DeclaringType);
            sb.Append("::");
            sb.Append(method.Name);
            var parameterNames =
                parameters.Any() ? String.Join(",", parameters.Select(e => e.ParameterType.FullName)) : "";
            sb.Append($"({parameterNames})");
            return sb.ToString();
        }
    }
}
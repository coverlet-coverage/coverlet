using System;
using System.Collections.Generic;

namespace Coverlet.Core.Instrumentation
{
    internal static class InstrumenterFactory
    {
        private static readonly List<Func<IInstrumenter, IInstrumenter>>  DecoratorFunctions = new List<Func<IInstrumenter, IInstrumenter>>();

        public static IInstrumenter Create(string module, string identifier, string[] filters, string[] excludedFiles)
        {
            IInstrumenter result = new Instrumenter(module, identifier, filters, excludedFiles);

            // Decorate the real Instrumenter instance
            foreach (var decoratorFunc in DecoratorFunctions)
                result = decoratorFunc.Invoke(result);

            return result;
        }

        public static void RegisterDecorator(Func<IInstrumenter, IInstrumenter> decoratingFunc)
        {
            if (decoratingFunc == null)
                throw new ArgumentNullException(nameof(decoratingFunc));

            DecoratorFunctions.Add(decoratingFunc);
        }
    }
}
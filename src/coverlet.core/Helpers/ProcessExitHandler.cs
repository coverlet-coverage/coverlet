using System;
using Coverlet.Core.Abstractions;

namespace Coverlet.Core.Helpers
{
    internal class ProcessExitHandler : IProcessExitHandler
    {
        public void Add(EventHandler handler)
        {
            AppDomain.CurrentDomain.ProcessExit += handler;
        }
    }
}

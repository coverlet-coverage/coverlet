using System;

namespace Coverlet.Core.Abstractions
{
    internal interface IProcessExitHandler
    {
        void Add(EventHandler handler);
    }
}

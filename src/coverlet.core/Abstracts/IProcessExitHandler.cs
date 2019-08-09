using System;

namespace Coverlet.Core.Abstracts
{
    internal interface IProcessExitHandler
    {
        void Add(EventHandler handler);
    }
}

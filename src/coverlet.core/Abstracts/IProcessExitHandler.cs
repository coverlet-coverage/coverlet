using System;

namespace Coverlet.Core.Abstracts
{
    public interface IProcessExitHandler
    {
        void Add(EventHandler handler);
    }
}

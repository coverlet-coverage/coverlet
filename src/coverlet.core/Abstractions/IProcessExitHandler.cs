// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

#nullable disable

namespace Coverlet.Core.Abstractions
{
    internal interface IProcessExitHandler
    {
        void Add(EventHandler handler);
    }
}

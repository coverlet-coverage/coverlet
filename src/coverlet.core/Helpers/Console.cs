// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Coverlet.Core.Abstractions;

#nullable disable

namespace Coverlet.Core.Helpers
{
    public class SystemConsole : IConsole
    {
        public void WriteLine(string value)
        {
            Console.WriteLine(value);
        }
    }
}

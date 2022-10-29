// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Build.Utilities;

#nullable disable

namespace Coverlet.MSbuild.Tasks
{
    public abstract class BaseTask : Task
    {
        protected static IServiceProvider ServiceProvider { get; set; }
    }
}

// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using Microsoft.Build.Utilities;

namespace Coverlet.MSbuild.Tasks
{
    public abstract class BaseTask : Task
    {
        public static IServiceProvider ServiceProvider { get; protected internal set; }
    }
}

// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Core.Attributes
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Constructor | AttributeTargets.Class)]
    internal class DoesNotReturnAttribute : Attribute { }
}

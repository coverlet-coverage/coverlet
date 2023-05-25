// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.// This file is used by Code Analysis to maintain SuppressMessage

// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Design", "CA1002:Do not expose generic lists", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Design", "CA1003:Use generic event handler instances", Justification = "test sample file", Scope = "member", Target = "~E:Coverlet.Core.Samples.Tests.EventSource_Issue_689.Handle")]
[assembly: SuppressMessage("Design", "CA1030:Use events where appropriate", Justification = "test sample file", Scope = "member", Target = "~M:Coverlet.Core.Samples.Tests.EventSource_Issue_689.RaiseEvent")]
[assembly: SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Design", "CA1041: Provide ObsoleteAttribute message", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Design", "CA1044:Properties should not be write only", Justification = "test sample file", Scope = "member", Target = "~P:Coverlet.Core.Samples.Tests.ClassWithSetterOnlyPropertyExcludedByObsoleteAttr.Property")]
[assembly: SuppressMessage("Design", "CA1052:Static holder types should be Static or NotInheritable", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Globalization", "CA1310:Specify StringComparison for correctness", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Performance", "CA1805:Do not initialize unnecessarily", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Performance", "CA1812: Avoid uninstantiated internal classes", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Performance", "CA1822:Mark members as static", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Reliability", "CA2007:Consider calling ConfigureAwait on the awaited task", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Reliability", "CA2008: Do not create tasks without passing a TaskScheduler", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Security", "CA5394:Do not use insecure randomness", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Usage", "CA2201:Do not raise reserved exception types", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]
[assembly: SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "test sample file", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Samples.Tests")]

[assembly: SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "test source", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Tests")]
[assembly: SuppressMessage("Design", "CA1031: Do not catch general exception types", Justification = "test source", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Helpers")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "test source", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Tests")]
[assembly: SuppressMessage("Design", "CA1062:Validate arguments of public methods", Justification = "test source", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Instrumentation.Tests")]
[assembly: SuppressMessage("Design", "CA1812: Avoid uninstantiated internal classes", Justification = "test source", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Tests")]
[assembly: SuppressMessage("Design", "CA1724: Type names should not match namespaces", Justification = "test source", Scope = "namespaceanddescendants", Target = "Coverlet.Core.Tests")]

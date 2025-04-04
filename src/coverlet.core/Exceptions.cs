// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Core.Exceptions
{
  public class CoverletException : Exception
  {
    public CoverletException() { }
    public CoverletException(string message) : base(message) { }
    public CoverletException(string message, System.Exception inner) : base(message, inner) { }
  }

  internal class CecilAssemblyResolutionException : CoverletException
  {
    public CecilAssemblyResolutionException() { }
    public CecilAssemblyResolutionException(string message) : base(message) { }
    public CecilAssemblyResolutionException(string message, System.Exception inner) : base(message, inner) { }
  }
}

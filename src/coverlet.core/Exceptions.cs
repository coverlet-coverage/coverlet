using System;

namespace Coverlet.Core.Exceptions
{
    [Serializable]
    internal class CoverletException : Exception
    {
        public CoverletException() { }
        public CoverletException(string message) : base(message) { }
        public CoverletException(string message, System.Exception inner) : base(message, inner) { }
        protected CoverletException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }

    [Serializable]
    internal class CecilAssemblyResolutionException : CoverletException
    {
        public CecilAssemblyResolutionException() { }
        public CecilAssemblyResolutionException(string message) : base(message) { }
        public CecilAssemblyResolutionException(string message, System.Exception inner) : base(message, inner) { }
        protected CecilAssemblyResolutionException(
          System.Runtime.Serialization.SerializationInfo info,
          System.Runtime.Serialization.StreamingContext context) : base(info, context) { }
    }
}

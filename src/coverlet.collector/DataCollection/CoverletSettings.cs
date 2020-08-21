using System.Linq;
using System.Text;

namespace Coverlet.Collector.DataCollection
{
    /// <summary>
    /// Coverlet settings
    /// </summary>
    internal class CoverletSettings
    {
        /// <summary>
        /// Test module
        /// </summary>
        public string TestModule { get; set; }

        /// <summary>
        /// Report formats
        /// </summary>
        public string[] ReportFormats { get; set; }

        /// <summary>
        /// Filters to include
        /// </summary>
        public string[] IncludeFilters { get; set; }

        /// <summary>
        /// Directories to include
        /// </summary>
        public string[] IncludeDirectories { get; set; }

        /// <summary>
        /// Filters to exclude
        /// </summary>
        public string[] ExcludeFilters { get; set; }

        /// <summary>
        /// Source files to exclude
        /// </summary>
        public string[] ExcludeSourceFiles { get; set; }

        /// <summary>
        /// Attributes to exclude
        /// </summary>
        public string[] ExcludeAttributes { get; set; }

        /// <summary>
        /// Coverate report path to merge with
        /// </summary>
        public string MergeWith { get; set; }

        /// <summary>
        /// Use source link flag
        /// </summary>
        public bool UseSourceLink { get; set; }

        /// <summary>
        /// Single hit flag
        /// </summary>
        public bool SingleHit { get; set; }

        /// <summary>
        /// Includes test assembly
        /// </summary>
        public bool IncludeTestAssembly { get; set; }

        /// <summary>
        /// Neither track nor record auto-implemented properties.
        /// </summary>
        public bool SkipAutoProps { get; set; }

        /// <summary>
        /// Attributes that mark methods that never return.
        /// </summary>
        public string[] DoesNotReturnAttributes { get; set; }

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("TestModule: '{0}', ", TestModule);
            builder.AppendFormat("IncludeFilters: '{0}', ", string.Join(",", IncludeFilters ?? Enumerable.Empty<string>()));
            builder.AppendFormat("IncludeDirectories: '{0}', ", string.Join(",", IncludeDirectories ?? Enumerable.Empty<string>()));
            builder.AppendFormat("ExcludeFilters: '{0}', ", string.Join(",", ExcludeFilters ?? Enumerable.Empty<string>()));
            builder.AppendFormat("ExcludeSourceFiles: '{0}', ", string.Join(",", ExcludeSourceFiles ?? Enumerable.Empty<string>()));
            builder.AppendFormat("ExcludeAttributes: '{0}', ", string.Join(",", ExcludeAttributes ?? Enumerable.Empty<string>()));
            builder.AppendFormat("MergeWith: '{0}', ", MergeWith);
            builder.AppendFormat("UseSourceLink: '{0}'", UseSourceLink);
            builder.AppendFormat("SingleHit: '{0}'", SingleHit);
            builder.AppendFormat("IncludeTestAssembly: '{0}'", IncludeTestAssembly);
            builder.AppendFormat("SkipAutoProps: '{0}'", SkipAutoProps);
            builder.AppendFormat("DoesNotReturnAttributes: '{0}'", string.Join(",", DoesNotReturnAttributes ?? Enumerable.Empty<string>()));

            return builder.ToString();
        }
    }
}

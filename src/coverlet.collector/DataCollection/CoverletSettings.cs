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

        public override string ToString()
        {
            var builder = new StringBuilder();

            builder.AppendFormat("TestModule: '{0}', ", this.TestModule);
            builder.AppendFormat("IncludeFilters: '{0}', ", string.Join(",", this.IncludeFilters ?? Enumerable.Empty<string>()));
            builder.AppendFormat("IncludeDirectories: '{0}', ", string.Join(",", this.IncludeDirectories ?? Enumerable.Empty<string>()));
            builder.AppendFormat("ExcludeFilters: '{0}', ", string.Join(",", this.ExcludeFilters ?? Enumerable.Empty<string>()));
            builder.AppendFormat("ExcludeSourceFiles: '{0}', ", string.Join(",", this.ExcludeSourceFiles ?? Enumerable.Empty<string>()));
            builder.AppendFormat("ExcludeAttributes: '{0}', ", string.Join(",", this.ExcludeAttributes ?? Enumerable.Empty<string>()));
            builder.AppendFormat("MergeWith: '{0}', ", this.MergeWith);
            builder.AppendFormat("UseSourceLink: '{0}'", this.UseSourceLink);
            builder.AppendFormat("SingleHit: '{0}'", this.SingleHit);
            builder.AppendFormat("IncludeTestAssembly: '{0}'", this.IncludeTestAssembly);

            return builder.ToString();
        }
    }
}

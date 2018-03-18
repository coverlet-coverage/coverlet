namespace Coverlet.Core
{
    public class CoverageSummary
    {
        private CoverageResult _result;

        public CoverageSummary(CoverageResult result)
        {
            _result = result;
        }

        public CoverageSummaryResult CalculateSummary()
        {
            CoverageSummaryResult result = new CoverageSummaryResult();
            foreach (var mod in _result.Modules)
            {
                int totalLines = 0, linesCovered = 0;
                foreach (var doc in mod.Value)
                {
                    foreach (var @class in doc.Value)
                    {
                        foreach (var method in @class.Value)
                        {
                            foreach (var line in method.Value)
                            {
                                totalLines++;
                                if (line.Value > 0)
                                    linesCovered++;
                            }
                        }
                    }
                }

                result.Add(System.IO.Path.GetFileNameWithoutExtension(mod.Key), (linesCovered * 100) / totalLines);
            }

            return result;
        }
    }
}
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
            foreach (var mod in _result.Data)
            {
                int totalLines = 0, linesCovered = 0;
                foreach (var doc in mod.Value)
                {
                    totalLines += doc.Value.Count;
                    foreach (var line in doc.Value)
                    {
                        if (line.Value > 0)
                            linesCovered++;
                    }
                }

                result.Add(mod.Key, (linesCovered * 100) / totalLines);
            }

            return result;
        }
    }
}
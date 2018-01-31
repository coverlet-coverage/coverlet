namespace Coverlet.Core
{
    public class Coverage
    {
        private string _module;
        private string _identifier;

        public Coverage(string module, string identifier)
        {
            _module = module;
            _identifier = identifier;
        }

        public void PrepareModules()
        {

        }

        public string GetCoverageResults()
        {
            return string.Empty;
        }
    }
}
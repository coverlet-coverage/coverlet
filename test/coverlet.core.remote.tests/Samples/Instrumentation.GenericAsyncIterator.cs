// Remember to use full name because adding new using directives change line numbers

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace coverlet.core.remote.samples.tests
{
    public class GenericAsyncIterator<T>
    {
        public async Task<List<int>> Issue1383()
        {
            var sequence = await CreateSequenceAsync().ToListAsync();
            return sequence;
        }


        public async IAsyncEnumerable<int> CreateSequenceAsync()
        {
            await Task.CompletedTask;
            yield return 5;
            yield return 2;
        }
    }
}

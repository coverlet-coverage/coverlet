// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.Tests
{
    public class Issue_669_2
    {
        private readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

        async public System.Threading.Tasks.ValueTask<System.Net.Http.HttpResponseMessage> SendRequest()
        {
            using (var requestMessage = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://www.google.it"))
            {
                return await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            }
        }
    }
}

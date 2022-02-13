// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Coverlet.Core.Tests
{
    public class Issue_669_2
    {
        private readonly System.Net.Http.HttpClient _httpClient = new System.Net.Http.HttpClient();

        public async System.Threading.Tasks.ValueTask<System.Net.Http.HttpResponseMessage> SendRequest()
        {
            using (var requestMessage = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, "https://www.google.it"))
            {
                return await _httpClient.SendAsync(requestMessage).ConfigureAwait(false);
            }
        }
    }
}

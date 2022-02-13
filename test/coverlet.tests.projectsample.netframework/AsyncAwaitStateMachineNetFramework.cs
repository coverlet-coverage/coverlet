// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Threading.Tasks;

namespace coverlet.tests.projectsample.netframework
{
    public class AsyncAwaitStateMachineNetFramework
    {
        public async Task AsyncAwait()
        {
            await Task.CompletedTask;
        }
    }
}

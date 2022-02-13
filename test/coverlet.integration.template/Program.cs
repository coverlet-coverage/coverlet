// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

using Coverlet.Integration.Template;

namespace HelloWorld
{
    class Program
    {
        static void Main(string[] args)
        {
            var dt = new DeepThought();
            dt.AnswerToTheUltimateQuestionOfLifeTheUniverseAndEverything();
            Console.WriteLine("Hello World!");
        }
    }
}
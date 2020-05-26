using System;

namespace Quibble.CSharp.Examples
{
    class Program
    {
        private static void RunDiff((string, string) example)
        {
            Console.WriteLine($"Diff: ({example.Item1}, {example.Item2})");
            var diffs = JsonStrings.Diff(example.Item1, example.Item2);
            foreach (var diff in diffs)
            {
                Console.WriteLine(diff);
            }

            Console.WriteLine();
        }

        static void Main(string[] args)
        {
            var examples = new[]
            {
               ("1", "2"),
               ("[ 1 ]", "[ 2, 1 ]"),
               (@"{ ""item"": ""widget"", ""price"": 12.20 }", @"{ ""item"": ""widget"" }"),
               (@"{ ""books"": [ { ""title"": ""Data and Reality"", ""author"": ""William Kent"" }, { ""title"": ""Thinking Forth"", ""author"": ""Chuck Moore"" } ] }",
                @"{ ""books"": [ { ""title"": ""Data and Reality"", ""author"": ""William Kent"" }, { ""title"": ""Thinking Forth"", ""author"": ""Leo Brodie"" } ] }")
            };

            foreach (var ex in examples)
            {
                RunDiff(ex);
            }
        }
    }
}

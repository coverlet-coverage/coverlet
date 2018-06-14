using System;
using Xunit;
using System.Collections.Generic;
using coverlet.core.Extensions;

namespace Coverlet.Core.Extensions.Tests
{
    public class CoverageResultExtensionsTests
    {
        [Fact]
        public void TestMergeTwoDifferent()
        {
            var first = CreateResult("ResultA()");
            var second = CreateResult("ResultB()");

            first.Merge(second);

            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Lines[2].Hits);

            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Lines[2].Hits);
        }

        [Fact]
        public void TestMergeTwoSimilar()
        {
            var first = CreateResult("Result()");
            var second = CreateResult("Result()");

            first.Merge(second);

            Assert.Equal(2, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(2, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(2, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Lines[2].Hits);
        }

        [Fact]
        public void TestMergeThreeDifferent()
        {
            var first = CreateResult("ResultA()");
            var second = CreateResult("ResultB()");
            var third = CreateResult("ResultC()");

            first.Merge(second);
            first.Merge(third);

            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["ResultA()"].Lines[2].Hits);

            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["ResultB()"].Lines[2].Hits);

            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultC()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultC()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["ResultC()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["ResultC()"].Lines[2].Hits);
        }

        [Fact]
        public void TestMergeThreeSimilar()
        {
            var first = CreateResult("Result()");
            var second = CreateResult("Result()");
            var third = CreateResult("Result()");

            first.Merge(second);
            first.Merge(third);

            Assert.Equal(3, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(3, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(3, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Lines[2].Hits);
        }

        [Fact]
        public void TestMergeBothEmpty()
        {
            var first = CreateResultEmpty();
            var second = CreateResultEmpty();

            first.Merge(second);

            Assert.Empty(first.Modules);
        }

        [Fact]
        public void TestMergeOneEmpty()
        {
            var first = CreateResultEmpty();
            var second = CreateResult("Result()");

            first.Merge(second);

            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)].Hits);
            Assert.Equal(1, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Lines[1].Hits);
            Assert.Equal(0, first.Modules["Module"]["Doc.cs"]["Class"]["Result()"].Lines[2].Hits);
        }

        [Fact]
        public void TestMergeUnmodified()
        {
            var first = CreateResult("Result()");
            var second = CreateResultEmpty();

            first.Merge(second);

            Assert.Empty(second.Modules);
        }

        [Fact]
        public void TestMergeIdentifier()
        {
            var first = CreateResult("ResultA()");
            var second = CreateResult("ResultA()");

            var beforeFirst = first.Identifier;
            var beforeSecond = second.Identifier;

            first.Merge(second);

            Assert.Equal(beforeFirst, first.Identifier);
            Assert.Equal(beforeSecond, second.Identifier);
        }

        private CoverageResult CreateResultEmpty()
        {
            return new CoverageResult()
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = new Modules()
            };
        }

        private CoverageResult CreateResult(string methodString)
        {
            Lines lines = new Lines();
            lines.Add(1, new HitInfo { Hits = 1 });
            lines.Add(2, new HitInfo { Hits = 0 });

            Branches branches = new Branches();
            branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 0, Ordinal: 1)] = new HitInfo { Hits = 1 };
            branches[(Number: 1, Offset: 1, EndOffset: 2, Path: 1, Ordinal: 2)] = new HitInfo { Hits = 1 };

            Methods methods = new Methods();
            methods.Add(methodString, new Method());
            methods[methodString].Lines = lines;
            methods[methodString].Branches = branches;

            Classes classes = new Classes();
            classes.Add("Class", methods);

            Documents documents = new Documents();
            documents.Add("Doc.cs", classes);

            Modules modules = new Modules();
            modules.Add("Module", documents);

            return new CoverageResult()
            {
                Identifier = Guid.NewGuid().ToString(),
                Modules = modules
            };
        }
    }
}
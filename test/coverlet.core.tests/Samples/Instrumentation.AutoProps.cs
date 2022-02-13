// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace Coverlet.Core.Samples.Tests
{
    public class AutoProps
    {
        private readonly int _myVal = 0;
        public AutoProps()
        {
            _myVal = new Random().Next();
        }
        public int AutoPropsNonInit { get; set; }
        public int AutoPropsInit { get; set; } = 10;
    }

    public record RecordWithPropertyInit
    {
        private readonly int _myRecordVal = 0;
        public RecordWithPropertyInit()
        {
            _myRecordVal = new Random().Next();
        }
        public string RecordAutoPropsNonInit { get; set; }
        public string RecordAutoPropsInit { get; set; } = string.Empty;
    }

    public class ClassWithAutoRecordProperties
    {
        record AutoRecordWithProperties(string Prop1, string Prop2);

        public ClassWithAutoRecordProperties()
        {
            var record = new AutoRecordWithProperties(string.Empty, string.Empty);
        }
    }
}

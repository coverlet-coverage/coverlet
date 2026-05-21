using System;

namespace Coverlet.Core.CoverageSamples.Tests
{
    public record RecordWithPropertyInit
    {
        private int _myRecordVal = 0;
        public RecordWithPropertyInit()
        {
            _myRecordVal = new Random().Next();
        }
        public string RecordAutoPropsNonInit { get; set; }
        public string RecordAutoPropsInit { get; set; } = string.Empty;
    }

    public class ClassWithRecordsAutoProperties
    {
        record RecordWithPrimaryConstructor(string Prop1, string Prop2);

        public ClassWithRecordsAutoProperties()
        {
            var record = new RecordWithPrimaryConstructor(string.Empty, string.Empty);
        }
    }

    public class ClassWithInheritingRecordsAndPrimaryConstructor
    {
        record BaseRecord(int A);

        record InheritedRecord(int A) : BaseRecord(A);

        public ClassWithInheritingRecordsAndPrimaryConstructor()
        {
            var record = new InheritedRecord(1);
        }
    }

    public class ClassWithRecordsEmptyPrimaryConstructor
    {
        record First
        {
            public string Bar() => "baz";
        }

        record Second()
        {
            public string Bar() => "baz";
        }

        public ClassWithRecordsEmptyPrimaryConstructor()
        {
            new First().Bar();
            new Second().Bar();
        }
    }

    public class ClassWithAbstractRecords
    {
        public abstract record FirstAuditData()
        {
            public abstract string GetAuditType();
        }

        public abstract record SecondAuditData
        {
            private protected SecondAuditData()
            {

            }

            public abstract string GetAuditType();
        }

        public record ConcreteFirstAuditData : FirstAuditData
        {
            public override string GetAuditType()
            {
                return string.Empty;
            }
        }

        public record ConcreteSecondAuditData : SecondAuditData
        {
            public override string GetAuditType()
            {
                return string.Empty;
            }
        }

        public ClassWithAbstractRecords()
        {
            new ConcreteFirstAuditData().GetAuditType();
            new ConcreteSecondAuditData().GetAuditType();
        }
    }
}

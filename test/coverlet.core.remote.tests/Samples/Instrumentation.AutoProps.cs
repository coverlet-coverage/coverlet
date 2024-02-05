using System;

namespace coverlet.core.remote.samples.tests
{
    public class AutoProps
    {
        private int _myVal = 0;
        public AutoProps()
        {
            _myVal = new Random().Next();
        }
        public int AutoPropsNonInit { get; set; }
        public int AutoPropsInit { get; set; } = 10;
    }

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

    public class ClassWithInheritingRecordsAndAutoProperties
    {
        record BaseRecord(int A);

        record InheritedRecord(int A) : BaseRecord(A);

        public ClassWithInheritingRecordsAndAutoProperties()
        {
            var record = new InheritedRecord(1);
        }
    }


}

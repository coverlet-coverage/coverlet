using System;

namespace Coverlet.Core.CoverageSamples.Tests
{
  public class ClassWithAutoProps
  {
    private int _myVal = 0;
    public ClassWithAutoProps()
    {
      _myVal = new Random().Next();
    }
    public int AutoPropsNonInit { get; set; }
    public int AutoPropsInit { get; set; } = 10;
    public int AutoPropsInitKeyword { get; init; }
  }

    public class ClassWithAutoPropsPrimaryConstructor
    {
        public ClassWithAutoPropsPrimaryConstructor()
        {
            var instance = new InnerClassWithAutoProps(1)
            {
                AutoPropsNonInit = 20,
                AutoPropsInit = 30,
                AutoPropsInitKeyword = 33
            };
        }
        private class InnerClassWithAutoProps(int myVal = 10)
        {
            public int AutoPropsNonInit { get; set; }
            public int AutoPropsInit { get; set; } = myVal;
            public int AutoPropsInitKeyword { get; init; }
        }
    }

    public record RecordWithAutoProps
  {
      private int _myVal = 0;
      public RecordWithAutoProps()
      {
          _myVal = new Random().Next();
      }
      public int AutoPropsNonInit { get; set; }
      public int AutoPropsInit { get; set; } = 10;
      public int AutoPropsInitKeyword { get; init; }
  }

    public class RecordsWithPrimaryConstructor
    {
        public record RecordWithAutoPropsPrimaryConstructor(int AutoPropsNonInit, int AutoPropsInit = 10);

        public record RecordWithAutoPropsPrimaryConstructorMultiline(
            int AutoPropsNonInit,
            int AutoPropsInit = 10
        );

        public RecordsWithPrimaryConstructor()
        {
            var primaryConstructor = new RecordWithAutoPropsPrimaryConstructor(20, 30);
            var primaryConstructorMultiline = new RecordWithAutoPropsPrimaryConstructorMultiline(20);
        }
    }
}

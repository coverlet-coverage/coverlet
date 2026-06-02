using System;

namespace Coverlet.Core.CoverageSamples.Tests
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

  // Samples for issue 1633: records without primary constructor show no coverage
  public class ClassWithRecordsNoPrimaryConstructor
  {
    // Record without primary constructor parentheses — previously showed no coverage at all
    record RecordNoCtor
    {
      public string Bar() => "baz";
    }

    // Record with empty primary constructor parentheses — workaround that users found
    record RecordEmptyCtor()
    {
      public string Bar() => "baz";
    }

    public ClassWithRecordsNoPrimaryConstructor()
    {
      new RecordNoCtor().Bar();
      new RecordEmptyCtor().Bar();
    }
  }

  public class ClassWithAbstractRecordsNoPrimaryConstructor
  {
    public abstract record AbstractBase
    {
      public abstract string GetValue();
    }

    public abstract record AbstractBaseWithCtor()
    {
      public abstract string GetValue();
    }

    public record ConcreteFromBase : AbstractBase
    {
      public override string GetValue() => "concrete";
    }

    public record ConcreteFromBaseCtor : AbstractBaseWithCtor
    {
      public override string GetValue() => "concrete-ctor";
    }

    public ClassWithAbstractRecordsNoPrimaryConstructor()
    {
      new ConcreteFromBase().GetValue();
      new ConcreteFromBaseCtor().GetValue();
    }
  }

}
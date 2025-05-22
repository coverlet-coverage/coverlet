using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace coverlet.testsubject
{
  class Program
  {
    static void Main(string[] args)
    {
      Console.WriteLine("Execute BigClass");
      var big = new BigClass();
      for (var i = 0; i < 1000; i++)
        big.Do(i);
    }
  }
}

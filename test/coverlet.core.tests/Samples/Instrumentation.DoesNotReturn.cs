namespace Coverlet.Core.Samples.Tests
{
    public class DoesNotReturn
    {
        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public int Throws()
        {
            throw new System.Exception();
        }

        public void NoBranches()
        {
            System.Console.WriteLine("Before");
            Throws();
            System.Console.WriteLine("After");  // unreachable
        }                                       // unreachable

        public void If()
        {
            System.Console.WriteLine("In-After");

            if (System.Console.ReadKey().KeyChar == 'Y')
            {
                System.Console.WriteLine("In-Before");
                Throws();
                System.Console.WriteLine("In-After");   // unreachable
            }                                           // unreachable

            System.Console.WriteLine("Out-After");
        }

        public void Switch()
        {
            System.Console.WriteLine("Out-Before");

            switch (System.Console.ReadKey().KeyChar)
            {
                case 'A':
                    System.Console.WriteLine("In-Before");
                    Throws();
                    System.Console.WriteLine("In-After");   // should be unreachable
                    break;                                  // should be unreachable
                case 'B':
                    System.Console.WriteLine("In-Constant-1");
                    break;

                // need a number of additional, in order, branches to get a Switch generated
                case 'C':
                    System.Console.WriteLine("In-Constant-2");
                    break;
                case 'D':
                    System.Console.WriteLine("In-Constant-3");
                    break;
                case 'E':
                    System.Console.WriteLine("In-Constant-4");
                    break;
                case 'F':
                    System.Console.WriteLine("In-Constant-5");
                    break;
                case 'G':
                    System.Console.WriteLine("In-Constant-6");
                    break;
                case 'H':
                    System.Console.WriteLine("In-Constant-7");
                    break;
            }

            System.Console.WriteLine("Out-After");
        }

        public void Subtle()
        {
            var key = System.Console.ReadKey();

            switch (key.KeyChar)
            {
                case 'A':
                    Throws();
                    System.Console.WriteLine("In-Constant-1");  // unreachable
                    goto case 'B';                              // unreachable
                case 'B':
                    System.Console.WriteLine("In-Constant-2");
                    break;

                case 'C':
                    System.Console.WriteLine("In-Constant-3");
                    Throws();
                    goto alwayUnreachable;                      // unreachable

                case 'D':
                    System.Console.WriteLine("In-Constant-4");
                    goto subtlyReachable;
            }

            Throws();
            System.Console.WriteLine("Out-Constant-1");         // unreachable

        alwayUnreachable:                                       // unreachable
            System.Console.WriteLine("Out-Constant-2");         // unreachable

        subtlyReachable:
            System.Console.WriteLine("Out-Constant-3");
        }

        public void UnreachableBranch()
        {
            var key = System.Console.ReadKey();
            Throws();

            if (key.KeyChar == 'A')                             // unreachable
            {                                                   // unreachable
                System.Console.WriteLine("Constant-1");         // unreachable
            }                                                   // unreachable
        }                                                       // unreachable

        [System.Diagnostics.CodeAnalysis.DoesNotReturn]
        public void ThrowsGeneric<T>()
        {
            throw new System.Exception(typeof(T).Name);
        }


        public void CallsGenericMethodDoesNotReturn()
        {
            System.Console.WriteLine("Constant-1");
            ThrowsGeneric<string>();
            System.Console.WriteLine("Constant-2");         // unreachable
        }

        private class GenericClass<T>
        {
            [System.Diagnostics.CodeAnalysis.DoesNotReturn]
            public static void AlsoThrows()
            {
                throw new System.Exception(typeof(T).Name);
            }
        }

        public void CallsGenericClassDoesNotReturn()
        {
            System.Console.WriteLine("Constant-1");
            GenericClass<int>.AlsoThrows();
            System.Console.WriteLine("Constant-2");         // unreachable
        }

        public void WithLeave()
        {
            try
            {
                System.Console.WriteLine("Constant-1");
            }
            catch (System.Exception e)
            {
                if (e is System.InvalidOperationException)
                {
                    goto endOfMethod;
                }

                System.Console.WriteLine("InCatch-1");

                Throws();

                System.Console.WriteLine("InCatch-2");      // unreachable
            }                                               // unreachable

        endOfMethod:
            System.Console.WriteLine("Constant-2");
        }

        public void FiltersAndFinallies()
        {
            try
            {
                System.Console.WriteLine("Constant-1");
                Throws();
                System.Console.WriteLine("Constant-2");     //unreachable
            }                                               //unreachable
            catch (System.InvalidOperationException e) 
                when (e.Message != null)
            {
                System.Console.WriteLine("InCatch-1");
                Throws();
                System.Console.WriteLine("InCatch-2");      //unreachable
            }                                               //unreachable
            catch (System.InvalidOperationException)
            {
                System.Console.WriteLine("InCatch-3");
                Throws();
                System.Console.WriteLine("InCatch-4");      //unreachable
            }                                               //unreachable
            finally
            {
                System.Console.WriteLine("InFinally-1");
                Throws();
                System.Console.WriteLine("InFinally-2");    //unreachable
            }                                               //unreachable
        }                                                   //unreachable
    }
}

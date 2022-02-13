// Remember to use full name because adding new using directives change line numbers

namespace Coverlet.Core.Samples.Tests
{
    public class SelectionStatements
    {
        public int If(bool condition)
        {
            if (condition)
            {
                return 1;
            }
            else
            {
                return 0;
            }
        }

        public int Switch(int caseSwitch)
        {
            switch (caseSwitch)
            {
                case 1:
                    return 1;
                case 2:
                    return 2;
                default:
                    return 0;
            }
        }

        public string SwitchCsharp8(object value) =>
        value
        switch
        {
            int i => i.ToString(System.Globalization.CultureInfo.InvariantCulture),
            uint ui => ui.ToString(System.Globalization.CultureInfo.InvariantCulture),
            short s => s.ToString(System.Globalization.CultureInfo.InvariantCulture),
            _ => throw new System.NotSupportedException()
        };
    }
}

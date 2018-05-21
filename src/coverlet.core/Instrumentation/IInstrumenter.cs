namespace Coverlet.Core.Instrumentation
{
    internal interface IInstrumenter
    {
        bool CanInstrument();

        InstrumenterResult Instrument();
    }
}
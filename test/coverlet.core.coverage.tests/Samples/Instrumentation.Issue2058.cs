namespace Coverlet.Core.CoverageSamples.Tests
{
  public sealed class Issue2058
  {
    private readonly object _gate = new object();
    private int _n;

    public void WhileTrue_IfThenTryFinally()
    {
      while (true)
      {
        if (_n > 0)
        {
          _n--;
        }

        try
        {
          _n++;
        }
        finally
        {
          _n++;
        }
      }
    }

    public void WhileTrue_TryFinallyIfTryFinally()
    {
      while (true)
      {
        try
        {
          _n++;
        }
        finally
        {
          _n++;
        }

        if (_n > 0)
        {
          _n--;
        }

        try
        {
          _n++;
        }
        finally
        {
          _n++;
        }
      }
    }

    public void WhileTrue_LockIfLock()
    {
      while (true)
      {
        lock (_gate)
        {
          _n++;
        }

        if (_n > 0)
        {
          _n--;
        }

        lock (_gate)
        {
          _n++;
        }
      }
    }

    public void TryFinally_DoWhileBackEdge()
    {
      try
      {
        do
        {
          Tick();
        }
        while (Continue());
      }
      finally
      {
        Cleanup();
      }
    }

    private void Tick()
    {
      _n++;
    }

    private bool Continue()
    {
      return _n >= 0;
    }

    private void Cleanup()
    {
      _n++;
    }
  }
}

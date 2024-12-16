// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Windows;
using System.Windows.Controls;

namespace coverlet.tests.projectsample.wpf
{
  public class TestClass
  {
    public UserControl? Control { get; set; }
  }

  public class issue_1713
  {
    public MessageBoxButton Method_1713(MessageBoxButton button = MessageBoxButton.OK)
    {
      return button;
    }
  }
}

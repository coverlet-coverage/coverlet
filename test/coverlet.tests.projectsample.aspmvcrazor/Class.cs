// Copyright (c) Toni Solarin-Sodara
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Mvc.Razor;

namespace coverlet.tests.projectsample.aspmvcrazor
{
  public static class Class
  {
    public static IMvcBuilder AddLocalization(this IMvcBuilder mvcBuilder,
        LanguageViewLocationExpanderFormat viewLocationExpanderFormat = LanguageViewLocationExpanderFormat.Suffix)
    {
      return mvcBuilder;
    }
  }
}

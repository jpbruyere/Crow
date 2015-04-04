
using System;
using Mono.Addins;
using Mono.Addins.Description;

[assembly:Addin ("GOLib", 
        Namespace = "MonoDevelop",
        Version = MonoDevelop.BuildInfo.Version,
        Category = "IDE extensions")]

[assembly:AddinName ("MonoDevelop GOLib interface designer")]
[assembly:AddinDescription ("")]

[assembly:AddinDependency ("Core", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Ide", MonoDevelop.BuildInfo.Version)]
[assembly:AddinDependency ("Debugger", MonoDevelop.BuildInfo.Version)]

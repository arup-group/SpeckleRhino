﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.PlugIns;
using Grasshopper.Kernel;


[assembly: PlugInDescription( DescriptionType.Email, "hello@speckle.works" )]
[assembly: PlugInDescription( DescriptionType.Organization, "Speckle Works, an Open Source Project." )]
[assembly: PlugInDescription( DescriptionType.WebSite, "https://speckle.works" )]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Speckle Grasshopper (CX)")]
[assembly: AssemblyDescription("The Speckle Grasshopper plugin, community extensions edition.")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Speckle")]
[assembly: AssemblyProduct("Speckle")]
[assembly: AssemblyCopyright("Copyright Speckle.Works & Community Contributors © 2016-2020")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Force directly loading because some SpeckleKit elements do not appear if we load via COFF byte arrays
[assembly: GH_Loading(GH_LoadingDemand.ForceDirect)]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("82ec14d9-01c7-46c7-8600-db75f871740d")] // This will also be the Guid of the Rhino plug-in

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
// NOTE: these version numbers are ovewritten in the CI/CD process
[assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyFileVersion("1.0.0.0")]

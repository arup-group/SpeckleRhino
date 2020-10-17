﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Rhino.PlugIns;

// Plug-in Description Attributes - all of these are optional
// These will show in Rhino's option dialog, in the tab Plug-in
[assembly: PlugInDescription(DescriptionType.Email, "hello@speckle.works")]
[assembly: PlugInDescription(DescriptionType.Organization, "Speckle Works, an Open Source Project.")]
[assembly: PlugInDescription(DescriptionType.WebSite, "https://speckle.works")]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle( "Speckle Rhino" )]
[assembly: AssemblyDescription( "The Speckle Rhino plugin." )]
[assembly: AssemblyConfiguration( "" )]
[assembly: AssemblyCompany( "Speckle.Works" )]
[assembly: AssemblyProduct( "Speckle" )]
[assembly: AssemblyCopyright( "Copyright Speckle.Works © 2016-2019" )]
[assembly: AssemblyTrademark( "" )]
[assembly: AssemblyCulture( "" )]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("512D9705-6F92-49CA-A606-D6D5C1AC6AA2")] // This will also be the Guid of the Rhino plug-in

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
// NOTE: these version numbers are to be ovewritten in the CI/CD process
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]

// Make compatible with Rhino Installer Engine
[assembly: AssemblyInformationalVersion("2")]

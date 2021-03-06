﻿//extern alias SpeckleNewtonsoft;
using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;
//using Newtonsoft.Json;
using Rhino.Collections;
using Grasshopper.Kernel.Types;
using System.Windows.Forms;
using System.IO;
using Grasshopper.Kernel.Parameters;
using SpeckleCore;
using System.Linq;

namespace SpeckleGrasshopper
{
  public class GetSpeckleObjectApplicationIdComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public GetSpeckleObjectApplicationIdComponent()
      : base("Get ApplicationId", "GAID",
          "Gets a speckle object and returns the ApplicationId.",
          "Speckle", "Advanced")
    {
    }
    public override GH_Exposure Exposure => GH_Exposure.hidden;

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleObjectParameter(), "Speckle Object", "SO", "The Speckle Object whose ApplicationId you want", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("ApplicationID", "AID", "The input object's Application ID.", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      GH_SpeckleObject GHspeckleObject = null;
      if (!DA.GetData(0, ref GHspeckleObject))
        return;

      var speckleObject = GHspeckleObject.Value;

      string key = "ApplicationId";

      try
      {
        var output = speckleObject.GetType().GetProperty(key).GetValue(speckleObject, null);
        DA.SetData(0, output);
      } 
      catch (System.NullReferenceException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Could not find a parameter with that key on the input object");
      }

    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.GetApplicationID;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("{D5F6EC27-E2D3-466F-AEE8-BF9DEDE51B24}"); }
    }
  }
}

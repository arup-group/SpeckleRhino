//extern alias SpeckleNewtonsoft;
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
  public class QuerySpeckleObjectComponent : GH_Component
  {
    HashSet<string> properties;
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public QuerySpeckleObjectComponent()
      : base("Query Speckle Object test!!!!!!!!", "GNV",
          "Gets a value from a dictionary by string of concatenated keys. \n For example, 'prop.subprop.subsubprop'.",
          "Speckle", "Special")
    {
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddParameter(new SpeckleObjectParameter(), "Speckle Object", "SO", "The Speckle Object you want to query", GH_ParamAccess.item);
      pManager.AddTextParameter("Path", "P", "Path of desired property, separated by dots.\nExample:'turtle.smallerTurtle.microTurtle'", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Output", "O", "Output value.", GH_ParamAccess.list);
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

      string key = null;
      if (!DA.GetData(1, ref key))
        return;

      var propertyDict = speckleObject.Properties;

      if (propertyDict.ContainsKey(key))
      {
        DA.SetData(0, propertyDict[key]);
        return;
      }
      else
      {
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


    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.json;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("{5AA1F56B-3174-4A4F-AB79-4B39E21D2BD5}"); }
    }
  }
}

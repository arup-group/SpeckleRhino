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
      : base("Query Speckle Object", "QSO",
          "Gets a value from a dictionary by string of concatenated keys. \n For example, 'prop.subprop.subsubprop'.",
          "Speckle", "Special")
    {
    }

    /*the graft option needs to be set in a DataMapping property of the GH_Param when being added to the component.*/

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
      pManager[0].DataMapping = GH_DataMapping.Graft;
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

      /// Split user input path into pieces
      string[] keySplit = key.Split('.');
      var propertyDict = speckleObject.Properties;

      /*try
      {*/
        var output = GetNestedProp(speckleObject, keySplit, 0);
        DA.SetData(0, output);
      /*}
      catch (System.NullReferenceException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not find a parameter with the key, " + key + ", on the input object");
      }
      catch (System.Collections.Generic.KeyNotFoundException e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not find a parameter with the key, " + key + ", on the input object");
      }*/
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

    private object GetNestedProp(object property, string[] keySplit, int keyIndex)
    {
      try
      {
        object subProperty;
        if (property is IDictionary<string, object> dictProperty)
        {
          subProperty = dictProperty[keySplit[keyIndex]];
        }
        else
        {
          subProperty = property.GetType().GetProperty(keySplit[keyIndex]).GetValue(property, null);
        }
        if (keyIndex == keySplit.Length - 1)
        {
          return subProperty;
        }
        else
        {
          var newKeyIndex = keyIndex + 1;
          return GetNestedProp(subProperty, keySplit, newKeyIndex);
        }
      }
      catch (Exception e) when (e is System.NullReferenceException || e is System.Collections.Generic.KeyNotFoundException)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Could not find a parameter with the key, " + keySplit[keyIndex] + ", on the input object");
        return null;
      }
    }

  }


}



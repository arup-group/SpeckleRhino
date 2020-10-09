using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using System.Windows.Forms;

namespace SpeckleGrasshopper
{
  public class QuerySpeckleObjectComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the MyComponent1 class.
    /// </summary>
    public QuerySpeckleObjectComponent()
      : base("Query Speckle Object", "QSO",
          "Gets a value from a dictionary by string of concatenated keys. \n For example, 'prop.subprop.subsubprop'.",
          "Speckle", " Properties")
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
      pManager[0].DataMapping = GH_DataMapping.Graft;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Output", "O", "Output value.", GH_ParamAccess.item);
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

      var output = GetNestedProp(speckleObject, keySplit, 0);
      DA.SetData(0, output);
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
        // A holder array to hold the elements that have been visited
        List<string> temp = new List<string>();

        // Make sure length > 0 first before trying to access element 0
        if (keySplit.Length > 0)
        {
          // for each element in our array up to the currentIndex, add that element at that index to a temp list
          for (int i = 0; i <= keyIndex; i++)
          {
            temp.Add(keySplit[i]);
          }
        }
        var result = String.Join(".", temp.ToArray());
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Could not find parameter with key: '{result}'.");
        return null;
      }
    }

  }


}



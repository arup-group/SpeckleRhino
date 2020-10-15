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

//First Pass on Iteration 0
      if (DA.Iteration == 0)
      {
        //Get All the Paths, even if on a tree
        var allData = Params.Input.OfType<Param_String>()
               .First()
               .VolatileData.AllData(true)
               .OfType<GH_String>()
               .Select(s => s.Value);
        if (!allData.Any())
        {
          return;
        }
        properties = new HashSet<string>();
        foreach (var p in allData)
        {
          properties.Add(p);
        }
      }

      if (OutputMismatch() && DA.Iteration == 0)
      {
        OnPingDocument().ScheduleSolution(5, d =>
        {
          AutoCreateOutputs(false);
        });
      }
      else if (!OutputMismatch())
      {
        int o = 0;
        foreach (var p in properties)
        {

          var temp = dict;
          var keys = p.Split('.');
          object target = null;

          for (int i = 0; i < keys.Length; i++)
          {
            if (i == keys.Length - 1)
              if (temp.ContainsKey(keys[i]))
              {
                target = temp[keys[i]];
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter {o + 1} is missing data at [{i}]{keys[i]}");
                break;
              }
            else
            {
              if (temp.ContainsKey(keys[i]))
              {
                var t = temp[keys[i]];
                if (t is Dictionary<string, object> d)
                  temp = d;
                else if (t is SpeckleObject speckleObject)
                  temp = speckleObject.Properties;
              }
              else
              {
                AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, $"Parameter {o + 1} is missing data at {keys[i]}");
                break;
              }
            }
          }

          if (target is List<object> myList)
          {
            DA.SetDataList(o, myList);
          }
          else if (target is object)
          {
            DA.SetDataList(o, new List<object> { target });
          }
          o++;
        }
      }

    }
    private void AutoCreateOutputs(bool recompute)
    {

      var tokenCount = properties.Count();
      if (tokenCount == 0) return;

      if (OutputMismatch())
      {
        RecordUndoEvent("Creating Outputs");
        if (Params.Output.Count < tokenCount)
        {
          while (Params.Output.Count < tokenCount)
          {
            var new_param = CreateParameter(GH_ParameterSide.Output, Params.Output.Count);
            Params.RegisterOutputParam(new_param);
          }
        }
        else if (Params.Output.Count > tokenCount)
        {
          while (Params.Output.Count > tokenCount)
          {
            Params.UnregisterOutputParameter(Params.Output[Params.Output.Count - 1]);
          }
        }
        Params.OnParametersChanged();
        VariableParameterMaintenance();
        ExpireSolution(recompute);
      }
    }

    private bool OutputMismatch()
    {
      var countMatch = properties.Count() == Params.Output.Count;
      if (!countMatch) return true;

      var list = properties.ToList();
      for (int i = 0; i < properties.Count; i++)
      {
        if (!(Params.Output[i].NickName == list[i]))
        {
          return true;
        }
      }

      return false;
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return false;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      return new Param_GenericObject();
    }

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



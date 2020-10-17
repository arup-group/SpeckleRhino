﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Data;
using Grasshopper.Kernel.Parameters;
using Grasshopper.Kernel.Types;
using SpeckleCore;

namespace SpeckleGrasshopper
{
  //public class ExpandSpeckleObject : GH_Component { }

  public class CreateSpeckleObject : GH_Component, IGH_VariableParameterComponent
  {
    private Timer Debouncer;

    public CreateSpeckleObject()
      : base("Create a Generic Speckle Object", "CGO",
          "Creates a Generic speckle object with custom properties. The properties you define are stored in the Speckle Object's 'Properties' dictionary.",
          "Speckle", "  Create Objects")
    {
      this.Params.ParameterNickNameChanged += Params_ParameterNickNameChanged;
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      Debouncer = new Timer(2000); Debouncer.AutoReset = false;
      Debouncer.Elapsed += (sender, e) =>
      {
        Rhino.RhinoApp.MainApplicationWindow.Invoke((Action)delegate { this.ExpireSolution(true); });
      };

      foreach (var param in Params.Input)
      {
        param.ObjectChanged += (sender, e) =>
        {
          Debouncer.Start();
        };
      }
    }

    private void Params_ParameterNickNameChanged(object sender, GH_ParamServerEventArgs e)
    {
      ExpireSolution(true);
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("{7b758641-d781-4e15-bfd8-b6c723c9dd28}"); }
    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.CreateUserData;
      }
    }

    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      if (side == GH_ParameterSide.Input && Params.Input.Count > 1)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      var param = new Param_GenericAccess();

      param.Name = GH_ComponentParamServer.InventUniqueNickname("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input);
      param.NickName = param.Name;
      param.Description = "Property Name";
      param.Optional = true;
      param.Access = GH_ParamAccess.item;

      param.ObjectChanged += (sender, e) => Debouncer.Start();

      return param;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return true;
    }

    public void VariableParameterMaintenance()
    {
      //ExpireSolution( true );
    }

    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddParameter(new Param_GenericAccess(), "A", "A", "Data.", GH_ParamAccess.item);
    }

    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Speckle Object", "SO", "The newly created speckle object.", GH_ParamAccess.item);
      pManager.AddGenericParameter("Dictionary", "D", "Just the dictionary.", GH_ParamAccess.item);
    }

    protected override void SolveInstance(IGH_DataAccess DA)
    {

      var check = ValidateKeys();
      if (check.Item1)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, check.Item2);
        return;
      }

      var myDictionary = new Dictionary<string, object>();

      for (int i = 0; i < Params.Input.Count; i++)
      {
        var key = Params.Input[i].NickName;

        dynamic ghInputProperty = null;
        object valueExtract = null;
        switch (Params.Input[i].Access)
        {
          case GH_ParamAccess.item:
            DA.GetData(i, ref ghInputProperty);
            valueExtract = ghInputProperty?.Value;
            break;
          case GH_ParamAccess.list:
            var dataValues = new List<dynamic>();
            DA.GetDataList(i, dataValues);
            valueExtract = dataValues.Select(x => x.Value).ToList();
            break;
          case GH_ParamAccess.tree:
            var tree = new GH_Structure<IGH_Goo>();
            DA.GetDataTree(i, out tree);
            var dict = new Dictionary<string, IEnumerable<object>>();

            for (int j = 0; j < tree.PathCount; j++)
            {
              var branch = tree.Branches[j];
              var list = new List<object>();
              for (int k = 0; k < branch.Count; k++)
              {
                list.Add(branch[k].GetType().GetProperty("Value").GetValue(branch[k], null));
              }
              dict.Add(j.ToString(), list);
            }

            myDictionary.Add(key, dict);
            continue;
          default:
            continue;
        }

        try
        {
          var tests = valueExtract is IEnumerable<object>;
          if (valueExtract is IEnumerable<object>)
          {
            var val = valueExtract as List<object>;
            //Is this important? It replaced IEnumerable<int> 
            if(val.Count > 0 && val[0] is int || val[0] is double || val[0] is string || val[1] is bool)
            {
              myDictionary.Add(key, val);
            }
            else
            {
              myDictionary.Add(key, Converter.Serialise(val));
            }
          }
          else if (valueExtract is System.Collections.IDictionary)
          {
            myDictionary.Add(key, valueExtract);
          }
          else
          {
            if (valueExtract is bool || valueExtract is string || valueExtract is double || valueExtract is int)
              myDictionary.Add(key, valueExtract);
            else
              myDictionary.Add(key, Converter.Serialise(valueExtract));
          }
        }
        catch (Exception)
        {
          continue;
        }
      }
      var test = myDictionary;
      var myObject = new SpeckleObject() { Properties = myDictionary };
      myObject.GenerateHash();

      DA.SetData(0, myObject);
      DA.SetData(1, myDictionary);
      //new GH_SpeckleObject()
    }

    public Tuple<bool, string> ValidateKeys()
    {
      List<string> keyNames = new List<string>();
      bool hasErrors = false;
      string validationErrors = "";
      for (int i = 0; i < Params.Input.Count; i++)
      {
        var param = Params.Input[i];
        if (keyNames.Contains(param.NickName))
        {
          this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Duplicate  key names found (" + param.NickName + "). Please use different values.");

          validationErrors += "Duplicate  key names found (" + param.NickName + "). Please use different values.\n";

          hasErrors = true;
        }

        if (param.NickName == "type" || param.NickName == "Type")
        {
          this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Using 'Type' or 'type' as a key name is not possible. Please use different name, for example 'familiyType'. Thanks!");

          validationErrors += "Using 'Type' or 'type' as a key name is not possible. Please use different name, for example 'familiyType'. Thanks!";

          hasErrors = true;
        }

        if (param.NickName.Contains("."))
        {
          this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Dots in key names are not supported. Sorry!");

          validationErrors += "Dots in key names are not supported. Sorry!";
        }

        keyNames.Add(param.NickName);
      }

      return new Tuple<bool, string>(hasErrors, validationErrors);
    }
  }
}

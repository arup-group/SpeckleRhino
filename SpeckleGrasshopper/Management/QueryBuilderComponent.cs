using System;
using System.Collections.Generic;

using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SpeckleGrasshopper.Management
{
  public class QueryBuilderComponent : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the QueryBuilderComponent class.
    /// </summary>
    public QueryBuilderComponent()
      : base("Query Builder", "QB",
          "Build a query string for use on the receiver component",
          "Speckle", "Management")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("Types", "T", "Type of objects you want", GH_ParamAccess.list);
      pManager[0].Optional = true;
      pManager.AddTextParameter("Layers", "L", "Layers you want to get", GH_ParamAccess.list);
      pManager[1].Optional = true;
      pManager.AddTextParameter("Fields", "F", "Fields you want to get", GH_ParamAccess.list);
      pManager[2].Optional = true;
      pManager.AddTextParameter("Conditions", "C", "Conditions, for example: limit=10, offset=10. Do not provide commas but you need to provide the equals symbol.", GH_ParamAccess.list);
      pManager[3].Optional = true;
      pManager.AddTextParameter("Sort", "S", "Asceding or desceding of certain fields. Use minus for descending. Example: name, -age. Do not provide commas, just the fields with or without minus", GH_ParamAccess.list);
      pManager[4].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("Query", "Q", "The query string", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      var types = new List<string>();
      DA.GetDataList("Types", types);
      var Layers = new List<string>();
      DA.GetDataList("Layers", Layers);
      var Fields = new List<string>();
      DA.GetDataList("Fields", Fields);
      var Conditions = new List<string>();
      DA.GetDataList("Conditions", Conditions);
      var Sort = new List<string>();
      DA.GetDataList("Sort", Sort);

      string query = "";

      if(types.Count > 0)
      {
        query += $"type={string.Join(",", types)}";
      }

      if (Fields.Count > 0)
      {
        query += $"&fields={string.Join(",", Fields)}";
      }

      if(Conditions.Count > 0)
      {
        foreach (var condition in Conditions)
        {
          query += $"&{condition}";
        }
      }

      if (Sort.Count > 0)
      {
        query += $"&sort={string.Join(",", Sort)}";
      }

      // Add this in the end with a Questionmark so it will not be part of the Mongo query.
      // We will manage it on the client side
      if (Layers.Count > 0)
      {
        query += $"?layers={string.Join(",", Layers)}";
      }

      DA.SetData(0, query);

    }

    /// <summary>
    /// Provides an Icon for the component.
    /// </summary>
    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        //You can add image files to your project resources and access them like this:
        // return Resources.IconForThisComponent;
        return null;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("c27f2bc0-e333-4a4c-af19-cb03714ecea9"); }
    }
  }
}

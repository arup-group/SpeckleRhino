using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;

namespace SpeckleGrasshopper.ExtendedComponents
{
  public class GlobalRhinoComputeComponent : GH_Component
  {
    public bool ProvideAccount { get; private set; } = false;
    public const string guidString = "ba63f5a3-900f-4319-b892-96afc89ca3a1";

    /// <summary>
    /// Initializes a new instance of the GlobalRhinoComputeComponent class.
    /// </summary>
    public GlobalRhinoComputeComponent()
      : base("Rhino.Compute mode", "R.C M",
          "Toggle Rhino.Compute mode for Speckle",
          "Speckle", "Advanced")
    {
      this.IconDisplayMode = GH_IconDisplayMode.name;
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      if (document.Objects.Where(x => x.ComponentGuid == ComponentGuid).Count() > 1)
      {
        document.RemoveObject(Attributes, false);
      }
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      //pManager.AddGenericParameter("Update", "U", "Update this component", GH_ParamAccess.tree);
      //pManager[0].Optional = true;
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      Menu_AppendSeparator(menu);
      Menu_AppendItem(menu, "Toggle Rhino.Compute mode", OnAccountGlobal, true, ProvideAccount);
    }

    private void OnAccountGlobal(object sender, EventArgs e)
    {
      Toggle();
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      DisplayStatus();
    }

    private void DisplayStatus()
    {
      var name = $"Rhino.Compute Mode: {ProvideAccount}";
      base.NickName = name;
    }

    public override bool Write(GH_IWriter writer)
    {
      writer.SetBoolean("RhinoComputeMode", ProvideAccount);
      return base.Write(writer);
    }

    public void Toggle()
    {
      ProvideAccount = !ProvideAccount;
      DisplayStatus();

      Grasshopper.Instances.ActiveCanvas.Document.ExpireSolution();
      Grasshopper.Instances.ActiveCanvas.Document.ScheduleSolution(200);

    }

    public override bool Read(GH_IReader reader)
    {
      ProvideAccount = reader.GetBoolean("RhinoComputeMode");

      return base.Read(reader);
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
      get { return new Guid(guidString); }
    }

    public static GlobalRhinoComputeComponent GetFromDocument(GH_Document document)
    {
      var obj = document.Objects.Where(x => x.ComponentGuid.ToString() == GlobalRhinoComputeComponent.guidString).FirstOrDefault();
      return obj as GlobalRhinoComputeComponent;
    }

    public static bool RhinoComputeOn(GH_Document document)
    {
      var obj = GetFromDocument(document);
      return obj != null && obj.ProvideAccount;
    }

  }
}

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCore;
using SpeckleGrasshopper.Properties;

namespace SpeckleGrasshopper.Management
{
  public class CreateAccount : GH_Component
  {
    /// <summary>
    /// Initializes a new instance of the CreateAccount class.
    /// </summary>
    public CreateAccount()
      : base("CreateAccount", "CA",
          "Create an Account object for speckle, this will not create the account online but will help you specify what account you want to use when uploading or downloading data from the senders / receivers",
          "Speckle", "Advanced")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("RestApi", "R", "The address of the API, ex. https://hestia.speckle.works/api", GH_ParamAccess.item);
      pManager.AddTextParameter("Token", "T", "The token used to authenticate you, you can find it in your Profile page", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Account object to use with receivers", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      string restApi = "";
      if (!DA.GetData(0, ref restApi))
        return;
      string token = "";
      if (!DA.GetData(1, ref token))
        return;

      DA.SetData(0, new Account()
      {
        RestApi = restApi,
        Token = token,
      });
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
        return Resources.AddAccount;
      }
    }

    /// <summary>
    /// Gets the unique ID for this component. Do not change this ID after release.
    /// </summary>
    public override Guid ComponentGuid
    {
      get { return new Guid("3033cba4-c3c2-4c1e-828b-4f12112a6d42"); }
    }
  }
}

using System;
using System.Collections.Generic;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCore;

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
          "Speckle", "Management")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_InputParamManager pManager)
    {
      pManager.AddIntegerParameter("AccountId", "id", "AccountId", GH_ParamAccess.item);
      pManager.AddTextParameter("ServerName", "S", "ServerName", GH_ParamAccess.item);
      pManager.AddTextParameter("RestApi", "R", "RestApi", GH_ParamAccess.item);
      pManager.AddTextParameter("Email", "E", "Email", GH_ParamAccess.item);
      pManager.AddTextParameter("Token", "T", "Token", GH_ParamAccess.item);
      pManager.AddBooleanParameter("IsDefault", "D", "IsDefault", GH_ParamAccess.item);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Account object to use with senders / receivers", GH_ParamAccess.item);
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      int accountId = 0;
      if (!DA.GetData(0, ref accountId))
        return;
      string serverName = "";
      if (!DA.GetData(1, ref serverName))
        return;
      string restApi = "";
      if (!DA.GetData(2, ref restApi))
        return;
      string email = "";
      if (!DA.GetData(3, ref email))
        return;
      string token = "";
      if (!DA.GetData(4, ref token))
        return;
      bool isDefault = false;
      if (!DA.GetData(5, ref isDefault))
        return;

      DA.SetData(0, new Account()
      {
        AccountId = accountId,
        ServerName = serverName,
        RestApi = restApi,
        Email = email,
        Token = token,
        IsDefault = isDefault,
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
        return null;
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

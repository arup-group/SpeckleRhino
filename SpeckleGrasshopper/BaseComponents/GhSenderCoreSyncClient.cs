using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Grasshopper.Kernel;
using Grasshopper.Kernel.Parameters;
using Rhino.Geometry;
using SpeckleCore;
using SpeckleGrasshopper.Utilities;

namespace SpeckleGrasshopper.BaseComponents
{
  public class GhSenderCoreSyncClient : GH_TaskCapableComponent<string>, IGH_VariableParameterComponent
  {

    /// <summary>
    /// Initializes a new instance of the TaskCapableMultiThread class.
    /// </summary>
    /// 
    CancellationTokenSource source;

    /// <summary>
    /// Initializes a new instance of the GhSenderCoreSyncClient class.
    /// </summary>
    public GhSenderCoreSyncClient()
      : base("Data Sender Sync", "DSS",
          "Sends data from Speckle Synchronously. Use this instead of the main component when you want to be sure that the data have been send before triggering anything downstream.",
          "Speckle", "Beta")
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Account to get stream from", GH_ParamAccess.item);
      pManager.AddGenericParameter("Stream Id", "S", "Specify the stream Id to use", GH_ParamAccess.item);
      pManager.AddGenericParameter("Project", "Pr", "Optional input for either a Project type (get it from ListMyProject component) or a string for the ProjectId", GH_ParamAccess.item);
      pManager[2].Optional = true;
      // Classic Data
      pManager.AddGenericParameter("A", "A", "A is for Apple", GH_ParamAccess.tree);
      pManager[3].Optional = true;
      pManager.AddGenericParameter("B", "B", "B is for Book", GH_ParamAccess.tree);
      pManager[4].Optional = true;
      pManager.AddGenericParameter("C", "C", "C is for Car", GH_ParamAccess.tree);
      pManager[5].Optional = true;
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddTextParameter("log", "L", "Log data.", GH_ParamAccess.item);
    }

    public override void CreateAttributes()
    {
      // Use this extracting an interface because omg
      //m_attributes = new GhSenderClientAttributes(this);
      base.CreateAttributes();
    }

    /// <summary>
    /// This is the method that actually does the work.
    /// </summary>
    /// <param name="DA">The DA object is used to retrieve from inputs and store in outputs.</param>
    protected override void SolveInstance(IGH_DataAccess DA)
    {
      if (RunCount == 1)
      {
        source = new CancellationTokenSource(10000);
      }

      if (InPreSolve)
      {
        // Get Data
        Account account = null;
        if (!DA.GetData(0, ref account))
          return;
        string streamId = "";
        if (!DA.GetData(1, ref streamId))
          return;
        Project project = null;
        DA.GetData(2, ref project);
        var optionProject = project == null;

        var task = Task.Run(() =>
        {
          // Get Inputs
          var bucketLayers = GetLayers();
          var bucketObjects = new List<object>();

          // Need to fill in with placeholders
          var updateStream = new SpeckleStream();

          // Need to use the stream Id
          var Client = new SpeckleApiClient(account.RestApi, true);
          Client.Stream = new SpeckleStream { StreamId = streamId };

          // Need to pass the log to the output
          string Log = "";

          // Is this stream part of a Project?

          var myTask = Client.StreamUpdateAsync(Client.StreamId, updateStream).ContinueWith(x =>
        {
          if (x.Status == TaskStatus.RanToCompletion)
          {
            var response = x.Result;
            Log += response.Message;
          }
        })
        .ContinueWith(y =>
        {
          if (project != null)
          {
            if (!project.Streams.Contains(Client.StreamId))
            {
              project.Streams.Add(Client.StreamId);
              Client.ProjectUpdateAsync(project._id, project);
            }
            else
            {
              AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Stream is already part of the project!");
            }
          }
          else if (project != null)
          {
            Client.ProjectGetAllAsync().ContinueWith
            (tsk =>
            {
              if (tsk.Result.Success == true)
              {
                var projectReceived = tsk.Result.Resources.Where(x => x._id == project._id).FirstOrDefault();
                if (projectReceived != null)
                {
                  if (!projectReceived.Streams.Contains(Client.StreamId))
                  {
                    projectReceived.Streams.Add(Client.StreamId);
                    Client.ProjectUpdateAsync(projectReceived._id, projectReceived);
                  }
                  else
                    AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Stream is already part of the project!");
                }
                else
                {
                  AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "ProjectId didn't match any known projects");
                }
              }

            });
          }
        });


          return Log;
        });

        TaskList.Add(task);
      }

      if (source.IsCancellationRequested)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Run out of time!");
      }
      else if (!GetSolveResults(DA, out var data))
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Not running multithread");
      }
      else
      {
        DA.SetData(0, data);
      }
    }

    /// <summary>
    /// This goes through the inputs and creates Layer objects. Skips the first three params that don't contain data.
    /// </summary>
    /// <returns></returns>
    public List<Layer> GetLayers()
    {
      var parameters = new List<IGH_Param>();
      for (var i = 3; i < Params.Input.Count; i++)
      {
        var myParam = Params.Input[i];
        parameters.Add(myParam);
      }
      return SpeckleUtilities.GetLayers(parameters);
    }


    public bool CanInsertParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index > 3;
    }

    public bool CanRemoveParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index > 3;
    }

    public IGH_Param CreateParameter(GH_ParameterSide side, int index)
    {
      Param_GenericObject param = new Param_GenericObject()
      {
        Name = GH_ComponentParamServer.InventUniqueNickname("ABCDEFGHIJKLMNOPQRSTUVWXYZ", Params.Input)
      };
      param.NickName = param.Name;
      param.Description = "Things to be sent around.";
      param.Optional = true;
      param.Access = GH_ParamAccess.tree;
      ExpirePreview(true);
      return param;
    }

    public bool DestroyParameter(GH_ParameterSide side, int index)
    {
      return side == GH_ParameterSide.Input && index > 3;
    }

    public void VariableParameterMaintenance()
    {
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
      get { return new Guid("a788591d-fd4d-43df-b9ba-d4fd962586b8"); }
    }
  }
}

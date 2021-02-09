using SNJ = Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using Rhino.Geometry;
using SpeckleCore;

namespace SpeckleGrasshopper.BaseComponents
{
  public class GhReceiverCoreSyncClient : GH_TaskCapableComponent<List<SpeckleObject>>
  {
    /// <summary>
    /// Initializes a new instance of the TaskCapableMultiThread class.
    /// </summary>
    /// 
    CancellationTokenSource source;
    //Account account;

    /// <summary>
    /// Initializes a new instance of the GhReceiverCoreSyncClient class.
    /// </summary>
    public GhReceiverCoreSyncClient()
      : base("Data Receiver Sync", "DRS",
          "Receives data from Speckle Synchronously.",
          "Speckle", "   Server")
    {
    }

    /// <summary>
    /// Registers all the input parameters for this component.
    /// </summary>
    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddGenericParameter("Account", "A", "Account to get stream from", GH_ParamAccess.item);
      pManager.AddGenericParameter("Stream", "S", "Stream to get", GH_ParamAccess.item);
      pManager.AddTextParameter("Query", "Q", "Query to simplify the call to get data from the stream", GH_ParamAccess.item);
      pManager[2].Optional = true;
    }

    public override bool Write(GH_IWriter writer)
    {
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      return base.Read(reader);
    }

    /// <summary>
    /// Registers all the output parameters for this component.
    /// </summary>
    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
      pManager.AddGenericParameter("Objects", "O", "Received objects", GH_ParamAccess.list);
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
        Account account = null;
        if (!DA.GetData(0, ref account))
          return;

        string streamId = null;
        if (!DA.GetData(1, ref streamId))
          return;

        string query_ = null;
        DA.GetData(2, ref query_);
        string[] layers = new string[] { };

        if (query_ != null && query_.Contains("layers"))
        {
          var l = query_.Split(new string[] { "?layers=" }, StringSplitOptions.RemoveEmptyEntries).Last();
          l = l.Split('&').First();
          layers = l.Split(',');
          query_ = query_.Replace($"?layers={string.Join(",", layers)}", "");
        }

        var task = Task.Run(() =>
        {
          var Client = new SpeckleApiClient(account.RestApi, true);
          Client.AuthToken = account.Token;
          var query = query_ == null || query_.Equals("") ? null : query_;
          var getStream = Client.StreamGetAsync(streamId, null).Result;
          Client.Stream = getStream.Resource;
          var choosenLayers = Client.Stream.Layers.Where(x => layers.Contains(x.Name));
          // filter out the objects that were not in the cache and still need to be retrieved
          var payload = Client.Stream.Objects.Where(o => o.Type == "Placeholder").Select(obj => obj._id).ToArray();

          // how many objects to request from the api at a time
          int maxObjRequestCount = 42;

          // list to hold them into
          var newObjects = new List<SpeckleObject>();

          if (layers.Length > 0)
            payload = FilterFromLayers(payload, choosenLayers).ToArray();

          // jump in `maxObjRequestCount` increments through the payload array
          for (int i = 0; i < payload.Length; i += maxObjRequestCount)
          {
            // create a subset
            var subPayload = payload.Skip(i).Take(maxObjRequestCount).ToArray();

            // get it sync as this is always execed out of the main thread
            var query1 = query;// + "&omit=displayValue";
            var res = Client.ObjectGetBulkAsync(subPayload, query1).Result;

            // put them in our bucket
            newObjects.AddRange(res.Resources);
          }

          
          newObjects = FilterEmpty(newObjects);


          foreach (var obj in newObjects)
          {
            var matches = Client.Stream.Objects.FindAll(o => o._id == obj._id);

            //TODO: Do this efficiently, this is rather brute force
            for (int i = Client.Stream.Objects.Count - 1; i >= 0; i--)
            {
              if (Client.Stream.Objects[i]._id == obj._id)
              {
                Client.Stream.Objects[i] = obj;
              }
            }
          }

          return newObjects;//getStream.Message; //HardTask();
        }, source.Token);

        TaskList.Add(task);
        return;
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
        DA.SetDataList(0, data);
      }
    }

    public List<SpeckleObject> FilterEmpty(List<SpeckleObject> objects)
    {
      var myObjects = new List<SpeckleObject>();

      for (int i = 0; i < objects.Count; i++)
      {
        var obj = objects[i];
        var IsString = obj.GetType().GetProperty("Type")?.GetValue(obj).Equals("String");
        var IsEmpty = objects[i].GetType().GetProperty("Value")?.GetValue(obj).Equals("You do not have permissions to view this object");

        if (IsString != null && IsString.Value == true && IsEmpty != null && IsEmpty == true)
        {
          continue;
        }

        myObjects.Add(objects[i]);
      }

      return myObjects;
    }

    public List<T> FilterFromLayers<T>(IEnumerable<T> objects, IEnumerable<Layer> layers)
    {
      var myObjects = new List<T>();

      foreach (var layer in layers)
      {
        myObjects.AddRange(objects.Skip((int)layer.StartIndex).Take((int)layer.ObjectCount));
      }
      return myObjects;
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
      get { return new Guid("e1ca81c9-bd01-4fc6-87c2-578cbe4d3f51"); }
    }
  }
}

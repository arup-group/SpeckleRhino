//extern alias SpeckleNewtonsoft;
using SNJ = Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;

using Grasshopper.Kernel;
using Rhino.Geometry;

using GH_IO.Serialization;
using System.Diagnostics;
using Grasshopper.Kernel.Parameters;

using SpeckleCore;

using Grasshopper;
using Grasshopper.Kernel.Data;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Reflection;
using SpeckleGrasshopper.ExtendedComponents;
using SpeckleGrasshopper.Attributes;

namespace SpeckleGrasshopper
{
  public class GhReceiverClient : GH_Component, IGH_VariableParameterComponent
  {
    Account account;
    string AuthToken;
    string RestApi;
    public string StreamId;
    public bool Deserialize = true;
    public bool Paused = false;
    public bool Expired = false;
    private bool AccountRequired = false;
    public GH_Document Document;

    public SpeckleApiClient Client;
    List<Layer> Layers;
    List<SpeckleObject> SpeckleObjects;
    List<object> ConvertedObjects;

    Action expireComponentAction;

    public BoundingBox BBox;

    System.Timers.Timer StreamIdChanger;

    public bool IsUpdating = false;
    public override GH_Exposure Exposure => GH_Exposure.primary;
    public GhReceiverClient()
      : base("Data Receiver", "DR",
          "Receives data from Speckle.",
          "Speckle", "   Server")
    {
      SpeckleCore.SpeckleInitializer.Initialize();
      SpeckleCore.LocalContext.Init();
    }

    public override void CreateAttributes()
    {
      m_attributes = new GhReceiverClientAttributes(this);
    }

    public override bool Write(GH_IWriter writer)
    {
      try
      {
        if (Client != null)
          using (var ms = new MemoryStream())
          {
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, Client);
            writer.SetByteArray("speckleclient", ms.ToArray());
          }
        writer.SetBoolean("deserialize", this.Deserialize);
        writer.SetBoolean("AccountRequired", AccountRequired);

        if (account != null)
        {
          writer.SetString("AccountToken", account.Token);
          writer.SetString("AccountApi", account.RestApi);
        }
      }
      catch (Exception e) { }
      return base.Write(writer);
    }

    public override bool Read(GH_IReader reader)
    {
      try
      {
        Debug.WriteLine("Trying to read client!");
        var serialisedClient = reader.GetByteArray("speckleclient");
        using (var ms = new MemoryStream())
        {
          ms.Write(serialisedClient, 0, serialisedClient.Length);
          ms.Seek(0, SeekOrigin.Begin);
          Client = (SpeckleApiClient)new BinaryFormatter().Deserialize(ms);

          StreamId = Client.StreamId;
          AuthToken = Client.AuthToken;
          RestApi = Client.BaseUrl;

          InitReceiverEventsAndGlobals();
        }

        string accountToken = "";
        string accountApi = "";
        if (reader.TryGetString("AccountToken", ref accountToken) && reader.TryGetString("AccountApi", ref accountApi))
        {
          account = new Account() { RestApi = accountApi, Token = accountToken };
        }
        reader.TryGetBoolean("AccountRequired", ref AccountRequired);
        this.Deserialize = reader.GetBoolean("deserialize");
      }
      catch
      {
        Debug.WriteLine("No client was present.");
      }
      return base.Read(reader);
    }

    public override void AddedToDocument(GH_Document document)
    {
      base.AddedToDocument(document);
      Document = this.OnPingDocument();

      if (Client == null)
      {
        Account account = null;
        try
        {
          account = LocalContext.GetDefaultAccount();
        }
        catch (Exception err)
        {
        }

        if (account == null)
        {
          account = GhSenderClient.SignInWindow();
        }

        RestApi = account.RestApi;
        AuthToken = account.Token;
      }

      StreamIdChanger = new System.Timers.Timer(1000)
      {
        Enabled = false,
        AutoReset = false
      };
      StreamIdChanger.Elapsed += ChangeStreamId;
    }

    private async void ChangeStreamId(object sender, System.Timers.ElapsedEventArgs e)
    {
      Debug.WriteLine("Changing streams to {0}.", StreamId);
      UpdateClient(RestApi, AuthToken, null);
    }

    private async void UpdateClientWrap(string RestApi, string AuthToken, IGH_DataAccess DA)
    {
      UpdateClient(RestApi, AuthToken, DA);
    }

    private async void UpdateClient(string RestApi, string AuthToken, IGH_DataAccess DA)
    {
      Client = new SpeckleApiClient(RestApi, true);
      InitReceiverEventsAndGlobals();
      if (StreamId != null)
        await Client.IntializeReceiver(StreamId, Document.DisplayName, "Grasshopper", Document.DocumentID.ToString(), AuthToken);
    }

    public void InitReceiverEventsAndGlobals()
    {
      SpeckleObjects = new List<SpeckleObject>();

      ConvertedObjects = new List<object>();

      Client.OnReady += (sender, e) =>
      {
        UpdateGlobal();
      };

      Client.OnWsMessage += OnWsMessage;

      Client.OnError += (sender, e) =>
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.EventName + ": " + e.EventData);
      };

      expireComponentAction = () => ExpireSolution(true);
    }

    public override void AppendAdditionalMenuItems(ToolStripDropDown menu)
    {
      base.AppendAdditionalMenuItems(menu);
      //Menu_AppendSeparator(menu);
      //Menu_AppendItem(menu, "Specify Account", OnAddAccount, true, AccountRequired);
      //Menu_AppendSeparator(menu);

      GH_DocumentObject.Menu_AppendItem(menu, "Copy streamId (" + StreamId + ") to clipboard.", (sender, e) =>
       {
         if (StreamId != null)
           System.Windows.Clipboard.SetText(StreamId);
       });

      GH_DocumentObject.Menu_AppendSeparator(menu);

      base.AppendAdditionalMenuItems(menu);
      var toggleItem = new ToolStripMenuItem("Deserialize objects.") { Name = "Deserialize objects.", Checked = this.Deserialize, CheckOnClick = true };
      toggleItem.CheckStateChanged += (sender, e) =>
      {
        this.Deserialize = ((ToolStripMenuItem)sender).Checked;
        Rhino.RhinoApp.InvokeOnUiThread(expireComponentAction);
      };
      menu.Items.Add(toggleItem);

      GH_DocumentObject.Menu_AppendSeparator(menu);

      GH_DocumentObject.Menu_AppendItem(menu, "Force refresh.", (sender, e) =>
     {
       if (StreamId != null)
       {
         UpdateGlobal();
         ExpireSolution(true);
       }
     });

      GH_DocumentObject.Menu_AppendSeparator(menu);

      GH_DocumentObject.Menu_AppendItem(menu, "View stream.", (sender, e) =>
      {
        if (StreamId == null) return;
        System.Diagnostics.Process.Start(RestApi.Replace("/api/v1", "/#/view").Replace("/api", "/#/view") + @"/" + StreamId);
      });

      GH_DocumentObject.Menu_AppendItem(menu, "(API) View stream data.", (sender, e) =>
      {
        if (StreamId == null) return;
        System.Diagnostics.Process.Start(RestApi + @"/streams/" + StreamId);
      });

      GH_DocumentObject.Menu_AppendItem(menu, "(API) View objects data online.", (sender, e) =>
      {
        if (StreamId == null) return;
        System.Diagnostics.Process.Start(RestApi + @"/streams/" + StreamId + @"/objects?omit=displayValue,base64");
      });

      if (Client == null || Client.Stream == null) return;

      GH_DocumentObject.Menu_AppendSeparator(menu);
      if (Client.Stream.Parent == null)
        GH_DocumentObject.Menu_AppendItem(menu: menu, text: "This is a parent stream.", enabled: false, click: null);
      else
        GH_DocumentObject.Menu_AppendItem(menu: menu, text: "Parent: " + Client.Stream.Parent, click: (sender, e) =>
        {
          System.Windows.Clipboard.SetText(Client.Stream.Parent);
          System.Windows.MessageBox.Show("Parent id copied to clipboard. Share away!");
        });
      GH_DocumentObject.Menu_AppendSeparator(menu);

      GH_DocumentObject.Menu_AppendSeparator(menu);
      GH_DocumentObject.Menu_AppendItem(menu, "Children:");
      GH_DocumentObject.Menu_AppendSeparator(menu);

      var maxChildren = Client.Stream.Children;
      if (maxChildren.Count > 10)
        maxChildren = maxChildren.Take(10).ToList();
      foreach (string childId in maxChildren)
      {
        GH_DocumentObject.Menu_AppendItem(menu, "Child " + childId, (sender, e) =>
        {
          System.Windows.Clipboard.SetText(childId);
          System.Windows.MessageBox.Show("Child id copied to clipboard. Share away!");
        });
      }
    }

    private void AddAccount()
    {
      //AccountRequired = !AccountRequired;

      if (AccountRequired)
      {
        Params.Input.Insert(1, new Param_GenericObject()
        {
          Name = "Account",
          NickName = "Ac",
          Description = "An Account object, find it either from \"List my Accounts\" component or \"Create Account\"",
          Access = GH_ParamAccess.item,
          Optional = true,
        });
        Params.OnParametersChanged();
        ExpireSolution(false);
      }
      else
      {
        var paramMatch = Params.Input.Where(x => x.Name.Equals("Account")).FirstOrDefault();
        if (paramMatch != null)
        {
          Params.Input.Remove(paramMatch);
          Params.OnParametersChanged();
          ExpireSolution(false);
        }
      }
    }

    public virtual void OnWsMessage(object source, SpeckleEventArgs e)
    {
      if (Paused)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "Update available since " + DateTime.Now);
        Expired = true;
        return;
      }
      if (e.EventObject != null)
        try
        {
          switch ((string)e.EventObject.args.eventType)
          {
            case "update-global":
              UpdateGlobal();
              break;
            case "update-meta":
              UpdateMeta();
              break;
            case "update-name":
              UpdateMeta();
              break;
            case "update-children":
              UpdateChildren();
              break;
            default:
              CustomMessageHandler((string)e.EventObject.args.eventType, e);
              break;
          }
        }
        catch (Exception ex)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, ex.ToString());
        }
    }

    public virtual void UpdateGlobal()
    {
      if (IsUpdating)
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Warning, "New update received while update was in progress. Please refresh.");
        return;
      }

      IsUpdating = true;

      try
      {
        if (Client == null)
        {
          AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Client is missing");
          throw new Exception("While updating global, Client was null");
        }
        var getStream = Client.StreamGetAsync(Client.StreamId, null).Result;

        NickName = getStream.Resource.Name;
        Layers = getStream.Resource.Layers.ToList();

        Client.Stream = getStream.Resource;

        // add or update the newly received stream in the cache.
        LocalContext.AddOrUpdateStream(Client.Stream, Client.BaseUrl);

        this.Message = "Getting objects!";

        // pass the object list through a cache check 
        LocalContext.GetCachedObjects(Client.Stream.Objects, Client.BaseUrl);

        // filter out the objects that were not in the cache and still need to be retrieved
        var payload = Client.Stream.Objects.Where(o => o.Type == "Placeholder").Select(obj => obj._id).ToArray();

        // how many objects to request from the api at a time
        int maxObjRequestCount = 42;

        // list to hold them into
        var newObjects = new List<SpeckleObject>();

        // jump in `maxObjRequestCount` increments through the payload array
        for (int i = 0; i < payload.Length; i += maxObjRequestCount)
        {
          // create a subset
          var subPayload = payload.Skip(i).Take(maxObjRequestCount).ToArray();

          // get it sync as this is always execed out of the main thread
          var res = Client.ObjectGetBulkAsync(subPayload, "omit=displayValue").Result;

          // put them in our bucket
          newObjects.AddRange(res.Resources);
          this.Message = SNJ.JsonConvert.SerializeObject(String.Format("{0}/{1}", i, payload.Length));
        }

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

        // add objects to cache async
        var myTask1 = Task.Run(() =>
       {
         foreach (var obj in newObjects)
         {
           LocalContext.AddCachedObject(obj, Client.BaseUrl);
         }
       });

        // set ports
        UpdateOutputStructure();

        this.Message = "Converting...";

        SpeckleObjects.Clear();

        var myTask2 = Task.Run(() =>
        {
          ConvertedObjects = SpeckleCore.Converter.Deserialise(Client.Stream.Objects);
          IsUpdating = false;

          if (!AccountRequired)
            Rhino.RhinoApp.InvokeOnUiThread(expireComponentAction);

          this.Message = "Got data\n@" + DateTime.Now.ToString("hh:mm:ss");
        });

        if (AccountRequired)
        {
          myTask1.Wait();
          myTask2.Wait();
        }


      }
      catch (Exception e)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, e.Message);
      }
      finally
      {
        IsUpdating = false;
      }
    }

    public virtual void UpdateMeta()
    {
      var result = Client.StreamGetAsync(StreamId, "fields=name,layers").Result;

      NickName = result.Resource.Name;
      Layers = result.Resource.Layers.ToList();
      UpdateOutputStructure();
    }

    public virtual void UpdateChildren()
    {
      var result = Client.StreamGetAsync(Client.StreamId, "fields=children").Result;
      Client.Stream.Children = result.Resource.Children;
    }

    public virtual void CustomMessageHandler(string eventType, SpeckleEventArgs e)
    {
      Debug.WriteLine("Received {0} type message.", eventType);
    }

    public override void RemovedFromDocument(GH_Document document)
    {
      if (Client != null)
        Client.Dispose();
      base.RemovedFromDocument(document);
    }

    public override void DocumentContextChanged(GH_Document document, GH_DocumentContext context)
    {
      if (context == GH_DocumentContext.Close)
      {
        Client?.Dispose();
      }

      base.DocumentContextChanged(document, context);
    }

    protected override void RegisterInputParams(GH_Component.GH_InputParamManager pManager)
    {
      pManager.AddTextParameter("ID", "ID", "The stream's short id.", GH_ParamAccess.item);
      pManager[0].Optional = true;
    }

    protected override void RegisterOutputParams(GH_Component.GH_OutputParamManager pManager)
    {
    }

    public bool HasAccountAdded()
    {
      return Params.Input.Where(x => x.Name.Equals("Account")).Count() != 0;
    }

    protected override async void SolveInstance(IGH_DataAccess DA)
    {
      var doc = OnPingDocument();
      var accountObj = GlobalRhinoComputeComponent.GetFromDocument(doc);

      if (accountObj != null && (accountObj.ProvideAccount != AccountRequired || (accountObj.ProvideAccount && !HasAccountAdded())))
      {
        AccountRequired = accountObj.ProvideAccount;
        if ((AccountRequired && !HasAccountAdded()) || (!AccountRequired && HasAccountAdded()))
        {
          OnPingDocument().ScheduleSolution(5, d =>
          {
            AddAccount();
          });
        }

        return;
      }

      if (AccountRequired)
      {
        Account acc = null;
        if (!DA.GetData(1, ref acc))
          return;

        if (acc != account)
        {
          account = acc;
          RestApi = account.RestApi;
          AuthToken = account.Token;
          UpdateClientWrap(RestApi, AuthToken, DA);
        }

        if (!IsUpdating)
          Expired = true;

      }

      if (account == null)
      {
        account = GhSenderClient.SignInWindow();
        RestApi = account.RestApi;
        AuthToken = account.Token;
        UpdateClientWrap(RestApi, AuthToken, DA);
      }

      if (Paused)
      {
        SetObjects(DA);
        return;
      }

      string inputId = null;
      DA.GetData(0, ref inputId);

      if (inputId == null && StreamId == null)
      {
        AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Both id is null");
        return;
      }

      if (inputId != StreamId)
      {
        Client?.Dispose(true);
        Client = null;

        StreamId = inputId;

        StreamIdChanger.Start();
        return;
      }

      if (Client == null)
      {
        this.AddRuntimeMessage(GH_RuntimeMessageLevel.Remark, "Receiver not intialised.");
        return;
      }

      if (!Client.IsConnected)
      {
        //this.AddRuntimeMessage(GH_RuntimeMessageLevel.Error, "Client hasn't connected");
        //return;
      }

      if (Expired)
      {
        Expired = false;
        UpdateGlobal();
        if (AccountRequired)
        {
          CalculateBoundingBox();
          SetObjects(DA);
        }
        return;
      }

      CalculateBoundingBox();
      ExpirePreview(true);
      SetObjects(DA);
    }

    public void UpdateOutputStructure()
    {
      //TODO: Check if we're out or under range, and add default layers as such.
      List<Layer> toRemove, toAdd, toUpdate;
      toRemove = new List<Layer>(); toAdd = new List<Layer>(); toUpdate = new List<Layer>();

      Layer.DiffLayerLists(GetLayers(), Layers, ref toRemove, ref toAdd, ref toUpdate);

      foreach (Layer layer in toRemove)
      {
        var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.Guid; });

        if (myparam != null)
          Params.UnregisterOutputParameter(myparam);
      }

      int k = 0;
      foreach (var layer in toAdd)
      {
        Param_GenericObject newParam = getGhParameter(layer);
        Params.RegisterOutputParam(newParam, layer.OrderIndex != null ? (int)layer.OrderIndex : k);
        k++;
      }

      foreach (var layer in toUpdate)
      {
        var myparam = Params.Output.FirstOrDefault(item => { return item.Name == layer.Guid; });
        myparam.NickName = layer.Name;
      }
      Params.OnParametersChanged();
    }

    public void SetObjects(IGH_DataAccess DA)
    {
      if (Layers == null) return;
      if (ConvertedObjects.Count == 0 && this.Deserialize) return;
      if (Client.Stream.Objects.Count == 0 && !this.Deserialize) return;

      List<object> chosenObjects;
      if (this.Deserialize)
        chosenObjects = ConvertedObjects;
      else
        chosenObjects = Client.Stream.Objects.Cast<object>().ToList();

      int k = 0;

      foreach (Layer layer in Layers)
      {
        //TODO: Check if we're out or under range, and add default layers as such.
        var subset = chosenObjects.GetRange((int)layer.StartIndex, (int)layer.ObjectCount);

        if (subset.Count == 0) continue;

        if (layer.Topology == "")
          DA.SetDataList((int)layer.OrderIndex, subset);
        else
        {
          //HIC SVNT DRACONES
          var tree = new DataTree<object>();
          var treeTopo = layer.Topology.Split(' ');
          int subsetCount = 0;
          foreach (var branch in treeTopo)
          {
            if (branch != "")
            {
              var branchTopo = branch.Split('-')[0].Split(';');
              var branchIndexes = new List<int>();
              foreach (var t in branchTopo) branchIndexes.Add(Convert.ToInt32(t));

              var elCount = Convert.ToInt32(branch.Split('-')[1]);
              GH_Path myPath = new GH_Path(branchIndexes.ToArray());

              for (int i = 0; i < elCount; i++)
                tree.EnsurePath(myPath).Add(new Grasshopper.Kernel.Types.GH_ObjectWrapper(subset[subsetCount + i]));
              subsetCount += elCount;
            }
          }
          DA.SetDataTree(layer.OrderIndex != null ? (int)layer.OrderIndex : k, tree);
          k++;
        }
      }
    }

    public void CalculateBoundingBox()
    {
      BBox = new BoundingBox(-1, -1, -1, 1, 1, 1);
      foreach (var obj in ConvertedObjects)
      {
        if (obj is GeometryBase)
          BBox.Union(((GeometryBase)obj).GetBoundingBox(false));
      }
    }

    public override BoundingBox ClippingBox => BBox;

    public override void DrawViewportMeshes(IGH_PreviewArgs args)
    {
      base.DrawViewportMeshes(args);
      if (this.Hidden || this.Locked) return;

      if (ConvertedObjects != null)
        foreach (var obj in ConvertedObjects)
        {
          //if(obj is Rhino.DocObject) { 
          //obj.GetType();
        }

      // TODO
    }

    public override void DrawViewportWires(IGH_PreviewArgs args)
    {
      base.DrawViewportWires(args);

      if (this.Hidden || this.Locked) return;
      System.Drawing.Color solidClr = !this.Attributes.Selected ? args.ShadeMaterial.Diffuse : args.ShadeMaterial_Selected.Diffuse;

      if (ConvertedObjects != null)
        foreach (var obj in ConvertedObjects)
        {
          if (!(obj is GeometryBase)) continue;
          if (((GeometryBase)obj).IsDocumentControlled) continue;
          switch (((GeometryBase)obj).ObjectType)
          {
            case Rhino.DocObjects.ObjectType.Point:
              args.Display.DrawPoint(((Rhino.Geometry.Point)obj).Location, Rhino.Display.PointStyle.X, 2, solidClr);
              break;

            case Rhino.DocObjects.ObjectType.Curve:
              args.Display.DrawCurve((Curve)obj, solidClr);
              break;

            case Rhino.DocObjects.ObjectType.Extrusion:
              Rhino.Display.DisplayMaterial eMaterial = new Rhino.Display.DisplayMaterial(solidClr, 0.5);
              args.Display.DrawBrepShaded(((Extrusion)obj).ToBrep(), eMaterial);
              break;
            case Rhino.DocObjects.ObjectType.Brep:
              Rhino.Display.DisplayMaterial bMaterial = new Rhino.Display.DisplayMaterial(solidClr, 0.5);
              args.Display.DrawBrepShaded((Brep)obj, bMaterial);
              //e.Display.DrawBrepWires((Brep)obj, Color.DarkGray, 1);
              break;

            case Rhino.DocObjects.ObjectType.Mesh:
              var mesh = obj as Mesh;
              if (mesh.VertexColors.Count > 0)
              {
                for (int i = 0; i < mesh.VertexColors.Count; i++)
                  mesh.VertexColors[i] = System.Drawing.Color.FromArgb(100, mesh.VertexColors[i]);

                args.Display.DrawMeshFalseColors(mesh);
              }
              else
              {
                Rhino.Display.DisplayMaterial mMaterial = new Rhino.Display.DisplayMaterial(solidClr, 0.5);
                args.Display.DrawMeshShaded(mesh, mMaterial);
              }
              //e.Display.DrawMeshWires((Mesh)obj, Color.DarkGray);
              break;

            case Rhino.DocObjects.ObjectType.TextDot:
              //e.Display.Draw3dText( ((TextDot)obj).Text, Colors[count], new Plane(((TextDot)obj).Point));
              //todo
              break;

            case Rhino.DocObjects.ObjectType.Annotation:
              //todo
              break;
          }
          // TODO
        }
    }

    #region Variable Parm

    private Param_GenericObject getGhParameter(Layer param)
    {
      Param_GenericObject newParam = new Param_GenericObject();
      newParam.Name = (string)param.Guid;
      newParam.NickName = (string)param.Name;
      newParam.MutableNickName = false;
      newParam.Access = GH_ParamAccess.tree;
      return newParam;
    }

    bool IGH_VariableParameterComponent.CanInsertParameter(GH_ParameterSide side, Int32 index)
    {
      return false;
    }
    bool IGH_VariableParameterComponent.CanRemoveParameter(GH_ParameterSide side, Int32 index)
    {
      return false;
    }
    bool IGH_VariableParameterComponent.DestroyParameter(GH_ParameterSide side, Int32 index)
    {
      return false;
    }
    IGH_Param IGH_VariableParameterComponent.CreateParameter(GH_ParameterSide side, Int32 index)
    {
      return null;
    }
    public void VariableParameterMaintenance()
    {
    }

    #endregion

    public string GetParamTopology(IGH_Param param)
    {
      string topology = "";
      foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
      {
        topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
      }
      return topology;
    }

    public List<Layer> GetLayers()
    {
      List<Layer> layers = new List<Layer>();
      int startIndex = 0;
      int count = 0;
      foreach (IGH_Param myParam in Params.Output)
      {
        // NOTE: For gh receivers, we store the original guid of the sender component layer inside the parametr name.
        Layer myLayer = new Layer(
            myParam.NickName,
            myParam.Name /* aka the orignal guid*/, GetParamTopology(myParam),
            myParam.VolatileDataCount,
            startIndex,
            count);

        layers.Add(myLayer);
        startIndex += myParam.VolatileDataCount;
        count++;
      }
      return layers;
    }

    protected override System.Drawing.Bitmap Icon
    {
      get
      {
        return Properties.Resources.receiver_2;
      }
    }

    public override Guid ComponentGuid
    {
      get { return new Guid("{e35c72a5-9e1c-4d79-8879-a9d6db8006fb}"); }
    }
  }
};

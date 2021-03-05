using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Grasshopper.Kernel;
using SpeckleCore;

namespace SpeckleGrasshopper.Utilities
{
  public static class SpeckleUtilities
  {
    /// <summary>
    /// Get a script description of the "Tree" topology used in Grasshopper.
    /// </summary>
    /// <param name="param"></param>
    /// <returns></returns>
    public static string GetParamTopology(IGH_Param param)
    {
      string topology = "";
      foreach (Grasshopper.Kernel.Data.GH_Path mypath in param.VolatileData.Paths)
      {
        topology += mypath.ToString(false) + "-" + param.VolatileData.get_Branch(mypath).Count + " ";
      }
      return topology;
    }

    /// <summary>
    /// Basic constructor of a Layer from a Parameter
    /// </summary>
    /// <param name="startIndex"></param>
    /// <param name="count"></param>
    /// <param name="myParam"></param>
    /// <returns></returns>
    public static Layer CreateLayer(int startIndex, int count, IGH_Param myParam)
    {
      return new Layer(
                  myParam.NickName,
                  myParam.InstanceGuid.ToString(),
                  SpeckleUtilities.GetParamTopology(myParam),
                  myParam.VolatileDataCount,
                  startIndex,
                  count);
    }

    /// <summary>
    /// Has the logic of how to convert a stack of parameters into sequential layers, moving the startIndex pointers accordingly
    /// </summary>
    /// <param name="parameters"></param>
    /// <returns></returns>
    public static List<Layer> GetLayers(List<IGH_Param> parameters)
    {
      var layers = new List<Layer>();
      int startIndex = 0;
      for (int i = 0; i < parameters.Count; i++)
      {
        var myParam = parameters[i];
        var layer = CreateLayer(startIndex, i, myParam);
        startIndex += myParam.VolatileDataCount;
        layers.Add(layer);
      }
      return layers;
    }

    /// <summary>
    /// Iterates through parameters and get all their data as objects. This can be used later for converting them to SpeckleObjects.
    /// </summary>
    /// <param name="Parameters"></param>
    /// <returns></returns>
    public static List<object> GetData(List<IGH_Param> Parameters)
    {
      var data = new List<object>();

      foreach (IGH_Param myParam in Parameters)
      {
        foreach (object o in myParam.VolatileData.AllData(false))
        {
          data.Add(o);
        }
      }

      data = data.Select(obj =>
      {
        try
        {
          return obj.GetType().GetProperty("Value").GetValue(obj);
        }
        catch
        {
          return null;
        }
      }).ToList();

      return data;
    }

    static public void AddClientRelatedSubMenus(ToolStripDropDown menu, SpeckleApiClient Client)
    {
      var streamId = Client?.Stream?.StreamId;
      GH_DocumentObject.Menu_AppendItem(menu, "Copy streamId (" + streamId + ") to clipboard.", (sender, e) =>
      {
        if (streamId != null)
        {
          System.Windows.Clipboard.SetText(streamId);
        }
      });

      var RestApi = Client?.BaseUrl;
      GH_DocumentObject.Menu_AppendItem(menu, "View stream.", (sender, e) =>
      {
        if (streamId == null)
        {
          return;
        }

        System.Diagnostics.Process.Start(RestApi.Replace("/api/v1", "/#/view").Replace("/api", "/#/view") + @"/" + streamId);
      });

      GH_DocumentObject.Menu_AppendItem(menu, "(API) View stream data.", (sender, e) =>
      {
        if (streamId == null)
        {
          return;
        }

        System.Diagnostics.Process.Start(RestApi + @"/streams/" + streamId);
      });
    }
  }
}

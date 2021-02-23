﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
        Layer myLayer = SpeckleUtilities.CreateLayer(startIndex, i, myParam);
        startIndex += myParam.VolatileDataCount;
      }
      return layers;
    }

  }
}

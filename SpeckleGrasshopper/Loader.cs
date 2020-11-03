using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Windows.Interop;
using System.Windows.Media.Effects;
using GH_IO.Serialization;
using Grasshopper.Kernel;
using SpeckleGrasshopper.ExtendedComponents;
using SpeckleGrasshopper.Management;

namespace SpeckleGrasshopper
{
  public class Loader : GH_AssemblyPriority
  {
    System.Timers.Timer loadTimer;
    static bool MenuHasBeenAdded = false;
    private bool isStarting;
    public Loader() { }

    public override GH_LoadingInstruction PriorityLoad()
    {
      loadTimer = new System.Timers.Timer(500);
      loadTimer.Start();
      loadTimer.Elapsed += AddSpeckleMenu;
      return GH_LoadingInstruction.Proceed;
    }

    private void AddSpeckleMenu(object sender, ElapsedEventArgs e)
    {
      if (Grasshopper.Instances.DocumentEditor == null) return;
      if (isStarting) return;

      if (MenuHasBeenAdded)
      {
        loadTimer.Stop();
        return;
      }

      isStarting = true;

      var speckleMenu = new ToolStripMenuItem("Speckle");
      speckleMenu.DropDown.Items.Add("Speckle Account Manager", null, (s, a) =>
      {
        var signInWindow = new SpecklePopup.SignInWindow(false);
        var helper = new System.Windows.Interop.WindowInteropHelper(signInWindow);
        helper.Owner = Rhino.RhinoApp.MainWindowHandle();
        signInWindow.Show();
      });

      speckleMenu.DropDown.Items.Add(new ToolStripSeparator());

      speckleMenu.DropDown.Items.Add("Speckle Home", null, (s, a) =>
      {
        Process.Start(@"https://speckle.works");
      });

      speckleMenu.DropDown.Items.Add("Speckle Documentation", null, (s, a) =>
      {
        Process.Start(@"https://speckle.works/docs/essentials/start");
      });

      speckleMenu.DropDown.Items.Add("Speckle Forum", null, (s, a) =>
      {
        Process.Start(@"https://discourse.speckle.works");
      });

      speckleMenu.DropDown.Items.Add("Rhino.Compute toggle", null, (s, a) =>
      {
        Debug.WriteLine("Rhino.Compute mode");

        var doc = Grasshopper.Instances.ActiveCanvas.Document;
        if (doc == null)
          return;

        var accountObj = GlobalRhinoComputeComponent.GetFromDocument(doc);

        if (accountObj != null)
        {
          accountObj.Toggle();
        }
        else
        {
          var docObject = new GlobalRhinoComputeComponent();
          doc.AddObject(docObject, false);
          docObject.Toggle();
        }
      });

      try
      {
        var mainMenu = Grasshopper.Instances.DocumentEditor.MainMenuStrip;
        Grasshopper.Instances.DocumentEditor.Invoke(new Action(() =>
      {
        mainMenu.Items.Insert(mainMenu.Items.Count - 2, speckleMenu);
      }));
        MenuHasBeenAdded = true;
        loadTimer.Stop();
      }
      catch (Exception err)
      {
        isStarting = false;
        Debug.WriteLine(err.Message);
      }
    }
  }
}

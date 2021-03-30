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
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;
using SpeckleGrasshopper.ExtendedComponents;
using SpeckleGrasshopper.Management;
using SpeckleGrasshopper.Properties;
//using SpecklePopup;

namespace SpeckleGrasshopper
{
  public class Loader : GH_AssemblyPriority
  {
    System.Timers.Timer loadTimer;
    static bool MenuHasBeenAdded = false;
    //SignInWindow signInWindow;
    private bool isGlobalOn;
    ToolStripMenuItem toolStripItem;
    public Loader() { }

    public override GH_LoadingInstruction PriorityLoad()
    {
      Grasshopper.Instances.CanvasCreated += AddSpeckleMenu;
      return GH_LoadingInstruction.Proceed;
    }


    private void AddSpeckleMenu(GH_Canvas canvas)
    {
      Grasshopper.Instances.CanvasCreated -= AddSpeckleMenu;

      var docEditor = Grasshopper.Instances.DocumentEditor;
      if (docEditor == null)
        return;
      var mainMenu = docEditor.MainMenuStrip;


      var speckleMenu = new ToolStripMenuItem("Speckle");
      docEditor.MainMenuStrip.SuspendLayout();
      docEditor.MainMenuStrip.Items.AddRange(new ToolStripItem[] { speckleMenu });

      // Create dropdown items
      /////////////////////////////////////////////////////
      speckleMenu.DropDown.Items.Add("Speckle Account Manager", null, (s, a) =>
      {
      //  if (signInWindow != null)
      //  {
      //    signInWindow.Close();
      //  }
      //  signInWindow = new SpecklePopup.SignInWindow( false );
      //  var helper = new System.Windows.Interop.WindowInteropHelper( signInWindow );
      //  helper.Owner = Rhino.RhinoApp.MainWindowHandle();
      //  signInWindow.Show();
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

      toolStripItem = GH_DocumentObject.Menu_AppendItem(mainMenu, "Toggle Rhino.Compute mode", (s, a) =>
      {
        Debug.WriteLine("Rhino.Compute mode");

        var doc = Grasshopper.Instances.ActiveCanvas.Document;
        if (doc == null)
          return;

        var accountObj = GlobalRhinoComputeComponent.GetFromDocument(doc);

        if (accountObj != null)
        {
          accountObj.Toggle();
          isGlobalOn = accountObj.ProvideAccount;
        }
        else
        {
          var docObject = new GlobalRhinoComputeComponent();
          doc.AddObject(docObject, false);
          docObject.Toggle();
          isGlobalOn = docObject.ProvideAccount;
        }
      },
      null, true, isGlobalOn);

      // Check if it should be checked or not
      speckleMenu.DropDown.Opened += (x, e) => 
      {
        var doc = Grasshopper.Instances.ActiveCanvas.Document;
        if (doc == null)
          return;
        var accountObj = GlobalRhinoComputeComponent.GetFromDocument(doc);

        if(accountObj != null)
        {
          isGlobalOn = accountObj.ProvideAccount;
        }

        if (toolStripItem != null) 
          toolStripItem.Checked = isGlobalOn;
      };

      speckleMenu.DropDown.Items.Add(toolStripItem);
      /////////////////////////////////////////////////////

      docEditor.MainMenuStrip.ResumeLayout(false);
      docEditor.MainMenuStrip.PerformLayout();
    }
  }
}

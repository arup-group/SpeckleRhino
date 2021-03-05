//extern alias SpeckleNewtonsoft;
using System.Linq;

using Grasshopper.Kernel;
using System.Drawing;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;
using SpeckleCore;

namespace SpeckleGrasshopper.Attributes
{

  public interface ISpeckleClient
  {
    SpeckleApiClient Client { get; }
    bool Paused { get; set; }

  }

  public class SpeckleClientAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
  {
    ISpeckleClient Base;
    GH_Component component;
    Rectangle BaseRectangle;
    Rectangle StreamIdBounds;
    Rectangle StreamNameBounds;
    private Rectangle ClientNameBounds;
    Rectangle PauseButtonBounds;
    bool displayPause = false;
    public SpeckleClientAttributes(ISpeckleClient speckleClient, GH_Component component, bool displayPause = true) : base(component)
    {
      Base = speckleClient;
      this.component = component;
      this.displayPause = displayPause;
    }

    protected override void Layout()
    {
      base.Layout();
      BaseRectangle = GH_Convert.ToRectangle(Bounds);
      StreamIdBounds = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 120) * 0.5), BaseRectangle.Y - 25, 120, 20);
      StreamNameBounds = new Rectangle(StreamIdBounds.X, BaseRectangle.Y - 50, 120, 20);
      ClientNameBounds = new Rectangle(StreamIdBounds.X, BaseRectangle.Y - 75, 120, 20);

      if (displayPause)
      {
        PauseButtonBounds = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 30) * 0.5), BaseRectangle.Y + BaseRectangle.Height, 30, 30);

        Rectangle newBaseRectangle = new Rectangle(BaseRectangle.X, BaseRectangle.Y, BaseRectangle.Width, BaseRectangle.Height + 33);
        Bounds = newBaseRectangle;
      }
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);
      if (channel == GH_CanvasChannel.Objects)
      {
        GH_PaletteStyle myStyle = new GH_PaletteStyle(System.Drawing.ColorTranslator.FromHtml("#B3B3B3"), System.Drawing.ColorTranslator.FromHtml("#FFFFFF"), System.Drawing.ColorTranslator.FromHtml("#4C4C4C"));

        GH_PaletteStyle myTransparentStyle = new GH_PaletteStyle(System.Drawing.Color.FromArgb(0, 0, 0, 0));

        var client = Base.Client;

        var streamIdCapsule = GH_Capsule.CreateTextCapsule(box: StreamIdBounds, textbox: StreamIdBounds, palette: GH_Palette.Transparent, text: "ID: " + client?.Stream?.StreamId, highlight: 0, radius: 5);
        streamIdCapsule.Render(graphics, myStyle);
        streamIdCapsule.Dispose();

        var streamNameCapsule = GH_Capsule.CreateTextCapsule(box: StreamNameBounds, textbox: StreamNameBounds, palette: GH_Palette.Black, text: "(R) " + client?.Stream?.Name + (Base.Paused ? " (Paused)" : ""), highlight: 0, radius: 5);
        streamNameCapsule.Render(graphics, myStyle);
        streamNameCapsule.Dispose();

        if (client?.BaseUrl != null)
        {
          var dotCom = client?.BaseUrl.Split('.').Last();
          var clintNickname = Base.Client.BaseUrl.Replace("https://", "").Replace("." + dotCom, "");
          var clientNameCapsule = GH_Capsule.CreateTextCapsule(box: ClientNameBounds, textbox: ClientNameBounds, palette: GH_Palette.Black, text: clintNickname, highlight: 0, radius: 5);
          clientNameCapsule.Render(graphics, myStyle);
          clientNameCapsule.Dispose();
        }

        //var pauseStreamingButton = GH_Capsule.CreateTextCapsule(PauseButtonBounds, PauseButtonBounds, GH_Palette.Black, "");
        //pauseStreamingButton.Text = Base.Paused() ? "Paused" : "Streaming";
        //pauseStreamingButton.Render(graphics, myStyle);

        var pauseStreamingButton = GH_Capsule.CreateCapsule(PauseButtonBounds, GH_Palette.Transparent, 30, 0);
        pauseStreamingButton.Render(graphics, Base.Paused ? Properties.Resources.play25px : Properties.Resources.pause25px, myTransparentStyle);
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button == System.Windows.Forms.MouseButtons.Left)
      {
        if (((RectangleF)PauseButtonBounds).Contains(e.CanvasLocation))
        {
          Base.Paused = !Base.Paused;
          component.ExpireSolution(true);
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown(sender, e);
    }
  }
};

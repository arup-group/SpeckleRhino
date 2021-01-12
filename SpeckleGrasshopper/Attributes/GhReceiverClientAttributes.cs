//extern alias SpeckleNewtonsoft;
using System.Linq;

using Grasshopper.Kernel;
using System.Drawing;
using Grasshopper.GUI.Canvas;
using Grasshopper.GUI;

namespace SpeckleGrasshopper.Attributes
{
  public class GhReceiverClientAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
  {
    GhReceiverClient Base;
    Rectangle BaseRectangle;
    Rectangle StreamIdBounds;
    Rectangle StreamNameBounds;
    private Rectangle ClientNameBounds;
    Rectangle PauseButtonBounds;

    public GhReceiverClientAttributes(GhReceiverClient component) : base(component)
    {
      Base = component;
    }

    protected override void Layout()
    {
      base.Layout();
      BaseRectangle = GH_Convert.ToRectangle(Bounds);
      StreamIdBounds = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 120) * 0.5), BaseRectangle.Y - 25, 120, 20);
      StreamNameBounds = new Rectangle(StreamIdBounds.X, BaseRectangle.Y - 50, 120, 20);
      ClientNameBounds = new Rectangle(StreamIdBounds.X, BaseRectangle.Y - 75, 120, 20);
      PauseButtonBounds = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 30) * 0.5), BaseRectangle.Y + BaseRectangle.Height, 30, 30);

      var newBaseRectangle = new Rectangle(BaseRectangle.X, BaseRectangle.Y, BaseRectangle.Width, BaseRectangle.Height + 33);
      Bounds = newBaseRectangle;
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);
      if (channel == GH_CanvasChannel.Objects)
      {
        var myStyle = new GH_PaletteStyle(ColorTranslator.FromHtml("#B3B3B3"), ColorTranslator.FromHtml("#FFFFFF"), ColorTranslator.FromHtml("#4C4C4C"));

        var myTransparentStyle = new GH_PaletteStyle(Color.FromArgb(0, 0, 0, 0));

        var streamIdCapsule = GH_Capsule.CreateTextCapsule(box: StreamIdBounds, textbox: StreamIdBounds, palette: GH_Palette.Transparent, text: "ID: " + Base.StreamId, highlight: 0, radius: 5);
        streamIdCapsule.Render(graphics, myStyle);
        streamIdCapsule.Dispose();

        var streamNameCapsule = GH_Capsule.CreateTextCapsule(box: StreamNameBounds, textbox: StreamNameBounds, palette: GH_Palette.Black, text: "(R) " + Base.NickName + (Base.Paused ? " (Paused)" : ""), highlight: 0, radius: 5);
        streamNameCapsule.Render(graphics, myStyle);
        streamNameCapsule.Dispose();

        if (Base.Client != null && Base.Client.BaseUrl != null)
        {
          var dotCom = Base.Client.BaseUrl.Split('.').Last();
          var clintNickname = Base.Client.BaseUrl.Replace("https://", "").Replace("." + dotCom, "");
          var clientNameCapsule = GH_Capsule.CreateTextCapsule(box: ClientNameBounds, textbox: ClientNameBounds, palette: GH_Palette.Black, text: clintNickname, highlight: 0, radius: 5);
          clientNameCapsule.Render(graphics, myStyle);
          clientNameCapsule.Dispose();
        }

        //var pauseStreamingButton = GH_Capsule.CreateTextCapsule(PauseButtonBounds, PauseButtonBounds, GH_Palette.Black, "");
        //pauseStreamingButton.Text = Base.Paused ? "Paused" : "Streaming";
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
          Base.ExpireSolution(true);
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown(sender, e);
    }

  }
};

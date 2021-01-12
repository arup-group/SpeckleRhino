using System.Drawing;
using System.Linq;
using Grasshopper.GUI;
using Grasshopper.GUI.Canvas;
using Grasshopper.Kernel;

namespace SpeckleGrasshopper.Attributes
{
  public class GhSenderClientAttributes : Grasshopper.Kernel.Attributes.GH_ComponentAttributes
  {
    private GhSenderClient Base;
    private Rectangle BaseRectangle;
    private Rectangle StreamIdBounds;
    private Rectangle StreamNameBounds;
    private Rectangle ClientNameBounds;
    private Rectangle PushStreamButtonRectangle;

    public GhSenderClientAttributes(GhSenderClient component) : base(component)
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
      PushStreamButtonRectangle = new Rectangle((int)(BaseRectangle.X + (BaseRectangle.Width - 30) * 0.5), BaseRectangle.Y + BaseRectangle.Height, 30, 30);

      if (Base.ManualMode)
      {
        Rectangle newBaseRectangle = new Rectangle(BaseRectangle.X, BaseRectangle.Y, BaseRectangle.Width, BaseRectangle.Height + 33);
        Bounds = newBaseRectangle;
      }
    }

    protected override void Render(GH_Canvas canvas, Graphics graphics, GH_CanvasChannel channel)
    {
      base.Render(canvas, graphics, channel);

      if (channel == GH_CanvasChannel.Objects)
      {
        GH_PaletteStyle myStyle = new GH_PaletteStyle(System.Drawing.ColorTranslator.FromHtml(Base.EnableRemoteControl ? "#147DE9" : "#B3B3B3"), System.Drawing.ColorTranslator.FromHtml("#FFFFFF"), System.Drawing.ColorTranslator.FromHtml(Base.EnableRemoteControl ? "#ffffff" : "#4C4C4C"));

        GH_PaletteStyle myTransparentStyle = new GH_PaletteStyle(System.Drawing.Color.FromArgb(0, 0, 0, 0));

        var streamIdCapsule = GH_Capsule.CreateTextCapsule(box: StreamIdBounds, textbox: StreamIdBounds, palette: Base.EnableRemoteControl ? GH_Palette.Black : GH_Palette.Transparent, text: Base.EnableRemoteControl ? "Remote Controller" : "ID: " + (Base.Client != null ? Base.Client.StreamId : "error"), highlight: 0, radius: 5);
        streamIdCapsule.Render(graphics, myStyle);
        streamIdCapsule.Dispose();

        var streamNameCapsule = GH_Capsule.CreateTextCapsule(box: StreamNameBounds, textbox: StreamNameBounds, palette: GH_Palette.Black, text: "(S) " + Base.NickName, highlight: 0, radius: 5);
        streamNameCapsule.Render(graphics, myStyle);
        streamNameCapsule.Dispose();

        var dotCom = Base.Client.BaseUrl.Split('.').Last();
        var clintNickname = Base.Client.BaseUrl.Replace("https://", "").Replace("." + dotCom, "");
        var clientNameCapsule = GH_Capsule.CreateTextCapsule(box: ClientNameBounds, textbox: ClientNameBounds, palette: GH_Palette.Black, text: clintNickname, highlight: 0, radius: 5);
        clientNameCapsule.Render(graphics, myStyle);
        clientNameCapsule.Dispose();

        if (Base.ManualMode)
        {
          var pushStreamButton = GH_Capsule.CreateCapsule(PushStreamButtonRectangle, GH_Palette.Pink, 2, 0);
          pushStreamButton.Render(graphics, true ? Properties.Resources.play25px : Properties.Resources.pause25px, myTransparentStyle);
        }
      }
    }

    public override GH_ObjectResponse RespondToMouseDown(GH_Canvas sender, GH_CanvasMouseEvent e)
    {
      if (e.Button == System.Windows.Forms.MouseButtons.Left)
      {
        if (((RectangleF)PushStreamButtonRectangle).Contains(e.CanvasLocation))
        {
          Base.ManualUpdate();
          //Base.ExpireSolution( true );
          return GH_ObjectResponse.Handled;
        }
      }
      return base.RespondToMouseDown(sender, e);
    }

  }
}

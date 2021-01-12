using System;

namespace SpeckleGrasshopper
{
  //source: https://stackoverflow.com/questions/2278525/system-timers-timer-how-to-get-the-time-remaining-until-elapse
  public class TimerPlus : System.Timers.Timer
  {
    private DateTime m_dueTime;

    public TimerPlus() : base() => this.Elapsed += this.ElapsedAction;
    public TimerPlus(double interval) : base(interval) => this.Elapsed += this.ElapsedAction;

    protected new void Dispose()
    {
      this.Elapsed -= this.ElapsedAction;
      base.Dispose();
    }

    public double TimeLeft => (m_dueTime - DateTime.Now).TotalMilliseconds;
    public new void Start()
    {
      m_dueTime = DateTime.Now.AddMilliseconds(Interval);
      base.Start();
    }

    private void ElapsedAction(object sender, System.Timers.ElapsedEventArgs e)
    {
      if (this.AutoReset)
        this.m_dueTime = DateTime.Now.AddMilliseconds(this.Interval);
    }
  }
}



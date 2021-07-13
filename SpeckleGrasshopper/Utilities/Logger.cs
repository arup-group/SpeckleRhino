using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeckleGrasshopper.Utilities
{
  /// <summary>
  /// Logs to file or console
  /// </summary>
  /// <remarks>the Logger looks for "SPECKLE_RHINO_LOG_FILE" and "SPECKLE_RHINO_LOG_CONSOLE" env vars to
  /// log to file (path "%TEMP%rhinospeckle.txt") or console respectively</remarks>
  public class Logger
  {
    private const string LOG_FILE_VAR_NAME = "SPECKLE_RHINO_LOG_FILE";
    private const string LOG_CONSOLE_VAR_NAME = "SPECKLE_RHINO_LOG_CONSOLE";
    private const string _Log_File_Name = @"rhinospeckle.txt";

    private static string _tempPath;
    private static ReaderWriterLockSlim lock_ = new ReaderWriterLockSlim();

    static Logger()
    {
      if (Env.GetEnvironmentBool(LOG_FILE_VAR_NAME, false))
      {
        _tempPath = Path.GetTempPath();
      }
    }

    public static void Log(string msg)
    {
      WriteData(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff - ") + msg);
    }

    private static void WriteData(string dataWh)
    {
      lock_.EnterWriteLock();
      try
      {
        if (Env.GetEnvironmentBool(LOG_CONSOLE_VAR_NAME, false))
        {
          Console.WriteLine(dataWh);
        }
        if (Env.GetEnvironmentBool(LOG_FILE_VAR_NAME, false))
        {
          string filePath = Path.Combine(_tempPath, _Log_File_Name);
          dataWh += Environment.NewLine;
          using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
          {
            byte[] dataAsByteArray = new UTF8Encoding(true).GetBytes(dataWh);
            fs.Write(dataAsByteArray, 0, dataWh.Length);
          }
        }
      }
      finally
      {
        lock_.ExitWriteLock();
      }
    }
  }
}

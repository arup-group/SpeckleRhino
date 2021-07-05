using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpeckleGrasshopper
{
  public class FileLogger
  {
    private static string _tempPath;
    static FileLogger()
    {
      _tempPath = Path.GetTempPath();
    }

    private const string _Log_File_Name = @"rhinospeckle.txt";
    public static void Log(string msg)
    {
      WriteData(Path.Combine(_tempPath, _Log_File_Name), DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff - ") + msg);
    }

    public static void Log(Exception ex)
    {
      File.AppendAllLines(Path.Combine(_tempPath, _Log_File_Name), new string[2] { DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fff - [ERROR] ") + ex.Message, ex.StackTrace });
    }

    private static ReaderWriterLockSlim lock_ = new ReaderWriterLockSlim();
    private static void WriteData(string filePath, string dataWh)
    {
      lock_.EnterWriteLock();
      Console.WriteLine(dataWh);
      dataWh += Environment.NewLine;
      try
      {
        using (var fs = new FileStream(filePath, FileMode.Append, FileAccess.Write))
        {
          byte[] dataAsByteArray = new UTF8Encoding(true).GetBytes(dataWh);
          fs.Write(dataAsByteArray, 0, dataWh.Length);
        }
      }
      finally
      {
        lock_.ExitWriteLock();
      }
    }
  }
}

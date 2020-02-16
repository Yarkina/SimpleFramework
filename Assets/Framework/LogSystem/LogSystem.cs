using System;

public enum LogLvl
{
  Debug,
  Info,
  Warn,
  Error,
  Assert,
}
public delegate void OutputDelegation(LogLvl type, string msg);

public class LogSystem
{
  public static OutputDelegation OnOutput;

  public static void Debug(string format, params object[] args)
  {
    string str = string.Format("[Debug]:" + format, args);
    Output(LogLvl.Debug, str);
  }
  public static void Info(string format, params object[] args)
  {
    string str = string.Format("[Info]:" + format, args);
    Output(LogLvl.Info, str);
  }
  public static void Warn(string format, params object[] args)
  {
    string str = string.Format("[Warn]:" + format, args);
    Output(LogLvl.Warn, str);
  }
  public static void Error(string format, params object[] args)
  {
    string str = string.Format("[Error]:" + format, args);
    Output(LogLvl.Error, str);
  }
  public static void Assert(bool check, string format, params object[] args)
  {
    if (!check)
    {
      string str = string.Format("[Assert]:" + format, args);
      Output(LogLvl.Assert, str);
    }
  }

  private static void Output(LogLvl type, string msg)
  {
    if (null != OnOutput)
    {
      OnOutput(type, msg);
    }
  }
}
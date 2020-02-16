using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Helper
{
    public sealed class LogHelper
    {
#if !UNITY_2017_1_OR_NEWER
        class LogData
        {
            public string Data
            {
                get;
                set;
            }
            public ConsoleColor Color
            {
                get;
                set;
            }
        }

        static LogHelper()
        {
            Task.Run(() =>
            {
                LogData ld;
                while (true)
                {
                    if (logDatas.TryDequeue(out ld))
                    {
                        Console.ForegroundColor = ld.Color;
                        Console.WriteLine(ld.Data);
                    }
                    else
                    {
                        Thread.Sleep(50);
                    }
                }
            });
        }

        static ConcurrentQueue<LogData> logDatas = new ConcurrentQueue<LogData>();
        static void PushLog(string log, ConsoleColor color)
        {
            logDatas.Enqueue(new LogData { Data = log, Color = color });
        }
#endif
        public enum Level {
            Debug = 0,
            Log = 1,
            Warning = 2,
            Exception = 3,
            Error = 4,
            None = 5,
        }
        public static Level LogLevel = Level.Debug;
        public static void Debug(string format, params object[] args)
        {
            if (LogLevel > Level.Debug) { return; }
            FixFileLineMethod(ref format);
#if UNITY_2017_1_OR_NEWER
            UnityEngine.Debug.Log(string.Format(format,args));
#else
            PushLog(string.Format(format, args), ConsoleColor.Green);
#endif
        }
        public static void Log(string format, params object[] args)
        {
            if (LogLevel > Level.Log) { return; }
            FixFileLineMethod(ref format);
#if UNITY_2017_1_OR_NEWER
            UnityEngine.Debug.Log(string.Format(format, args));
#else
            PushLog(string.Format(format, args), ConsoleColor.White);
#endif
        }
        public static void Warning(string format, params object[] args)
        {
            if (LogLevel > Level.Warning) { return; }
            FixFileLineMethod(ref format);
#if UNITY_2017_1_OR_NEWER
            UnityEngine.Debug.LogWarning(string.Format(format, args));
#else
            PushLog(string.Format(format, args), ConsoleColor.Yellow);
#endif
        }
        public static void Exception(Exception e, [CallerLineNumber] int lineNumber = 0, [CallerMemberName] string caller = null, [CallerFilePath] string file = null)
        {
            if (LogLevel > Level.Exception) { return; }
            var format = string.Format("{0}", e);
            FixFileLineMethod(ref format, lineNumber, caller, file);
#if UNITY_2017_1_OR_NEWER
            UnityEngine.Debug.LogError(format);
#else
            format += "\n" + Environment.StackTrace;
            PushLog(format, ConsoleColor.Red);
#endif
        }
        public static void Error(string format, params object[] args)
        {
            if (LogLevel > Level.Error) { return; }
            FixFileLineMethod(ref format);
#if UNITY_2017_1_OR_NEWER
            UnityEngine.Debug.LogError(string.Format(format, args));
#else
            format += "\n" + Environment.StackTrace;
            PushLog(string.Format(format, args), ConsoleColor.DarkRed);
#endif
        }

        static string FixFileLineMethod(ref string format, int lineNumber = 0, string caller = null, string file = null)
        {
            if (string.IsNullOrEmpty(caller) || string.IsNullOrEmpty(file))
            {
                var stack = new StackTrace(true);
                if(stack != null)
                {
                    var frame = stack.GetFrame(2);
                    if(frame != null)
                    {
                        var method = frame.GetMethod();
                        if (method != null)
                        {
                            caller = frame.GetMethod().Name;
                        }
                        lineNumber = frame.GetFileLineNumber();
                        file = frame.GetFileName();
                    }
                }
            }
            format = file + "<" + Thread.CurrentThread.ManagedThreadId + ">" + ":[" + caller + "]" + lineNumber + " " + format;
            return format;
        }
    }
}

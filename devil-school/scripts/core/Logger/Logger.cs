
using Godot;
using System;
using System.Diagnostics;

namespace EGame
{
    public static class Logger
    {
        public enum LogLevel
        {
            VeryDebug = 0,
            Debug = 1,
            Warn = 2,
            Error = 3
        }

        public static void VeyDebug(string message, int skip_frame = 2)
        {
            LogMessage(LogLevel.VeryDebug, message, false, skip_frame);
        }

        public static void Debug(string message, int skip_frame = 2)
        {
            LogMessage(LogLevel.Debug, message, false, skip_frame);
        }

        public static void Warn(string message, int skip_frame = 2)
        {
            LogMessage(LogLevel.Warn, message, false, skip_frame);
        }

        public static void Error(string message, bool is_show_stack_trace = false, int skip_frame = 2)
        {
            LogMessage(LogLevel.Error, message, is_show_stack_trace, skip_frame);
        }

        /// <summary>
        /// skip_frame为略过调用栈的顶部
        /// </summary>
        private static void LogMessage(LogLevel level, string message, bool is_show_stack_trace, int skip_frame, bool is_color = true)
        {
            if (level < Settins.LogLevel)
                return;

            var time_stamp = DateTime.Now.ToString("HH:mm:ss");

            string normal_message = $"{GetLevelStr(level)}";

            if (is_color)
                normal_message = $"[color={GetColorCode(level)}]" + normal_message + "[/color]";

            normal_message += $" {time_stamp} {message}";

            //报错信息要输出报错位置
            if(is_show_stack_trace)
            {
                StackTrace st = new StackTrace(skip_frame, true);
                normal_message += $"\n{st}";
            }

            GD.PrintRich(normal_message);
        }
        
        private static string GetLevelStr(LogLevel level)
        {
            switch(level)
            {
                case LogLevel.VeryDebug:    return "[VRBDBG]";  // Very Debug
                case LogLevel.Debug:        return "[DEBUG]";
                case LogLevel.Warn:         return "[WARN]";   // 修正拼写
                case LogLevel.Error:        return "[ERROR]";
            }
            return "";
        }

        public static string GetColorCode(LogLevel level)
        {
            switch (level)
            {
                case LogLevel.VeryDebug:        return "#888888";   // 灰色，表示更详细的调试信息
                case LogLevel.Debug:            return "#76FF56";   // 绿色
                case LogLevel.Warn:             return "#FFCB3D";   // 黄色
                case LogLevel.Error:            return "#FF4747";   // 红色
            }
            return "";
        }
    }
}
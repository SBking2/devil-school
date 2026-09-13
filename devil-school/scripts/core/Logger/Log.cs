
using Godot;
using System;
using System.Diagnostics;

namespace EGame
{
    public static class Log
    {
        public enum LogLevel
        {
            VeryDebug = 0,
            Debug = 1,
            Warn = 2,
            Error = 3
        }

        public enum LogType
        {
            None = 0,
            World = 1,
            Combat = 2,
            NetWork = 3,
            Generic = 4,
            GameSync = 5
        }

        public static void VeryDebug(string message, bool is_show_stack_trace = false, int skip_frame = 2, LogType type = LogType.None)
        {
            LogMessage(LogLevel.VeryDebug, message, is_show_stack_trace, skip_frame, type);
        }

        public static void Debug(string message, bool is_show_stack_trace = false, int skip_frame = 2, LogType type = LogType.None)
        {
            LogMessage(LogLevel.Debug, message, is_show_stack_trace, skip_frame, type);
        }

        public static void Warn(string message, bool is_show_stack_trace = false, int skip_frame = 2, LogType type = LogType.None)
        {
            LogMessage(LogLevel.Warn, message, is_show_stack_trace, skip_frame, type);
        }

        public static void Error(string message, bool is_show_stack_trace = true, int skip_frame = 2, LogType type = LogType.None)
        {
            LogMessage(LogLevel.Error, message, is_show_stack_trace, skip_frame, type);
        }

        /// <summary>
        /// skip_frame为略过调用栈的顶部
        /// </summary>
        private static void LogMessage(LogLevel level, string message, bool is_show_stack_trace, int skip_frame, LogType type, bool is_color = true)
        {
            if (level < Settins.LogLevel)
                return;

            // 分类过滤：Settins.LogType 或者本条日志的 type 有一个是 None（不限分类），或者两者相同，才输出
            if (Settins.LogType != LogType.None && type != LogType.None && Settins.LogType != type)
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
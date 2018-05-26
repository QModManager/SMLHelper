﻿#if DEBUG
using System;

namespace SMLHelper
{
    internal static class Logger
    {
        internal static void Log(string text, params object[] args)
        {
            if (args != null && args.Length > 0)
                text = string.Format(text, args);
            Console.WriteLine($"[SMLHelper] {text}");
        }
    }
}
#endif
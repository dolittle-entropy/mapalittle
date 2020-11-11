using System;

namespace MapEvents.Processors
{
    public static class Out
    {
        public static void Dump(string content, int insets = 0, bool underline = false, bool overline = false)
        {
            var insetString = new string(' ', 3 * insets);
            var message = $"[{DateTime.Now:HH:mm:ss.fff:00}] {insetString}{content}";
            var line = new string('-', message.Length);

            if(overline)
                Console.WriteLine(line);

            Console.WriteLine(message);
            if(underline)
            {
                Console.WriteLine(line);
            }
        }

        internal static void Fail(string message)
        {
            Dump($"[ERR] {message}", 0);
        }

        internal static void Warn(string message, int insets = 1)
        {
            Dump($"[WRN] {message}", insets);
        }
    }
}

using System;

namespace VeemExercise.Infrastructure.Common
{
    class ConsoleIndicator
    {
        public static void ShowProgress(long position, long length)
        {
            Console.CursorVisible = false;
            Console.SetCursorPosition(0, 1);
            var percents = (position / (length / 100));
            Console.Write($"Progress: {percents}%");
            Console.WriteLine();
            Console.CursorVisible = true;
        }
    }
}

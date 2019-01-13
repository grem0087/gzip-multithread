using System;

namespace VeemExercise.Infrastructure
{
    static class CommandValidator
    {
        private static string UnknownCommand => @"Unknown command. Please follow the next template:" + Environment.NewLine +
                    "GZipTest.exe compress/decompress [имя исходного файла] [имя результирующего файла] ";

        public static void Validate(string[] args)
        {
            if (args.Length != 3)
            {
                throw new Exception(UnknownCommand);
            }
        }
    }
}

namespace VeemExercise.Infrastructure.Commands
{
    public class Command
    {
        public string Type { get; }
        public string InputFilename { get; }
        public string OutputFilename { get; }

        public Command(string[] args)
        {
            CommandValidator.Validate(args);

            Type = args[0].ToLower();
            InputFilename = args[1].ToLower();
            OutputFilename = args[2].ToLower();
        }
    }
}

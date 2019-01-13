namespace VeemExercise.Infrastructure.Interfaces
{
    interface IGZipPackager
    {
        void Read(string fileName, MyCancellationToken cancellationToken);
        void Write(string fileName, MyCancellationToken cancellationToken);
        void Process(MyCancellationToken cancellationToken);
    }
}
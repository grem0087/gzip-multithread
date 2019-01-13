namespace VeemExercise.Infrastructure.DataContainers
{
    internal class DecompressedData : IObjectWithId
    {
        public int? Id { get; set; }
        public byte[] Buffer { get; set; }
    }
}

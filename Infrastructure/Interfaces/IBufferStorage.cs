namespace VeemExercise.Infrastructure.Interfaces
{
    public interface IBufferStorage<T> where T: IObjectWithId
    {
        void Add(T obj);
        void Close();
        T ReadNext();
    }
}
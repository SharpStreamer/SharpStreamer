namespace SharpStreamer.Abstractions;

public interface ITransaction : IDisposable
{
    void Commit();

    void Rollback();
}
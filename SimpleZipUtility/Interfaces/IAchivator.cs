
using System.IO;
namespace SimpleZipUtility.Interfaces
{
    public interface IArchivator
    {
        long ToralBytesRead { get;}
        bool DoAction(Stream init, Stream archive, long bufferSizeMb);
    }
}

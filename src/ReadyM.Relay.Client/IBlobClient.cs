using System.Threading.Tasks;
using ReadyM.Relay.Common;

namespace ReadyM.Relay.Client;

public interface IBlobClient
{
    /// <returns>Whether upload was successful.</returns>
    Task<bool> UploadBlob(BlobInfo blob);

    Task<BlobInfo> DownloadBlob(string name);
}
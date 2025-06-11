using System.Threading;
using System.Threading.Tasks;
using ReadyM.Relay.Common;

namespace ReadyM.Relay.Client;

public interface IBlobClient
{
    /// <returns>Whether upload was successful.</returns>
    Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default);

    Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default);
}
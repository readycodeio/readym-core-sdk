using System.Threading;
using System.Threading.Tasks;

namespace ReadyM.Api.Multiplayer.Client.Blobs;

public interface IBlobClient
{
    /// <returns>Whether upload was successful.</returns>
    Task<bool> UploadBlobAsync(BlobInfo blob, CancellationToken ct = default);

    Task<BlobInfo?> DownloadBlobAsync(string name, CancellationToken ct = default);
}
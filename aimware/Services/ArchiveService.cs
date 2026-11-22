using System.IO;
using SharpCompress.Archives;
using SharpCompress.Common;

namespace aimware.Services;

public class ArchiveService
{
    public void Extract(string archive, string destination)
    {
        Directory.CreateDirectory(destination);
        using (var mode = ArchiveFactory.OpenArchive(archive))
        {
            foreach (var entry in mode.Entries)
                if (!entry.IsDirectory)
                    entry.WriteToDirectory(destination, new ExtractionOptions
                    {
                        ExtractFullPath = true,
                        Overwrite = true
                    });
        }
    }
}

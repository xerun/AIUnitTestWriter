using AIUnitTestWriter.Interfaces;
using AIUnitTestWriter.SettingOptions;
using Microsoft.Extensions.Options;

namespace AIUnitTestWriter
{
    public class SkippedFilesManager : ISkippedFilesManager
    {
        private readonly HashSet<string> _skippedFiles;

        public SkippedFilesManager(IOptions<SkippedFilesSettings> options)
        {
            _skippedFiles = new HashSet<string>(options.Value.SkippedFiles, StringComparer.OrdinalIgnoreCase);
        }

        /// <inheritdoc/>
        public bool ShouldSkip(string filePath)
        {
            return _skippedFiles.Contains(Path.GetFileName(filePath));
        }
    }
}

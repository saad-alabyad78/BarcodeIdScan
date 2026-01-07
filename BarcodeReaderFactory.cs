using BarcodeIdScan.Implementations;

namespace BarcodeIdScan {
    /// <summary>
    /// Factory for creating barcode reader instances
    /// </summary>
    public static class BarcodeReaderFactory {
        public enum ReaderType {
            SDK,
            CLI
        }

        /// <summary>
        /// Creates a barcode reader instance
        /// </summary>
        /// <param name="type">Type of reader to create</param>
        /// <param name="cliPath">Path to CLI exe (only needed for CLI type)</param>
        /// <returns>IBarcodeReader instance</returns>
        public static IBarcodeReader CreateReader(ReaderType type) {
            return type switch {
                ReaderType.SDK => new SdkBarcodeReader(),
                ReaderType.CLI => new CliBarcodeReader(),
                _ => throw new ArgumentException($"Unknown reader type: {type}")
            };
        }
    }
}

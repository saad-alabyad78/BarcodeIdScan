// File: BarcodeIdScan/IBarcodeReader.cs
using System.Threading.Tasks;

namespace BarcodeIdScan
{
    /// <summary>
    /// Interface for barcode reading operations
    /// </summary>
    public interface IBarcodeReader
    {
        /// <summary>
        /// Reads barcode from an image file
        /// </summary>
        /// <param name="imagePath">Path to the image file</param>
        /// <param name="barcodeType">Type of barcode (e.g., "pdf417", "qr", "code128")</param>
        /// <param name="tbrCode">TBR optimization code</param>
        /// <returns>Barcode reading result</returns>
        BarcodeReadResult ReadBarcode(string imagePath, string barcodeType = "pdf417", int tbrCode = 103);

        /// <summary>
        /// Reads barcode asynchronously
        /// </summary>
        Task<BarcodeReadResult> ReadBarcodeAsync(string imagePath, string barcodeType = "pdf417", int tbrCode = 103);

        /// <summary>
        /// Gets the reader type/implementation name
        /// </summary>
        string ReaderType { get; }
    }

    /// <summary>
    /// Result object for barcode reading operations
    /// </summary>
    public class BarcodeReadResult
    {
        public bool Success { get; set; }
        public string? Type { get; set; }
        public string? Text { get; set; }
        public string? DataBase64 { get; set; }
        public int Length { get; set; }
        public BarcodeRectangle? Rectangle { get; set; }
        public string? Rotation { get; set; }
        public string? Error { get; set; }
        public string? RawJson { get; set; }
    }

    /// <summary>
    /// Barcode position information
    /// </summary>
    public class BarcodeRectangle
    {
        public int Left { get; set; }
        public int Top { get; set; }
        public int Right { get; set; }
        public int Bottom { get; set; }
    }
}
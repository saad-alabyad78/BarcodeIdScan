// File: BarcodeIdScan/Implementations/SdkBarcodeReader.cs
using System;
using System.IO;
using System.Threading.Tasks;
using Inlite.ClearImageNet;

namespace BarcodeIdScan.Implementations {
    /// <summary>
    /// Barcode reader implementation using Inlite SDK
    /// </summary>
    public class SdkBarcodeReader : IBarcodeReader {
        public string ReaderType => "SDK";

        public BarcodeReadResult ReadBarcode(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            var result = new BarcodeReadResult { Success = false };

            try {
                if (!File.Exists(imagePath)) {
                    result.Error = $"File not found: {imagePath}";
                    return result;
                }

                using (Inlite.ClearImageNet.BarcodeReader reader = new Inlite.ClearImageNet.BarcodeReader()) {
                    ConfigureBarcodeType(reader, barcodeType);
                    reader.TbrCode = (uint)tbrCode;

                    Inlite.ClearImageNet.Barcode[] barcodes = reader.Read(imagePath);

                    if (barcodes.Length == 0) {
                        result.Error = "No barcodes found in image";
                        return result;
                    }

                    var bc = barcodes[0];

                    result.Success = true;
                    result.Type = bc.Type.ToString();
                    // ✅ NO ENCODING - Return raw base64, let consumer decode
                    result.Text = null; // Will be decoded by consumer
                    result.DataBase64 = Convert.ToBase64String(bc.Data);
                    result.Length = bc.Length;
                    result.Rectangle = new BarcodeRectangle {
                        Left = bc.Rectangle.Left,
                        Top = bc.Rectangle.Top,
                        Right = bc.Rectangle.Right,
                        Bottom = bc.Rectangle.Bottom
                    };
                    result.Rotation = bc.Rotation.ToString();
                }
            }
            catch (Exception ex) {
                result.Error = $"SDK Error: {ex.Message}";
            }

            return result;
        }

        public async Task<BarcodeReadResult> ReadBarcodeAsync(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            return await Task.Run(() => ReadBarcode(imagePath, barcodeType, tbrCode));
        }

        private void ConfigureBarcodeType(Inlite.ClearImageNet.BarcodeReader reader, string barcodeType) {
            switch (barcodeType.ToLower()) {
                case "pdf417":
                case "p4":
                    reader.Pdf417 = true;
                    break;
                case "qr":
                    reader.QR = true;
                    break;
                case "datamatrix":
                case "dm":
                    reader.DataMatrix = true;
                    break;
                case "code128":
                case "c128":
                    reader.Code128 = true;
                    break;
                case "code39":
                case "c39":
                    reader.Code39 = true;
                    break;
                case "drvlic":
                case "dl":
                    reader.DrvLicID = true;
                    break;
                case "1d":
                    reader.Auto1D = true;
                    break;
                default:
                    reader.Pdf417 = true;
                    break;
            }
        }
    }
}
// File: D:\BarcodeReader\Implementations\SdkBarcodeReader.cs
using BarcodeIdScan.Services;

namespace BarcodeIdScan.Implementations {
    public class SdkBarcodeReader : IBarcodeReader {
        private readonly ImageEnhancementService _enhancementService;
        private string? _lastImagePath;

        public string ReaderType => "SDK";

        public SdkBarcodeReader() {
            _enhancementService = new ImageEnhancementService();
        }

        public BarcodeReadResult ReadBarcode(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            _lastImagePath = imagePath;
            var result = new BarcodeReadResult { Success = false, ImagePath = imagePath };

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
                    result.Text = null;
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

        public async Task<BarcodeReadResult?> ReadAsync(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            var result = await ReadBarcodeAsync(imagePath, barcodeType, tbrCode);
            return result.Success ? result : null;
        }

        public async Task<BarcodeReadResult> ReadBarcodeAsync(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            return await Task.Run(() => ReadBarcode(imagePath, barcodeType, tbrCode));
        }

        public async Task<BarcodeReadResult> ReadBarcodeWithEnhancementAsync(
            string? imagePath = null,
            EnhancementTechnique[]? enhancements = null,
            string barcodeType = "pdf417",
            int tbrCode = 103) {

            return await Task.Run(() => {
                var effectiveImagePath = imagePath ?? _lastImagePath;

                if (string.IsNullOrEmpty(effectiveImagePath)) {
                    return new BarcodeReadResult {
                        Success = false,
                        Error = "No image path provided"
                    };
                }

                var effectiveEnhancements = enhancements ?? new[] {
                    EnhancementTechnique.Sharpening
                };

                var result = ReadBarcode(effectiveImagePath, barcodeType, tbrCode);
                if (result.Success) {
                    result.SuccessfulEnhancement = "None (Original)";
                    return result;
                }

                foreach (var technique in effectiveEnhancements) {
                    if (technique == EnhancementTechnique.None) continue;

                    string? enhancedPath = null;
                    try {
                        enhancedPath = _enhancementService.ApplyEnhancement(effectiveImagePath, technique);
                        if (enhancedPath == null) continue;

                        var enhancedResult = ReadBarcode(enhancedPath, barcodeType, tbrCode);

                        if (enhancedResult.Success) {
                            enhancedResult.SuccessfulEnhancement = technique.ToString();
                            enhancedResult.EnhancedImagePath = enhancedPath;
                            return enhancedResult;
                        }
                    }
                    finally {
                        if (enhancedPath != null && File.Exists(enhancedPath)) {
                            try { File.Delete(enhancedPath); } catch { }
                        }
                    }
                }

                result.Error = "No barcodes found";
                return result;
            });
        }

        private void ConfigureBarcodeType(Inlite.ClearImageNet.BarcodeReader reader, string barcodeType) {
            switch (barcodeType.ToLower()) {
                case "pdf417": case "p4": reader.Pdf417 = true; break;
                case "qr": reader.QR = true; break;
                case "datamatrix": case "dm": reader.DataMatrix = true; break;
                case "code128": case "c128": reader.Code128 = true; break;
                case "code39": case "c39": reader.Code39 = true; break;
                case "drvlic": case "dl": reader.DrvLicID = true; break;
                case "1d": reader.Auto1D = true; break;
                default: reader.Pdf417 = true; break;
            }
        }
    }
}
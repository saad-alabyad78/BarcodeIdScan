// File: D:\BarcodeReader\Implementations\CliBarcodeReader.cs
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using BarcodeIdScan.Services;

namespace BarcodeIdScan.Implementations {
    public class CliBarcodeReader : IBarcodeReader {
        private readonly string _cliPath;
        private readonly ImageEnhancementService _enhancementService;
        private string? _lastImagePath;

        public string ReaderType => "CLI";

        public CliBarcodeReader() {
            _enhancementService = new ImageEnhancementService();

            var locations = new[] {
                Path.Combine(Path.GetDirectoryName(typeof(CliBarcodeReader).Assembly.Location) ?? "", "BarcodeReaderCLI.exe"),
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BarcodeReaderCLI.exe"),
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName ?? "", "BarcodeReaderCLI.exe")
            };

            _cliPath = locations.FirstOrDefault(File.Exists) ?? locations[0];

            if (!File.Exists(_cliPath)) {
                throw new FileNotFoundException($"BarcodeReaderCLI.exe not found.");
            }
        }

        public BarcodeReadResult ReadBarcode(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            _lastImagePath = imagePath;
            var result = new BarcodeReadResult { Success = false, ImagePath = imagePath };

            try {
                if (!File.Exists(imagePath)) {
                    result.Error = $"File not found: {imagePath}";
                    return result;
                }

                string arguments = $"-type={barcodeType} \"{imagePath}\"";

                var processInfo = new ProcessStartInfo {
                    FileName = _cliPath,
                    Arguments = arguments,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    StandardOutputEncoding = Encoding.UTF8
                };

                using (var process = Process.Start(processInfo)) {
                    if (process == null) {
                        result.Error = "Failed to start CLI process";
                        return result;
                    }

                    string output = process.StandardOutput.ReadToEnd();
                    string error = process.StandardError.ReadToEnd();
                    process.WaitForExit();

                    if (process.ExitCode != 0 || !string.IsNullOrEmpty(error)) {
                        result.Error = $"CLI error: {error}";
                        return result;
                    }

                    result.RawJson = output;
                    ParseCLIOutput(output, result);
                }
            }
            catch (Exception ex) {
                result.Error = $"CLI Exception: {ex.Message}";
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
                        Error = "No image path provided and no cached path available"
                    };
                }

                var effectiveEnhancements = enhancements ?? new[] { EnhancementTechnique.Sharpening };
                var result = new BarcodeReadResult { Success = false, ImagePath = effectiveImagePath };

                try {
                    if (!File.Exists(effectiveImagePath)) {
                        result.Error = $"File not found: {effectiveImagePath}";
                        return result;
                    }

                    result = ReadBarcode(effectiveImagePath, barcodeType, tbrCode);
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
                                enhancedResult.ImagePath = effectiveImagePath;
                                return enhancedResult;
                            }
                        }
                        finally {
                            if (enhancedPath != null && File.Exists(enhancedPath)) {
                                try { File.Delete(enhancedPath); } catch { }
                            }
                        }
                    }

                    result.Error = "No barcodes found in image (even with enhancements)";
                    return result;
                }
                catch (Exception ex) {
                    result.Error = $"Enhancement Error: {ex.Message}";
                    return result;
                }
            });
        }

        private void ParseCLIOutput(string json, BarcodeReadResult result) {
            try {
                using (JsonDocument doc = JsonDocument.Parse(json)) {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("sessions", out var sessions) && sessions.GetArrayLength() > 0) {
                        var session = sessions[0];

                        if (session.TryGetProperty("barcodes", out var barcodes) && barcodes.GetArrayLength() > 0) {
                            var barcode = barcodes[0];

                            result.Success = true;
                            result.Type = barcode.GetProperty("type").GetString();
                            result.Text = barcode.GetProperty("text").GetString();
                            result.DataBase64 = barcode.GetProperty("data").GetString();
                            result.Length = barcode.GetProperty("length").GetInt32();

                            if (barcode.TryGetProperty("rectangle", out var rect)) {
                                result.Rectangle = new BarcodeRectangle {
                                    Left = rect.GetProperty("left").GetInt32(),
                                    Top = rect.GetProperty("top").GetInt32(),
                                    Right = rect.GetProperty("right").GetInt32(),
                                    Bottom = rect.GetProperty("bottom").GetInt32()
                                };
                            }
                        } else {
                            result.Error = "No barcodes found in image";
                        }
                    }
                }
            }
            catch (Exception ex) {
                result.Error = $"Failed to parse CLI JSON: {ex.Message}";
            }
        }
    }
}


using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace BarcodeIdScan.Implementations {
    /// <summary>
    /// Barcode reader implementation using CLI executable
    /// </summary>
    public class CliBarcodeReader : IBarcodeReader {
        private readonly string _cliPath;

        public string ReaderType => "CLI";


        public CliBarcodeReader() {

            // Try multiple locations
            var locations = new[]
            {
                // 1. Same directory as this assembly (BarcodeIdScan.dll)
                Path.Combine(Path.GetDirectoryName(typeof(CliBarcodeReader).Assembly.Location) ?? "", "BarcodeReaderCLI.exe"),
            
                // 2. Application base directory
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BarcodeReaderCLI.exe"),
            
                // 3. Parent directory
                Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory)?.FullName ?? "", "BarcodeReaderCLI.exe")
            };

            _cliPath = locations.FirstOrDefault(File.Exists) ?? locations[0];

            Console.WriteLine(_cliPath);

            if (!File.Exists(_cliPath)) {
                throw new FileNotFoundException($"BarcodeReaderCLI.exe not found. Searched locations:\n" +
                    $"- {Path.Combine(Path.GetDirectoryName(typeof(CliBarcodeReader).Assembly.Location) ?? "", "BarcodeReaderCLI.exe")}\n" +
                    $"- {Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "BarcodeReaderCLI.exe")}");
            }
        }

        public BarcodeReadResult ReadBarcode(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            var result = new BarcodeReadResult { Success = false };

            try {
                if (!File.Exists(imagePath)) {
                    result.Error = $"File not found: {imagePath}";
                    return result;
                }

                // Build command arguments
                string arguments = $"-type={barcodeType} -tbr={tbrCode} \"{imagePath}\"";

                // Execute CLI
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

                    // Store raw JSON
                    result.RawJson = output;

                    // Parse JSON output
                    ParseCLIOutput(output, result);
                }
            }
            catch (Exception ex) {
                result.Error = $"CLI Exception: {ex.Message}";
            }

            return result;
        }

        public async Task<BarcodeReadResult> ReadBarcodeAsync(string imagePath, string barcodeType = "pdf417", int tbrCode = 103) {
            return await Task.Run(() => ReadBarcode(imagePath, barcodeType, tbrCode));
        }

        private void ParseCLIOutput(string json, BarcodeReadResult result) {
            try {
                using (JsonDocument doc = JsonDocument.Parse(json)) {
                    var root = doc.RootElement;

                    if (root.TryGetProperty("sessions", out var sessions) &&
                        sessions.GetArrayLength() > 0) {
                        var session = sessions[0];

                        if (session.TryGetProperty("barcodes", out var barcodes) &&
                            barcodes.GetArrayLength() > 0) {
                            var barcode = barcodes[0];

                            result.Success = true;
                            result.Type = barcode.GetProperty("type").GetString();
                            result.Text = barcode.GetProperty("text").GetString();
                            result.DataBase64 = barcode.GetProperty("data").GetString();
                            result.Length = barcode.GetProperty("length").GetInt32();

                            // Parse rectangle if available
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

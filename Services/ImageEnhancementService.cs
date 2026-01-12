// File: BarcodeIdScan/Services/ImageEnhancementService.cs
using System.Drawing;
using System.Drawing.Imaging;

namespace BarcodeIdScan.Services {
    /// <summary>
    /// Service for applying image enhancement techniques to improve barcode detection
    /// </summary>
    public class ImageEnhancementService {
        private static readonly string TEMP_DIR = Path.Combine(Path.GetTempPath(), "BarcodeEnhancement");

        public ImageEnhancementService() {
            // Ensure temp directory exists
            if (!Directory.Exists(TEMP_DIR)) {
                Directory.CreateDirectory(TEMP_DIR);
                Console.WriteLine($"[ImageEnhancement] Created temp directory: {TEMP_DIR}");
            }
        }

        /// <summary>
        /// Applies the specified enhancement technique to an image and saves it to a temporary file
        /// </summary>
        /// <param name="imagePath">Path to the original image</param>
        /// <param name="technique">Enhancement technique to apply</param>
        /// <returns>Path to the enhanced image file, or null if enhancement failed</returns>
#pragma warning disable CA1416 // Validate platform compatibility - This library is Windows-only
        public string? ApplyEnhancement(string imagePath, EnhancementTechnique technique) {
            Console.WriteLine($"[ImageEnhancement] Starting enhancement: {technique}");
            Console.WriteLine($"[ImageEnhancement] Input image: {imagePath}");

            try {
                if (!File.Exists(imagePath)) {
                    Console.WriteLine($"[ImageEnhancement] ERROR: Image file not found: {imagePath}");
                    return null;
                }

                using (var original = new Bitmap(imagePath)) {
                    Console.WriteLine($"[ImageEnhancement] Image loaded: {original.Width}x{original.Height} pixels");

                    Bitmap? enhanced = technique switch {
                        EnhancementTechnique.Sharpening => ApplySharpening(original),
                        _ => null
                    };

                    if (enhanced == null) {
                        Console.WriteLine($"[ImageEnhancement] Enhancement technique not supported: {technique}");
                        return null;
                    }

                    string tempPath = Path.Combine(TEMP_DIR,
                        $"{Path.GetFileNameWithoutExtension(imagePath)}_{technique}_{Guid.NewGuid()}.jpg");

                    Console.WriteLine($"[ImageEnhancement] Saving enhanced image to: {tempPath}");
                    enhanced.Save(tempPath, ImageFormat.Jpeg);
                    enhanced.Dispose();

                    var fileInfo = new FileInfo(tempPath);
                    Console.WriteLine($"[ImageEnhancement] ✓ Enhancement complete. File size: {fileInfo.Length / 1024}KB");

                    return tempPath;
                }
            }
            catch (Exception ex) {
                Console.WriteLine($"[ImageEnhancement] ERROR: Enhancement {technique} failed: {ex.Message}");
                Console.WriteLine($"[ImageEnhancement] Stack trace: {ex.StackTrace}");
                return null;
            }
        }

        /// <summary>
        /// Applies sharpening filter to enhance edges
        /// </summary>
        public Bitmap ApplySharpening(Bitmap original) {
            Console.WriteLine($"[ImageEnhancement] Applying sharpening filter...");
            var startTime = DateTime.Now;

            var result = new Bitmap(original.Width, original.Height);
            float[][] kernel = new float[][] {
                new float[] {0, -1, 0},
                new float[] {-1, 5, -1},
                new float[] {0, -1, 0}
            };

            ConvolutionFilter(original, result, kernel);

            var duration = (DateTime.Now - startTime).TotalMilliseconds;
            Console.WriteLine($"[ImageEnhancement] Sharpening applied in {duration:F0}ms");

            return result;
        }

        /// <summary>
        /// Applies convolution filter using a kernel matrix
        /// </summary>
        private void ConvolutionFilter(Bitmap source, Bitmap dest, float[][] kernel) {
            int kernelSize = kernel.Length;
            int offset = kernelSize / 2;
            int totalPixels = (source.Height - 2 * offset) * (source.Width - 2 * offset);
            int processedPixels = 0;
            int lastPercent = 0;

            Console.WriteLine($"[ImageEnhancement] Processing {totalPixels:N0} pixels...");

            for (int y = offset; y < source.Height - offset; y++) {
                for (int x = offset; x < source.Width - offset; x++) {
                    float r = 0, g = 0, b = 0;

                    for (int ky = 0; ky < kernelSize; ky++) {
                        for (int kx = 0; kx < kernelSize; kx++) {
                            var pixel = source.GetPixel(x + kx - offset, y + ky - offset);
                            float weight = kernel[ky][kx];
                            r += pixel.R * weight;
                            g += pixel.G * weight;
                            b += pixel.B * weight;
                        }
                    }

                    r = Math.Max(0, Math.Min(255, r));
                    g = Math.Max(0, Math.Min(255, g));
                    b = Math.Max(0, Math.Min(255, b));

                    dest.SetPixel(x, y, Color.FromArgb((int)r, (int)g, (int)b));

                    processedPixels++;

                    // Log progress every 25%
                    int currentPercent = (processedPixels * 100) / totalPixels;
                    if (currentPercent >= lastPercent + 25 && currentPercent <= 100) {
                        Console.WriteLine($"[ImageEnhancement] Progress: {currentPercent}%");
                        lastPercent = currentPercent;
                    }
                }
            }

            Console.WriteLine($"[ImageEnhancement] Convolution filter complete: {processedPixels:N0} pixels processed");
        }
#pragma warning restore CA1416
    }
}
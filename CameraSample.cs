using Windows.Devices.Enumeration;
using Windows.Graphics.Imaging;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage.Streams;

namespace AutoBright;

internal static class CameraSample
{
    internal static Task<double> ReadAsync(string cameraId) => StaWorker.RunAsync(() => ReadCoreAsync(cameraId));

    private static async Task<double> ReadCoreAsync(string cameraId)
    {
        string stage = "enumerating cameras";
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(30));
            var devices = await DeviceInformation.FindAllAsync(DeviceClass.VideoCapture).AsTask(timeout.Token);
            string id = string.IsNullOrEmpty(cameraId) ? devices.FirstOrDefault()?.Id
                ?? throw new IOException("No camera found. Connect a webcam and try again.") : cameraId;
            if (!devices.Any(d => d.Id == id)) throw new IOException("The selected camera is disconnected. Choose another camera.");
            using var capture = new MediaCapture();
            stage = "initializing the camera";
            await capture.InitializeAsync(new MediaCaptureInitializationSettings
            {
                VideoDeviceId = id,
                StreamingCaptureMode = StreamingCaptureMode.Video,
                MemoryPreference = MediaCaptureMemoryPreference.Cpu
            }).AsTask(timeout.Token);
            using var stream = new InMemoryRandomAccessStream();
            stage = "capturing a photo";
            await capture.CapturePhotoToStreamAsync(ImageEncodingProperties.CreateJpeg(), stream).AsTask(timeout.Token);
            stream.Seek(0);
            stage = "decoding the camera image";
            var decoder = await BitmapDecoder.CreateAsync(stream);
            var pixels = await decoder.GetPixelDataAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore,
                new BitmapTransform { ScaledWidth = 64, ScaledHeight = 48 }, ExifOrientationMode.IgnoreExifOrientation,
                ColorManagementMode.DoNotColorManage);
            return Luminance(pixels.DetachPixelData());
        }
        catch (OperationCanceledException ex)
        {
            throw new IOException($"Camera timed out while {stage}. Close other camera apps and retry.", ex);
        }
        catch (Exception ex)
        {
            throw new IOException($"Camera failed while {stage} (0x{ex.HResult:X8}): {ex.Message}", ex);
        }
    }

    internal static double Luminance(byte[] bgra)
    {
        if (bgra.Length == 0 || bgra.Length % 4 != 0) throw new ArgumentException("Expected BGRA pixels.");
        double sum = 0;
        for (int i = 0; i < bgra.Length; i += 4)
            sum += .0722 * bgra[i] + .7152 * bgra[i + 1] + .2126 * bgra[i + 2];
        return sum / (bgra.Length / 4) / 255;
    }

    internal static byte Target(double light, byte current)
    {
        if (!double.IsFinite(light) || light < 0 || light > 1) throw new ArgumentOutOfRangeException(nameof(light));
        int desired = (int)Math.Round(20 + 64 * Math.Sqrt(light));
        if (Math.Abs(desired - current) < 5) return current;
        return (byte)Math.Clamp(desired, Math.Max(20, current - 15), Math.Max(20, Math.Min(100, current + 15)));
    }
}

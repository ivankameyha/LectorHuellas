using DPUruNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class LectorHuellas : IDisposable
{
    private Reader _lector;
    private Dictionary<int, Fmd> _huellasRegistradas;
    private CancellationTokenSource _internalCts;

    public event Action<string> OnEstadoCambiado;
    public event Action<Bitmap> OnImagenCapturada;

    public LectorHuellas()
    {
        _huellasRegistradas = new Dictionary<int, Fmd>();
    }

    public bool InicializarLector()
    {
        try
        {
            var readers = ReaderCollection.GetReaders();
            if (readers.Count == 0)
            {
                OnEstadoCambiado?.Invoke("No se encontraron lectores conectados");
                return false;
            }

            _lector = readers[0];
            var result = _lector.Open(Constants.CapturePriority.DP_PRIORITY_COOPERATIVE);

            if (result != Constants.ResultCode.DP_SUCCESS)
            {
                OnEstadoCambiado?.Invoke($"Error al abrir lector: {result}");
                return false;
            }

            OnEstadoCambiado?.Invoke("Lector inicializado correctamente");
            return true;
        }
        catch (Exception ex)
        {
            OnEstadoCambiado?.Invoke($"Error inicializando lector: {ex.Message}");
            return false;
        }
    }

    public async Task<Fmd> CapturarHuellaAsync(CancellationToken externalToken = default)
    {
        using (_internalCts = new CancellationTokenSource())
        using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(externalToken, _internalCts.Token))
        {
            try
            {
                OnEstadoCambiado?.Invoke("Coloque su dedo en el lector...");

                int resolution = _lector.Capabilities.Resolutions[0];
                int timeout = 10000;

                // Configurar cancelación
                linkedCts.Token.Register(() =>
                {
                    _lector.CancelCapture();
                    OnEstadoCambiado?.Invoke("Cancelando captura...");
                });

                var captureResult = await Task.Run(() =>
                {
                    if (linkedCts.Token.IsCancellationRequested)
                        return null;

                    return _lector.Capture(
                        Constants.Formats.Fid.ANSI,
                        Constants.CaptureProcessing.DP_IMG_PROC_DEFAULT,
                        timeout,
                        resolution);
                }, linkedCts.Token);

                if (linkedCts.Token.IsCancellationRequested)
                {
                    OnEstadoCambiado?.Invoke("Captura cancelada");
                    return null;
                }

                if (captureResult == null || captureResult.Quality == Constants.CaptureQuality.DP_QUALITY_CANCELED)
                {
                    OnEstadoCambiado?.Invoke("Captura cancelada o fallida");
                    return null;
                }

                // Mostrar imagen
                if (captureResult.Data != null)
                {
                    foreach (Fid.Fiv view in captureResult.Data.Views)
                    {
                        var bmp = CrearBitmap(view.RawImage, view.Width, view.Height);
                        OnImagenCapturada?.Invoke(bmp);
                    }
                }

                OnEstadoCambiado?.Invoke("Procesando huella...");

                var conversionResult = FeatureExtraction.CreateFmdFromFid(
                    captureResult.Data,
                    Constants.Formats.Fmd.ANSI);

                if (conversionResult?.ResultCode != Constants.ResultCode.DP_SUCCESS)
                {
                    throw new Exception(conversionResult?.ResultCode.ToString() ?? "Error en conversión");
                }

                OnEstadoCambiado?.Invoke("Huella capturada con éxito");
                return conversionResult.Data;
            }
            catch (OperationCanceledException)
            {
                OnEstadoCambiado?.Invoke("Captura cancelada por el usuario");
                return null;
            }
            catch (Exception ex)
            {
                OnEstadoCambiado?.Invoke($"Error: {ex.Message}");
                return null;
            }
        }
    }

    public void CancelarCaptura()
    {
        _internalCts?.Cancel();
    }

    private Bitmap CrearBitmap(byte[] bytes, int width, int height)
    {
        byte[] rgbBytes = new byte[bytes.Length * 3];
        for (int i = 0; i < bytes.Length; i++)
        {
            rgbBytes[i * 3] = bytes[i];
            rgbBytes[i * 3 + 1] = bytes[i];
            rgbBytes[i * 3 + 2] = bytes[i];
        }

        Bitmap bmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);
        BitmapData data = bmp.LockBits(new Rectangle(0, 0, width, height),
                                     ImageLockMode.WriteOnly,
                                     PixelFormat.Format24bppRgb);

        for (int i = 0; i < height; i++)
        {
            IntPtr p = new IntPtr(data.Scan0.ToInt64() + data.Stride * i);
            Marshal.Copy(rgbBytes, i * width * 3, p, width * 3);
        }

        bmp.UnlockBits(data);
        return bmp;
    }

    public void RegistrarHuella(int idMiembro, Fmd huella)
    {
        if (_huellasRegistradas.ContainsKey(idMiembro))
        {
            _huellasRegistradas[idMiembro] = huella;
        }
        else
        {
            _huellasRegistradas.Add(idMiembro, huella);
        }
        OnEstadoCambiado?.Invoke($"Huella registrada para ID: {idMiembro}");
    }

    public int VerificarHuella(Fmd huella)
    {
        const int PROBABILITY_ONE = 0x7fffffff;
        int thresholdScore = PROBABILITY_ONE / 100000;

        foreach (var registro in _huellasRegistradas)
        {
            var resultado = Comparison.Compare(huella, 0, registro.Value, 0);

            if (resultado?.ResultCode == Constants.ResultCode.DP_SUCCESS &&
                resultado.Score < thresholdScore)
            {
                return registro.Key;
            }
        }

        return -1;
    }

    public void CerrarLector()
    {
        try
        {
            CancelarCaptura();

            if (_lector != null)
            {
                _lector.Dispose();
                _lector = null;
                OnEstadoCambiado?.Invoke("Lector desconectado");
            }
        }
        catch (Exception ex)
        {
            OnEstadoCambiado?.Invoke($"Error al cerrar lector: {ex.Message}");
        }
    }

    public void Dispose()
    {
        CerrarLector();
    }
}
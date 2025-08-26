using System.IO.Ports;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Dairy.Infrastructure
{
    public interface IWeighingMachineService
    {
        Task<bool> ConnectAsync(string portName, int baudRate = 9600);
        void Disconnect();
        bool IsConnected { get; }
        event EventHandler<WeightReadingEventArgs> WeightReceived;
        Task<decimal?> GetCurrentWeightAsync();
    }

    public class WeighingMachineService : IWeighingMachineService, IDisposable
    {
        private SerialPort? _serialPort;
        private readonly ILogger<WeighingMachineService> _logger;
        private bool _isReading;
        private readonly StringBuilder _buffer = new();

        public WeighingMachineService(ILogger<WeighingMachineService> logger)
        {
            _logger = logger;
        }

        public bool IsConnected => _serialPort?.IsOpen ?? false;

        public event EventHandler<WeightReadingEventArgs>? WeightReceived;

        public async Task<bool> ConnectAsync(string portName, int baudRate = 9600)
        {
            try
            {
                _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One)
                {
                    Handshake = Handshake.None,
                    ReadTimeout = 1000,
                    WriteTimeout = 1000
                };

                _serialPort.DataReceived += OnDataReceived;
                _serialPort.Open();
                _isReading = true;

                _logger.LogInformation("Connected to weighing machine on port {Port}", portName);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to weighing machine on port {Port}", portName);
                return false;
            }
        }

        public void Disconnect()
        {
            _isReading = false;
            if (_serialPort?.IsOpen == true)
            {
                _serialPort.Close();
                _logger.LogInformation("Disconnected from weighing machine");
            }
        }

        public async Task<decimal?> GetCurrentWeightAsync()
        {
            if (!IsConnected) return null;

            try
            {
                // Send command to request weight (varies by manufacturer)
                _serialPort!.WriteLine("W\r\n"); // Common command
                await Task.Delay(500); // Wait for response
                
                var response = _serialPort.ReadExisting();
                return ParseWeight(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current weight");
                return null;
            }
        }

        private void OnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!_isReading) return;

            try
            {
                var data = _serialPort!.ReadExisting();
                _buffer.Append(data);

                // Process complete lines
                var content = _buffer.ToString();
                var lines = content.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                
                if (lines.Length > 0)
                {
                    foreach (var line in lines.Take(lines.Length - 1))
                    {
                        ProcessWeightData(line);
                    }
                    
                    // Keep the last incomplete line in buffer
                    _buffer.Clear();
                    if (!content.EndsWith('\n') && !content.EndsWith('\r'))
                    {
                        _buffer.Append(lines.Last());
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing weight data");
            }
        }

        private void ProcessWeightData(string data)
        {
            var weight = ParseWeight(data);
            if (weight.HasValue)
            {
                WeightReceived?.Invoke(this, new WeightReadingEventArgs(weight.Value, DateTime.Now));
            }
        }

        private decimal? ParseWeight(string data)
        {
            try
            {
                // Common weight data formats:
                // "ST,GS,+00012.34kg" (A&D scales)
                // "12.34 kg" (simple format)
                // "W 12.34" (custom format)
                
                data = data.Trim();
                
                // Remove common prefixes and suffixes
                data = data.Replace("ST,GS,", "")
                          .Replace("kg", "")
                          .Replace("g", "")
                          .Replace("W ", "")
                          .Replace("+", "")
                          .Trim();

                if (decimal.TryParse(data, out var weight))
                {
                    // Convert grams to kg if needed
                    if (weight > 100) // Assume values > 100 are in grams
                    {
                        weight /= 1000;
                    }
                    return weight;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse weight data: {Data}", data);
            }
            
            return null;
        }

        public void Dispose()
        {
            Disconnect();
            _serialPort?.Dispose();
        }
    }

    public class WeightReadingEventArgs : EventArgs
    {
        public decimal Weight { get; }
        public DateTime Timestamp { get; }

        public WeightReadingEventArgs(decimal weight, DateTime timestamp)
        {
            Weight = weight;
            Timestamp = timestamp;
        }
    }
}
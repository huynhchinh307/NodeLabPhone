using AdvancedSharpAdbClient;
using AdvancedSharpAdbClient.Models;
using AdvancedSharpAdbClient.Receivers; // Speculative fix for ConsoleOutputReceiver
using NodeLabFarm.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading; // For CancellationToken
using System.Threading.Tasks;

namespace NodeLabFarm.Services
{
    public interface IAdbService
    {
        Task<(bool Success, string Message)> StartServerAsync(string adbPath);
        Task<IEnumerable<DeviceData>> GetDevicesAsync();
        Task<string> ExecuteCommandAsync(DeviceData device, string command);
        string GetDeviceCustomName(string serial);
        void SetDeviceCustomName(string serial, string name);
        IEnumerable<KnownDevice> GetAllKnownDevices();
        string GetAdbPath();
        void SetAdbPath(string path);
        Task OpenDeviceAsync(string serial, string deviceName, int index);
        Task<System.Windows.Media.Imaging.BitmapSource?> GetScreenshotAsync(string serial);
        Task<bool> ExecuteStepAsync(string serial, ScriptStepModel step);
        Task<(int X, int Y)?> GetElementBoundsAsync(string serial, string attributeValue, string attributeName = "text");
        Task<Dictionary<string, string>?> GetElementAtPointAsync(string serial, int x, int y);
        Task<List<Dictionary<string, string>>?> GetUIHierarchyAsync(string serial);
    }

    public class AdbService : IAdbService
    {
        private readonly IAdbClient _client;
        private readonly string _devicesFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "known_devices.json");
        private readonly string _settingsFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
        private Dictionary<string, KnownDevice> _knownDevices = new Dictionary<string, KnownDevice>();
        private string _adbPath = "adb.exe";

        public AdbService()
        {
            _client = new AdbClient();
            LoadKnownDevices();
            LoadSettings();
        }

        private void LoadSettings()
        {
            try
            {
                if (File.Exists(_settingsFilePath))
                {
                    var json = File.ReadAllText(_settingsFilePath);
                    var settings = System.Text.Json.JsonSerializer.Deserialize<AdbSettings>(json);
                    if (settings != null)
                    {
                        _adbPath = settings.AdbPath ?? "adb.exe";
                    }
                }
            }
            catch { }
        }

        private void SaveSettings()
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(new AdbSettings { AdbPath = _adbPath }, options);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch { }
        }

        public string GetAdbPath() => _adbPath;

        public void SetAdbPath(string path)
        {
            _adbPath = path;
            SaveSettings();
        }

        private void LoadKnownDevices()
        {
            try
            {
                if (File.Exists(_devicesFilePath))
                {
                    var json = File.ReadAllText(_devicesFilePath);
                    var devices = System.Text.Json.JsonSerializer.Deserialize<List<KnownDevice>>(json);
                    if (devices != null)
                    {
                        foreach (var d in devices)
                        {
                            _knownDevices[d.Serial] = d;
                        }
                    }
                }
            }
            catch { }
        }

        private void SaveKnownDevices()
        {
            try
            {
                var options = new System.Text.Json.JsonSerializerOptions { WriteIndented = true };
                var json = System.Text.Json.JsonSerializer.Serialize(_knownDevices.Values.ToList(), options);
                File.WriteAllText(_devicesFilePath, json);
            }
            catch { }
        }

        public string GetDeviceCustomName(string serial)
        {
            return _knownDevices.TryGetValue(serial, out var device) ? (device.CustomName ?? string.Empty) : string.Empty;
        }

        public void SetDeviceCustomName(string serial, string name)
        {
            if (_knownDevices.TryGetValue(serial, out var device))
            {
                device.CustomName = name;
                SaveKnownDevices();
            }
        }

        public async Task<(bool Success, string Message)> StartServerAsync(string adbPath)
        {
            if (string.IsNullOrWhiteSpace(adbPath))
            {
                return (false, "ADB path is empty.");
            }

            if (!File.Exists(adbPath) && adbPath != "adb.exe")
            {
                return (false, $"File not found: {adbPath}");
            }

            var server = new AdbServer();
            try
            {
                var result = await server.StartServerAsync(adbPath, restartServerIfNewer: false);
                if (result == StartServerResult.Started || result == StartServerResult.AlreadyRunning)
                {
                    return (true, "ADB server is running.");
                }
                return (false, $"ADB server returned error: {result}");
            }
            catch (Exception ex)
            {
                return (false, $"Exception starting ADB server: {ex.Message}");
            }
        }

        public async Task<IEnumerable<DeviceData>> GetDevicesAsync()
        {
            var onlineDevices = (await _client.GetDevicesAsync(CancellationToken.None)).ToList();
            
            // Update known devices with new online info
            foreach (var d in onlineDevices)
            {
                if (!_knownDevices.ContainsKey(d.Serial))
                {
                    _knownDevices[d.Serial] = new KnownDevice { Serial = d.Serial, Model = d.Model };
                }
                else 
                {
                    _knownDevices[d.Serial].Model = d.Model; // In case it changed
                }
            }
            SaveKnownDevices();

            // Prepare the list of all "Known" devices to be returned as DeviceData-like objects
            // However, the VM will handle the merging better. Let's return the online ones
            // and maybe a full list of everything we know.
            return onlineDevices;
        }

        // New method to get all known devices from history
        public IEnumerable<KnownDevice> GetAllKnownDevices()
        {
            return _knownDevices.Values.ToList();
        }

        public async Task<string> ExecuteCommandAsync(DeviceData device, string command)
        {
            if (device == null || device.State != DeviceState.Online) return "Device is offline.";

            var receiver = new ConsoleOutputReceiver();
            await _client.ExecuteRemoteCommandAsync(command, device, receiver, CancellationToken.None);
            return receiver.ToString();
        }

        public Task OpenDeviceAsync(string serial, string deviceName, int index)
        {
            return Task.Run(() =>
            {
                try
                {
                    // Check if scrcpy for this serial is already running
                    var scrcpyProcesses = System.Diagnostics.Process.GetProcessesByName("scrcpy");
                    foreach (var p in scrcpyProcesses)
                    {
                        try
                        {
                            // This check is a bit naive but works if scrcpy is started by us with -s
                            // For a more robust check, one would use WMI to check command line arguments
                            // but we can also use Windows Title as a marker
                            if (p.MainWindowTitle.Contains($"({serial})"))
                            {
                                // Focus the window if possible (optional)
                                return;
                            }
                        }
                        catch { }
                    }

                    string adbDir = Path.GetDirectoryName(_adbPath) ?? string.Empty;
                    string scrcpyPath = Path.Combine(adbDir, "scrcpy.exe");

                    if (!File.Exists(scrcpyPath)) scrcpyPath = "scrcpy.exe";

                    string title = $"[{index}] {deviceName} ({serial}) - NodeLabFarm";

                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = scrcpyPath,
                        Arguments = $"-s {serial} --window-title \"{title}\" --max-fps 15 -b 2M --always-on-top",
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };
                    System.Diagnostics.Process.Start(psi);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error launching scrcpy: {ex.Message}");
                }
            });
        }

        public async Task<System.Windows.Media.Imaging.BitmapSource?> GetScreenshotAsync(string serial)
        {
            try
            {
                // Cache or get devices once to avoid overhead
                var device = (await _client.GetDevicesAsync(CancellationToken.None)).FirstOrDefault(d => d.Serial == serial);
                if (device == null || device.State != DeviceState.Online) return null;

                // Using FrameBuffer service is the most direct way for raw pixels
                using (var framebuffer = await _client.GetFrameBufferAsync(device, CancellationToken.None))
                {
                    if (framebuffer == null || framebuffer.Data == null || framebuffer.Header.Width == 0) return null;

                    var header = framebuffer.Header;
                    int width = (int)header.Width;
                    int height = (int)header.Height;
                    
                    // Direct pixel copy with color channel swap (RGBA -> BGRA)
                    byte[] pixels = new byte[width * height * 4];
                    for (int i = 0; i < framebuffer.Data.Length && i + 3 < pixels.Length; i += 4)
                    {
                        pixels[i] = framebuffer.Data[i + 2];     // B
                        pixels[i + 1] = framebuffer.Data[i + 1]; // G
                        pixels[i + 2] = framebuffer.Data[i];     // R
                        pixels[i + 3] = framebuffer.Data[i + 3]; // A
                    }

                    var bitmap = System.Windows.Media.Imaging.BitmapSource.Create(
                        width, height, 96, 96, 
                        System.Windows.Media.PixelFormats.Bgra32, 
                        null, pixels, width * 4);
                    
                    bitmap.Freeze(); // Crucial for cross-thread UI binding
                    return bitmap;
                }
            }
            catch { return null; }
        }
        public async Task<bool> ExecuteStepAsync(string serial, ScriptStepModel step)
        {
            try
            {
                var devices = await _client.GetDevicesAsync(CancellationToken.None);
                var device = devices.FirstOrDefault(d => d.Serial == serial);
                if (device == null || device.State != DeviceState.Online) return false;

                switch (step.Type)
                {
                    case StepType.OpenApp:
                    case StepType.StartApp:
                        await _client.ExecuteRemoteCommandAsync($"monkey -p {step.Target} -c android.intent.category.LAUNCHER 1", device, CancellationToken.None);
                        break;
                    case StepType.Tap:
                        if (step.SelectorType == "Text" || step.SelectorType == "ID" || step.SelectorType == "Selector-Xpath" || step.SelectorType == "Selector-Selector")
                        {
                            string attrValue = (step.SelectorType == "Text" || step.SelectorType == "ID") ? step.Value : step.Target;
                            string attrName = step.SelectorType == "Text" ? "text" : (step.SelectorType == "ID" ? "resource-id" : "auto");
                            
                            var b = await GetElementBoundsAsync(serial, attrValue, attrName);
                            if (b != null) 
                            {
                                await _client.ExecuteRemoteCommandAsync($"input tap {b.Value.X} {b.Value.Y}", device, CancellationToken.None);
                            }
                        }
                        else
                        {
                            // Coordinates logic
                            if (step.Target.Contains(",") || step.Target.Contains(" "))
                            {
                                var parts = step.Target.Split(new[] { ',', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 2)
                                {
                                    await _client.ExecuteRemoteCommandAsync($"input tap {parts[0].Trim()} {parts[1].Trim()}", device, CancellationToken.None);
                                }
                            }
                            else if (!string.IsNullOrEmpty(step.Target) && !string.IsNullOrEmpty(step.Value))
                            {
                                await _client.ExecuteRemoteCommandAsync($"input tap {step.Target.Trim()} {step.Value.Trim()}", device, CancellationToken.None);
                            }
                            else if (!string.IsNullOrEmpty(step.Target))
                            {
                                await _client.ExecuteRemoteCommandAsync($"input tap {step.Target}", device, CancellationToken.None);
                            }
                        }
                        break;
                    case StepType.FindText:
                    case StepType.ElementExists:
                        var EB = await GetElementBoundsAsync(serial, step.Value, "text");
                        if (EB == null) EB = await GetElementBoundsAsync(serial, step.Value, "resource-id");
                        return EB != null;
                    case StepType.Swipe:
                        await _client.ExecuteRemoteCommandAsync($"input swipe {step.StartX} {step.StartY} {step.EndX} {step.EndY} {step.Duration}", device, CancellationToken.None);
                        break;
                    case StepType.Type:
                        if (!string.IsNullOrEmpty(step.Target))
                        {
                            // Focus first
                            var b = await GetElementBoundsAsync(serial, step.Target, "auto");
                            if (b != null) await _client.ExecuteRemoteCommandAsync($"input tap {b.Value.X} {b.Value.Y}", device, CancellationToken.None);
                            await Task.Delay(500);
                        }
                        string text = step.Value.Replace(" ", "%s");
                        await _client.ExecuteRemoteCommandAsync($"input text {text}", device, CancellationToken.None);
                        break;
                    case StepType.Home:
                        await _client.ExecuteRemoteCommandAsync("input keyevent 3", device, CancellationToken.None);
                        break;
                    case StepType.Back:
                        await _client.ExecuteRemoteCommandAsync("input keyevent 4", device, CancellationToken.None);
                        break;
                    case StepType.StopApp:
                        await _client.ExecuteRemoteCommandAsync($"am force-stop {step.Target}", device, CancellationToken.None);
                        break;
                    case StepType.ClearDataApp:
                        await _client.ExecuteRemoteCommandAsync($"pm clear {step.Target}", device, CancellationToken.None);
                        break;
                    case StepType.PressKey:
                        await _client.ExecuteRemoteCommandAsync($"input keyevent {step.Value}", device, CancellationToken.None);
                        break;
                    case StepType.AdbCommand:
                        await _client.ExecuteRemoteCommandAsync(step.Value, device, CancellationToken.None);
                        break;
                    case StepType.Pause:
                        int delayMs = 2000;
                        if (int.TryParse(step.Value, out int v)) delayMs = v;
                        else if (step.Delay.Contains("sec")) 
                        {
                            if (int.TryParse(step.Delay.Replace("sec", "").Trim(), out int s)) delayMs = s * 1000;
                        }
                        await Task.Delay(delayMs);
                        break;
                }
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Step execution error: {ex.Message}");
                return false;
            }
        }
        public async Task<(int X, int Y)?> GetElementBoundsAsync(string serial, string attributeValue, string attributeName = "text")
        {
            try
            {
                var device = (await _client.GetDevicesAsync(CancellationToken.None)).FirstOrDefault(d => d.Serial == serial);
                if (device == null) return null;

                // 1. Dump UI to a temporary file on device
                string remotePath = "/data/local/tmp/uidump.xml";
                await _client.ExecuteRemoteCommandAsync($"uiautomator dump {remotePath}", device, CancellationToken.None);

                // 2. Read the dump output (simplified: use cat to read directly if small, or pull)
                // For speed in this version, we'll try to read via cat
                var receiver = new ConsoleOutputReceiver();
                await _client.ExecuteRemoteCommandAsync($"cat {remotePath}", device, receiver, CancellationToken.None);
                string xmlContent = receiver.ToString();

                if (string.IsNullOrEmpty(xmlContent) || !xmlContent.Contains("<node")) return null;

                // 3. Parse XML to find the element
                try
                {
                    var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
                    System.Xml.Linq.XElement? node = null;

                    if (attributeName == "auto")
                    {
                        // Try to extract from fake XPath: //*[@id='...'] or //*[@text='...']
                        string searchVal = attributeValue;
                        if (searchVal.Contains("'"))
                        {
                            var m = System.Text.RegularExpressions.Regex.Match(searchVal, @"@([\w-]+)='([^']+)'");
                            if (m.Success)
                            {
                                string attr = m.Groups[1].Value;
                                string val = m.Groups[2].Value;
                                node = doc.Descendants("node").FirstOrDefault(n => (string?)n.Attribute(attr) == val);
                            }
                        }
                        
                        if (node == null)
                        {
                            node = doc.Descendants("node").FirstOrDefault(n => 
                                (string?)n.Attribute("text") == searchVal || 
                                (string?)n.Attribute("resource-id") == searchVal ||
                                ((string?)n.Attribute("resource-id") != null && ((string?)n.Attribute("resource-id")).EndsWith("/" + searchVal)) ||
                                (string?)n.Attribute("content-desc") == searchVal);
                        }
                    }
                    else
                    {
                        node = doc.Descendants("node").FirstOrDefault(n => (string?)n.Attribute(attributeName) == attributeValue);
                    }

                    if (node != null)
                    {
                        string? bounds = (string?)node.Attribute("bounds");
                        if (!string.IsNullOrEmpty(bounds))
                        {
                            // Parse "[x1,y1][x2,y2]"
                            var matches = System.Text.RegularExpressions.Regex.Matches(bounds, @"\d+");
                            if (matches.Count >= 4)
                            {
                                int x1 = int.Parse(matches[0].Value);
                                int y1 = int.Parse(matches[1].Value);
                                int x2 = int.Parse(matches[2].Value);
                                int y2 = int.Parse(matches[3].Value);

                                // Return center point
                                return (x1 + (x2 - x1) / 2, y1 + (y2 - y1) / 2);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"XML Parse Error: {ex.Message}");
                }

                return null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"UiAutomator Error: {ex.Message}");
                return null;
            }
        }
        public async Task<Dictionary<string, string>?> GetElementAtPointAsync(string serial, int x, int y)
        {
            try
            {
                var device = (await _client.GetDevicesAsync(CancellationToken.None)).FirstOrDefault(d => d.Serial == serial);
                if (device == null) return null;

                string remotePath = "/data/local/tmp/uidump.xml";
                await _client.ExecuteRemoteCommandAsync($"uiautomator dump {remotePath}", device, CancellationToken.None);

                var receiver = new ConsoleOutputReceiver();
                await _client.ExecuteRemoteCommandAsync($"cat {remotePath}", device, receiver, CancellationToken.None);
                string xmlContent = receiver.ToString();

                if (string.IsNullOrEmpty(xmlContent) || !xmlContent.Contains("<node")) return null;

                var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
                var nodes = doc.Descendants("node").ToList();

                System.Xml.Linq.XElement? target = null;
                int minArea = int.MaxValue;

                foreach (var node in nodes)
                {
                    string? bounds = (string?)node.Attribute("bounds");
                    if (string.IsNullOrEmpty(bounds)) continue;

                    var m = System.Text.RegularExpressions.Regex.Matches(bounds, @"\d+");
                    if (m.Count >= 4)
                    {
                        int x1 = int.Parse(m[0].Value);
                        int y1 = int.Parse(m[1].Value);
                        int x2 = int.Parse(m[2].Value);
                        int y2 = int.Parse(m[3].Value);

                        if (x >= x1 && x <= x2 && y >= y1 && y <= y2)
                        {
                            int area = (x2 - x1) * (y2 - y1);
                            if (area < minArea)
                            {
                                minArea = area;
                                target = node;
                            }
                        }
                    }
                }

                if (target != null)
                {
                    return target.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value);
                }
                return null;
            }
            catch { return null; }
        }
        public async Task<List<Dictionary<string, string>>?> GetUIHierarchyAsync(string serial)
        {
            try
            {
                var device = (await _client.GetDevicesAsync(CancellationToken.None)).FirstOrDefault(d => d.Serial == serial);
                if (device == null) return null;

                string remotePath = "/data/local/tmp/uidump.xml";
                await _client.ExecuteRemoteCommandAsync($"uiautomator dump {remotePath}", device, CancellationToken.None);

                var receiver = new ConsoleOutputReceiver();
                await _client.ExecuteRemoteCommandAsync($"cat {remotePath}", device, receiver, CancellationToken.None);
                string xmlContent = receiver.ToString();

                if (string.IsNullOrEmpty(xmlContent) || !xmlContent.Contains("<node")) return null;

                var doc = System.Xml.Linq.XDocument.Parse(xmlContent);
                return doc.Descendants("node")
                          .Select(n => n.Attributes().ToDictionary(a => a.Name.LocalName, a => a.Value))
                          .ToList();
            }
            catch { return null; }
        }
    }

    public class KnownDevice
    {
        public string Serial { get; set; } = string.Empty;
        public string Model { get; set; } = string.Empty;
        public string? CustomName { get; set; }
    }

    public class AdbSettings
    {
        public string? AdbPath { get; set; }
    }
}

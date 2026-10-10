using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;

namespace ProGo
{
    // Standalone owner-run measurement. No settings/log/key reads, network connections
    // or Windows mutations. Also compiled as installed source for PowerShell 5.1.
    public sealed class RescueInventory
    {
        public string ExecutablePath, FileVersion, BuildIdentity, SshPath, SshFileVersion;
        public string AgentState = "Unknown", AgentStartMode = "Unknown";
        public bool ProcessSelected, ProcessAvailable, ExecutableExists, ExpectedPathMatches = true;
        public bool? SocksListening, HttpListening;
    }
    public sealed class RescueSample
    {
        public bool Exited;
        public double ElapsedSeconds, CpuMilliseconds, WorkingSetBytes, PrivateBytes;
        public ulong ReadBytes, WriteBytes, OtherBytes;
        public bool IoAvailable;
        public bool? Responding;
        public int SshChildren = -1;
    }
    public interface IRescueMeasurementProbe
    {
        RescueInventory Inventory();
        RescueSample Sample();
    }
    public sealed class RescueMeasurementResult
    {
        public RescueInventory Inventory;
        public readonly List<RescueSample> Samples = new List<RescueSample>();
        public string StopReason;
        public double DurationSeconds;
        public int ProcessorCount, SocksPort, HttpPort;
    }
    public static class RescueMeasurement
    {
        public static RescueMeasurementResult Collect(IRescueMeasurementProbe probe, int durationMs,
            int intervalMs, int socksPort, int httpPort, CancellationToken cancellation)
        {
            if (probe == null) throw new ArgumentNullException("probe");
            if (durationMs < 250 || durationMs > 300000 || intervalMs < 100 || intervalMs > 5000)
                throw new ArgumentOutOfRangeException("durationMs");
            if (socksPort < 0 || socksPort > 65535 || httpPort < 0 || httpPort > 65535)
                throw new ArgumentOutOfRangeException("socksPort");
            var result = new RescueMeasurementResult { ProcessorCount = Math.Max(1, Environment.ProcessorCount),
                SocksPort = socksPort, HttpPort = httpPort };
            var clock = Stopwatch.StartNew();
            try
            {
                RescueInventory inventory;
                if (!ReadBounded(probe.Inventory, clock, durationMs, cancellation, out inventory))
                { result.StopReason = cancellation.IsCancellationRequested ? "cancelled" : "inventory-timeout"; return result; }
                result.Inventory = inventory;
                if (inventory == null || !inventory.ProcessSelected || !inventory.ProcessAvailable)
                { result.StopReason = "no-process"; return result; }
                while (clock.ElapsedMilliseconds < durationMs)
                {
                    RescueSample sample;
                    if (!ReadBounded(probe.Sample, clock, durationMs, cancellation, out sample))
                    { result.StopReason = cancellation.IsCancellationRequested ? "cancelled" : "sample-timeout"; break; }
                    if (sample == null) { result.StopReason = "probe-failed"; break; }
                    sample.ElapsedSeconds = clock.Elapsed.TotalSeconds;
                    if (sample.Exited) { result.StopReason = "exited"; break; }
                    result.Samples.Add(sample);
                    int wait = Math.Min(intervalMs, Math.Max(0, durationMs - (int)clock.ElapsedMilliseconds));
                    if (wait > 0 && cancellation.WaitHandle.WaitOne(wait)) { result.StopReason = "cancelled"; break; }
                }
                if (result.StopReason == null) result.StopReason = "completed";
            }
            catch (OperationCanceledException) { result.StopReason = "cancelled"; }
            catch { result.StopReason = "probe-failed"; }
            finally { result.DurationSeconds = clock.Elapsed.TotalSeconds; }
            return result;
        }
        private static bool ReadBounded<T>(Func<T> read, Stopwatch clock, int durationMs,
            CancellationToken cancellation, out T value)
        {
            value = default(T);
            cancellation.ThrowIfCancellationRequested();
            int remaining = Math.Min(5000, Math.Max(0, durationMs - (int)clock.ElapsedMilliseconds));
            if (remaining == 0) return false;
            // Exactly one outstanding probe. A blocked read ends this session, never
            // creates a new polling worker or holds up process exit (pool threads).
            var task = Task.Factory.StartNew(read, CancellationToken.None, TaskCreationOptions.DenyChildAttach, TaskScheduler.Default);
            if (!task.Wait(remaining, cancellation)) return false;
            value = task.GetAwaiter().GetResult(); return true;
        }
        private static string Status(bool? value) { return !value.HasValue ? "NOT_CHECKED" : value.Value ? "PASS" : "FAIL"; }
        private static string Number(double value) { return value.ToString("0.00", CultureInfo.InvariantCulture); }
        private static string Version(string value)
        { return value != null && Regex.IsMatch(value, @"\A[0-9A-Za-z.+_-]{1,100}\z") ? value : "не определена"; }
        private static string VersionLine(string title, string value)
        { string version = Version(value); return (version == "не определена" ? "NOT_CHECKED" : "PASS") + " — " + title + ": " + version + "."; }
        private static string DisplayPath(string path)
        {
            if (String.IsNullOrEmpty(path)) return "не определён";
            string profile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (!String.IsNullOrEmpty(profile) && (String.Equals(path, profile, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith(profile + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)))
                path = "%USERPROFILE%" + path.Substring(profile.Length);
            path = Regex.Replace(path, @"[\x00-\x1f\x7f]", "?");
            return path.Length <= 512 ? path : "путь слишком длинный";
        }
        public static string Report(RescueMeasurementResult result)
        {
            if (result == null) throw new ArgumentNullException("result");
            var text = new StringBuilder();
            text.AppendLine("ProGo: локальное измерение Windows Rescue (только чтение)");
            text.AppendLine("Продолжительность: " + Number(result.DurationSeconds) + " с; образцов: " + result.Samples.Count + ".");
            var inventory = result.Inventory;
            if (inventory == null) text.AppendLine("NOT_CHECKED — начальная диагностика не завершилась в отведённое время.");
            else
            {
                text.AppendLine((inventory.ExecutableExists ? "PASS" : "FAIL") + " — выбранный EXE: " + DisplayPath(inventory.ExecutablePath));
                text.AppendLine(VersionLine("версия файла EXE", inventory.FileVersion));
                text.AppendLine(VersionLine("build identity EXE (ProductVersion)", inventory.BuildIdentity));
                text.AppendLine(!inventory.ProcessSelected ? "NOT_CHECKED — PID не выбран; версия относится к указанному файлу, запуск не подтверждён."
                    : !inventory.ProcessAvailable ? "FAIL — выбранный процесс завершён или недоступен."
                    : !inventory.ExpectedPathMatches ? "FAIL — выбранный PID запускает другой EXE; показана версия его фактического файла."
                    : "PASS — фактический путь выбранного процесса прочитан; VERSION репозитория не использовался.");
                text.AppendLine((String.IsNullOrEmpty(inventory.SshPath) ? "FAIL" : "PASS") + " — ssh.exe: " + DisplayPath(inventory.SshPath));
                text.AppendLine(VersionLine("версия файла OpenSSH", inventory.SshFileVersion) + " Программа ssh.exe не запускалась.");
                string state = inventory.AgentState == "Running" || inventory.AgentState == "Stopped" || inventory.AgentState == "Missing"
                    || inventory.AgentState == "Transitioning" ? inventory.AgentState : "Unknown";
                string mode = inventory.AgentStartMode == "Disabled" || inventory.AgentStartMode == "Automatic" || inventory.AgentStartMode == "Manual"
                    ? inventory.AgentStartMode : "Unknown";
                text.AppendLine((state == "Running" ? "PASS" : state == "Stopped" || state == "Missing" ? "FAIL" : "NOT_CHECKED")
                    + " — ssh-agent: " + state + "; режим запуска: " + mode + ".");
                if (state == "Stopped" || mode == "Disabled")
                    text.AppendLine("Запуск/включение службы выполняет владелец отдельно, с правами администратора. Этот отчёт службу не меняет.");
                text.AppendLine((result.SocksPort == 0 ? "NOT_CHECKED" : Status(inventory.SocksListening)) + " — SOCKS TCP listener: "
                    + (result.SocksPort == 0 ? "фактический порт не указан" : "127.0.0.1:" + result.SocksPort) + ".");
                text.AppendLine((result.HttpPort == 0 ? "NOT_CHECKED" : Status(inventory.HttpListening)) + " — HTTP bridge TCP listener: "
                    + (result.HttpPort == 0 ? "фактический порт не указан" : "127.0.0.1:" + result.HttpPort) + ".");
            }
            if (result.Samples.Count >= 2)
            {
                var first = result.Samples[0]; var last = result.Samples[result.Samples.Count - 1];
                double seconds = last.ElapsedSeconds - first.ElapsedSeconds;
                double cpu = seconds > 0 ? Math.Max(0, last.CpuMilliseconds - first.CpuMilliseconds) / (seconds * 10 * result.ProcessorCount) : 0;
                double maximumWorking = 0, maximumPrivate = 0; int hung = 0, windows = 0, maximumSsh = -1;
                foreach (var sample in result.Samples)
                {
                    maximumWorking = Math.Max(maximumWorking, sample.WorkingSetBytes); maximumPrivate = Math.Max(maximumPrivate, sample.PrivateBytes);
                    if (sample.Responding.HasValue) { windows++; if (!sample.Responding.Value) hung++; }
                    maximumSsh = Math.Max(maximumSsh, sample.SshChildren);
                }
                text.AppendLine("PASS — образцы процесса: CPU " + Number(cpu) + "% всех логических процессоров; пик RAM working set "
                    + Number(maximumWorking / 1048576) + " МиБ; private bytes " + Number(maximumPrivate / 1048576) + " МиБ.");
                bool io = first.IoAvailable && last.IoAvailable && seconds > 0 && last.ReadBytes >= first.ReadBytes && last.WriteBytes >= first.WriteBytes && last.OtherBytes >= first.OtherBytes;
                text.AppendLine(io ? "PASS — I/O процесса: чтение " + Number((last.ReadBytes - first.ReadBytes) / seconds / 1024) + " КиБ/с; запись "
                    + Number((last.WriteBytes - first.WriteBytes) / seconds / 1024) + " КиБ/с; прочее " + Number((last.OtherBytes - first.OtherBytes) / seconds / 1024) + " КиБ/с."
                    : "NOT_CHECKED — сопоставимые счётчики I/O процесса не получены.");
                text.AppendLine((windows == 0 ? "NOT_CHECKED" : hung > 0 ? "FAIL" : "PASS") + " — Windows Responding: оконных образцов " + windows + ", признаков зависания " + hung + ".");
                text.AppendLine((maximumSsh >= 0 ? "PASS" : "NOT_CHECKED") + " — максимум дочерних процессов ssh.exe: " + (maximumSsh >= 0 ? maximumSsh.ToString(CultureInfo.InvariantCulture) : "не определён") + ".");
            }
            else text.AppendLine("NOT_CHECKED — недостаточно образцов для CPU/RAM/I/O и состояния окна.");
            string stop = result.StopReason == "completed" ? "интервал завершён" : result.StopReason == "exited" ? "процесс завершился"
                : result.StopReason == "cancelled" ? "измерение отменено" : result.StopReason == "no-process" ? "процесс не выбран/недоступен"
                : result.StopReason == "sample-timeout" || result.StopReason == "inventory-timeout" ? "истёк предел ожидания; повторный probe не запускался" : "локальная проверка недоступна";
            text.AppendLine("Остановка: " + stop + ".");
            text.AppendLine("NOT_CHECKED — реальные задержки действий UI, нагрузка физического HDD/VHDX и SSH дочерних процессов: требуют отдельного профилирования.");
            text.AppendLine("NOT_CHECKED — ключи/аутентификация, фаза ожидания SSH, SOCKS5 протокол и внешний HTTP/HTTPS: попыток подключения не было.");
            text.AppendLine("TCP listener не доказывает принадлежность ProGo или работу прокси. Порты проверены один раз; I/O включает кэш, устройства и другие операции процесса.");
            text.AppendLine("Responding — признак Windows, а не измерение задержки клика. Командные строки, заголовки окон, настройки, журналы и секреты не читались.");
            return text.ToString();
        }
    }
    public sealed class WindowsRescueMeasurementProbe : IRescueMeasurementProbe, IDisposable
    {
        private readonly int processId, socksPort, httpPort;
        private readonly string expectedPath;
        private readonly ProcessHandle process;
        public WindowsRescueMeasurementProbe(int processId, string executablePath, int socksPort, int httpPort)
        {
            if (processId < 0) throw new ArgumentOutOfRangeException("processId");
            if (processId == 0 && String.IsNullOrWhiteSpace(executablePath)) throw new ArgumentException("Select PID or executable");
            this.processId = processId; this.expectedPath = String.IsNullOrWhiteSpace(executablePath) ? null : Path.GetFullPath(executablePath);
            this.socksPort = socksPort; this.httpPort = httpPort;
            if (processId != 0) process = OpenProcess(0x1000 | 0x100000, false, (uint)processId);
        }
        public RescueInventory Inventory()
        {
            var result = new RescueInventory { ProcessSelected = processId != 0 };
            string actual = null;
            if (process != null && !process.IsInvalid && WaitForSingleObject(process, 0) == 258)
            {
                var buffer = new StringBuilder(32768); int size = buffer.Capacity;
                if (QueryFullProcessImageName(process, 0, buffer, ref size)) { actual = buffer.ToString(); result.ProcessAvailable = true; }
            }
            result.ExecutablePath = actual ?? expectedPath;
            result.ExpectedPathMatches = expectedPath == null || actual == null || String.Equals(expectedPath, actual, StringComparison.OrdinalIgnoreCase);
            if (!String.IsNullOrEmpty(result.ExecutablePath))
            {
                result.ExecutableExists = File.Exists(result.ExecutablePath);
                if (result.ExecutableExists) try { var version = FileVersionInfo.GetVersionInfo(result.ExecutablePath);
                    result.FileVersion = version.FileVersion; result.BuildIdentity = version.ProductVersion; } catch { }
            }
            result.SshPath = FindSsh();
            if (result.SshPath != null) try { result.SshFileVersion = FileVersionInfo.GetVersionInfo(result.SshPath).FileVersion; } catch { }
            ReadAgent(result);
            try
            {
                var listeners = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners();
                if (socksPort != 0) result.SocksListening = Listens(listeners, socksPort);
                if (httpPort != 0) result.HttpListening = Listens(listeners, httpPort);
            }
            catch { }
            return result;
        }
        private static bool Listens(IPEndPoint[] listeners, int port)
        { foreach (var item in listeners) if (item.Port == port && (item.Address.Equals(IPAddress.Loopback) || item.Address.Equals(IPAddress.Any))) return true; return false; }
        private static string FindSsh()
        {
            string windows = Environment.GetFolderPath(Environment.SpecialFolder.Windows);
            foreach (string directory in new[] { Path.Combine(windows, "Sysnative", "OpenSSH"), Path.Combine(windows, "System32", "OpenSSH") })
            { string path = Path.Combine(directory, "ssh.exe"); if (File.Exists(path)) return path; }
            foreach (string item in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(';'))
            { string directory = item.Trim().Trim('"'); if (!Path.IsPathRooted(directory)) continue;
                try { string path = Path.GetFullPath(Path.Combine(directory, "ssh.exe")); if (File.Exists(path)) return path; } catch { } }
            return null;
        }
        public RescueSample Sample()
        {
            var result = new RescueSample();
            if (process == null || process.IsInvalid || process.IsClosed || WaitForSingleObject(process, 0) != 258) { result.Exited = true; return result; }
            long created, exited, kernel, user;
            if (!GetProcessTimes(process, out created, out exited, out kernel, out user)) throw new InvalidOperationException("Process metrics unavailable");
            result.CpuMilliseconds = (kernel + user) / 10000.0;
            var memory = new MemoryCounters(); memory.Size = (uint)Marshal.SizeOf(typeof(MemoryCounters));
            if (!GetProcessMemoryInfo(process, ref memory, memory.Size)) throw new InvalidOperationException("Memory metrics unavailable");
            result.WorkingSetBytes = memory.WorkingSet.ToUInt64(); result.PrivateBytes = memory.PrivateUsage.ToUInt64();
            IoCounters io; result.IoAvailable = GetProcessIoCounters(process, out io);
            if (result.IoAvailable) { result.ReadBytes = io.ReadBytes; result.WriteBytes = io.WriteBytes; result.OtherBytes = io.OtherBytes; }
            bool seen = false, hung = false;
            EnumWindows(delegate(IntPtr window, IntPtr unused) { uint owner; GetWindowThreadProcessId(window, out owner);
                if (owner == processId && IsWindowVisible(window)) { seen = true; if (IsHungAppWindow(window)) hung = true; } return true; }, IntPtr.Zero);
            if (seen) result.Responding = !hung;
            result.SshChildren = CountSshChildren(processId);
            return result;
        }
        private static int CountSshChildren(int root)
        {
            IntPtr snapshot = CreateToolhelp32Snapshot(2, 0);
            if (snapshot == new IntPtr(-1)) return -1;
            try
            {
                var entries = new Dictionary<uint, ProcessEntry>(); var entry = new ProcessEntry(); entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry));
                if (!Process32First(snapshot, ref entry)) return -1;
                do { if (entries.Count >= 65536) return -1; entries[entry.Id] = entry; entry.Size = (uint)Marshal.SizeOf(typeof(ProcessEntry)); } while (Process32Next(snapshot, ref entry));
                int count = 0;
                foreach (var candidate in entries.Values)
                {
                    if (!String.Equals(candidate.Executable, "ssh.exe", StringComparison.OrdinalIgnoreCase)) continue;
                    uint parent = candidate.Parent; var visited = new HashSet<uint>();
                    while (parent != 0 && visited.Add(parent))
                    { if (parent == root) { count++; break; } ProcessEntry ancestor; if (!entries.TryGetValue(parent, out ancestor)) break; parent = ancestor.Parent; }
                }
                return count;
            }
            finally { CloseHandle(snapshot); }
        }
        private static void ReadAgent(RescueInventory result)
        {
            IntPtr manager = OpenSCManager(null, null, 1), service = IntPtr.Zero, config = IntPtr.Zero;
            if (manager == IntPtr.Zero) return;
            try
            {
                service = OpenService(manager, "ssh-agent", 1 | 4);
                if (service == IntPtr.Zero) { if (Marshal.GetLastWin32Error() == 1060) result.AgentState = "Missing"; return; }
                ServiceStatus status;
                if (QueryServiceStatus(service, out status)) result.AgentState = status.State == 4 ? "Running" : status.State == 1 ? "Stopped" : "Transitioning";
                uint needed; QueryServiceConfig(service, IntPtr.Zero, 0, out needed);
                if (needed < 8 || needed > 8192) return;
                config = Marshal.AllocHGlobal((int)needed);
                if (QueryServiceConfig(service, config, needed, out needed))
                { int start = Marshal.ReadInt32(config, 4); result.AgentStartMode = start == 4 ? "Disabled" : start == 2 ? "Automatic" : start == 3 ? "Manual" : "Unknown"; }
            }
            finally { if (config != IntPtr.Zero) Marshal.FreeHGlobal(config); if (service != IntPtr.Zero) CloseServiceHandle(service); CloseServiceHandle(manager); }
        }
        public void Dispose() { if (process != null) process.Dispose(); }
        private sealed class ProcessHandle : SafeHandleZeroOrMinusOneIsInvalid
        { private ProcessHandle() : base(true) { } protected override bool ReleaseHandle() { return CloseHandle(handle); } }
        [StructLayout(LayoutKind.Sequential)] private struct MemoryCounters
        { internal uint Size, PageFaultCount; internal UIntPtr PeakWorkingSet, WorkingSet, QuotaPeakPaged, QuotaPaged, QuotaPeakNonPaged, QuotaNonPaged, PagefileUsage, PeakPagefileUsage, PrivateUsage; }
        [StructLayout(LayoutKind.Sequential)] private struct IoCounters
        { internal ulong ReadOperations, WriteOperations, OtherOperations, ReadBytes, WriteBytes, OtherBytes; }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] private struct ProcessEntry
        { internal uint Size, Usage, Id; internal UIntPtr Heap; internal uint Module, Threads, Parent; internal int Priority; internal uint Flags; [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)] internal string Executable; }
        [StructLayout(LayoutKind.Sequential)] private struct ServiceStatus
        { internal uint Type, State, Controls, ExitCode, ServiceExitCode, Checkpoint, WaitHint; }
        private delegate bool WindowCallback(IntPtr window, IntPtr parameter);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern ProcessHandle OpenProcess(uint access, bool inherit, uint id);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool CloseHandle(IntPtr handle);
        [DllImport("kernel32.dll")] private static extern uint WaitForSingleObject(ProcessHandle handle, uint milliseconds);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryFullProcessImageName(ProcessHandle handle, uint flags, StringBuilder path, ref int size);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetProcessTimes(ProcessHandle handle, out long created, out long exited, out long kernel, out long user);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern bool GetProcessIoCounters(ProcessHandle handle, out IoCounters counters);
        [DllImport("psapi.dll", SetLastError = true)] private static extern bool GetProcessMemoryInfo(ProcessHandle handle, ref MemoryCounters counters, uint size);
        [DllImport("user32.dll")] private static extern bool EnumWindows(WindowCallback callback, IntPtr parameter);
        [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out uint id);
        [DllImport("user32.dll")] private static extern bool IsWindowVisible(IntPtr window);
        [DllImport("user32.dll")] private static extern bool IsHungAppWindow(IntPtr window);
        [DllImport("kernel32.dll", SetLastError = true)] private static extern IntPtr CreateToolhelp32Snapshot(uint flags, uint id);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32First(IntPtr snapshot, ref ProcessEntry entry);
        [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool Process32Next(IntPtr snapshot, ref ProcessEntry entry);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenSCManager(string machine, string database, uint access);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern IntPtr OpenService(IntPtr manager, string name, uint access);
        [DllImport("advapi32.dll", SetLastError = true)] private static extern bool QueryServiceStatus(IntPtr service, out ServiceStatus status);
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)] private static extern bool QueryServiceConfig(IntPtr service, IntPtr config, uint size, out uint needed);
        [DllImport("advapi32.dll")] private static extern bool CloseServiceHandle(IntPtr handle);
    }
}

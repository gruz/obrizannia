using System;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

static class L10n
{
    public static readonly string AppName, VersionHeader, TrayTooltip, Watching;
    public static readonly string MenuOpenWatch, MenuOpenLast, MenuCheckUpdates, MenuSettings, MenuExit;
    public static readonly string BalloonWatching, BalloonFileReady, BalloonClickToOpen;
    public static readonly string BalloonUpdateAvailable, BalloonUpdateBody;
    public static readonly string BalloonProcessingError, BalloonWatchFolderMissing;
    public static readonly string AlreadyRunning, DownloadFfmpegFailed, SettingsSaved, FolderMissing;
    public static readonly string UpdateCheckTitle, UpdateCheckNoInfo, UpdateCheckUpToDate;
    public static readonly string UpdateCheckLatest, UpdateCheckFailed;
    public static readonly string UpdateAvailableTitle, UpdateAvailablePrompt;
    public static readonly string SettingsDialogTitle, SettingsWatchFolder, SettingsSeconds;
    public static readonly string SettingsFastCut, SettingsBrowse, SettingsOk, SettingsCancel;
    public static readonly string DownloadDialogTitle, DownloadingLabel, ExtractingLabel;

    static L10n()
    {
        bool uk = CultureInfo.CurrentUICulture.Name.StartsWith("uk");
        if (uk)
        {
            AppName = "Mp4TailWatcher";
            VersionHeader = "Mp4TailWatcher v{0}";
            TrayTooltip = "Спостерігач MP4 v{0}";
            Watching = "Спостереження: {0}";
            MenuOpenWatch = "Відкрити теку";
            MenuOpenLast = "Відкрити останній файл";
            MenuCheckUpdates = "Перевірити оновлення";
            MenuSettings = "Налаштування...";
            MenuExit = "Вихід";
            BalloonWatching = "Спостереження за {0}";
            BalloonFileReady = "Файл готовий";
            BalloonClickToOpen = " - клікніть, щоб відкрити";
            BalloonUpdateAvailable = "Доступне оновлення";
            BalloonUpdateBody = "v{0} доступна (у вас v{1}) - клікніть для сторінки завантаження";
            BalloonProcessingError = "Помилка обробки";
            BalloonWatchFolderMissing = "Тека не існує: {0}";
            AlreadyRunning = "Mp4TailWatcher вже запущено (подивіться в трей).";
            DownloadFfmpegFailed = "Не вдалося завантажити ffmpeg:\n";
            SettingsSaved = "Налаштування збережено. Спостереження за {0}";
            FolderMissing = "Тека не існує:\n{0}";
            UpdateCheckTitle = "Перевірка оновлень";
            UpdateCheckNoInfo = "Не вдалося прочитати інформацію про останній реліз.";
            UpdateCheckUpToDate = "У вас остання версія (v{0}).";
            UpdateCheckLatest = "Останній реліз: {0}  Ваша збірка: {1}";
            UpdateCheckFailed = "Перевірка не вдалася:\n{0}";
            UpdateAvailableTitle = "Доступне оновлення";
            UpdateAvailablePrompt = "Доступна версія v{0} (у вас v{1}). Відкрити сторінку завантаження?";
            SettingsDialogTitle = "Mp4TailWatcher - налаштування";
            SettingsWatchFolder = "Тека для спостереження:";
            SettingsSeconds = "Секунд з кінця:";
            SettingsFastCut = "Швидкий зріз по ключовому кадру\r\n(знято = точна довжина, перекодування, повільніше)";
            SettingsBrowse = "Огляд...";
            SettingsOk = "OK";
            SettingsCancel = "Скасувати";
            DownloadDialogTitle = "Mp4TailWatcher - встановлення";
            DownloadingLabel = "Завантаження ffmpeg.exe...";
            ExtractingLabel = "Розпаковка...";
        }
        else
        {
            AppName = "Mp4TailWatcher";
            VersionHeader = "Mp4TailWatcher v{0}";
            TrayTooltip = "MP4 tail watcher v{0}";
            Watching = "Watching: {0}";
            MenuOpenWatch = "Open watch folder";
            MenuOpenLast = "Open last output";
            MenuCheckUpdates = "Check for updates";
            MenuSettings = "Settings...";
            MenuExit = "Exit";
            BalloonWatching = "Watching {0}";
            BalloonFileReady = "File ready";
            BalloonClickToOpen = " - click to open";
            BalloonUpdateAvailable = "Update available";
            BalloonUpdateBody = "v{0} is out (you have v{1}) - click to open the release page";
            BalloonProcessingError = "Processing error";
            BalloonWatchFolderMissing = "Watch folder does not exist: {0}";
            AlreadyRunning = "Mp4TailWatcher is already running (check the system tray).";
            DownloadFfmpegFailed = "Failed to download ffmpeg:\n";
            SettingsSaved = "Settings saved. Watching {0}";
            FolderMissing = "Folder does not exist:\n{0}";
            UpdateCheckTitle = "Update check";
            UpdateCheckNoInfo = "Could not read latest release info.";
            UpdateCheckUpToDate = "You are up to date (v{0}).";
            UpdateCheckLatest = "Latest release: {0}  Your build: {1}";
            UpdateCheckFailed = "Update check failed:\n{0}";
            UpdateAvailableTitle = "Update available";
            UpdateAvailablePrompt = "New version v{0} is available (you have v{1}). Open the release page?";
            SettingsDialogTitle = "Mp4TailWatcher - settings";
            SettingsWatchFolder = "Watch folder:";
            SettingsSeconds = "Seconds to keep:";
            SettingsFastCut = "Fast cut by keyframe\r\n(unchecked = exact length, re-encode - slower)";
            SettingsBrowse = "Browse...";
            SettingsOk = "OK";
            SettingsCancel = "Cancel";
            DownloadDialogTitle = "Mp4TailWatcher - setup";
            DownloadingLabel = "Downloading ffmpeg.exe...";
            ExtractingLabel = "Extracting...";
        }
    }
}

static class Program
{
    [STAThread]
    static int Main()
    {
        bool created;
        using (var mutex = new Mutex(true, "Mp4TailWatcher.SingleInstance", out created))
        {
            if (!created)
            {
                MessageBox.Show(L10n.AlreadyRunning, L10n.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                return 0;
            }
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.ThreadException += (s, e) => WatcherApp.Log("UI error: " + e.Exception.Message);
            var app = new WatcherApp();
            if (!app.Failed) Application.Run(app);
        }
        return 0;
    }
}

class WatcherApp : ApplicationContext
{
    static readonly string AppDir = AppDomain.CurrentDomain.BaseDirectory.TrimEnd('\\');
    static readonly string LogPath = Path.Combine(AppDir, "Mp4TailWatcher.log");
    const string OutputSuffix = "_last30";
    const string DefaultFfmpegUrl =
        "https://github.com/ffbinaries/ffbinaries-prebuilt/releases/download/v4.4.1/ffmpeg-4.4.1-win-64.zip";

    readonly string iniPath;
    readonly string ffmpegPath;
    string watchDir, ffmpegUrl;
    int seconds = 30;
    bool keyframeFast = true;
    public bool Failed;

    NotifyIcon tray;
    ContextMenuStrip menu;
    ToolStripMenuItem miStatus, miOpenLast;
    FileSystemWatcher fsw;
    volatile string lastOutput;
    volatile string pendingUrl;
    string updateRepo = "gruz/obrizannia";

    public static void Log(string msg)
    {
        try { File.AppendAllText(LogPath, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "  " + msg + Environment.NewLine); }
        catch { }
    }

    public WatcherApp()
    {
        iniPath = Path.ChangeExtension(System.Reflection.Assembly.GetExecutingAssembly().Location, ".ini");
        LoadConfig();
        ffmpegPath = Path.Combine(AppDir, "ffmpeg.exe");

        menu = new ContextMenuStrip();
        menu.Items.Add(new ToolStripMenuItem(string.Format(L10n.VersionHeader, BuildInfo.Version)) { Enabled = false });
        miStatus = new ToolStripMenuItem { Enabled = false };
        menu.Items.Add(miStatus);
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L10n.MenuOpenWatch, null, (s, e) => OpenPath(watchDir, false));
        miOpenLast = new ToolStripMenuItem(L10n.MenuOpenLast, null, (s, e) => OpenLast());
        menu.Items.Add(miOpenLast);
        menu.Items.Add(L10n.MenuCheckUpdates, null, (s, e) => CheckUpdates(true));
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L10n.MenuSettings, null, (s, e) => ShowSettings());
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add(L10n.MenuExit, null, (s, e) => ExitApp());
        menu.Opening += (s, e) =>
        {
            miStatus.Text = string.Format(L10n.Watching, watchDir);
            miOpenLast.Enabled = lastOutput != null && File.Exists(lastOutput);
        };

        tray = new NotifyIcon
        {
            Icon = SystemIcons.Information,
            Text = string.Format(L10n.TrayTooltip, BuildInfo.Version),
            Visible = true,
            ContextMenuStrip = menu
        };
        tray.BalloonTipClicked += (s, e) => { if (pendingUrl != null) OpenUrl(pendingUrl); else OpenLast(); };
        tray.DoubleClick += (s, e) => OpenLast();

        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | (SecurityProtocolType)12288;
        if (!EnsureFfmpeg()) { Failed = true; tray.Visible = false; return; }
        StartWatcher();
        Log("Started. Watching " + watchDir);
        Notify(L10n.AppName, string.Format(L10n.BalloonWatching, watchDir));
        CheckUpdates(false);
    }

    static string GetDownloadsFolder()
    {
        try
        {
            object v = Microsoft.Win32.Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Explorer\User Shell Folders",
                "{374DE290-123F-4565-9164-39C4925E467B}", null);
            if (v != null)
            {
                string p = Environment.ExpandEnvironmentVariables(v.ToString());
                if (Directory.Exists(p)) return p;
            }
        }
        catch { }
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "Downloads");
    }

    void LoadConfig()
    {
        watchDir = GetDownloadsFolder();
        ffmpegUrl = DefaultFfmpegUrl;
        if (!File.Exists(iniPath)) SaveConfig();
        foreach (var raw in File.ReadAllLines(iniPath))
        {
            string line = raw.Trim();
            if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;
            int eq = line.IndexOf('=');
            if (eq <= 0) continue;
            string key = line.Substring(0, eq).Trim();
            string val = line.Substring(eq + 1).Trim();
            switch (key.ToLowerInvariant())
            {
                case "watchdir": if (val.Length > 0) watchDir = val; break;
                case "seconds": int s; if (int.TryParse(val, out s) && s > 0) seconds = s; break;
                case "keyframefast": bool b; if (bool.TryParse(val, out b)) keyframeFast = b; break;
                case "ffmpegurl": if (val.Length > 0) ffmpegUrl = val; break;
                case "updaterepo": if (val.Length > 0) updateRepo = val; break;
            }
        }
        if (!Path.IsPathRooted(watchDir)) watchDir = Path.GetFullPath(Path.Combine(AppDir, watchDir));
    }

    void SaveConfig()
    {
        File.WriteAllLines(iniPath, new[]
        {
            "; Folder to watch. . = folder where the exe is. Default if unset: your Downloads folder",
            "WatchDir=" + (watchDir == AppDir ? "." : watchDir),
            "",
            "; How many seconds to keep from the end of each new mp4",
            "Seconds=" + seconds,
            "",
            "; true  = fast stream copy, cut snaps to the nearest keyframe (length may differ slightly)",
            "; false = re-encode, exact length but slower",
            "KeyframeFast=" + keyframeFast.ToString().ToLowerInvariant(),
            "",
            "; ffmpeg zip used on first run if ffmpeg.exe is missing",
            "FfmpegUrl=" + ffmpegUrl,
            "; mirror: https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip",
            "",
            "; GitHub repo (owner/name) used for update checks",
            "UpdateRepo=" + updateRepo,
        });
    }

    void ShowSettings()
    {
        using (var f = new SettingsForm(watchDir, seconds, keyframeFast))
        {
            if (f.ShowDialog() != DialogResult.OK) return;
            string dir = f.tbDir.Text.Trim();
            if (dir.Length == 0 || dir == ".") dir = AppDir;
            if (!Path.IsPathRooted(dir)) dir = Path.GetFullPath(Path.Combine(AppDir, dir));
            if (!Directory.Exists(dir))
            {
                MessageBox.Show(string.Format(L10n.FolderMissing, dir), L10n.AppName,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            watchDir = dir;
            seconds = (int)f.numSec.Value;
            keyframeFast = f.chkFast.Checked;
            SaveConfig();
            StartWatcher();
            Log("Settings updated: dir=" + watchDir + " seconds=" + seconds + " fast=" + keyframeFast);
            Notify(L10n.AppName, string.Format(L10n.SettingsSaved, watchDir));
        }
    }

    bool EnsureFfmpeg()
    {
        if (File.Exists(ffmpegPath)) return true;
        string err = null;
        using (var dlg = new DownloadForm())
        {
            var task = Task.Run(() =>
            {
                string tmp = Path.Combine(Path.GetTempPath(), "ffmpeg_" + Guid.NewGuid().ToString("N"));
                try
                {
                    Directory.CreateDirectory(tmp);
                    string zip = Path.Combine(tmp, "ffmpeg.zip");
                    using (var wc = new WebClient())
                    {
                        wc.DownloadProgressChanged += (s, e) =>
                            dlg.SetProgress(e.ProgressPercentage, e.BytesReceived, e.TotalBytesToReceive);
                        wc.DownloadFileTaskAsync(new Uri(ffmpegUrl), zip).GetAwaiter().GetResult();
                    }
                    dlg.SetExtracting();
                    ZipFile.ExtractToDirectory(zip, tmp);
                    string exe = null;
                    foreach (var file in Directory.GetFiles(tmp, "ffmpeg.exe", SearchOption.AllDirectories)) { exe = file; break; }
                    if (exe == null) throw new Exception("ffmpeg.exe was not found inside the downloaded archive");
                    File.Copy(exe, ffmpegPath, true);
                    Log("ffmpeg.exe downloaded to " + ffmpegPath);
                }
                catch (Exception ex) { err = ex.Message; }
                finally { try { Directory.Delete(tmp, true); } catch { } }
            });
            task.ContinueWith(t => dlg.BeginInvoke((Action)(() => dlg.Close())));
            dlg.ShowDialog();
        }
        if (err == null) return true;
        Log("ffmpeg download failed: " + err);
        MessageBox.Show(L10n.DownloadFfmpegFailed + err, L10n.AppName,
            MessageBoxButtons.OK, MessageBoxIcon.Error);
        return false;
    }

    void StartWatcher()
    {
        if (fsw != null) { fsw.EnableRaisingEvents = false; fsw.Dispose(); fsw = null; }
        if (!Directory.Exists(watchDir))
        {
            Log("Watch folder missing: " + watchDir);
            Notify(L10n.AppName, string.Format(L10n.BalloonWatchFolderMissing, watchDir));
            return;
        }
        fsw = new FileSystemWatcher(watchDir, "*.mp4") { EnableRaisingEvents = true };
        fsw.Created += (s, e) => Handle(e.FullPath);
        fsw.Renamed += (s, e) => Handle(e.FullPath);
        miStatus.Text = string.Format(L10n.Watching, watchDir);
    }

    void Handle(string path)
    {
        if (Path.GetFileNameWithoutExtension(path).EndsWith(OutputSuffix, StringComparison.OrdinalIgnoreCase)) return;
        Task.Run(() => ProcessFile(path));
    }

    static bool WaitStable(string path)
    {
        var sw = Stopwatch.StartNew();
        long lastLen = -1;
        while (sw.Elapsed < TimeSpan.FromMinutes(10))
        {
            if (!File.Exists(path)) return false;
            long len;
            try { len = new FileInfo(path).Length; } catch { len = -1; }
            bool locked;
            try { using (File.Open(path, FileMode.Open, FileAccess.Read, FileShare.None)) { } locked = false; }
            catch (IOException) { locked = true; }
            catch { locked = false; }
            if (!locked && len > 0 && len == lastLen) return true;
            lastLen = len;
            Thread.Sleep(1500);
        }
        return false;
    }

    double? GetDuration(string path)
    {
        var psi = new ProcessStartInfo(ffmpegPath, "-hide_banner -i \"" + path + "\"")
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true
        };
        var p = System.Diagnostics.Process.Start(psi);
        string err = p.StandardError.ReadToEnd();
        p.StandardOutput.ReadToEnd();
        p.WaitForExit();
        var m = Regex.Match(err, @"Duration:\s*(\d+):(\d+):(\d+(?:\.\d+)?)");
        if (!m.Success) return null;
        return int.Parse(m.Groups[1].Value) * 3600 + int.Parse(m.Groups[2].Value) * 60
             + double.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
    }

    int RunFfmpeg(string input, string outp, double startSec, bool fast)
    {
        string args = "-hide_banner -loglevel error -y -i \"" + input + "\" -ss " +
            startSec.ToString("0.###", CultureInfo.InvariantCulture) + " " +
            (fast ? "-c copy" : "-c:v libx264 -preset veryfast -c:a aac") +
            " \"" + outp + "\"";
        var psi = new ProcessStartInfo(ffmpegPath, args)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true
        };
        var p = System.Diagnostics.Process.Start(psi);
        string err = p.StandardError.ReadToEnd();
        p.WaitForExit();
        if (p.ExitCode != 0 && err.Trim().Length > 0) Log("[ffmpeg] " + err.Trim());
        return p.ExitCode;
    }

    void ProcessFile(string path)
    {
        try
        {
            Log("[new] " + path + " - waiting for it to finish writing...");
            if (!WaitStable(path)) { Log("[skip] " + path); return; }
            string baseName = Path.GetFileNameWithoutExtension(path);
            string outp = Path.Combine(Path.GetDirectoryName(path), baseName + OutputSuffix + ".mp4");

            double? dur = GetDuration(path);
            double start = (dur.HasValue && dur.Value > seconds) ? dur.Value - seconds : 0;

            int code = 0;
            if (start > 0)
            {
                code = RunFfmpeg(path, outp, start, keyframeFast);
                if (code != 0 || !File.Exists(outp)) code = RunFfmpeg(path, outp, start, false);
            }
            else
            {
                File.Copy(path, outp, true); // video is shorter than the requested tail
            }

            if (code == 0 && File.Exists(outp))
            {
                lastOutput = outp;
                Log("[ok] " + outp);
                Notify(L10n.BalloonFileReady, baseName + OutputSuffix + ".mp4" + L10n.BalloonClickToOpen);
            }
            else
            {
                Log("[fail] " + path);
                Notify(L10n.BalloonProcessingError, baseName + ".mp4");
            }
        }
        catch (Exception ex)
        {
            Log("[fail] " + path + " : " + ex.Message);
        }
    }

    void OpenPath(string p, bool selectFile)
    {
        if (p == null) return;
        try { System.Diagnostics.Process.Start("explorer.exe", selectFile ? "/select,\"" + p + "\"" : "\"" + p + "\""); }
        catch { }
    }

    void OpenLast()
    {
        if (lastOutput != null && File.Exists(lastOutput)) OpenPath(lastOutput, true);
    }

    static void OpenUrl(string url)
    {
        try { System.Diagnostics.Process.Start(url); } catch { }
    }

    void Notify(string title, string msg, string url = null)
    {
        pendingUrl = url;
        try { tray.BalloonTipTitle = title; tray.BalloonTipText = msg; tray.ShowBalloonTip(8000); } catch { }
    }

    void CheckUpdates(bool manual)
    {
        Task.Run(() =>
        {
            try
            {
                var req = (HttpWebRequest)WebRequest.Create(
                    "https://api.github.com/repos/" + updateRepo + "/releases/latest");
                req.UserAgent = "Mp4TailWatcher/" + BuildInfo.Version;
                req.Timeout = 15000;
                string json;
                using (var resp = (HttpWebResponse)req.GetResponse())
                using (var sr = new StreamReader(resp.GetResponseStream())) json = sr.ReadToEnd();

                var mt = Regex.Match(json, "\"tag_name\"\\s*:\\s*\"([^\"]+)\"");
                var mu = Regex.Match(json, "\"html_url\"\\s*:\\s*\"([^\"]+)\"");
                if (!mt.Success)
                {
                    if (manual) MessageBox.Show(L10n.UpdateCheckNoInfo, L10n.UpdateCheckTitle,
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
                string url = mu.Success ? mu.Groups[1].Value
                                        : "https://github.com/" + updateRepo + "/releases";
                Version latest, current;
                if (Version.TryParse(mt.Groups[1].Value.TrimStart('v', 'V'), out latest)
                    && Version.TryParse(BuildInfo.Version, out current))
                {
                    if (latest > current)
                    {
                        Log("new version available: " + mt.Groups[1].Value);
                        Notify(L10n.BalloonUpdateAvailable,
                            string.Format(L10n.BalloonUpdateBody, latest, current), url);
                        if (manual && MessageBox.Show(
                            string.Format(L10n.UpdateAvailablePrompt, latest, current),
                            L10n.UpdateAvailableTitle, MessageBoxButtons.YesNo,
                            MessageBoxIcon.Information) == DialogResult.Yes)
                            OpenUrl(url);
                    }
                    else if (manual)
                    {
                        MessageBox.Show(string.Format(L10n.UpdateCheckUpToDate, current), L10n.UpdateCheckTitle,
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                else if (manual)
                {
                    MessageBox.Show(string.Format(L10n.UpdateCheckLatest, mt.Groups[1].Value, BuildInfo.Version),
                        L10n.UpdateCheckTitle, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
            catch (Exception ex)
            {
                Log("update check failed: " + ex.Message);
                if (manual) MessageBox.Show(string.Format(L10n.UpdateCheckFailed, ex.Message), L10n.UpdateCheckTitle,
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        });
    }

    void ExitApp()
    {
        Log("Stopped.");
        tray.Visible = false;
        if (fsw != null) fsw.Dispose();
        Application.Exit();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (fsw != null) fsw.Dispose();
            if (tray != null) { tray.Visible = false; tray.Dispose(); }
            if (menu != null) menu.Dispose();
        }
        base.Dispose(disposing);
    }
}

class DownloadForm : Form
{
    readonly ProgressBar bar = new ProgressBar();
    readonly Label lbl = new Label();

    public DownloadForm()
    {
        Text = L10n.DownloadDialogTitle;
        Width = 430; Height = 120;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false; ControlBox = false;
        lbl.SetBounds(15, 15, 385, 20);
        lbl.Text = L10n.DownloadingLabel;
        bar.SetBounds(15, 40, 385, 23);
        bar.Minimum = 0; bar.Maximum = 100;
        Controls.Add(lbl); Controls.Add(bar);
    }

    public void SetProgress(int pct, long got, long total)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => SetProgress(pct, got, total))); return; }
        if (pct >= 0 && pct <= 100)
        {
            bar.Style = ProgressBarStyle.Continuous;
            bar.Value = pct;
            lbl.Text = string.Format("Downloading ffmpeg.exe... {0}% ({1:0.0} / {2:0.0} MB)",
                pct, got / 1048576.0, total / 1048576.0);
        }
        else bar.Style = ProgressBarStyle.Marquee;
    }

    public void SetStatus(string text)
    {
        if (InvokeRequired) { BeginInvoke((Action)(() => SetStatus(text))); return; }
        lbl.Text = text;
        bar.Style = ProgressBarStyle.Marquee;
    }

    public void SetExtracting()
    {
        SetStatus(L10n.ExtractingLabel);
    }
}

class SettingsForm : Form
{
    public TextBox tbDir = new TextBox();
    public NumericUpDown numSec = new NumericUpDown();
    public CheckBox chkFast = new CheckBox();

    public SettingsForm(string dir, int sec, bool fast)
    {
        Text = L10n.SettingsDialogTitle;
        Width = 480; Height = 220;
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false; MinimizeBox = false;

        var l1 = new Label { Text = L10n.SettingsWatchFolder, AutoSize = true };
        l1.SetBounds(15, 17, 130, 20);
        tbDir.SetBounds(150, 14, 210, 22);
        tbDir.Text = dir;
        var bBrowse = new Button { Text = L10n.SettingsBrowse };
        bBrowse.SetBounds(365, 12, 80, 25);
        bBrowse.Click += (s, e) =>
        {
            using (var d = new FolderBrowserDialog { SelectedPath = tbDir.Text })
                if (d.ShowDialog(this) == DialogResult.OK) tbDir.Text = d.SelectedPath;
        };

        var l2 = new Label { Text = L10n.SettingsSeconds, AutoSize = true };
        l2.SetBounds(15, 52, 130, 20);
        numSec.SetBounds(150, 49, 80, 22);
        numSec.Minimum = 1; numSec.Maximum = 86400;
        numSec.Value = Math.Max(1, Math.Min(86400, sec));

        chkFast.SetBounds(150, 80, 310, 40);
        chkFast.Text = L10n.SettingsFastCut;
        chkFast.Checked = fast;

        var bOk = new Button { Text = L10n.SettingsOk, DialogResult = DialogResult.OK };
        bOk.SetBounds(245, 130, 100, 27);
        var bCancel = new Button { Text = L10n.SettingsCancel, DialogResult = DialogResult.Cancel };
        bCancel.SetBounds(355, 130, 90, 27);
        AcceptButton = bOk; CancelButton = bCancel;

        Controls.AddRange(new Control[] { l1, tbDir, bBrowse, l2, numSec, chkFast, bOk, bCancel });
    }
}

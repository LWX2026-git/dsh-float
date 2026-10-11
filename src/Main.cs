// dsh-float 0.7.32 beta - C# 5 兼容版
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Web.Script.Serialization;
using System.Windows.Forms;
using Microsoft.Win32;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace DshFloat
{
    // ============================================================
    // Program
    // ============================================================
    static class Program
    {
        public const string APP_VERSION = "0.7.32";
        public const string APP_BUILD   = "beta";

        public static Config Cfg;

        [STAThread]
        static void Main(string[] args)
        {
            try
            {
                // ★ 后台更新检查进程（开机自启走这条路径）
                if (args != null && args.Length > 0 &&
                    (args[0] == "--update-checker" || args[0] == "-u"))
                {
                    UpdateCheckerDaemon.Run();
                    return;
                }

                // 正常 GUI 启动
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);

                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                Cfg = Config.Load(Path.Combine(exeDir, "config.json"));

                Application.ApplicationExit += delegate
                {
                    try { Cfg.Save(); } catch { }
                };

                Application.Run(new MainForm());
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "启动失败：\n" + ex.Message + "\n\n" + ex.StackTrace,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    // ============================================================
    // LogHelper
    // ============================================================
    public static class LogHelper
    {
        private static readonly object _lock = new object();
        private static string _path = null;

        public static string LogPath
        {
            get
            {
                if (_path == null)
                {
                    string dir = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                        "dsh-float");
                    Directory.CreateDirectory(dir);
                    _path = Path.Combine(dir, "dsh_cs.log");
                }
                return _path;
            }
        }

        public static void Write(string msg)
        {
            try
            {
                lock (_lock)
                {
                    File.AppendAllText(LogPath,
                        "[" + DateTime.Now.ToString("HH:mm:ss.fff") + "] " + msg + "\r\n");
                }
            }
            catch { }
        }
    }

    // ============================================================
    // Config
    // ============================================================
    public class Config
    {
        public string ClientId        { get; set; }
        public string ServerUrl       { get; set; }
        public string OutputDir       { get; set; }
        public string NamePrefix      { get; set; }
        public int    VideoFps        { get; set; }
        public bool   WarnDeepThink   { get; set; }
        public bool   AutoAttachMusic { get; set; }
        public bool   AutoClickContinue { get; set; }
        public bool   AutoSaveToast   { get; set; }
        public bool   WindowTopmost   { get; set; }
        public bool   ToolbarMode     { get; set; }
        public bool   SplashEnable    { get; set; }
        public bool   AutoStart       { get; set; }
        public string Theme           { get; set; }
        public bool   Devtools        { get; set; }
        public bool   AutoCheckUpdate { get; set; }
        public int    ToastDuration   { get; set; }
        public string ToastCorner     { get; set; }
        public int    WindowAlpha     { get; set; }
        public int    MiniW           { get; set; }
        public int    MiniH           { get; set; }
        public bool   RememberWindow  { get; set; }
        public string WindowGeom      { get; set; }
        public int    ToolbarX        { get; set; }
        public int    ToolbarY        { get; set; }
        public bool   HotkeyEnable    { get; set; }
        public bool   LogToFile       { get; set; }

        public string ConfigPath { get; set; }

        public Config()
        {
            ClientId = "";
            ServerUrl = "https://api.lwx3dp.de5.net/api/dsh";
            OutputDir = "";
            NamePrefix = "ds";
            VideoFps = 15;
            WarnDeepThink = true;
            AutoAttachMusic = true;
            AutoClickContinue = true;
            AutoSaveToast = true;
            WindowTopmost = false;
            ToolbarMode = false;
            SplashEnable = true;
            AutoStart = false;
            Theme = "dark";
            Devtools = false;
            AutoCheckUpdate = true;
            ToastDuration = 3000;
            ToastCorner = "br";
            WindowAlpha = 100;
            MiniW = 500;
            MiniH = 700;
            RememberWindow = true;
            WindowGeom = "";
            ToolbarX = -1;
            ToolbarY = -1;
            HotkeyEnable = true;
            LogToFile = false;
            ConfigPath = "";
        }

        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer();

        public static Config Load(string path)
        {
            Config cfg = new Config();
            cfg.ConfigPath = path;

            try
            {
                if (File.Exists(path))
                {
                    string json = File.ReadAllText(path);
                    Config loaded = Serializer.Deserialize<Config>(json);
                    if (loaded != null)
                    {
                        loaded.ConfigPath = path;
                        cfg = loaded;
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(cfg.ClientId))
            {
                cfg.ClientId = Guid.NewGuid().ToString("N").Substring(0, 16);
                cfg.Save();
            }
            return cfg;
        }

        public void Save()
        {
            try
            {
                string json = Serializer.Serialize(this);
                File.WriteAllText(ConfigPath, json);
            }
            catch { }
        }

        public string EnsureOutputsDir()
        {
            string d = OutputDir;
            if (string.IsNullOrEmpty(d))
                d = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "outputs");
            Directory.CreateDirectory(d);
            return d;
        }
    }

    // ============================================================
    // AutoStart —— 注册表命令带 --update-checker
    // ============================================================
    public static class AutoStart
    {
        private const string RUN_KEY = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
        private const string RUN_NAME = "dsh-float";

        public static bool Set(bool enable)
        {
            try
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey(RUN_KEY, true);
                if (key == null) return false;

                if (enable)
                {
                    string exe = System.Reflection.Assembly
                        .GetExecutingAssembly().Location;
                    key.SetValue(RUN_NAME, "\"" + exe + "\" --update-checker");
                }
                else
                {
                    try { key.DeleteValue(RUN_NAME, false); } catch { }
                }
                key.Close();
                return true;
            }
            catch { return false; }
        }
    }

    // ============================================================
    // HotkeyManager
    // ============================================================
    public static class HotkeyManager
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id,
            uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint MOD_ALT      = 0x0001;
        private const uint MOD_NOREPEAT = 0x4000;
        private const int  VK_SPACE     = 0x20;
        private const int  HOTKEY_ID    = 0x5555;

        public static bool Register(IntPtr hwnd)
        {
            try
            {
                return RegisterHotKey(hwnd, HOTKEY_ID,
                    MOD_ALT | MOD_NOREPEAT, VK_SPACE);
            }
            catch { return false; }
        }

        public static void Unregister(IntPtr hwnd)
        {
            try { UnregisterHotKey(hwnd, HOTKEY_ID); } catch { }
        }
    }

    // ============================================================
    // DshLauncher
    // ============================================================
    public static class DshLauncher
    {
        public const int PORT = 3080;
        public const string NODE_URL = "https://nodejs.org/zh-cn/download";

        public static string DshLogPath
        {
            get { return Path.Combine(Path.GetTempPath(), "dsh_log.log"); }
        }

        public static bool IsNodeOk()
        {
            return Which("node") && Which("npx");
        }

        private static bool Which(string name)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c where " + name;
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;
                using (Process p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    p.WaitForExit(3000);
                    return p.ExitCode == 0;
                }
            }
            catch { return false; }
        }

        public static bool IsPortOpen()
        {
            try
            {
                using (TcpClient c = new TcpClient())
                {
                    c.Connect("127.0.0.1", PORT);
                    return true;
                }
            }
            catch { return false; }
        }

        public static void StartService()
        {
            try
            {
                try { if (File.Exists(DshLogPath)) File.Delete(DshLogPath); }
                catch { }

                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "cmd.exe";
                psi.Arguments = "/c npx @deepseek-ai/dsh web --no-open > \"" +
                                DshLogPath + "\" 2>&1";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);
                LogHelper.Write("[DSH] 已启动 npx");
            }
            catch (Exception ex)
            {
                LogHelper.Write("[DSH] 启动失败: " + ex.Message);
            }
        }

        public static string GetDshUrl()
        {
            if (IsPortOpen())
            {
                LogHelper.Write("[DSH] 端口已开，直接访问 http://127.0.0.1:3080/");
                return "http://127.0.0.1:3080/";
            }

            if (!IsNodeOk())
            {
                LogHelper.Write("[DSH] 未装 node，打开下载页");
                return NODE_URL;
            }

            LogHelper.Write("[DSH] 启动服务");
            StartService();
            return "about:dsh-starting";
        }

        public static string WaitHtml()
        {
            return ReadResFile("dsh_wait.html",
                "<html><body style='background:#1e1e1e;color:#fff'>DSH starting...</body></html>");
        }

        public static string PortBusyHtml()
        {
            return ReadResFile("dsh_busy.html",
                "<html><body style='background:#1e1e1e;color:#fff'>Port 3080 busy</body></html>");
        }

        private static string ResFile(string name, string fallback)
        {
            try
            {
                string path = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, name);
                if (File.Exists(path))
                    return File.ReadAllText(path, Encoding.UTF8);
            }
            catch { }
            return fallback;
        }

        public static string ReadResFile(string name, string fallback)
        {
            return ResFile(name, fallback);
        }
    }

    // ============================================================
    // UpdateCheckerDaemon —— 独立后台更新检查进程
    // 启动方式：dsh-float.exe --update-checker
    //
    // ★ 新增：命名停止事件 STOP_EVENT_NAME
    //   - 主程序启动时，通过 Set 停止事件通知本进程退出
    //   - 本进程在 sleep 中每秒检查一次事件，收到信号立即返回
    // ============================================================
    public static class UpdateCheckerDaemon
    {
        public const int UPDATE_INTERVAL = 3 * 3600;   // 3 小时
        public const int RETRY_DELAY = 30;              // 30 秒
        public const int MAX_FAIL = 10;                 // 连续失败 10 次
        public const string MUTEX_NAME = "Local\\dsh-float-bg-updater-v1";
        public const string STOP_EVENT_NAME = "Local\\dsh-float-bg-stop-v1";

        public static void Run()
        {
            Mutex mutex = null;
            EventWaitHandle stopEvent = null;
            bool owned = false;
            try
            {
                bool createdNew;
                mutex = new Mutex(true, MUTEX_NAME, out createdNew);
                if (createdNew)
                {
                    owned = true;
                }
                else
                {
                    try
                    {
                        if (mutex.WaitOne(0)) owned = true;
                    }
                    catch (AbandonedMutexException) { owned = true; }
                }
                if (!owned)
                {
                    try { mutex.Close(); } catch { }
                    return;
                }

                // ★ 打开停止事件并清空旧信号
                try
                {
                    stopEvent = new EventWaitHandle(false,
                        EventResetMode.ManualReset, STOP_EVENT_NAME);
                    try { stopEvent.Reset(); } catch { }
                }
                catch { stopEvent = null; }

                int fail = 0;
                while (true)
                {
                    if (stopEvent != null && stopEvent.WaitOne(0)) return;

                    string exeDir = Path.GetDirectoryName(
                        System.Reflection.Assembly.GetExecutingAssembly().Location);
                    Config cfg = Config.Load(Path.Combine(exeDir, "config.json"));

                    if (!cfg.AutoCheckUpdate)
                    {
                        // 用户关了自动检查 → 后台进程退出
                        return;
                    }

                    int r = CheckOnce(cfg);

                    if (r == 2)
                    {
                        fail++;
                        if (fail >= MAX_FAIL)
                        {
                            fail = 0;
                            if (SleepInterruptible(stopEvent, 300)) return;
                        }
                        else
                        {
                            if (SleepInterruptible(stopEvent, RETRY_DELAY)) return;
                        }
                        continue;
                    }

                    fail = 0;

                    // 分片睡眠，最长 3 小时；收到停止信号立即返回
                    if (SleepInterruptible(stopEvent, UPDATE_INTERVAL)) return;
                }
            }
            catch { }
            finally
            {
                if (owned && mutex != null)
                {
                    try { mutex.ReleaseMutex(); } catch { }
                }
                if (mutex != null)
                {
                    try { mutex.Close(); } catch { }
                }
                if (stopEvent != null)
                {
                    try { stopEvent.Close(); } catch { }
                }
            }
        }

        // 可被停止事件打断的睡眠；返回 true 表示收到停止信号
        private static bool SleepInterruptible(EventWaitHandle stopEvent, int seconds)
        {
            for (int i = 0; i < seconds; i++)
            {
                if (stopEvent != null)
                {
                    if (stopEvent.WaitOne(1000)) return true;
                }
                else
                {
                    Thread.Sleep(1000);
                }
            }
            return stopEvent != null && stopEvent.WaitOne(0);
        }

        // 返回值：0=已最新，1=有新版，2=失败
        public static int CheckOnce(Config cfg)
        {
            try
            {
                string baseUrl = cfg.ServerUrl;
                if (string.IsNullOrEmpty(baseUrl))
                    baseUrl = "https://api.lwx3dp.de5.net/api/dsh";

                string url = baseUrl + "/updata?v=" + Program.APP_VERSION +
                             "&id=" + cfg.ClientId + "&platform=win64";

                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                req.Timeout = 15000;
                req.UserAgent = "dsh-float/" + Program.APP_VERSION;

                using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                {
                    string json = sr.ReadToEnd();
                    JavaScriptSerializer ser = new JavaScriptSerializer();
                    object parsed = ser.DeserializeObject(json);
                    Dictionary<string, object> d =
                        parsed as Dictionary<string, object>;
                    if (d == null) return 0;

                    bool hasNew = false;
                    if (d.ContainsKey("has_new"))
                    {
                        object hn = d["has_new"];
                        if (hn is bool) hasNew = (bool)hn;
                        else hasNew = Convert.ToBoolean(hn);
                    }
                    if (!hasNew) return 0;

                    string latest = d.ContainsKey("latest")
                        ? Convert.ToString(d["latest"]) : "";
                    string dl = d.ContainsKey("download")
                        ? Convert.ToString(d["download"]) : "";

                    DialogResult res = MessageBox.Show(
                        "发现新版本：" + latest + "\n" +
                        "当前版本：v" + Program.APP_VERSION + "\n\n" +
                        "是否打开下载页面？",
                        "更新提示",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Information);

                    if (res == DialogResult.Yes && !string.IsNullOrEmpty(dl))
                    {
                        try { Process.Start(dl); } catch { }
                    }
                    return 1;
                }
            }
            catch
            {
                return 2;
            }
        }
    }

    // ============================================================
    // Updater
    // ============================================================
    public static class Updater
    {
        // ★ 启动时调用：
        //   1. 通知旧后台更新检查进程退出
        //   2. 等待互斥体释放
        //   3. 清空停止事件
        //   4. 拉起新的后台进程
        public static void StopAndRestartDaemon()
        {
            try
            {
                // 1. 通知旧进程退出
                try
                {
                    using (EventWaitHandle stop = EventWaitHandle.OpenExisting(
                        UpdateCheckerDaemon.STOP_EVENT_NAME))
                    {
                        stop.Set();
                    }
                    LogHelper.Write("[更新] 已通知旧后台进程退出");
                }
                catch (WaitHandleCannotBeOpenedException)
                {
                    // 事件不存在 → 没有旧进程
                }
                catch { }

                // 2. 等待旧进程释放互斥体（最多 5 秒）
                bool freed = false;
                for (int i = 0; i < 50; i++)
                {
                    try
                    {
                        using (Mutex m = Mutex.OpenExisting(
                            UpdateCheckerDaemon.MUTEX_NAME))
                        {
                            bool got = false;
                            try { got = m.WaitOne(0); }
                            catch (AbandonedMutexException) { got = true; }

                            if (got)
                            {
                                try { m.ReleaseMutex(); } catch { }
                                freed = true;
                            }
                        }
                    }
                    catch (WaitHandleCannotBeOpenedException) { freed = true; }
                    catch { freed = true; }

                    if (freed) break;
                    Thread.Sleep(100);
                }
                LogHelper.Write("[更新] 旧后台进程已退出: " + freed);

                // 3. 清空停止事件，避免新进程启动后立刻退出
                try
                {
                    using (EventWaitHandle stop = EventWaitHandle.OpenExisting(
                        UpdateCheckerDaemon.STOP_EVENT_NAME))
                    {
                        stop.Reset();
                    }
                }
                catch { }

                // 4. 拉起新的后台进程
                string exe = System.Reflection.Assembly.GetExecutingAssembly().Location;
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = exe;
                psi.Arguments = "--update-checker";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                Process.Start(psi);

                LogHelper.Write("[更新] 已拉起新后台更新检查进程");
            }
            catch (Exception ex)
            {
                LogHelper.Write("[更新] 重启后台进程失败: " + ex.Message);
            }
        }

        // 手动检查：回调 (hasNew, latest, downloadOrError)
        public static void CheckNow(Action<bool, string, string> callback)
        {
            Thread th = new Thread(delegate()
            {
                try
                {
                    string baseUrl = Program.Cfg.ServerUrl;
                    if (string.IsNullOrEmpty(baseUrl))
                        baseUrl = "https://api.lwx3dp.de5.net/api/dsh";

                    string url = baseUrl + "/updata?v=" + Program.APP_VERSION +
                                 "&id=" + Program.Cfg.ClientId +
                                 "&platform=win64";

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 15000;
                    req.UserAgent = "dsh-float/" + Program.APP_VERSION;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string json = sr.ReadToEnd();
                        JavaScriptSerializer ser = new JavaScriptSerializer();
                        object parsed = ser.DeserializeObject(json);
                        Dictionary<string, object> d =
                            parsed as Dictionary<string, object>;
                        if (d == null)
                        {
                            callback(false, "", "error:返回数据格式异常");
                            return;
                        }

                        bool hasNew = false;
                        if (d.ContainsKey("has_new"))
                        {
                            object hn = d["has_new"];
                            if (hn is bool) hasNew = (bool)hn;
                            else hasNew = Convert.ToBoolean(hn);
                        }
                        string latest = d.ContainsKey("latest")
                            ? Convert.ToString(d["latest"]) : "";
                        string dl = d.ContainsKey("download")
                            ? Convert.ToString(d["download"]) : "";

                        callback(hasNew, latest, dl);
                    }
                }
                catch (Exception ex)
                {
                    callback(false, "", "error:" + ex.Message);
                }
            });
            th.IsBackground = true;
            th.Start();
        }
    }

    // ============================================================
    // ProgressForm
    // ============================================================
    public class ProgressForm : Form
    {
        private static ProgressForm _instance = null;

        private Label _lblTitle;
        private Label _lblDetail;
        private ProgressBar _bar;
        private Button _btnClose;

        public ProgressForm()
        {
            Text = "生成中";
            Width = 480;
            Height = 200;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;

            _lblTitle = new Label();
            _lblTitle.Text = "准备中...";
            _lblTitle.Left = 20; _lblTitle.Top = 20;
            _lblTitle.Width = 440; _lblTitle.Height = 30;
            _lblTitle.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
            _lblTitle.ForeColor = Color.White;
            Controls.Add(_lblTitle);

            _bar = new ProgressBar();
            _bar.Left = 20; _bar.Top = 60;
            _bar.Width = 440; _bar.Height = 26;
            _bar.Style = ProgressBarStyle.Marquee;
            Controls.Add(_bar);

            _lblDetail = new Label();
            _lblDetail.Text = "";
            _lblDetail.Left = 20; _lblDetail.Top = 92;
            _lblDetail.Width = 440; _lblDetail.Height = 40;
            _lblDetail.ForeColor = Color.FromArgb(180, 180, 180);
            _lblDetail.Font = new Font("Microsoft YaHei", 9);
            Controls.Add(_lblDetail);

            _btnClose = new Button();
            _btnClose.Text = "关闭";
            _btnClose.Left = 380; _btnClose.Top = 130;
            _btnClose.Width = 80; _btnClose.Height = 28;
            _btnClose.BackColor = Color.FromArgb(0, 120, 215);
            _btnClose.ForeColor = Color.White;
            _btnClose.FlatStyle = FlatStyle.Flat;
            _btnClose.FlatAppearance.BorderSize = 0;
            _btnClose.Visible = false;
            _btnClose.Click += delegate { try { Close(); } catch { } };
            Controls.Add(_btnClose);

            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (!_btnClose.Visible) e.Cancel = true;
            };
        }

        private static void EnsureInstance()
        {
            if (_instance == null || _instance.IsDisposed)
            {
                _instance = new ProgressForm();
                _instance.Show();
            }
        }

        public static void ShowProgress(string title, string detail)
        {
            try
            {
                EnsureInstance();
                _instance.BeginInvoke((MethodInvoker)delegate
                {
                    _instance._lblTitle.Text = title;
                    _instance._lblDetail.Text = detail;
                    _instance._bar.Style = ProgressBarStyle.Marquee;
                    _instance._btnClose.Visible = false;
                    _instance.Text = "生成中";
                });
            }
            catch { }
        }

        public static void UpdateDetail(string detail)
        {
            try
            {
                if (_instance == null || _instance.IsDisposed) return;
                _instance.BeginInvoke((MethodInvoker)delegate
                {
                    _instance._lblDetail.Text = detail;
                });
            }
            catch { }
        }

        public static void ShowDone(string title, string detail)
        {
            try
            {
                if (_instance == null || _instance.IsDisposed) return;
                _instance.BeginInvoke((MethodInvoker)delegate
                {
                    _instance._lblTitle.Text = title;
                    _instance._lblDetail.Text = detail;
                    _instance._bar.Style = ProgressBarStyle.Continuous;
                    _instance._bar.Value = 100;
                    _instance._btnClose.Visible = true;
                    _instance.Text = "完成";
                });
            }
            catch { }
        }

        public static void ShowError(string title, string detail)
        {
            try
            {
                if (_instance == null || _instance.IsDisposed) return;
                _instance.BeginInvoke((MethodInvoker)delegate
                {
                    _instance._lblTitle.Text = title;
                    _instance._lblDetail.Text = detail;
                    _instance._bar.Style = ProgressBarStyle.Continuous;
                    _instance._bar.Value = 0;
                    _instance._btnClose.Visible = true;
                    _instance.Text = "失败";
                });
            }
            catch { }
        }

        public static void CloseIfOpen()
        {
            try
            {
                if (_instance != null && !_instance.IsDisposed)
                {
                    _instance._btnClose.Visible = true;
                    _instance.Close();
                }
            }
            catch { }
            _instance = null;
        }
    }

    // ============================================================
    // FeedbackForm
    // ============================================================
    public class FeedbackForm : Form
    {
        private TextBox _txtMsg;
        private Label _lblStatus;
        private Button _btnSend;
        private Button _btnCancel;

        public FeedbackForm()
        {
            Text = "问题反馈";
            Width = 520;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei", 10);

            Label lblTitle = new Label();
            lblTitle.Text = "问题反馈";
            lblTitle.Left = 16; lblTitle.Top = 14;
            lblTitle.Width = 460; lblTitle.Height = 26;
            lblTitle.Font = new Font("Microsoft YaHei", 12, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            Controls.Add(lblTitle);

            Label lblInfo = new Label();
            lblInfo.Text = "客户端 ID：" + Program.Cfg.ClientId +
                           "\n版本：v" + Program.APP_VERSION + " " + Program.APP_BUILD;
            lblInfo.Left = 16; lblInfo.Top = 46;
            lblInfo.Width = 480; lblInfo.Height = 40;
            lblInfo.ForeColor = Color.FromArgb(160, 160, 160);
            lblInfo.Font = new Font("Microsoft YaHei", 9);
            Controls.Add(lblInfo);

            _txtMsg = new TextBox();
            _txtMsg.Left = 16; _txtMsg.Top = 92;
            _txtMsg.Width = 476; _txtMsg.Height = 220;
            _txtMsg.Multiline = true;
            _txtMsg.ScrollBars = ScrollBars.Vertical;
            _txtMsg.BackColor = Color.FromArgb(30, 30, 30);
            _txtMsg.ForeColor = Color.White;
            _txtMsg.BorderStyle = BorderStyle.FixedSingle;
            _txtMsg.Font = new Font("Microsoft YaHei", 10);
            Controls.Add(_txtMsg);

            _lblStatus = new Label();
            _lblStatus.Text = "";
            _lblStatus.Left = 16; _lblStatus.Top = 320;
            _lblStatus.Width = 300; _lblStatus.Height = 24;
            _lblStatus.ForeColor = Color.FromArgb(160, 160, 160);
            _lblStatus.Font = new Font("Microsoft YaHei", 9);
            Controls.Add(_lblStatus);

            _btnSend = new Button();
            _btnSend.Text = "发送";
            _btnSend.Left = 320; _btnSend.Top = 324;
            _btnSend.Width = 80; _btnSend.Height = 30;
            _btnSend.BackColor = Color.FromArgb(0, 120, 215);
            _btnSend.ForeColor = Color.White;
            _btnSend.FlatStyle = FlatStyle.Flat;
            _btnSend.FlatAppearance.BorderSize = 0;
            _btnSend.Font = new Font("Microsoft YaHei", 10);
            _btnSend.Click += delegate { SendFeedback(); };
            Controls.Add(_btnSend);

            _btnCancel = new Button();
            _btnCancel.Text = "取消";
            _btnCancel.Left = 412; _btnCancel.Top = 324;
            _btnCancel.Width = 80; _btnCancel.Height = 30;
            _btnCancel.BackColor = Color.FromArgb(60, 60, 60);
            _btnCancel.ForeColor = Color.White;
            _btnCancel.FlatStyle = FlatStyle.Flat;
            _btnCancel.FlatAppearance.BorderSize = 0;
            _btnCancel.Font = new Font("Microsoft YaHei", 10);
            _btnCancel.Click += delegate { Close(); };
            Controls.Add(_btnCancel);

            AcceptButton = _btnSend;
            CancelButton = _btnCancel;
            _txtMsg.Focus();
        }

        private void SendFeedback()
        {
            string msg = _txtMsg.Text.Trim();
            if (string.IsNullOrEmpty(msg))
            {
                _lblStatus.Text = "请填写内容";
                return;
            }

            _btnSend.Enabled = false;
            _btnCancel.Enabled = false;
            _lblStatus.Text = "发送中...";

            Thread th = new Thread(delegate()
            {
                bool ok = false;
                string errMsg = "";
                try
                {
                    string baseUrl = Program.Cfg.ServerUrl;
                    if (string.IsNullOrEmpty(baseUrl))
                        baseUrl = "https://api.lwx3dp.de5.net/api/dsh";

                    string url = baseUrl + "/u?q=" + Uri.EscapeDataString(msg) +
                                 "&id=" + Program.Cfg.ClientId +
                                 "&v=" + Program.APP_VERSION +
                                 "&platform=win64";

                    HttpWebRequest req = (HttpWebRequest)WebRequest.Create(url);
                    req.Timeout = 15000;
                    req.UserAgent = "dsh-float/" + Program.APP_VERSION;

                    using (HttpWebResponse resp = (HttpWebResponse)req.GetResponse())
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        string json = sr.ReadToEnd();
                        ok = json.IndexOf("true", StringComparison.OrdinalIgnoreCase) >= 0;
                    }
                }
                catch (Exception ex)
                {
                    errMsg = ex.Message;
                }

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        _btnSend.Enabled = true;
                        _btnCancel.Enabled = true;

                        if (ok)
                        {
                            _lblStatus.Text = "发送成功，感谢反馈！";
                            _txtMsg.Text = "";
                        }
                        else if (!string.IsNullOrEmpty(errMsg))
                        {
                            _lblStatus.Text = "发送失败：" + errMsg;
                        }
                        else
                        {
                            _lblStatus.Text = "发送失败，请稍后再试";
                        }
                    });
                }
                catch { }
            });
            th.IsBackground = true;
            th.Start();
        }
    }

    // ============================================================
    // FfmpegDownloader
    // ============================================================
    public static class FfmpegDownloader
    {
        public const string DownloadUrl =
            "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip";

        public static string TargetPath
        {
            get
            {
                return Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
            }
        }

        public static bool Exists()
        {
            return File.Exists(TargetPath);
        }

        public static bool PromptAndDownload(IWin32Window owner)
        {
            if (Exists()) return true;

            DialogResult r = MessageBox.Show(
                owner,
                "生成视频需要 ffmpeg 程序（约 80 MB）。\n\n" +
                "是否现在从官方自动下载？\n\n" +
                "下载源：\n" + DownloadUrl + "\n\n" +
                "下载完成后会保存到程序目录。",
                "需要下载 ffmpeg",
                MessageBoxButtons.YesNo,
                MessageBoxIcon.Question);

            if (r != DialogResult.Yes) return false;

            DownloadForm dlg = new DownloadForm();
            dlg.ShowDialog(owner);
            return dlg.Success && Exists();
        }
    }

    public class DownloadForm : Form
    {
        public bool Success { get; private set; }

        private ProgressBar bar;
        private Label lblStatus;
        private WebClient client;
        private string zipPath;
        private bool cancelled;

        public DownloadForm()
        {
            Success = false;
            cancelled = false;

            Text = "下载 ffmpeg";
            Width = 500;
            Height = 170;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;

            Label lblTitle = new Label();
            lblTitle.Text = "正在下载 ffmpeg...";
            lblTitle.Left = 16; lblTitle.Top = 14;
            lblTitle.Width = 460; lblTitle.Height = 26;
            lblTitle.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold);
            lblTitle.ForeColor = Color.White;
            Controls.Add(lblTitle);

            bar = new ProgressBar();
            bar.Left = 16; bar.Top = 48;
            bar.Width = 460; bar.Height = 24;
            bar.Minimum = 0; bar.Maximum = 100;
            Controls.Add(bar);

            lblStatus = new Label();
            lblStatus.Text = "连接服务器中...";
            lblStatus.Left = 16; lblStatus.Top = 82;
            lblStatus.Width = 340; lblStatus.Height = 20;
            lblStatus.ForeColor = Color.FromArgb(180, 180, 180);
            lblStatus.Font = new Font("Microsoft YaHei", 9);
            Controls.Add(lblStatus);

            Button btnCancel = new Button();
            btnCancel.Text = "取消";
            btnCancel.Left = 396; btnCancel.Top = 80;
            btnCancel.Width = 80; btnCancel.Height = 28;
            btnCancel.BackColor = Color.FromArgb(60, 60, 60);
            btnCancel.ForeColor = Color.White;
            btnCancel.FlatStyle = FlatStyle.Flat;
            btnCancel.FlatAppearance.BorderSize = 0;
            btnCancel.Click += delegate { CancelDownload(); };
            Controls.Add(btnCancel);

            FormClosing += delegate(object s, FormClosingEventArgs e)
            {
                if (!Success && client != null && client.IsBusy)
                    CancelDownload();
            };

            Load += delegate { StartDownload(); };
        }

        private void StartDownload()
        {
            try
            {
                zipPath = Path.Combine(Path.GetTempPath(), "ffmpeg_download.zip");
                if (File.Exists(zipPath))
                {
                    try { File.Delete(zipPath); } catch { }
                }

                client = new WebClient();
                client.DownloadProgressChanged += OnProgress;
                client.DownloadFileCompleted += OnComplete;
                client.DownloadFileAsync(new Uri(FfmpegDownloader.DownloadUrl), zipPath);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "下载启动失败：" + ex.Message,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                Close();
            }
        }

        private void OnProgress(object sender, DownloadProgressChangedEventArgs e)
        {
            try
            {
                if (bar.IsDisposed) return;
                double got = e.BytesReceived / 1048576.0;
                double total = e.TotalBytesToReceive / 1048576.0;
                string s = string.Format("{0:0.0} / {1:0.0} MB  ({2}%)",
                    got, total, e.ProgressPercentage);

                if (bar.InvokeRequired)
                {
                    bar.BeginInvoke((MethodInvoker)delegate
                    {
                        bar.Value = e.ProgressPercentage;
                        lblStatus.Text = s;
                    });
                }
                else
                {
                    bar.Value = e.ProgressPercentage;
                    lblStatus.Text = s;
                }
            }
            catch { }
        }

        private void OnComplete(object sender, AsyncCompletedEventArgs e)
        {
            if (cancelled)
            {
                try { BeginInvoke((MethodInvoker)delegate { Close(); }); }
                catch { }
                return;
            }

            if (e.Error != null)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(this, "下载失败：" + e.Error.Message +
                            "\n\n可手动下载：\n" + FfmpegDownloader.DownloadUrl,
                            "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                    });
                }
                catch { }
                return;
            }

            try
            {
                BeginInvoke((MethodInvoker)delegate
                {
                    lblStatus.Text = "解压 ffmpeg.exe 中...";
                    bar.Style = ProgressBarStyle.Marquee;
                });
            }
            catch { }

            try
            {
                bool extracted = false;
                using (FileStream fs = new FileStream(zipPath, FileMode.Open,
                    FileAccess.Read))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    foreach (ZipArchiveEntry entry in archive.Entries)
                    {
                        if (entry.Name.Equals("ffmpeg.exe",
                            StringComparison.OrdinalIgnoreCase))
                        {
                            using (Stream src = entry.Open())
                            using (FileStream dst = File.Create(
                                FfmpegDownloader.TargetPath))
                            {
                                CopyStream(src, dst);
                            }
                            extracted = true;
                            break;
                        }
                    }
                }

                if (!extracted)
                    throw new Exception("zip 里没找到 ffmpeg.exe");

                try { File.Delete(zipPath); } catch { }

                Success = true;

                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(this, "ffmpeg 下载完成！",
                            "完成", MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        Close();
                    });
                }
                catch { }
            }
            catch (Exception ex)
            {
                try
                {
                    BeginInvoke((MethodInvoker)delegate
                    {
                        MessageBox.Show(this,
                            "解压失败：" + ex.Message +
                            "\n\n请手动下载解压后，把 ffmpeg.exe 放到：\n" +
                            FfmpegDownloader.TargetPath,
                            "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        Close();
                    });
                }
                catch { }
            }
        }

        private static void CopyStream(Stream src, Stream dst)
        {
            byte[] buf = new byte[81920];
            int n;
            while ((n = src.Read(buf, 0, buf.Length)) > 0)
                dst.Write(buf, 0, n);
        }

        private void CancelDownload()
        {
            cancelled = true;
            try
            {
                if (client != null && client.IsBusy)
                    client.CancelAsync();
            }
            catch { }
            Close();
        }
    }

    // ============================================================
    // FfmpegHelper
    // ============================================================
    public static class FfmpegHelper
    {
        public static string FindFfmpeg()
        {
            try
            {
                string local = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "ffmpeg.exe");
                if (File.Exists(local)) return local;

                string path = Environment.GetEnvironmentVariable("PATH");
                if (!string.IsNullOrEmpty(path))
                {
                    string[] dirs = path.Split(';');
                    foreach (string d in dirs)
                    {
                        if (string.IsNullOrEmpty(d)) continue;
                        try
                        {
                            string p = Path.Combine(d.Trim(), "ffmpeg.exe");
                            if (File.Exists(p)) return p;
                        }
                        catch { }
                    }
                }
            }
            catch { }
            return "";
        }

        public static bool HasFfmpeg()
        {
            return !string.IsNullOrEmpty(FindFfmpeg());
        }

        public static bool FramesToMp4(string framesPattern, int fps,
            string outputMp4)
        {
            string ff = FindFfmpeg();
            if (string.IsNullOrEmpty(ff)) return false;

            if (fps < 1) fps = 15;
            if (fps > 60) fps = 60;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = ff;
                psi.Arguments = "-y -framerate " + fps +
                    " -i \"" + framesPattern + "\"" +
                    " -c:v libx264 -pix_fmt yuv420p -crf 16 " +
                    "\"" + outputMp4 + "\"";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                using (Process p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    string err = p.StandardError.ReadToEnd();
                    p.WaitForExit(180000);
                    if (p.ExitCode != 0)
                    {
                        LogHelper.Write("[ffmpeg] " + err);
                        return false;
                    }
                }
                return File.Exists(outputMp4);
            }
            catch (Exception ex)
            {
                LogHelper.Write("[ffmpeg] " + ex.Message);
                return false;
            }
        }

        public static bool MuxAudio(string videoIn, string audioIn,
            string outputMp4)
        {
            string ff = FindFfmpeg();
            if (string.IsNullOrEmpty(ff)) return false;

            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = ff;
                psi.Arguments = "-y -i \"" + videoIn + "\" -i \"" + audioIn + "\"" +
                    " -c:v copy -c:a aac -b:a 192k -shortest " +
                    "\"" + outputMp4 + "\"";
                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.RedirectStandardOutput = true;
                psi.RedirectStandardError = true;

                using (Process p = Process.Start(psi))
                {
                    p.StandardOutput.ReadToEnd();
                    p.StandardError.ReadToEnd();
                    p.WaitForExit(180000);
                    return p.ExitCode == 0 && File.Exists(outputMp4);
                }
            }
            catch { return false; }
        }
    }

    // ============================================================
    // SimplePdf
    // ============================================================
    public static class SimplePdf
    {
        public static void Write(string path, string title,
            List<string> paragraphs, List<List<string>> tables)
        {
            List<string[]> lines = new List<string[]>();
            lines.Add(new string[] { "title", title });

            foreach (string p in paragraphs)
                foreach (string ln in p.Split('\n'))
                    foreach (string w in WrapText(ln, 90))
                        lines.Add(new string[] { "text", w });

            foreach (List<string> row in tables)
                lines.Add(new string[] { "text", string.Join("  |  ", row.ToArray()) });

            const int W = 595;
            const int H = 842;
            const int TOP = H - 60;
            const int BOTTOM = 60;
            const int LINE_H = 16;
            const int TITLE_H = 26;

            List<List<string[]>> pages = new List<List<string[]>>();
            List<string[]> cur = new List<string[]>();
            int y = TOP;

            foreach (string[] item in lines)
            {
                int step = (item[0] == "title") ? TITLE_H : LINE_H;
                if (y - step < BOTTOM && cur.Count > 0)
                {
                    pages.Add(cur);
                    cur = new List<string[]>();
                    y = TOP;
                }
                cur.Add(new string[] { item[0], item[1], y.ToString() });
                y -= step;
            }
            if (cur.Count > 0) pages.Add(cur);
            if (pages.Count == 0) pages.Add(new List<string[]>());

            List<int[]> objNums = new List<int[]>();
            List<byte[]> objDatas = new List<byte[]>();

            objNums.Add(new int[] { 1 });
            objDatas.Add(Encoding.ASCII.GetBytes("<< /Type /Catalog /Pages 2 0 R >>"));

            StringBuilder kids = new StringBuilder();
            for (int i = 0; i < pages.Count; i++)
                kids.Append((5 + 2 * i).ToString()).Append(" 0 R ");
            objNums.Add(new int[] { 2 });
            objDatas.Add(Encoding.ASCII.GetBytes(
                "<< /Type /Pages /Kids [" + kids.ToString().Trim() +
                "] /Count " + pages.Count + " >>"));

            objNums.Add(new int[] { 3 });
            objDatas.Add(Encoding.ASCII.GetBytes(
                "<< /Type /Font /Subtype /Type0 /BaseFont /STSong-Light " +
                "/Encoding /UniGB-UCS2-H /DescendantFonts [4 0 R] >>"));

            objNums.Add(new int[] { 4 });
            objDatas.Add(Encoding.ASCII.GetBytes(
                "<< /Type /Font /Subtype /CIDFontType0 /BaseFont /STSong-Light " +
                "/CIDSystemInfo << /Registry (Adobe) /Ordering (GB1) /Supplement 2 >> /DW 1000 >>"));

            for (int i = 0; i < pages.Count; i++)
            {
                int pageNum = 5 + 2 * i;
                int contentNum = 6 + 2 * i;

                string pageHdr = "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " +
                    W + " " + H + "] /Contents " + contentNum +
                    " 0 R /Resources << /Font << /F1 3 0 R >> >> >>";
                objNums.Add(new int[] { pageNum });
                objDatas.Add(Encoding.ASCII.GetBytes(pageHdr));

                MemoryStream stream = new MemoryStream();
                StreamWriter sw = new StreamWriter(stream, new UTF8Encoding(false));
                sw.Write("BT\n");
                foreach (string[] entry in pages[i])
                {
                    int size = (entry[0] == "title") ? 18 : 11;
                    sw.Write("/F1 " + size + " Tf\n");
                    sw.Write("1 0 0 1 50 " + entry[2] + " Tm\n");

                    byte[] bytes = Encoding.BigEndianUnicode.GetBytes(entry[1]);
                    string hex = BitConverter.ToString(bytes).Replace("-", "");
                    sw.Write("<" + hex + "> Tj\n");
                }
                sw.Write("ET\n");
                sw.Flush();
                byte[] streamBytes = stream.ToArray();

                List<byte> contentBytes = new List<byte>();
                contentBytes.AddRange(Encoding.ASCII.GetBytes(
                    "<< /Length " + streamBytes.Length + " >>\nstream\n"));
                contentBytes.AddRange(streamBytes);
                contentBytes.AddRange(Encoding.ASCII.GetBytes("\nendstream"));

                objNums.Add(new int[] { contentNum });
                objDatas.Add(contentBytes.ToArray());
            }

            List<int> order = new List<int>();
            for (int i = 0; i < objNums.Count; i++) order.Add(i);
            order.Sort(delegate(int a, int b)
            {
                return objNums[a][0].CompareTo(objNums[b][0]);
            });

            MemoryStream buf = new MemoryStream();
            Action<string> Wr = delegate(string s)
            {
                byte[] b = Encoding.ASCII.GetBytes(s);
                buf.Write(b, 0, b.Length);
            };

            Wr("%PDF-1.4\n%\xe2\xe3\xcf\xd3\n");

            Dictionary<int, long> offsets = new Dictionary<int, long>();
            foreach (int idx in order)
            {
                int num = objNums[idx][0];
                byte[] data = objDatas[idx];

                offsets[num] = buf.Position;
                Wr(num + " 0 obj\n");
                buf.Write(data, 0, data.Length);
                Wr("\nendobj\n");
            }

            long xrefPos = buf.Position;
            int maxNum = 0;
            foreach (int k in offsets.Keys)
                if (k > maxNum) maxNum = k;

            Wr("xref\n0 " + (maxNum + 1) + "\n");
            Wr("0000000000 65535 f \n");
            for (int i = 1; i <= maxNum; i++)
            {
                if (offsets.ContainsKey(i))
                    Wr(offsets[i].ToString("D10") + " 00000 n \n");
                else
                    Wr("0000000000 65535 f \n");
            }

            Wr("trailer\n<< /Size " + (maxNum + 1) + " /Root 1 0 R >>\n" +
               "startxref\n" + xrefPos + "\n%%EOF\n");

            File.WriteAllBytes(path, buf.ToArray());
        }

        private static List<string> WrapText(string text, int maxUnits)
        {
            List<string> result = new List<string>();
            StringBuilder line = new StringBuilder();
            int n = 0;
            foreach (char ch in text)
            {
                int w = (ch > 0x7f) ? 2 : 1;
                if (n + w > maxUnits && line.Length > 0)
                {
                    result.Add(line.ToString());
                    line = new StringBuilder();
                    line.Append(ch);
                    n = w;
                }
                else
                {
                    line.Append(ch);
                    n += w;
                }
            }
            if (line.Length > 0 || result.Count == 0)
                result.Add(line.ToString());
            return result;
        }
    }

    // ============================================================
    // PromptTemplate
    // ============================================================
    public static class PromptTemplate
    {
        public static string Build(string kind, string userText)
        {
            if (userText == null) userText = "";
            userText = userText.Trim();

            if (kind == "image")
            {
                return "你是顶级 SVG 矢量插画师。请用 SVG 代码画一幅高质量的图。\n\n" +
                    "用户需求：" + userText + "\n\n" +
                    "只输出：\n\n" +
                    "```photocode\n" +
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1024 1024\" width=\"1024\" height=\"1024\">\n" +
                    "  ... SVG 内容 ...\n" +
                    "</svg>\n" +
                    "<!--END-->\n" +
                    "```\n\n" +
                    "要求：完整合法含 xmlns 和 viewBox；用 gradient/filter/path 做层次；" +
                    "标记写 photocode；结尾另起一行 <!--END-->";
            }

            if (kind == "video")
            {
                return "你是顶级 SVG 动画师。请用多帧 SVG 制作动画。\n\n" +
                    "用户需求：" + userText + "\n\n" +
                    "只输出一个 videocode 代码块，帧用 ---FRAME--- 分隔：\n\n" +
                    "```videocode\n" +
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1024 576\" width=\"1024\" height=\"576\"></svg>\n" +
                    "---FRAME---\n" +
                    "<svg xmlns=\"http://www.w3.org/2000/svg\" viewBox=\"0 0 1024 576\" width=\"1024\" height=\"576\"></svg>\n" +
                    "<!--END-->\n" +
                    "```\n\n" +
                    "要求：帧数 20~40；viewBox 一致；禁止 script/外链；" +
                    "标记写 videocode；结尾 <!--END-->";
            }

            if (kind == "music")
            {
                return "你是音乐作曲家。用 JSON 描述音乐。\n\n" +
                    "用户需求：" + userText + "\n\n" +
                    "只输出 musiccode 代码块：\n\n" +
                    "```musiccode\n" +
                    "{\"title\":\"曲名\",\"bpm\":90,\"duration\":30,\"tracks\":[\n" +
                    "  {\"instrument\":\"piano\",\"volume\":0.7,\"notes\":[{\"pitch\":\"C4\",\"start\":0.0,\"dur\":0.5,\"vel\":0.8}]},\n" +
                    "  {\"instrument\":\"bass\",\"volume\":0.4,\"notes\":[{\"pitch\":\"C2\",\"start\":0.0,\"dur\":1.0,\"vel\":0.9}]}\n" +
                    "]}\n" +
                    "<!--END-->\n" +
                    "```\n\n" +
                    "要求：duration 20~60；至少 2 轨；标记写 musiccode；结尾 <!--END-->";
            }

            if (kind == "pdf")
            {
                return "你是文档撰写专家。用 JSON 描述 PDF。\n\n" +
                    "用户需求：" + userText + "\n\n" +
                    "只输出 pdfcode 代码块：\n\n" +
                    "```pdfcode\n" +
                    "{\"title\":\"标题\",\"paragraphs\":[\"第一段\",\"第二段\"],\"tables\":[[[\"列1\",\"列2\"],[\"a\",\"b\"]]]}\n" +
                    "<!--END-->\n" +
                    "```\n\n" +
                    "要求：JSON 合法；标记写 pdfcode；结尾 <!--END-->";
            }

            if (kind == "file")
            {
                return "请生成一个文件的完整内容。\n\n" +
                    "用户需求：" + userText + "\n\n" +
                    "只输出 filecode 代码块：\n\n" +
                    "```filecode\n" +
                    "{\"filename\":\"example.txt\",\"content\":\"文件内容\"}\n" +
                    "<!--END-->\n" +
                    "```\n\n" +
                    "要求：filename 不含路径分隔符；标记写 filecode；结尾 <!--END-->";
            }

            return "请生成一个完整的小项目。\n\n" +
                "用户需求：" + userText + "\n\n" +
                "只输出 projectcode 代码块：\n\n" +
                "```projectcode\n" +
                "{\"name\":\"myproject\",\"files\":[\n" +
                "  {\"path\":\"README.md\",\"content\":\"# 项目说明\"},\n" +
                "  {\"path\":\"src/main.py\",\"content\":\"print('hi')\"}\n" +
                "]}\n" +
                "<!--END-->\n" +
                "```\n\n" +
                "要求：path 用 / 分隔，不允许 ..；标记写 projectcode；结尾 <!--END-->";
        }
    }

    // ============================================================
    // InjectedScript
    // ============================================================
    public static class InjectedScript
    {
        private static string _cached = null;

        public static string Script
        {
            get
            {
                if (_cached != null) return _cached;

                try
                {
                    string path = Path.Combine(
                        AppDomain.CurrentDomain.BaseDirectory, "injected.js");
                    if (File.Exists(path))
                    {
                        _cached = File.ReadAllText(path, Encoding.UTF8);
                        LogHelper.Write("[脚本] injected.js 已加载（" +
                            _cached.Length + " 字符）");
                        return _cached;
                    }
                }
                catch (Exception ex)
                {
                    LogHelper.Write("[脚本] 读 injected.js 失败: " + ex.Message);
                }

                _cached = "(function(){console.log('[ai-gen] injected.js missing');})();";
                LogHelper.Write("[脚本] injected.js 缺失");
                return _cached;
            }
        }
    }

    // ============================================================
    // Generation
    // ============================================================
    public static class Generation
    {
        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer();

        public static async void RenderImage(WebView2 web, string svg)
        {
            try
            {
                int w = 1024, h = 1024;
                ParseSvgSize(svg, ref w, ref h);
                ProgressForm.UpdateDetail("正在渲染 SVG（" + w + "x" + h + "）...");
                string escaped = Serializer.Serialize(svg);
                string js = "(async function(){try{" +
                    "const d=await window.__aiGenRenderSvg(" + escaped + "," +
                    w + "," + h + ");" +
                    "window.__outbox.push({type:'image_rendered',dataUrl:d});" +
                    "}catch(e){window.__outbox.push({type:'image_error',error:e.message});}})();";
                await web.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("图片渲染失败", ex.Message);
            }
        }

        public static void SaveDataUrl(string dataUrl, string outPath)
        {
            int idx = dataUrl.IndexOf(',');
            if (idx < 0) throw new Exception("无效 dataUrl");
            string b64 = dataUrl.Substring(idx + 1);
            byte[] bytes = Convert.FromBase64String(b64);
            File.WriteAllBytes(outPath, bytes);
        }

        public static async void RenderVideo(WebView2 web, string code)
        {
            string[] frames = Regex.Split(
                code, "^\\s*---FRAME---\\s*$",
                RegexOptions.Multiline);

            List<string> list = new List<string>();
            foreach (string f in frames)
                if (!string.IsNullOrEmpty(f) && f.Trim().Length > 0)
                    list.Add(f.Trim());

            if (list.Count == 0)
            {
                ProgressForm.ShowError("视频生成失败", "没有帧");
                return;
            }

            int w = 512, h = 512;
            ParseSvgSize(list[0], ref w, ref h);
            if (w > 800 || h > 800)
            {
                double sc = 800.0 / Math.Max(w, h);
                w = (int)(w * sc);
                h = (int)(h * sc);
            }

            ProgressForm.UpdateDetail("正在渲染 " + list.Count + " 帧（" + w + "x" + h + "）...");
            string framesJson = Serializer.Serialize(list);
            string js = "(async function(){" +
                "const fr=" + framesJson + ";const w=" + w + ",h=" + h + ";" +
                "for(let i=0;i<fr.length;i++){try{" +
                "const d=await window.__aiGenRenderSvg(fr[i],w,h);" +
                "window.__outbox.push({type:'video_frame',dataUrl:d,idx:i,total:fr.length});" +
                "}catch(e){window.__outbox.push({type:'video_error',error:e.message});return;}}" +
                "window.__outbox.push({type:'video_complete',fps:" +
                Program.Cfg.VideoFps + "});})();";
            await web.CoreWebView2.ExecuteScriptAsync(js);
        }

        private static string S(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key)) return "";
            object v = d[key];
            return v == null ? "" : v.ToString();
        }

        private static double D(Dictionary<string, object> d, string key, double def)
        {
            if (d == null || !d.ContainsKey(key)) return def;
            try { return Convert.ToDouble(d[key]); }
            catch { return def; }
        }

        public static string RenderMusic(string json)
        {
            try
            {
                Dictionary<string, object> root =
                    Serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null)
                {
                    ProgressForm.ShowError("音乐生成失败", "musiccode 格式错误");
                    return "";
                }

                double duration = D(root, "duration", 20.0);
                if (duration < 1) duration = 1;
                if (duration > 300) duration = 300;

                ProgressForm.UpdateDetail("正在合成音乐（" + duration.ToString("0.0") + " 秒）...");

                int sr = 44100;
                int totalSamples = (int)(sr * duration);
                float[] buffer = new float[totalSamples];

                if (root.ContainsKey("tracks"))
                {
                    object[] tracks = root["tracks"] as object[];
                    if (tracks != null)
                    {
                        foreach (object tk in tracks)
                        {
                            Dictionary<string, object> track =
                                tk as Dictionary<string, object>;
                            if (track == null) continue;

                            float vol = (float)D(track, "volume", 0.5);
                            string wt = S(track, "instrument").ToLower();
                            if (string.IsNullOrEmpty(wt)) wt = "sine";

                            if (!track.ContainsKey("notes")) continue;
                            object[] notes = track["notes"] as object[];
                            if (notes == null) continue;

                            foreach (object nt in notes)
                            {
                                Dictionary<string, object> note =
                                    nt as Dictionary<string, object>;
                                if (note == null) continue;

                                float f = NoteToFreq(S(note, "pitch"));
                                if (f <= 0) continue;

                                double start = D(note, "start", 0);
                                double ndur  = D(note, "dur", 0.5);
                                float vel    = (float)D(note, "vel", 0.8);

                                int i0 = (int)(start * sr);
                                int n = (int)(ndur * sr);
                                if (i0 >= totalSamples) continue;
                                if (i0 + n > totalSamples) n = totalSamples - i0;

                                for (int i = 0; i < n; i++)
                                {
                                    double t = (double)i / sr;
                                    float sample = Osc(wt, f, t);

                                    float env;
                                    double at = 0.01, dt = 0.1, rt = 0.2, sus = 0.7;
                                    if (t < at) env = (float)(t / at);
                                    else if (t < at + dt) env = (float)(1 - (1 - sus) * (t - at) / dt);
                                    else if (t < ndur - rt) env = (float)sus;
                                    else env = (float)(sus * (ndur - t) / rt);

                                    buffer[i0 + i] += sample * env * vol * vel;
                                }
                            }
                        }
                    }
                }

                float peak = 0;
                for (int i = 0; i < buffer.Length; i++)
                    if (Math.Abs(buffer[i]) > peak) peak = Math.Abs(buffer[i]);
                if (peak > 1e-6f)
                    for (int i = 0; i < buffer.Length; i++)
                        buffer[i] = buffer[i] / peak * 0.9f;

                string outPath = Path.Combine(Program.Cfg.EnsureOutputsDir(),
                    Program.Cfg.NamePrefix + "_music_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".wav");

                using (FileStream fs = new FileStream(outPath, FileMode.Create))
                using (BinaryWriter bw = new BinaryWriter(fs))
                {
                    int dataLen = buffer.Length * 2;
                    bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                    bw.Write(36 + dataLen);
                    bw.Write(Encoding.ASCII.GetBytes("WAVE"));
                    bw.Write(Encoding.ASCII.GetBytes("fmt "));
                    bw.Write(16);
                    bw.Write((short)1);
                    bw.Write((short)1);
                    bw.Write(sr);
                    bw.Write(sr * 2);
                    bw.Write((short)2);
                    bw.Write((short)16);
                    bw.Write(Encoding.ASCII.GetBytes("data"));
                    bw.Write(dataLen);
                    foreach (float s in buffer)
                        bw.Write((short)(Math.Max(-1, Math.Min(1, s)) * 32767));
                }

                ProgressForm.ShowDone("音乐已生成",
                    "文件：" + Path.GetFileName(outPath));
                return outPath;
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("音乐生成失败", ex.Message);
                return "";
            }
        }

        private static float Osc(string wt, float f, double t)
        {
            double ph = 2 * Math.PI * f * t;
            if (wt == "square" || wt == "organ") return (float)Math.Sign(Math.Sin(ph));
            if (wt == "saw" || wt == "strings" || wt == "guitar")
                return (float)(2.0 * ((f * t) % 1.0) - 1.0);
            if (wt == "triangle" || wt == "flute")
                return (float)(2.0 * Math.Abs(2.0 * ((f * t) % 1.0) - 1.0) - 1.0);
            if (wt == "pluck" || wt == "bell")
                return (float)(Math.Sin(ph) * Math.Exp(-3.0 * t));
            return (float)Math.Sin(ph);
        }

        private static float NoteToFreq(string note)
        {
            if (string.IsNullOrEmpty(note)) return 0;
            Match m = Regex.Match(note.Trim(), "^([A-Ga-g])([#b]?)(-?\\d+)$");
            if (!m.Success) return 0;

            string letter = m.Groups[1].Value.ToUpper();
            string acc = m.Groups[2].Value;
            int oct = int.Parse(m.Groups[3].Value);

            int[] semi = { 0, 2, 4, 5, 7, 9, 11 };
            string[] names = { "C", "D", "E", "F", "G", "A", "B" };
            int idx = Array.IndexOf(names, letter);
            if (idx < 0) return 0;

            int midi = 12 * (oct + 1) + semi[idx];
            if (acc == "#") midi++;
            else if (acc == "b") midi--;

            return (float)(440.0 * Math.Pow(2, (midi - 69) / 12.0));
        }

        public static void RenderPdf(string json)
        {
            try
            {
                Dictionary<string, object> root =
                    Serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null)
                {
                    ProgressForm.ShowError("PDF 生成失败", "pdfcode 格式错误");
                    return;
                }

                ProgressForm.UpdateDetail("正在生成 PDF...");

                string title = S(root, "title");
                if (string.IsNullOrEmpty(title)) title = "Document";

                List<string> paragraphs = new List<string>();
                if (root.ContainsKey("paragraphs"))
                {
                    object[] ps = root["paragraphs"] as object[];
                    if (ps != null)
                        foreach (object p in ps)
                            if (p != null) paragraphs.Add(p.ToString());
                }

                List<List<string>> tables = new List<List<string>>();
                if (root.ContainsKey("tables"))
                {
                    object[] ts = root["tables"] as object[];
                    if (ts != null)
                        foreach (object tb in ts)
                        {
                            object[] rows = tb as object[];
                            if (rows == null) continue;
                            foreach (object row in rows)
                            {
                                object[] cells = row as object[];
                                if (cells == null) continue;
                                List<string> r = new List<string>();
                                foreach (object c in cells)
                                    r.Add(c == null ? "" : c.ToString());
                                tables.Add(r);
                            }
                        }
                }

                string outPath = Path.Combine(Program.Cfg.EnsureOutputsDir(),
                    Program.Cfg.NamePrefix + "_doc_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".pdf");

                SimplePdf.Write(outPath, title, paragraphs, tables);
                ProgressForm.ShowDone("PDF 已生成",
                    "文件：" + Path.GetFileName(outPath));
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("PDF 生成失败", ex.Message);
            }
        }

        public static void RenderFile(string json)
        {
            try
            {
                Dictionary<string, object> root =
                    Serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null)
                {
                    ProgressForm.ShowError("文件生成失败", "filecode 格式错误");
                    return;
                }

                string fn = S(root, "filename");
                if (string.IsNullOrEmpty(fn)) fn = "output.txt";
                string content = S(root, "content");

                char[] bad = new char[] { '\\', '/', ':', '*', '?', '"', '<', '>', '|' };
                if (fn.IndexOfAny(bad) >= 0)
                {
                    ProgressForm.ShowError("文件生成失败", "文件名含非法字符");
                    return;
                }

                ProgressForm.UpdateDetail("等待选择保存位置...");

                SaveFileDialog sfd = new SaveFileDialog();
                sfd.FileName = fn;
                sfd.InitialDirectory = Program.Cfg.EnsureOutputsDir();
                if (sfd.ShowDialog() != DialogResult.OK)
                {
                    ProgressForm.CloseIfOpen();
                    return;
                }

                File.WriteAllText(sfd.FileName, content, Encoding.UTF8);
                ProgressForm.ShowDone("文件已保存",
                    "文件：" + Path.GetFileName(sfd.FileName));
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("文件保存失败", ex.Message);
            }
        }

        public static void RenderProject(string json)
        {
            try
            {
                Dictionary<string, object> root =
                    Serializer.DeserializeObject(json) as Dictionary<string, object>;
                if (root == null)
                {
                    ProgressForm.ShowError("项目生成失败", "projectcode 格式错误");
                    return;
                }

                string name = S(root, "name");
                if (string.IsNullOrEmpty(name)) name = "project";

                ProgressForm.UpdateDetail("正在创建项目：" + name);

                string projDir = Path.Combine(Program.Cfg.EnsureOutputsDir(), name);
                Directory.CreateDirectory(projDir);

                int saved = 0;
                if (root.ContainsKey("files"))
                {
                    object[] files = root["files"] as object[];
                    if (files != null)
                    {
                        string projAbs = Path.GetFullPath(projDir);
                        foreach (object fo in files)
                        {
                            Dictionary<string, object> f =
                                fo as Dictionary<string, object>;
                            if (f == null) continue;

                            string rel = S(f, "path").Replace('\\', '/');
                            string content = S(f, "content");
                            if (string.IsNullOrEmpty(rel)) continue;
                            if (rel.StartsWith("/") || rel.Contains("..")) continue;

                            string target = Path.GetFullPath(Path.Combine(projDir, rel));
                            if (!target.StartsWith(projAbs)) continue;

                            string parent = Path.GetDirectoryName(target);
                            if (!string.IsNullOrEmpty(parent))
                                Directory.CreateDirectory(parent);

                            File.WriteAllText(target, content, Encoding.UTF8);
                            saved++;
                        }
                    }
                }

                ProgressForm.ShowDone("项目已生成",
                    "目录：" + name + "（" + saved + " 个文件）");
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("项目生成失败", ex.Message);
            }
        }

        private static void ParseSvgSize(string svg, ref int w, ref int h)
        {
            Match m = Regex.Match(
                svg, "viewBox\\s*=\\s*[\"']([\\d\\.\\-\\s]+)[\"']",
                RegexOptions.IgnoreCase);

            if (m.Success)
            {
                string[] parts = m.Groups[1].Value.Split(
                    new char[] { ' ', '\t' },
                    StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length == 4)
                {
                    double pw, ph;
                    if (double.TryParse(parts[2], out pw) &&
                        double.TryParse(parts[3], out ph))
                    {
                        int iw = (int)Math.Abs(pw);
                        int ih = (int)Math.Abs(ph);
                        if (iw >= 16 && ih >= 16) { w = iw; h = ih; }
                    }
                }
            }
        }
    }

    // ============================================================
    // MainForm
    // ============================================================
    public class MainForm : Form
    {
        private WebView2 chatView;
        private WebView2 agentView;

        private Panel toolbar;
        private Button btnChat;
        private Button btnTask;
        private Button btnWindow;
        private Button btnTools;

        private string currentMode = "chat";
        private bool miniMode = false;
        private Rectangle bigBounds;

        private System.Windows.Forms.Timer pollTimer;
        private System.Windows.Forms.Timer dshTimer;
        private bool chatReady = false;
        private int dshTickCount = 0;

        private List<string> pendingVideoFrames = new List<string>();
        private int pendingVideoTotal = 0;
        private string _lastMusicPath = "";

        private static readonly JavaScriptSerializer Serializer =
            new JavaScriptSerializer();

        private bool dragging = false;
        private Point dragStart;
        private Point toolbarPos = new Point(-1, -1);

        private SplashForm splash;

        public MainForm()
        {
            Text = "dsh-float " + Program.APP_VERSION + " " + Program.APP_BUILD;
            Width = 1000;
            Height = 750;
            StartPosition = FormStartPosition.CenterScreen;
            BackColor = Color.FromArgb(30, 30, 30);

            try
            {
                string icoPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "favicon.ico");
                if (File.Exists(icoPath)) Icon = new Icon(icoPath);
            }
            catch { }

            if (Program.Cfg.RememberWindow &&
                !string.IsNullOrEmpty(Program.Cfg.WindowGeom))
            {
                try
                {
                    string[] parts = Program.Cfg.WindowGeom.Split(',');
                    if (parts.Length == 4)
                    {
                        StartPosition = FormStartPosition.Manual;
                        Location = new Point(int.Parse(parts[0]), int.Parse(parts[1]));
                        Size = new Size(int.Parse(parts[2]), int.Parse(parts[3]));
                    }
                }
                catch { }
            }

            try { Opacity = Program.Cfg.WindowAlpha / 100.0; } catch { }

            Panel webHost = new Panel();
            webHost.Dock = DockStyle.Fill;
            webHost.BackColor = Color.Black;
            Controls.Add(webHost);

            chatView = new WebView2();
            chatView.Dock = DockStyle.Fill;
            webHost.Controls.Add(chatView);

            agentView = new WebView2();
            agentView.Dock = DockStyle.Fill;
            webHost.Controls.Add(agentView);

            toolbar = new Panel();
            toolbar.Width = 208;
            toolbar.Height = 44;
            toolbar.BackColor = Color.FromArgb(250, 250, 250);
            toolbar.BorderStyle = BorderStyle.FixedSingle;
            Controls.Add(toolbar);
            toolbar.BringToFront();

            toolbar.MouseDown += OnToolbarMouseDown;
            toolbar.MouseMove += OnToolbarMouseMove;
            toolbar.MouseUp += OnToolbarMouseUp;

            Label handle = new Label();
            handle.Text = "||";
            handle.Left = 2; handle.Top = 4;
            handle.Width = 22; handle.Height = 34;
            handle.TextAlign = ContentAlignment.MiddleCenter;
            handle.ForeColor = Color.FromArgb(80, 80, 80);
            handle.Font = new Font("Microsoft YaHei", 10);
            handle.Cursor = Cursors.SizeAll;
            handle.BackColor = Color.Transparent;
            handle.MouseDown += OnToolbarMouseDown;
            handle.MouseMove += OnToolbarMouseMove;
            handle.MouseUp += OnToolbarMouseUp;
            toolbar.Controls.Add(handle);

            btnChat = MakeToolbarButton("\U0001F41B", "聊天", 26);
            btnChat.Click += delegate { SwitchMode("chat"); };

            btnTask = MakeToolbarButton("\U0001F4BB", "任务", 70);
            btnTask.Click += delegate { SwitchMode("agent"); };

            btnWindow = MakeToolbarButton("\U0001FA9F", "小窗", 114);
            btnWindow.Click += delegate { ToggleMiniWindow(); };

            btnTools = MakeToolbarButton("\U0001F6E0", "工具", 158);
            btnTools.Click += delegate { ShowToolsMenu(btnTools); };

            if (Program.Cfg.ToolbarX >= 0 && Program.Cfg.ToolbarY >= 0)
                toolbarPos = new Point(Program.Cfg.ToolbarX, Program.Cfg.ToolbarY);

            LayoutToolbar();

            Load += OnFormLoad;
            Resize += delegate { LayoutToolbar(); };
            FormClosing += OnFormClosing;

            pollTimer = new System.Windows.Forms.Timer();
            pollTimer.Interval = 150;
            pollTimer.Tick += OnPollTick;
            pollTimer.Start();

            dshTimer = new System.Windows.Forms.Timer();
            dshTimer.Interval = 1000;
            dshTimer.Tick += OnDshTick;

            ApplyTopmost();

            if (Program.Cfg.SplashEnable) ShowSplash();

            // ★ 启动时：重启后台更新进程 + 主程序自己也检查一次
            if (Program.Cfg.AutoCheckUpdate)
            {
                Thread th = new Thread(delegate()
                {
                    try
                    {
                        // 1. 结束旧后台更新检查进程，并拉起新的
                        Updater.StopAndRestartDaemon();

                        // 2. 主程序自己也检查一次（延迟一点，让主窗口先显示出来）
                        Thread.Sleep(2500);
                        UpdateCheckerDaemon.CheckOnce(Program.Cfg);
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write("[启动检查] " + ex.Message);
                    }
                });
                th.IsBackground = true;
                th.Start();
            }
        }

        private void ShowSplash()
        {
            splash = new SplashForm();
            splash.Show();
            System.Windows.Forms.Timer t = new System.Windows.Forms.Timer();
            t.Interval = 1500;
            t.Tick += delegate { t.Stop(); splash.Close(); splash = null; };
            t.Start();
        }

        private void OnFormClosing(object sender, FormClosingEventArgs e)
        {
            if (Program.Cfg.RememberWindow)
                Program.Cfg.WindowGeom = Location.X + "," + Location.Y +
                    "," + Width + "," + Height;

            if (toolbar != null)
            {
                Program.Cfg.ToolbarX = toolbar.Left;
                Program.Cfg.ToolbarY = toolbar.Top;
            }
            Program.Cfg.Save();
        }

        private void LayoutToolbar()
        {
            if (toolbar == null) return;
            if (toolbarPos.X >= 0 && toolbarPos.Y >= 0)
            {
                int x = Math.Max(0, Math.Min(ClientSize.Width - toolbar.Width, toolbarPos.X));
                int y = Math.Max(0, Math.Min(ClientSize.Height - toolbar.Height, toolbarPos.Y));
                toolbar.Location = new Point(x, y);
            }
            else
            {
                int x = ClientSize.Width - toolbar.Width - 16;
                int y = ClientSize.Height - toolbar.Height - 16;
                toolbar.Location = new Point(Math.Max(0, x), Math.Max(0, y));
            }
        }

        private void OnToolbarMouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Left)
            {
                dragging = true;
                dragStart = new Point(e.X, e.Y);
                Control c = sender as Control;
                if (c != null && c.Parent == toolbar)
                    dragStart = new Point(e.X + c.Left, e.Y + c.Top);
            }
        }

        private void OnToolbarMouseMove(object sender, MouseEventArgs e)
        {
            if (dragging && toolbar != null)
            {
                int nx = toolbar.Left + (e.X - dragStart.X);
                int ny = toolbar.Top + (e.Y - dragStart.Y);
                nx = Math.Max(0, Math.Min(ClientSize.Width - toolbar.Width, nx));
                ny = Math.Max(0, Math.Min(ClientSize.Height - toolbar.Height, ny));
                toolbar.Location = new Point(nx, ny);
                toolbarPos = new Point(nx, ny);
            }
        }

        private void OnToolbarMouseUp(object sender, MouseEventArgs e)
        {
            dragging = false;
        }

        private Button MakeToolbarButton(string text, string tooltip, int left)
        {
            Button b = new Button();
            b.Text = text;
            b.Width = 40;
            b.Height = 36;
            b.Left = left;
            b.Top = 4;
            b.FlatStyle = FlatStyle.Flat;
            b.BackColor = Color.FromArgb(250, 250, 250);
            b.ForeColor = Color.FromArgb(48, 48, 48);
            b.Font = new Font("Segoe UI Emoji", 13);
            b.Cursor = Cursors.Hand;
            b.TabStop = false;
            b.FlatAppearance.BorderSize = 0;
            ToolTip tt = new ToolTip();
            tt.SetToolTip(b, tooltip);
            toolbar.Controls.Add(b);
            return b;
        }

        private void ApplyTopmost()
        {
            try { TopMost = Program.Cfg.WindowTopmost; } catch { }
        }

        private void ApplyToolbarMode()
        {
            try
            {
                bool inject = Program.Cfg.ToolbarMode;
                LogHelper.Write("[工具栏] 应用模式 inject=" + inject);

                if (toolbar != null)
                    toolbar.Visible = !inject;

                string js = "window.__aiGenSetToolbar&&window.__aiGenSetToolbar(" +
                            (inject ? "true" : "false") + ")";

                if (chatView != null && chatView.CoreWebView2 != null)
                    chatView.CoreWebView2.ExecuteScriptAsync(js);
                if (agentView != null && agentView.CoreWebView2 != null)
                    agentView.CoreWebView2.ExecuteScriptAsync(js);
            }
            catch (Exception ex)
            {
                LogHelper.Write("[工具栏] 应用模式错误: " + ex.Message);
            }
        }

        private async void OnFormLoad(object sender, EventArgs e)
        {
            LogHelper.Write("[启动] 程序启动 v" + Program.APP_VERSION);
            LogHelper.Write("[启动] 日志: " + LogHelper.LogPath);

            await InitChatView();
            await InitAgentView();

            SwitchMode("chat");
            ApplyToolbarMode();
        }

        private async System.Threading.Tasks.Task InitChatView()
        {
            try
            {
                string cacheDir = Path.Combine(Path.GetTempPath(),
                    "deepseek-chromium_cache");
                Directory.CreateDirectory(cacheDir);
                LogHelper.Write("[WebView] chat cacheDir=" + cacheDir);

                CoreWebView2Environment env =
                    await CoreWebView2Environment.CreateAsync(null, cacheDir);
                await chatView.EnsureCoreWebView2Async(env);

                CoreWebView2Settings s = chatView.CoreWebView2.Settings;
                s.AreDevToolsEnabled = Program.Cfg.Devtools;
                s.IsWebMessageEnabled = true;
                s.AreDefaultContextMenusEnabled = true;

                chatView.CoreWebView2.WebMessageReceived += OnWebMessage;

                chatView.CoreWebView2.NavigationCompleted += async delegate
                {
                    try
                    {
                        LogHelper.Write("[WebView] chat 导航完成，注入脚本");
                        await chatView.CoreWebView2
                            .AddScriptToExecuteOnDocumentCreatedAsync(InjectedScript.Script);
                        await chatView.CoreWebView2.ExecuteScriptAsync(InjectedScript.Script);
                        string ac = Program.Cfg.AutoClickContinue ? "true" : "false";
                        await chatView.CoreWebView2.ExecuteScriptAsync(
                            "window.__aiGenAutoContinue=" + ac + ";");
                        chatReady = true;
                        ApplyToolbarMode();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write("[WebView] chat 注入错误: " + ex.Message);
                    }
                };

                chatView.CoreWebView2.Navigate("https://chat.deepseek.com");
            }
            catch (Exception ex)
            {
                LogHelper.Write("[WebView] chat 初始化失败: " + ex.Message);
                MessageBox.Show(
                    "聊天页面初始化失败：\n" + ex.Message +
                    "\n\n请安装 WebView2 Runtime：\n" +
                    "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async System.Threading.Tasks.Task InitAgentView()
        {
            try
            {
                string cacheDir = Path.Combine(Path.GetTempPath(),
                    "deepseek-chromium_cache");
                Directory.CreateDirectory(cacheDir);

                CoreWebView2Environment env =
                    await CoreWebView2Environment.CreateAsync(null, cacheDir);
                await agentView.EnsureCoreWebView2Async(env);

                CoreWebView2Settings s = agentView.CoreWebView2.Settings;
                s.AreDevToolsEnabled = Program.Cfg.Devtools;
                s.IsWebMessageEnabled = true;
                s.AreDefaultContextMenusEnabled = true;

                agentView.CoreWebView2.WebMessageReceived += OnWebMessage;

                agentView.CoreWebView2.NavigationCompleted += async delegate
                {
                    try
                    {
                        LogHelper.Write("[WebView] agent 导航完成，注入脚本");
                        await agentView.CoreWebView2
                            .AddScriptToExecuteOnDocumentCreatedAsync(InjectedScript.Script);
                        await agentView.CoreWebView2.ExecuteScriptAsync(InjectedScript.Script);
                        string ac = Program.Cfg.AutoClickContinue ? "true" : "false";
                        await agentView.CoreWebView2.ExecuteScriptAsync(
                            "window.__aiGenAutoContinue=" + ac + ";");
                        ApplyToolbarMode();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write("[WebView] agent 注入错误: " + ex.Message);
                    }
                };

                string url = DshLauncher.GetDshUrl();
                LogHelper.Write("[DSH] GetDshUrl 返回: " + url);

                if (url == "about:dsh-starting")
                {
                    agentView.CoreWebView2.NavigateToString(DshLauncher.WaitHtml());
                    dshTickCount = 0;
                    dshTimer.Start();
                }
                else if (url == "about:dsh-port-busy")
                {
                    agentView.CoreWebView2.NavigateToString(DshLauncher.PortBusyHtml());
                }
                else
                {
                    agentView.CoreWebView2.Navigate(url);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write("[WebView] agent 初始化失败: " + ex.Message);
            }
        }

        private void OnDshTick(object sender, EventArgs e)
        {
            try
            {
                if (DshLauncher.IsPortOpen() &&
                    agentView != null && agentView.CoreWebView2 != null)
                {
                    string url = "http://127.0.0.1:3080/";
                    LogHelper.Write("[DSH] 端口就绪，跳转: " + url);
                    try
                    {
                        agentView.CoreWebView2.Navigate(url);
                        agentView.CoreWebView2.ExecuteScriptAsync(InjectedScript.Script);
                        dshTimer.Stop();
                    }
                    catch (Exception ex)
                    {
                        LogHelper.Write("[DSH] 导航错误: " + ex.Message);
                    }
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write("[DSH] Tick 异常: " + ex.Message);
            }
        }

        private void SwitchMode(string mode)
        {
            currentMode = mode;
            LogHelper.Write("[模式] 切换到 " + mode);

            if (mode == "chat")
            {
                if (chatView != null) chatView.BringToFront();
                SetActiveButton(btnChat);
            }
            else if (mode == "agent")
            {
                if (agentView != null) agentView.BringToFront();
                SetActiveButton(btnTask);
            }

            if (toolbar != null) toolbar.BringToFront();

            try
            {
                WebView2 v = (mode == "agent") ? agentView : chatView;
                if (v != null && v.CoreWebView2 != null)
                {
                    v.CoreWebView2.ExecuteScriptAsync(InjectedScript.Script);
                    v.CoreWebView2.ExecuteScriptAsync(
                        "window.__aiGenToolbarWanted=" +
                        (Program.Cfg.ToolbarMode ? "true" : "false") + ";");
                }
            }
            catch { }
            ApplyToolbarMode();
        }

        private void SetActiveButton(Button active)
        {
            Color normal = Color.FromArgb(250, 250, 250);
            Color hi = Color.FromArgb(228, 237, 253);
            if (btnChat != null) btnChat.BackColor = (btnChat == active) ? hi : normal;
            if (btnTask != null) btnTask.BackColor = (btnTask == active) ? hi : normal;
            if (btnWindow != null) btnWindow.BackColor = normal;
            if (btnTools != null) btnTools.BackColor = normal;
        }

        private async void OnPollTick(object sender, EventArgs e)
        {
            await PollOne(chatView, "chat");
            await PollOne(agentView, "agent");
        }

        private async System.Threading.Tasks.Task PollOne(WebView2 view, string tag)
        {
            if (view == null || view.CoreWebView2 == null) return;
            try
            {
                string r = await view.CoreWebView2.ExecuteScriptAsync(
                    "(function(){var q=window.__outbox||[];window.__outbox=[];return JSON.stringify(q);})()");
                if (string.IsNullOrEmpty(r) || r == "null") return;

                string inner = Serializer.Deserialize<string>(r);
                if (string.IsNullOrEmpty(inner) || inner == "[]") return;

                LogHelper.Write("[" + tag + "] 收到 outbox: " + inner);

                object parsed = Serializer.DeserializeObject(inner);
                object[] arr = parsed as object[];
                if (arr == null) return;

                foreach (object item in arr)
                {
                    Dictionary<string, object> dict = item as Dictionary<string, object>;
                    if (dict != null) HandleMessage(dict);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write("[" + tag + "] Poll 错误: " + ex.Message);
            }
        }

        private string GetStr(Dictionary<string, object> d, string key)
        {
            if (d == null || !d.ContainsKey(key)) return "";
            object v = d[key];
            return v == null ? "" : v.ToString();
        }

        private int GetInt(Dictionary<string, object> d, string key, int def)
        {
            if (d == null || !d.ContainsKey(key)) return def;
            try { return Convert.ToInt32(d[key]); } catch { return def; }
        }

        private void HandleMessage(Dictionary<string, object> msg)
        {
            string t = GetStr(msg, "type");
            LogHelper.Write("[MSG] 收到 type=" + t);

            if (t == "log")
            {
                LogHelper.Write("[PAGE] " + GetStr(msg, "msg"));
            }
            else if (t == "sent")
            {
                ProgressForm.UpdateDetail("已发送，等待 AI 回复...");
                Toast("消息已发送");
            }
            else if (t == "scan")
            {
                HandleCode(GetStr(msg, "lang"), GetStr(msg, "code"));
            }
            else if (t == "image_rendered")
            {
                string outPath = Path.Combine(Program.Cfg.EnsureOutputsDir(),
                    Program.Cfg.NamePrefix + "_image_" +
                    DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");
                Generation.SaveDataUrl(GetStr(msg, "dataUrl"), outPath);
                ProgressForm.ShowDone("图片已生成",
                    "文件：" + Path.GetFileName(outPath));
            }
            else if (t == "image_error")
            {
                ProgressForm.ShowError("图片生成失败", GetStr(msg, "error"));
            }
            else if (t == "video_frame")
            {
                pendingVideoFrames.Add(GetStr(msg, "dataUrl"));
                int total = GetInt(msg, "total", 0);
                if (total > 0) pendingVideoTotal = total;
                int n = pendingVideoFrames.Count;
                if (pendingVideoTotal > 0)
                    ProgressForm.UpdateDetail("已收到 " + n + " / " +
                        pendingVideoTotal + " 帧");
                else
                    ProgressForm.UpdateDetail("已收到 " + n + " 帧");
            }
            else if (t == "video_complete")
            {
                OnVideoComplete(GetInt(msg, "fps", 15));
            }
            else if (t == "video_error")
            {
                ProgressForm.ShowError("视频渲染失败", GetStr(msg, "error"));
                pendingVideoFrames.Clear();
            }
            else if (t == "toolbar_gen")
            {
                string kind = GetStr(msg, "kind");
                string text = GetStr(msg, "text");
                HandleGenerate(kind, text);
            }
            else if (t == "toolbar_switch")
            {
                string to = GetStr(msg, "to");
                if (to == "agent") SwitchMode("agent");
                else SwitchMode("chat");
            }
            else if (t == "toolbar_settings")
            {
                try
                {
                    LogHelper.Write("[MSG] 打开设置");
                    SettingsForm sf = new SettingsForm();
                    sf.ShowDialog(this);
                    ApplyTopmost();
                    ApplyToolbarMode();
                }
                catch (Exception ex)
                {
                    LogHelper.Write("[MSG] 设置异常: " + ex.Message);
                }
            }
            else if (t == "toolbar_feedback")
            {
                LogHelper.Write("[MSG] 打开反馈窗口");
                OpenFeedbackDialog();
            }
            else if (t == "toolbar_files")
            {
                try
                {
                    LogHelper.Write("[MSG] 打开目录");
                    Process.Start("explorer.exe", Program.Cfg.EnsureOutputsDir());
                }
                catch (Exception ex)
                {
                    LogHelper.Write("[MSG] 目录异常: " + ex.Message);
                }
            }
            // ★ 新增：注入工具栏的"小窗"按钮
            else if (t == "toolbar_mini")
            {
                LogHelper.Write("[MSG] 切换小窗");
                ToggleMiniWindow();
            }
        }

        private FeedbackForm _feedbackDlg = null;

        private void OpenFeedbackDialog()
        {
            try
            {
                if (_feedbackDlg != null && !_feedbackDlg.IsDisposed)
                {
                    _feedbackDlg.Activate();
                    return;
                }
                _feedbackDlg = new FeedbackForm();
                _feedbackDlg.ShowDialog(this);
                _feedbackDlg = null;
            }
            catch (Exception ex)
            {
                LogHelper.Write("[反馈] 打开异常: " + ex.Message);
                MessageBox.Show(this, "打开反馈窗口失败：\n" + ex.Message,
                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void OnVideoComplete(int fps)
        {
            try
            {
                if (pendingVideoFrames.Count == 0)
                {
                    ProgressForm.ShowError("视频生成失败", "没有帧");
                    return;
                }

                string dir = Program.Cfg.EnsureOutputsDir();
                string baseName = Program.Cfg.NamePrefix + "_video_" +
                                  DateTime.Now.ToString("yyyyMMdd_HHmmss");

                ProgressForm.UpdateDetail("正在保存 " + pendingVideoFrames.Count + " 帧...");

                for (int i = 0; i < pendingVideoFrames.Count; i++)
                {
                    string framePath = Path.Combine(dir,
                        baseName + "_frame" + i.ToString("D4") + ".png");
                    Generation.SaveDataUrl(pendingVideoFrames[i], framePath);
                }

                string ff = FfmpegHelper.FindFfmpeg();
                if (string.IsNullOrEmpty(ff))
                {
                    ProgressForm.ShowError("无法合成 mp4",
                        "已保存 " + pendingVideoFrames.Count +
                        " 帧，但未找到 ffmpeg");
                    return;
                }

                ProgressForm.UpdateDetail("正在用 ffmpeg 合成 mp4...");

                string pattern = Path.Combine(dir, baseName + "_frame%04d.png");
                string outMp4 = Path.Combine(dir, baseName + ".mp4");

                if (!FfmpegHelper.FramesToMp4(pattern, fps, outMp4))
                {
                    ProgressForm.ShowError("mp4 合成失败", "帧已保存到 outputs/");
                    return;
                }

                if (Program.Cfg.AutoAttachMusic &&
                    !string.IsNullOrEmpty(_lastMusicPath) &&
                    File.Exists(_lastMusicPath))
                {
                    ProgressForm.UpdateDetail("正在附加背景音乐...");
                    string withMusic = Path.Combine(dir, baseName + "_with_music.mp4");
                    if (FfmpegHelper.MuxAudio(outMp4, _lastMusicPath, withMusic))
                    {
                        ProgressForm.ShowDone("有声视频已生成",
                            "文件：" + Path.GetFileName(withMusic));
                    }
                    else
                    {
                        ProgressForm.ShowDone("视频已生成",
                            "文件：" + Path.GetFileName(outMp4));
                    }
                }
                else
                {
                    ProgressForm.ShowDone("视频已生成",
                        "文件：" + Path.GetFileName(outMp4));
                }

                try
                {
                    for (int i = 0; i < pendingVideoFrames.Count; i++)
                    {
                        string fp = Path.Combine(dir,
                            baseName + "_frame" + i.ToString("D4") + ".png");
                        if (File.Exists(fp)) File.Delete(fp);
                    }
                }
                catch { }
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("视频保存失败", ex.Message);
            }
            finally
            {
                pendingVideoFrames.Clear();
                pendingVideoTotal = 0;
            }
        }

        private void HandleGenerate(string kind, string injectText)
        {
            LogHelper.Write("[生成] kind=" + kind +
                " injectText=" + (string.IsNullOrEmpty(injectText) ? "(空)" : "(有)"));

            if (kind == "video" && !FfmpegHelper.HasFfmpeg())
            {
                if (!FfmpegDownloader.PromptAndDownload(this))
                {
                    Toast("已取消：未安装 ffmpeg");
                    return;
                }
            }

            string label = kind;
            if (kind == "image") label = "图片";
            else if (kind == "video") label = "视频";
            else if (kind == "music") label = "音乐";
            else if (kind == "pdf") label = "PDF";
            else if (kind == "file") label = "文件";
            else if (kind == "project") label = "项目";

            string userText;

            if (injectText != null && injectText.Length > 0)
            {
                userText = injectText;
                LogHelper.Write("[生成] 注入模式，直接发送：" + userText);
                ProgressForm.ShowProgress("正在生成" + label,
                    "已从网页输入框读取需求：" + userText);
            }
            else
            {
                TextInputDialog dlg = new TextInputDialog(
                    "生成" + label, "描述你想要的" + label + "（留空则让模型自由发挥）：");
                DialogResult res = dlg.ShowDialog(this);
                userText = (res == DialogResult.OK) ? (dlg.InputText ?? "") : null;
                dlg.Dispose();

                if (res != DialogResult.OK) return;

                ProgressForm.ShowProgress("正在生成" + label, "正在发送请求...");
            }

            string prompt = PromptTemplate.Build(kind, userText);
            SendPrompt(prompt, kind);
        }

        private void HandleGenerateFromMenu(string kind)
        {
            HandleGenerate(kind, null);
        }

        private async void SendPrompt(string prompt, string kind)
        {
            if (!chatReady || chatView == null || chatView.CoreWebView2 == null)
            {
                ProgressForm.ShowError("发送失败", "页面未就绪");
                return;
            }
            try
            {
                string escaped = Serializer.Serialize(prompt);
                await chatView.CoreWebView2.ExecuteScriptAsync(
                    "(async function(){try{" +
                    "window.__aiGenMarkRequestStart&&window.__aiGenMarkRequestStart();" +
                    "await window.__aiGenSend(" + escaped + ");" +
                    "}catch(e){window.__outbox.push({type:'log',msg:'发送失败：'+e.message});}})();");
                ProgressForm.UpdateDetail("请求已发送，等待 AI 生成...");
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("发送失败", ex.Message);
            }
        }

        private void HandleCode(string lang, string code)
        {
            try
            {
                LogHelper.Write("[处理] lang=" + lang + " code.len=" + code.Length);
                if (lang == "photocode") Generation.RenderImage(chatView, code);
                else if (lang == "videocode") Generation.RenderVideo(chatView, code);
                else if (lang == "musiccode")
                {
                    string mp = Generation.RenderMusic(code);
                    if (!string.IsNullOrEmpty(mp)) _lastMusicPath = mp;
                }
                else if (lang == "pdfcode") Generation.RenderPdf(code);
                else if (lang == "filecode") Generation.RenderFile(code);
                else if (lang == "projectcode") Generation.RenderProject(code);
            }
            catch (Exception ex)
            {
                ProgressForm.ShowError("处理失败", ex.Message);
            }
        }

        private void ToggleMiniWindow()
        {
            if (!miniMode)
            {
                bigBounds = Bounds;
                int w = Program.Cfg.MiniW;
                int h = Program.Cfg.MiniH;
                if (w < 300) w = 500;
                if (h < 300) h = 700;
                Width = w;
                Height = h;
                Left = Screen.PrimaryScreen.WorkingArea.Right - w - 20;
                Top = Screen.PrimaryScreen.WorkingArea.Height / 2 - h / 2;
                TopMost = true;
                miniMode = true;
                if (btnWindow != null)
                    btnWindow.BackColor = Color.FromArgb(228, 237, 253);
            }
            else
            {
                Bounds = bigBounds;
                TopMost = Program.Cfg.WindowTopmost;
                miniMode = false;
                if (btnWindow != null)
                    btnWindow.BackColor = Color.FromArgb(250, 250, 250);
            }
            LayoutToolbar();
        }

        private void ShowToolsMenu(Control anchor)
        {
            ContextMenuStrip m = new ContextMenuStrip();
            m.Items.Add("🎨 生成图片", null, delegate { HandleGenerateFromMenu("image"); });
            m.Items.Add("🎬 生成视频", null, delegate { HandleGenerateFromMenu("video"); });
            m.Items.Add("🎵 生成音乐", null, delegate { HandleGenerateFromMenu("music"); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("📄 生成 PDF", null, delegate { HandleGenerateFromMenu("pdf"); });
            m.Items.Add("📁 生成文件", null, delegate { HandleGenerateFromMenu("file"); });
            m.Items.Add("📦 生成项目", null, delegate { HandleGenerateFromMenu("project"); });
            m.Items.Add(new ToolStripSeparator());
            m.Items.Add("📂 文件管理", null, delegate
            {
                try { Process.Start("explorer.exe", Program.Cfg.EnsureOutputsDir()); }
                catch { }
            });
            m.Items.Add("📋 打开日志", null, delegate
            {
                try { Process.Start("notepad.exe", LogHelper.LogPath); }
                catch { }
            });
            m.Items.Add("🐛 问题反馈", null, delegate { OpenFeedbackDialog(); });
            m.Items.Add("⚙️ 设置", null, delegate
            {
                SettingsForm sf = new SettingsForm();
                sf.ShowDialog(this);
                ApplyTopmost();
                ApplyToolbarMode();
            });
            m.Show(anchor, new Point(0, -m.Items.Count * 28));
        }

        private void OnWebMessage(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            try
            {
                object parsed = Serializer.DeserializeObject(e.WebMessageAsJson);
                Dictionary<string, object> dict = parsed as Dictionary<string, object>;
                if (dict != null) HandleMessage(dict);
            }
            catch (Exception ex)
            {
                LogHelper.Write("[WebMessage] 错误: " + ex.Message);
            }
        }

        private void Toast(string msg)
        {
            if (!Program.Cfg.AutoSaveToast) return;

            Form t = new Form();
            t.FormBorderStyle = FormBorderStyle.None;
            t.StartPosition = FormStartPosition.Manual;
            t.Size = new Size(320, 60);
            t.BackColor = Color.FromArgb(48, 48, 48);
            t.ShowInTaskbar = false;
            t.TopMost = true;

            int dur = Program.Cfg.ToastDuration;
            if (dur < 1000) dur = 3000;

            int w = t.Width;
            int h = t.Height;
            Rectangle wa = Screen.PrimaryScreen.WorkingArea;
            string corner = Program.Cfg.ToastCorner;
            int tx, ty;

            if (corner == "bl") { tx = wa.Left + 24; ty = wa.Bottom - h - 60; }
            else if (corner == "tr") { tx = wa.Right - w - 24; ty = wa.Top + 24; }
            else if (corner == "tl") { tx = wa.Left + 24; ty = wa.Top + 24; }
            else { tx = wa.Right - w - 24; ty = wa.Bottom - h - 60; }

            t.Location = new Point(tx, ty);

            Label lbl = new Label();
            lbl.Text = msg;
            lbl.ForeColor = Color.White;
            lbl.Dock = DockStyle.Fill;
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Font = new Font("Microsoft YaHei", 10);
            t.Controls.Add(lbl);
            t.Show();

            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = dur;
            timer.Tick += delegate { timer.Stop(); t.Close(); };
            timer.Start();
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            if (Program.Cfg.HotkeyEnable)
            {
                try { HotkeyManager.Register(Handle); } catch { }
            }
        }

        protected override void OnHandleDestroyed(EventArgs e)
        {
            try { HotkeyManager.Unregister(Handle); } catch { }
            base.OnHandleDestroyed(e);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == 0x0312)
            {
                if (WindowState == FormWindowState.Minimized || !Visible)
                {
                    Show();
                    WindowState = FormWindowState.Normal;
                    Activate();
                }
                else
                {
                    Hide();
                }
            }
            base.WndProc(ref m);
        }
    }

    // ============================================================
    // SplashForm —— 按图片原始比例设置窗口尺寸，并强制居中到主屏工作区中心
    // ============================================================
    public class SplashForm : Form
    {
        private const int DEFAULT_W = 400;
        private const int DEFAULT_H = 240;

        public SplashForm()
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            BackColor = Color.FromArgb(30, 30, 30);
            TopMost = true;
            ShowInTaskbar = false;
            DoubleBuffered = true;
            AutoScaleMode = AutoScaleMode.None;

            bool imageLoaded = false;

            // ★ 尝试加载 splash.png，并按图片原始宽高比设置窗口尺寸
            try
            {
                string splashPath = Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "splash.png");
                if (File.Exists(splashPath))
                {
                    Image img = Image.FromFile(splashPath);
                    int w = img.Width;
                    int h = img.Height;

                    // 限制最大尺寸，避免图片过大
                    if (w > 900) { double s = 900.0 / w; w = 900; h = (int)(h * s); }
                    if (h > 700) { double s = 700.0 / h; h = 700; w = (int)(w * s); }
                    if (w < 160) w = 160;
                    if (h < 100) h = 100;

                    Size = new Size(w, h);

                    PictureBox pb = new PictureBox();
                    pb.Dock = DockStyle.Fill;
                    pb.SizeMode = PictureBoxSizeMode.Zoom;
                    pb.Image = img;
                    pb.BackColor = Color.FromArgb(30, 30, 30);
                    Controls.Add(pb);

                    imageLoaded = true;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Write("[Splash] 加载图片失败: " + ex.Message);
            }

            if (!imageLoaded)
            {
                Size = new Size(DEFAULT_W, DEFAULT_H);

                Label lbl = new Label();
                lbl.Text = "dsh-float";
                lbl.Dock = DockStyle.Fill;
                lbl.TextAlign = ContentAlignment.MiddleCenter;
                lbl.ForeColor = Color.White;
                lbl.Font = new Font("Microsoft YaHei", 28, FontStyle.Bold);
                Controls.Add(lbl);

                Label sub = new Label();
                sub.Text = "v" + Program.APP_VERSION + " " + Program.APP_BUILD;
                sub.Dock = DockStyle.Bottom;
                sub.Height = 40;
                sub.TextAlign = ContentAlignment.MiddleCenter;
                sub.ForeColor = Color.FromArgb(140, 140, 140);
                sub.Font = new Font("Microsoft YaHei", 10);
                Controls.Add(sub);
            }

            // 初步居中（OnShown 会再居中一次，保险）
            CenterOnPrimaryScreen();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            // ★ 显示后强制再居中一次
            CenterOnPrimaryScreen();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // 尺寸变了也重新居中（图片加载/DPI 缩放导致尺寸变化时）
            CenterOnPrimaryScreen();
        }

        private void CenterOnPrimaryScreen()
        {
            try
            {
                Screen scr = Screen.PrimaryScreen;
                if (scr == null) return;
                Rectangle wa = scr.WorkingArea;
                int x = wa.Left + (wa.Width  - Width)  / 2;
                int y = wa.Top  + (wa.Height - Height) / 2;
                Location = new Point(x, y);
            }
            catch { }
        }
    }

    // ============================================================
    // TextInputDialog
    // ============================================================
    public class TextInputDialog : Form
    {
        public string InputText { get; private set; }

        public TextInputDialog(string title, string prompt)
        {
            InputText = "";
            Text = title;
            Width = 500;
            Height = 260;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(40, 40, 40);
            ForeColor = Color.White;

            Label lbl = new Label();
            lbl.Text = prompt;
            lbl.Left = 16; lbl.Top = 16; lbl.Width = 460; lbl.Height = 24;
            lbl.ForeColor = Color.White;
            lbl.Font = new Font("Microsoft YaHei", 10);

            TextBox txt = new TextBox();
            txt.Left = 16; txt.Top = 50; txt.Width = 460; txt.Height = 120;
            txt.Multiline = true;
            txt.ScrollBars = ScrollBars.Vertical;
            txt.BackColor = Color.FromArgb(30, 30, 30);
            txt.ForeColor = Color.White;
            txt.BorderStyle = BorderStyle.FixedSingle;
            txt.Font = new Font("Microsoft YaHei", 10);

            Button ok = new Button();
            ok.Text = "生成";
            ok.Left = 396; ok.Top = 180; ok.Width = 80; ok.Height = 30;
            ok.BackColor = Color.FromArgb(0, 120, 215);
            ok.ForeColor = Color.White;
            ok.FlatStyle = FlatStyle.Flat;
            ok.FlatAppearance.BorderSize = 0;
            ok.Click += delegate
            {
                InputText = txt.Text;
                DialogResult = DialogResult.OK;
                Close();
            };

            Button cancel = new Button();
            cancel.Text = "取消";
            cancel.Left = 310; cancel.Top = 180; cancel.Width = 80; cancel.Height = 30;
            cancel.BackColor = Color.FromArgb(60, 60, 60);
            cancel.ForeColor = Color.White;
            cancel.FlatStyle = FlatStyle.Flat;
            cancel.FlatAppearance.BorderSize = 0;
            cancel.Click += delegate
            {
                DialogResult = DialogResult.Cancel;
                Close();
            };

            Controls.Add(lbl);
            Controls.Add(txt);
            Controls.Add(ok);
            Controls.Add(cancel);
            AcceptButton = ok;
            CancelButton = cancel;
            txt.Focus();
        }
    }

    // ============================================================
    // SettingsForm
    // ============================================================
    public class SettingsForm : Form
    {
        private CheckBox _chkAutoStart, _chkSplash, _chkUpdate, _chkDevtools;
        private CheckBox _chkHotkey, _chkLog, _chkTopmost, _chkRemember;
        private TrackBar _tbAlpha;
        private NumericUpDown _numMiniW, _numMiniH;
        private RadioButton _rbFloat, _rbInject, _rbLight, _rbDark, _rbAuto;
        private TextBox _txtOut, _txtPrefix;
        private NumericUpDown _numFps;
        private CheckBox _chkDeepThink, _chkContinue, _chkAttach, _chkToast;
        private NumericUpDown _numToast;

        public SettingsForm()
        {
            Text = "设置 - dsh-float " + Program.APP_VERSION + " " + Program.APP_BUILD;
            Width = 640;
            Height = 720;
            StartPosition = FormStartPosition.CenterParent;
            BackColor = Color.FromArgb(30, 30, 30);
            ForeColor = Color.White;
            Font = new Font("Microsoft YaHei", 10);
            MinimumSize = new Size(560, 500);

            Label title = new Label();
            title.Text = "设置";
            title.Left = 20; title.Top = 14;
            title.Width = 400; title.Height = 30;
            title.Font = new Font("Microsoft YaHei", 14, FontStyle.Bold);
            title.ForeColor = Color.White;
            Controls.Add(title);

            Panel bbar = new Panel();
            bbar.Dock = DockStyle.Bottom;
            bbar.Height = 56;
            bbar.BackColor = Color.FromArgb(30, 30, 30);
            Controls.Add(bbar);

            Button btnOK = MakeButton("确定", 500, 12, Color.FromArgb(0, 120, 215));
            btnOK.Click += delegate { ApplyAndClose(); };
            bbar.Controls.Add(btnOK);

            Button btnCancel = MakeButton("取消", 410, 12, Color.FromArgb(60, 60, 60));
            btnCancel.Click += delegate { Close(); };
            bbar.Controls.Add(btnCancel);

            Panel scrollHost = new Panel();
            scrollHost.Dock = DockStyle.Fill;
            scrollHost.BackColor = Color.FromArgb(30, 30, 30);
            scrollHost.Padding = new Padding(0, 60, 0, 56);
            Controls.Add(scrollHost);
            scrollHost.BringToFront();

            Panel scroll = new Panel();
            scroll.Dock = DockStyle.Fill;
            scroll.AutoScroll = true;
            scroll.BackColor = Color.FromArgb(30, 30, 30);
            scrollHost.Controls.Add(scroll);

            int y = 10;

            // ============ 常规 ============
            y = AddSection(scroll, "常规", y);
            _chkAutoStart = AddCheck(scroll, "开机自动启动（后台更新检查进程）", y, Program.Cfg.AutoStart); y += 30;
            _chkSplash = AddCheck(scroll, "显示启动动画", y, Program.Cfg.SplashEnable); y += 30;
            _chkUpdate = AddCheck(scroll, "自动检查更新（启动时 + 每 3 小时）", y, Program.Cfg.AutoCheckUpdate); y += 30;
            _chkDevtools = AddCheck(scroll, "启用开发者工具（F12）", y, Program.Cfg.Devtools); y += 30;
            _chkHotkey = AddCheck(scroll, "启用 Alt+Space 隐藏/唤回", y, Program.Cfg.HotkeyEnable); y += 30;
            _chkLog = AddCheck(scroll, "日志写入文件", y, Program.Cfg.LogToFile); y += 36;

            // 立即检查更新按钮
            Button btnCheck = MakeButton("立即检查更新", 24, y, Color.FromArgb(0, 120, 215));
            btnCheck.Width = 130;
            btnCheck.Click += delegate
            {
                btnCheck.Enabled = false;
                btnCheck.Text = "检查中...";
                Updater.CheckNow(delegate(bool hasNew, string latest, string dl)
                {
                    try
                    {
                        BeginInvoke((MethodInvoker)delegate
                        {
                            if (IsDisposed) return;
                            btnCheck.Enabled = true;
                            btnCheck.Text = "立即检查更新";

                            if (dl != null && dl.StartsWith("error:"))
                            {
                                MessageBox.Show(this,
                                    "检查失败：\n" + dl.Substring(6),
                                    "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                            }
                            else if (hasNew)
                            {
                                string msg = "发现新版本：" + latest + "\n" +
                                             "当前版本：v" + Program.APP_VERSION + "\n\n" +
                                             "是否打开下载页面？";
                                if (MessageBox.Show(this, msg, "发现新版本",
                                    MessageBoxButtons.YesNo,
                                    MessageBoxIcon.Information) == DialogResult.Yes)
                                {
                                    try { Process.Start(dl); } catch { }
                                }
                            }
                            else
                            {
                                MessageBox.Show(this,
                                    "已是最新版本 v" + Program.APP_VERSION,
                                    "检查完成", MessageBoxButtons.OK,
                                    MessageBoxIcon.Information);
                            }
                        });
                    }
                    catch { }
                });
            };
            scroll.Controls.Add(btnCheck);
            y += 40;

            Label lblId = new Label();
            lblId.Text = "客户端 ID：";
            lblId.Left = 24; lblId.Top = y; lblId.Width = 100;
            lblId.ForeColor = Color.FromArgb(160, 160, 160);
            scroll.Controls.Add(lblId);

            TextBox txtId = new TextBox();
            txtId.Left = 130; txtId.Top = y - 2; txtId.Width = 400;
            txtId.Text = Program.Cfg.ClientId;
            txtId.ReadOnly = true;
            txtId.BackColor = Color.FromArgb(30, 30, 30);
            txtId.ForeColor = Color.FromArgb(160, 160, 160);
            txtId.BorderStyle = BorderStyle.FixedSingle;
            scroll.Controls.Add(txtId);
            y += 40;

            // ============ 窗口 ============
            y = AddSection(scroll, "窗口", y);
            _chkTopmost = AddCheck(scroll, "窗口置顶", y, Program.Cfg.WindowTopmost); y += 30;
            _chkRemember = AddCheck(scroll, "记住窗口位置和大小", y, Program.Cfg.RememberWindow); y += 36;

            Label lblAlpha = new Label();
            lblAlpha.Text = "透明度：";
            lblAlpha.Left = 24; lblAlpha.Top = y + 4; lblAlpha.Width = 100;
            lblAlpha.ForeColor = Color.White;
            scroll.Controls.Add(lblAlpha);

            _tbAlpha = new TrackBar();
            _tbAlpha.Left = 130; _tbAlpha.Top = y; _tbAlpha.Width = 300;
            _tbAlpha.Minimum = 40; _tbAlpha.Maximum = 100;
            _tbAlpha.Value = Program.Cfg.WindowAlpha;
            _tbAlpha.TickFrequency = 10;
            _tbAlpha.BackColor = Color.FromArgb(30, 30, 30);
            scroll.Controls.Add(_tbAlpha);
            y += 60;

            Label lblMini = new Label();
            lblMini.Text = "小窗尺寸：";
            lblMini.Left = 24; lblMini.Top = y + 4; lblMini.Width = 100;
            lblMini.ForeColor = Color.White;
            scroll.Controls.Add(lblMini);

            _numMiniW = new NumericUpDown();
            _numMiniW.Left = 130; _numMiniW.Top = y; _numMiniW.Width = 80;
            _numMiniW.Minimum = 300; _numMiniW.Maximum = 2000;
            _numMiniW.Value = Program.Cfg.MiniW;
            _numMiniW.BackColor = Color.FromArgb(30, 30, 30);
            _numMiniW.ForeColor = Color.White;
            scroll.Controls.Add(_numMiniW);

            Label lblX = new Label();
            lblX.Text = "×";
            lblX.Left = 220; lblX.Top = y + 4; lblX.Width = 20;
            lblX.ForeColor = Color.White;
            scroll.Controls.Add(lblX);

            _numMiniH = new NumericUpDown();
            _numMiniH.Left = 245; _numMiniH.Top = y; _numMiniH.Width = 80;
            _numMiniH.Minimum = 300; _numMiniH.Maximum = 2000;
            _numMiniH.Value = Program.Cfg.MiniH;
            _numMiniH.BackColor = Color.FromArgb(30, 30, 30);
            _numMiniH.ForeColor = Color.White;
            scroll.Controls.Add(_numMiniH);
            y += 46;

            Label lblMode = new Label();
            lblMode.Text = "工具栏模式（切换后立即生效）";
            lblMode.Left = 24; lblMode.Top = y; lblMode.Width = 400;
            lblMode.Font = new Font("Microsoft YaHei", 10, FontStyle.Bold);
            lblMode.ForeColor = Color.White;
            scroll.Controls.Add(lblMode);
            y += 30;

            _rbFloat = new RadioButton();
            _rbFloat.Text = "🖥 右下角悬浮栏（点击生成按钮弹窗输入）";
            _rbFloat.Left = 40; _rbFloat.Top = y; _rbFloat.Width = 500;
            _rbFloat.Checked = !Program.Cfg.ToolbarMode;
            _rbFloat.ForeColor = Color.White;
            scroll.Controls.Add(_rbFloat);
            y += 28;

            _rbInject = new RadioButton();
            _rbInject.Text = "🌐 注入到网页（读取输入框，点击直接发送）";
            _rbInject.Left = 40; _rbInject.Top = y; _rbInject.Width = 500;
            _rbInject.Checked = Program.Cfg.ToolbarMode;
            _rbInject.ForeColor = Color.White;
            scroll.Controls.Add(_rbInject);
            y += 40;

            // ============ 外观 ============
            y = AddSection(scroll, "外观", y);
            Label lblTheme = new Label();
            lblTheme.Text = "主题";
            lblTheme.Left = 24; lblTheme.Top = y; lblTheme.Width = 100;
            lblTheme.ForeColor = Color.White;
            scroll.Controls.Add(lblTheme);
            y += 28;

            _rbLight = new RadioButton();
            _rbLight.Text = "浅色"; _rbLight.Left = 44; _rbLight.Top = y;
            _rbLight.Width = 100; _rbLight.ForeColor = Color.White;
            _rbLight.Checked = Program.Cfg.Theme == "light";
            scroll.Controls.Add(_rbLight);
            y += 26;

            _rbDark = new RadioButton();
            _rbDark.Text = "深色"; _rbDark.Left = 44; _rbDark.Top = y;
            _rbDark.Width = 100; _rbDark.ForeColor = Color.White;
            _rbDark.Checked = Program.Cfg.Theme == "dark";
            scroll.Controls.Add(_rbDark);
            y += 26;

            _rbAuto = new RadioButton();
            _rbAuto.Text = "跟随系统"; _rbAuto.Left = 44; _rbAuto.Top = y;
            _rbAuto.Width = 100; _rbAuto.ForeColor = Color.White;
            _rbAuto.Checked = Program.Cfg.Theme == "auto";
            scroll.Controls.Add(_rbAuto);
            y += 36;

            // ============ 生成 ============
            y = AddSection(scroll, "生成", y);

            Label lblOut = new Label();
            lblOut.Text = "输出目录：";
            lblOut.Left = 24; lblOut.Top = y + 4; lblOut.Width = 100;
            lblOut.ForeColor = Color.White;
            scroll.Controls.Add(lblOut);

            _txtOut = new TextBox();
            _txtOut.Left = 130; _txtOut.Top = y; _txtOut.Width = 320;
            _txtOut.Text = Program.Cfg.OutputDir;
            _txtOut.BackColor = Color.FromArgb(30, 30, 30);
            _txtOut.ForeColor = Color.White;
            _txtOut.BorderStyle = BorderStyle.FixedSingle;
            scroll.Controls.Add(_txtOut);

            Button btnBrowse = MakeButton("浏览", 460, y, Color.FromArgb(60, 60, 60));
            btnBrowse.Width = 60; btnBrowse.Height = 24;
            btnBrowse.Click += delegate
            {
                using (FolderBrowserDialog fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                        _txtOut.Text = fbd.SelectedPath;
                }
            };
            scroll.Controls.Add(btnBrowse);
            y += 30;

            Label lblPrefix = new Label();
            lblPrefix.Text = "文件名前缀：";
            lblPrefix.Left = 24; lblPrefix.Top = y + 4; lblPrefix.Width = 100;
            lblPrefix.ForeColor = Color.White;
            scroll.Controls.Add(lblPrefix);

            _txtPrefix = new TextBox();
            _txtPrefix.Left = 130; _txtPrefix.Top = y; _txtPrefix.Width = 200;
            _txtPrefix.Text = Program.Cfg.NamePrefix;
            _txtPrefix.BackColor = Color.FromArgb(30, 30, 30);
            _txtPrefix.ForeColor = Color.White;
            _txtPrefix.BorderStyle = BorderStyle.FixedSingle;
            scroll.Controls.Add(_txtPrefix);
            y += 36;

            Label lblFps = new Label();
            lblFps.Text = "视频帧率：";
            lblFps.Left = 24; lblFps.Top = y + 4; lblFps.Width = 100;
            lblFps.ForeColor = Color.White;
            scroll.Controls.Add(lblFps);

            _numFps = new NumericUpDown();
            _numFps.Left = 130; _numFps.Top = y; _numFps.Width = 80;
            _numFps.Minimum = 5; _numFps.Maximum = 30;
            _numFps.Value = Program.Cfg.VideoFps;
            _numFps.BackColor = Color.FromArgb(30, 30, 30);
            _numFps.ForeColor = Color.White;
            scroll.Controls.Add(_numFps);
            y += 36;

            _chkDeepThink = AddCheck(scroll, "检测到【深度思考】开启时提示", y, Program.Cfg.WarnDeepThink); y += 30;
            _chkContinue = AddCheck(scroll, "回复被截断时自动点【继续生成】", y, Program.Cfg.AutoClickContinue); y += 30;
            _chkAttach = AddCheck(scroll, "生成视频时自动附加最近一次的音乐", y, Program.Cfg.AutoAttachMusic); y += 30;
            _chkToast = AddCheck(scroll, "生成完成时弹 Toast 提示", y, Program.Cfg.AutoSaveToast); y += 30;

            Label lblToastDur = new Label();
            lblToastDur.Text = "Toast 时长(ms)：";
            lblToastDur.Left = 24; lblToastDur.Top = y + 4; lblToastDur.Width = 110;
            lblToastDur.ForeColor = Color.White;
            scroll.Controls.Add(lblToastDur);

            _numToast = new NumericUpDown();
            _numToast.Left = 140; _numToast.Top = y; _numToast.Width = 90;
            _numToast.Minimum = 1000; _numToast.Maximum = 10000;
            _numToast.Increment = 500;
            _numToast.Value = Program.Cfg.ToastDuration;
            _numToast.BackColor = Color.FromArgb(30, 30, 30);
            _numToast.ForeColor = Color.White;
            scroll.Controls.Add(_numToast);
            y += 44;

            // ============ 关于 ============
            y = AddSection(scroll, "关于", y);

            Label lblApp = new Label();
            lblApp.Text = "dsh-float";
            lblApp.Left = 24; lblApp.Top = y; lblApp.Width = 400;
            lblApp.Font = new Font("Microsoft YaHei", 16, FontStyle.Bold);
            lblApp.ForeColor = Color.White;
            scroll.Controls.Add(lblApp);
            y += 32;

            Label lblVer = new Label();
            lblVer.Text = "版本 " + Program.APP_VERSION + "（" + Program.APP_BUILD + "）";
            lblVer.Left = 24; lblVer.Top = y; lblVer.Width = 400;
            lblVer.ForeColor = Color.FromArgb(160, 160, 160);
            scroll.Controls.Add(lblVer);
            y += 26;

            Label lblTech = new Label();
            lblTech.Text = "C# + .NET Framework + WebView2";
            lblTech.Left = 24; lblTech.Top = y; lblTech.Width = 400;
            lblTech.ForeColor = Color.FromArgb(160, 160, 160);
            lblTech.Font = new Font("Microsoft YaHei", 9);
            scroll.Controls.Add(lblTech);
            y += 36;

            Button btnOpenOut = MakeButton("打开输出目录", 24, y, Color.FromArgb(0, 120, 215));
            btnOpenOut.Width = 130;
            btnOpenOut.Click += delegate
            {
                try { Process.Start("explorer.exe", Program.Cfg.EnsureOutputsDir()); }
                catch { }
            };
            scroll.Controls.Add(btnOpenOut);
            y += 40;

            Button btnOpenLog = MakeButton("打开日志文件", 24, y, Color.FromArgb(0, 120, 215));
            btnOpenLog.Width = 130;
            btnOpenLog.Click += delegate
            {
                try { Process.Start("notepad.exe", LogHelper.LogPath); }
                catch { }
            };
            scroll.Controls.Add(btnOpenLog);
            y += 40;

            Button btnReset = MakeButton("重置配置", 24, y, Color.FromArgb(200, 60, 60));
            btnReset.Width = 130;
            btnReset.Click += delegate
            {
                if (MessageBox.Show("重置所有配置？程序需要重启。", "确认",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question)
                    == DialogResult.Yes)
                {
                    try
                    {
                        File.Delete(Program.Cfg.ConfigPath);
                        MessageBox.Show("已重置，重启后生效。");
                    }
                    catch { }
                }
            };
            scroll.Controls.Add(btnReset);
            y += 60;
        }

        private int AddSection(Panel parent, string title, int y)
        {
            Panel lf = new Panel();
            lf.Left = 10; lf.Top = y; lf.Width = parent.Width - 30;
            lf.Height = 32;
            lf.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            lf.BackColor = Color.FromArgb(40, 40, 40);
            parent.Controls.Add(lf);

            Label lbl = new Label();
            lbl.Text = title;
            lbl.Left = 14; lbl.Top = 6; lbl.Width = 300; lbl.Height = 22;
            lbl.ForeColor = Color.FromArgb(100, 170, 255);
            lbl.Font = new Font("Microsoft YaHei", 11, FontStyle.Bold);
            lf.Controls.Add(lbl);

            return y + 42;
        }

        private CheckBox AddCheck(Panel parent, string text, int y, bool val)
        {
            CheckBox cb = new CheckBox();
            cb.Text = text;
            cb.Left = 24; cb.Top = y; cb.Width = 560;
            cb.Checked = val;
            cb.ForeColor = Color.White;
            cb.BackColor = Color.Transparent;
            parent.Controls.Add(cb);
            return cb;
        }

        private Button MakeButton(string text, int left, int top, Color bg)
        {
            Button b = new Button();
            b.Text = text;
            b.Left = left; b.Top = top;
            b.Width = 80; b.Height = 30;
            b.BackColor = bg;
            b.ForeColor = Color.White;
            b.FlatStyle = FlatStyle.Flat;
            b.FlatAppearance.BorderSize = 0;
            b.Font = new Font("Microsoft YaHei", 10);
            return b;
        }

        private void ApplyAndClose()
        {
            bool oldAutoStart = Program.Cfg.AutoStart;

            Program.Cfg.AutoStart = _chkAutoStart.Checked;
            Program.Cfg.SplashEnable = _chkSplash.Checked;
            Program.Cfg.AutoCheckUpdate = _chkUpdate.Checked;
            Program.Cfg.Devtools = _chkDevtools.Checked;
            Program.Cfg.HotkeyEnable = _chkHotkey.Checked;
            Program.Cfg.LogToFile = _chkLog.Checked;
            Program.Cfg.WindowTopmost = _chkTopmost.Checked;
            Program.Cfg.RememberWindow = _chkRemember.Checked;
            Program.Cfg.WindowAlpha = _tbAlpha.Value;
            Program.Cfg.MiniW = (int)_numMiniW.Value;
            Program.Cfg.MiniH = (int)_numMiniH.Value;
            Program.Cfg.ToolbarMode = _rbInject.Checked;

            if (_rbLight.Checked) Program.Cfg.Theme = "light";
            else if (_rbDark.Checked) Program.Cfg.Theme = "dark";
            else Program.Cfg.Theme = "auto";

            Program.Cfg.OutputDir = _txtOut.Text.Trim();
            Program.Cfg.NamePrefix = _txtPrefix.Text.Trim();
            if (string.IsNullOrEmpty(Program.Cfg.NamePrefix))
                Program.Cfg.NamePrefix = "ds";
            Program.Cfg.VideoFps = (int)_numFps.Value;
            Program.Cfg.WarnDeepThink = _chkDeepThink.Checked;
            Program.Cfg.AutoClickContinue = _chkContinue.Checked;
            Program.Cfg.AutoAttachMusic = _chkAttach.Checked;
            Program.Cfg.AutoSaveToast = _chkToast.Checked;
            Program.Cfg.ToastDuration = (int)_numToast.Value;

            Program.Cfg.Save();

            if (Program.Cfg.AutoStart != oldAutoStart)
                AutoStart.Set(Program.Cfg.AutoStart);

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
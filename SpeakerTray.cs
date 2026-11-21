using System;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace SpeakerTray {

[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume {
    int f1(); int f2(); int GetChannelCount(out int c);
    int SetMasterVolumeLevel(float l, ref Guid g); int SetMasterVolumeLevelScalar(float l, ref Guid g);
    int GetMasterVolumeLevel(out float l); int GetMasterVolumeLevelScalar(out float l);
    int a(); int b(); int c(); int d();
    int SetMute([MarshalAs(UnmanagedType.Bool)] bool m, ref Guid g); int GetMute([MarshalAs(UnmanagedType.Bool)] out bool m);
}
[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice {
    int Activate(ref Guid id, int ctx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
    int OpenPropertyStore(int a, out IPropertyStore ps);
    int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
    int GetState(out int s);
}
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection { int GetCount(out int c); int Item(int i, out IMMDevice d); }
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator {
    int EnumAudioEndpoints(int flow, int mask, out IMMDeviceCollection c);
    int GetDefaultAudioEndpoint(int flow, int role, out IMMDevice d);
}
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore { int GetCount(out int c); int GetAt(int i, out PK k); int GetValue(ref PK k, out PV v); }
[StructLayout(LayoutKind.Sequential)] struct PK { public Guid f; public int p; }
[StructLayout(LayoutKind.Sequential)] struct PV { public short vt; short r1, r2, r3; public IntPtr p; IntPtr p2; }
[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDE {}
[ComImport, Guid("f8679f50-850a-41cf-9c72-430f290290c8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPolicyConfig {
    int GetMixFormat(); int GetDeviceFormat(); int ResetDeviceFormat(); int SetDeviceFormat();
    int GetProcessingPeriod(); int SetProcessingPeriod(); int GetShareMode(); int SetShareMode();
    int GetPropertyValue(); int SetPropertyValue();
    int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string id, int role);
    int SetEndpointVisibility();
}
[ComImport, Guid("870af99c-171d-4f9e-af0d-e63df40c2bc9")] class PolicyConfigClient {}

static class Audio {
    static string NameOf(IMMDevice d) {
        IPropertyStore ps; d.OpenPropertyStore(0, out ps);
        PK k = new PK(); k.f = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"); k.p = 14;
        PV v; ps.GetValue(ref k, out v);
        return v.p == IntPtr.Zero ? "" : Marshal.PtrToStringUni(v.p);
    }
    static IMMDevice Find(string match) {
        IMMDeviceCollection col; ((IMMDeviceEnumerator)new MMDE()).EnumAudioEndpoints(0, 1, out col);
        int n; col.GetCount(out n);
        for (int i = 0; i < n; i++) {
            IMMDevice d; col.Item(i, out d);
            if (NameOf(d).IndexOf(match, StringComparison.OrdinalIgnoreCase) >= 0) return d;
        }
        return null;
    }
    static IAudioEndpointVolume Vol(IMMDevice d) {
        Guid iid = typeof(IAudioEndpointVolume).GUID; object o;
        d.Activate(ref iid, 23, IntPtr.Zero, out o);
        return (IAudioEndpointVolume)o;
    }
    public static string DefaultName() {
        try { IMMDevice d; ((IMMDeviceEnumerator)new MMDE()).GetDefaultAudioEndpoint(0, 0, out d); return NameOf(d); }
        catch { return ""; }
    }
    public static bool SetDefault(string match) {
        IMMDevice d = Find(match); if (d == null) return false;
        string id; d.GetId(out id);
        IPolicyConfig pc = (IPolicyConfig)new PolicyConfigClient();
        for (int role = 0; role < 3; role++) pc.SetDefaultEndpoint(id, role);
        return true;
    }
    public static int GetVol(string match) {
        try { IMMDevice d = Find(match); if (d == null) return -1; float s; Vol(d).GetMasterVolumeLevelScalar(out s); return (int)Math.Round(s * 100); }
        catch { return -1; }
    }
    public static void SetVol(string match, int pct) {
        try { IMMDevice d = Find(match); if (d == null) return; Guid g = Guid.Empty; var v = Vol(d);
              v.SetMasterVolumeLevelScalar(pct / 100f, ref g); v.SetMute(false, ref g); } catch { }
    }
}

static class VM {
    const string Dir = @"C:\Program Files (x86)\VB\Voicemeeter\";
    const string Dll = @"C:\Program Files (x86)\VB\Voicemeeter\VoicemeeterRemote64.dll";
    [DllImport(Dll)] static extern int VBVMR_Login();
    [DllImport(Dll)] static extern int VBVMR_IsParametersDirty();
    [DllImport(Dll, CharSet = CharSet.Ansi)] static extern int VBVMR_SetParameters(string s);
    [DllImport(Dll, CharSet = CharSet.Ansi)] static extern int VBVMR_GetParameterFloat(string n, ref float v);
    static bool logged;
    public static bool Running() { return Process.GetProcessesByName("voicemeeterpro_x64").Length > 0 || Process.GetProcessesByName("voicemeeterpro").Length > 0; }
    public static void EnsureRunning() {
        if (!Running()) { try { Process.Start(Dir + "voicemeeterpro_x64.exe"); Thread.Sleep(3000); } catch { } }
        if (!logged) { try { VBVMR_Login(); logged = true; Thread.Sleep(300); } catch { } }
    }
    public static int GetDelay(int bus) {
        if (!Running()) return -1;
        EnsureRunning(); VBVMR_IsParametersDirty();
        float v = 0; return VBVMR_GetParameterFloat("Option.delay[" + bus + "]", ref v) == 0 ? (int)v : -1;
    }
    public static void SetDelay(int bus, int ms) {
        EnsureRunning(); VBVMR_SetParameters("Option.delay[" + bus + "]=" + ms);
        if (bus == 1) SavedPartyDelay = ms;
    }
    public static void Show() { EnsureRunning(); try { Process.Start(Dir + "voicemeeterpro_x64.exe"); } catch { } }

    // Sound Party delay is remembered in a file so it can be re-applied after a crash/restart.
    static string DelayFile { get { return System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpeakerTray", "party-delay.txt"); } }
    public static int SavedPartyDelay {
        get { try { return int.Parse(System.IO.File.ReadAllText(DelayFile).Trim()); } catch { return 100; } }
        set { try { System.IO.File.WriteAllText(DelayFile, value.ToString()); } catch { } }
    }
    static bool WaitReady(int ms) {
        float f = 0;
        for (int t = 0; t < ms; t += 500) {
            try { VBVMR_IsParametersDirty(); if (VBVMR_GetParameterFloat("Strip[3].A1", ref f) == 0) return true; } catch { }
            Thread.Sleep(500);
        }
        return false;
    }
    public static void Stop() {
        foreach (var n in new[] { "voicemeeterpro_x64", "voicemeeterpro" })
            foreach (var p in Process.GetProcessesByName(n)) {
                try { p.CloseMainWindow(); if (!p.WaitForExit(3000)) p.Kill(); } catch { }
            }
    }
    // Kill (if hung) and start Voicemeeter, then re-apply routing and delay. Returns true when it answers again.
    public static bool Restart(string barDev, string partyDev) {
        foreach (var n in new[] { "voicemeeterpro_x64", "voicemeeterpro" })
            foreach (var p in Process.GetProcessesByName(n)) { try { p.Kill(); p.WaitForExit(3000); } catch { } }
        Thread.Sleep(500);
        try { Process.Start(Dir + "voicemeeterpro_x64.exe"); } catch { return false; }
        if (!logged) { try { VBVMR_Login(); logged = true; } catch { } }
        if (!WaitReady(20000)) return false;
        VBVMR_SetParameters("Bus[0].device.wdm=\"" + barDev + "\";Bus[1].device.wdm=\"" + partyDev + "\";" +
            "Strip[3].A1=1;Strip[3].A2=1;Bus[0].Mute=0;Bus[1].Mute=0;Option.delay[0]=0;Option.delay[1]=" + SavedPartyDelay);
        return true;
    }
}

public class TrayApp : ApplicationContext {
    public const string VMIN = "Voicemeeter Input (", BAR = "Xiaomi Soundbar", PARTY = "Headphones (Xiaomi Sound Party)", PC = "Speakers (Realtek";
    public const string BAR_DEV = "Speakers (Xiaomi Soundbar 2.0)"; // exact Windows name, used for Voicemeeter A1
    public const string AIRPODS = "Headphones (AirPods Pro)";
    public const int MODE_BOTH = 0, MODE_PC = 3, MODE_AIRPODS = 4;
    public static readonly string[] ModeNames = { "Both speakers (soundbar + Sound Party)", "Soundbar only", "Sound Party only", "PC headphones (Realtek)", "AirPods Pro" };
    static readonly string[] ModeDev = { VMIN, BAR, PARTY, PC, AIRPODS };
    public static int ModeCount { get { return ModeNames.Length; } }

    NotifyIcon icon; ControlForm form; ToolStripMenuItem[] modeItems = new ToolStripMenuItem[ModeNames.Length];

    public TrayApp() {
        form = new ControlForm(this);
        var h = form.Handle; // create handle now so BeginInvoke works before the panel is first shown
        var menu = new ContextMenuStrip();
        for (int i = 0; i < ModeCount; i++) {
            int m = i; modeItems[i] = new ToolStripMenuItem(ModeNames[i], null, delegate { SetMode(m); });
            menu.Items.Add(modeItems[i]);
        }
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Back to default (Voicemeeter off)", null, delegate { NormalMode(MODE_PC); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Start / restart Voicemeeter (fix no sound)", null, delegate { RestartVM(); });
        menu.Items.Add("Open Voicemeeter", null, delegate { VM.Show(); });
        menu.Items.Add("Sound settings", null, delegate { Process.Start("ms-settings:sound"); });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("Exit", null, delegate { icon.Visible = false; Application.Exit(); });
        menu.Opening += delegate { int cur = CurrentMode(); for (int i = 0; i < ModeCount; i++) modeItems[i].Checked = i == cur; };

        icon = new NotifyIcon();
        try { icon.Icon = Icon.ExtractAssociatedIcon(Environment.SystemDirectory + "\\SndVol.exe"); } catch { icon.Icon = SystemIcons.Application; }
        icon.ContextMenuStrip = menu;
        icon.MouseClick += delegate(object s, MouseEventArgs e) { if (e.Button == MouseButtons.Left) form.Toggle(); };
        icon.Visible = true;
        UpdateTip();
        // After a reboot Windows still points at "Voicemeeter Input"; without Voicemeeter running there is no sound.
        if (CurrentMode() == 0 && !VM.Running()) {
            var t = new Thread(delegate() { VM.EnsureRunning(); }); t.IsBackground = true; t.Start();
        }
        // Watchdog: warn (with a one-click fix) if Voicemeeter disappears while it is the output.
        vmWasRunning = VM.Running();
        icon.BalloonTipClicked += delegate { if (offerRestart) { offerRestart = false; RestartVM(); } };
        var timer = new System.Windows.Forms.Timer(); timer.Interval = 5000;
        timer.Tick += delegate {
            bool now = VM.Running();
            if (vmWasRunning && !now && !restarting && CurrentMode() == 0) {
                offerRestart = true;
                icon.ShowBalloonTip(8000, "Voicemeeter stopped - no sound", "Click here to restart it.", ToolTipIcon.Warning);
            }
            vmWasRunning = now;
        };
        timer.Start();
    }
    bool vmWasRunning, offerRestart, restarting;
    public void RestartVM() { RestartVM(false); }
    // selectBoth: also make "Voicemeeter Input" the Windows output once it is back (used when leaving normal mode)
    public void RestartVM(bool selectBoth) {
        if (restarting) return;
        restarting = true;
        icon.ShowBalloonTip(3000, "Voicemeeter", "Starting Voicemeeter...", ToolTipIcon.Info);
        var t = new Thread(delegate() {
            bool ok = VM.Restart(BAR_DEV, PARTY);
            form.BeginInvoke((MethodInvoker)delegate {
                restarting = false; vmWasRunning = VM.Running();
                if (ok && selectBoth) Audio.SetDefault(VMIN);
                icon.ShowBalloonTip(3000, "Voicemeeter", ok ? "Voicemeeter is running - sound restored." : "Voicemeeter did not start. Try again or open it manually.", ok ? ToolTipIcon.Info : ToolTipIcon.Error);
                UpdateTip(); if (form.Visible) form.Reload();
            });
        });
        t.IsBackground = true; t.Start();
    }
    public int CurrentMode() {
        string d = Audio.DefaultName();
        for (int i = 0; i < ModeCount; i++) if (d.IndexOf(ModeDev[i], StringComparison.OrdinalIgnoreCase) >= 0) return i;
        return -1;
    }
    public void SetMode(int m) {
        // Voicemeeter off (normal mode)? start it and re-apply routing, then switch to it.
        if (m == MODE_BOTH && !VM.Running()) { RestartVM(true); return; }
        if (!Audio.SetDefault(ModeDev[m]))
            icon.ShowBalloonTip(3000, "Speaker switch", ModeNames[m] + " is not connected.", ToolTipIcon.Warning);
        UpdateTip();
    }
    // Undo the dual-speaker setup: plain Windows output to one device, Voicemeeter closed.
    // Nothing nags or auto-starts afterwards because Windows no longer points at Voicemeeter.
    public void NormalMode(int m) {
        if (!Audio.SetDefault(ModeDev[m])) {
            icon.ShowBalloonTip(4000, "Normal mode", ModeNames[m] + " is not connected - using PC headphones.", ToolTipIcon.Warning);
            m = MODE_PC; Audio.SetDefault(ModeDev[m]);
        }
        VM.Stop(); vmWasRunning = false; offerRestart = false;
        icon.ShowBalloonTip(3000, "Back to default", "Voicemeeter is off. Output: " + ModeNames[m] + ". Connect AirPods or pick another output any time.", ToolTipIcon.Info);
        UpdateTip(); if (form.Visible) form.Reload();
    }
    public void UpdateTip() {
        int m = CurrentMode();
        string t = "Speakers: " + (m < 0 ? Audio.DefaultName() : ModeNames[m]);
        icon.Text = t.Length > 63 ? t.Substring(0, 63) : t;
    }
}

public class ControlForm : Form {
    TrayApp app; bool loading;
    RadioButton[] radios = new RadioButton[TrayApp.ModeCount];
    TrackBar tMaster, tBar, tParty; Label lMaster, lBar, lParty, lVm; NumericUpDown nDelay;

    public ControlForm(TrayApp a) {
        app = a;
        Text = "Speakers"; FormBorderStyle = FormBorderStyle.FixedToolWindow; ShowInTaskbar = false; TopMost = true;
        StartPosition = FormStartPosition.Manual; Font = new Font("Segoe UI", 9f); ClientSize = new Size(310, 360);
        int y = 8;
        Head("Output", ref y);
        for (int i = 0; i < TrayApp.ModeCount; i++) {
            int m = i; var r = new RadioButton(); r.Text = TrayApp.ModeNames[i]; r.SetBounds(16, y, 285, 22); y += 23;
            r.CheckedChanged += delegate { if (!loading && r.Checked) { app.SetMode(m); Reload(); } };
            radios[i] = r; Controls.Add(r);
        }
        y += 6; Head("Volume", ref y);
        tMaster = Slider("Main (current output)", ref y, out lMaster, delegate(int v) { Audio.SetVol(Audio.DefaultName(), v); });
        tBar = Slider("Soundbar", ref y, out lBar, delegate(int v) { Audio.SetVol(TrayApp.BAR, v); });
        tParty = Slider("Sound Party", ref y, out lParty, delegate(int v) { Audio.SetVol(TrayApp.PARTY, v); });
        y += 4; Head("Sync", ref y);
        var ld = new Label(); ld.Text = "Sound Party delay (ms)"; ld.SetBounds(16, y + 3, 170, 20); Controls.Add(ld);
        nDelay = new NumericUpDown(); nDelay.Minimum = 0; nDelay.Maximum = 500; nDelay.Increment = 10; nDelay.SetBounds(200, y, 90, 24);
        nDelay.ValueChanged += delegate { if (!loading) VM.SetDelay(1, (int)nDelay.Value); };
        Controls.Add(nDelay);
        y += 34; Head("Voicemeeter", ref y);
        lVm = new Label(); lVm.SetBounds(16, y + 6, 120, 20); Controls.Add(lVm);
        var bVm = new Button(); bVm.Text = "Start / restart"; bVm.SetBounds(150, y, 140, 30);
        bVm.Click += delegate { app.RestartVM(); lVm.Text = "Restarting..."; lVm.ForeColor = Color.DarkOrange; };
        Controls.Add(bVm);
        y += 40;
        var bDef = new Button(); bDef.Text = "Back to default (Voicemeeter off)"; bDef.SetBounds(16, y, 274, 32);
        bDef.Click += delegate { app.NormalMode(TrayApp.MODE_PC); Reload(); };
        Controls.Add(bDef);
        y += 36;
        var hint = new Label(); hint.Text = "Pick \"Both speakers\" above to turn the setup back on.";
        hint.ForeColor = Color.Gray; hint.SetBounds(16, y, 285, 20); Controls.Add(hint);
        ClientSize = new Size(310, y + 26);
        Deactivate += delegate { Hide(); };
    }
    void Head(string t, ref int y) {
        var l = new Label(); l.Text = t; l.Font = new Font("Segoe UI Semibold", 9.5f); l.SetBounds(8, y, 290, 20); Controls.Add(l); y += 22;
    }
    delegate void IntAction(int v);
    TrackBar Slider(string title, ref int y, out Label val, IntAction onChange) {
        var l = new Label(); l.Text = title; l.SetBounds(16, y + 4, 140, 20); Controls.Add(l);
        var tb = new TrackBar(); tb.Minimum = 0; tb.Maximum = 100; tb.TickStyle = TickStyle.None; tb.SetBounds(150, y, 110, 28);
        var v = new Label(); v.SetBounds(262, y + 4, 45, 20); Controls.Add(v); Controls.Add(tb);
        tb.ValueChanged += delegate { v.Text = tb.Value + "%"; if (!loading) onChange(tb.Value); };
        val = v; y += 30; return tb;
    }
    void SetSlider(TrackBar tb, Label l, int v) {
        tb.Enabled = v >= 0; tb.Value = Math.Max(0, v); l.Text = v >= 0 ? v + "%" : "off";
    }
    public void Reload() {
        loading = true;
        int m = app.CurrentMode();
        for (int i = 0; i < TrayApp.ModeCount; i++) radios[i].Checked = i == m;
        SetSlider(tMaster, lMaster, Audio.GetVol(Audio.DefaultName()));
        SetSlider(tBar, lBar, Audio.GetVol(TrayApp.BAR));
        SetSlider(tParty, lParty, Audio.GetVol(TrayApp.PARTY));
        int d = VM.GetDelay(1); nDelay.Enabled = d >= 0; nDelay.Value = Math.Max(0, Math.Min(500, d));
        bool up = VM.Running(); lVm.Text = up ? "Running" : "Not running"; lVm.ForeColor = up ? Color.ForestGreen : Color.Firebrick;
        loading = false;
        app.UpdateTip();
    }
    public void Toggle() {
        if (Visible) { Hide(); return; }
        Reload();
        Rectangle wa = Screen.FromPoint(Cursor.Position).WorkingArea;
        Location = new Point(wa.Right - Width - 12, wa.Bottom - Height - 12);
        Show(); Activate();
    }
}

public static class Program {
    [STAThread]
    public static void Main() {
        try { Run(); }
        catch (Exception ex) {
            string log = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SpeakerTray", "SpeakerTray.log");
            string msg = DateTime.Now.ToString("s") + "  " + ex.GetType().FullName + ": " + ex.Message;
            try { System.IO.File.AppendAllText(log, msg + Environment.NewLine); } catch { }
            MessageBox.Show("SpeakerTray failed to start:\n\n" + msg + "\n\nLogged to " + log, "SpeakerTray");
        }
    }
    public static void Run() {
        bool fresh; var mx = new Mutex(true, "SpeakerTray_SingleInstance", out fresh);
        if (!fresh) return;
        Application.EnableVisualStyles();
        Application.Run(new TrayApp());
        GC.KeepAlive(mx);
    }
}
}

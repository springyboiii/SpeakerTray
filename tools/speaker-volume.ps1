param([string]$SetName = '', [float]$Level = -1)
Add-Type -TypeDefinition @"
using System; using System.Runtime.InteropServices;
[Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IAudioEndpointVolume { int f1(); int f2(); int GetChannelCount(out int c); int SetMasterVolumeLevel(float l, Guid g);
 int SetMasterVolumeLevelScalar(float l, Guid g); int GetMasterVolumeLevel(out float l); int GetMasterVolumeLevelScalar(out float l);
 int a(); int b(); int c(); int d(); int SetMute(bool m, Guid g); int GetMute(out bool m); }
[Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDevice { int Activate(ref Guid id, int ctx, IntPtr p, [MarshalAs(UnmanagedType.IUnknown)] out object o);
 int OpenPropertyStore(int a, out IPropertyStore ps); int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id); int GetState(out int s); }
[Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceCollection { int GetCount(out int c); int Item(int i, out IMMDevice d); }
[Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IMMDeviceEnumerator { int EnumAudioEndpoints(int flow, int mask, out IMMDeviceCollection c); }
[Guid("886d8eeb-8cf2-4446-8d02-cdba1dbdcf99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
interface IPropertyStore { int GetCount(out int c); int GetAt(int i, out PK k); int GetValue(ref PK k, out PV v); }
[StructLayout(LayoutKind.Sequential)] struct PK { public Guid f; public int p; }
[StructLayout(LayoutKind.Sequential)] struct PV { public short vt; short r1, r2, r3; public IntPtr p; IntPtr p2; }
[ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class MMDE {}
public static class Vol {
 public static string Run(string setName, float level) {
  var e = (IMMDeviceEnumerator)new MMDE(); IMMDeviceCollection col; e.EnumAudioEndpoints(0, 1, out col);
  int n; col.GetCount(out n); var sb = new System.Text.StringBuilder();
  for (int i = 0; i < n; i++) { IMMDevice d; col.Item(i, out d); IPropertyStore ps; d.OpenPropertyStore(0, out ps);
   var k = new PK { f = new Guid("a45c254e-df1c-4efd-8020-67d146a850e0"), p = 14 }; PV v; ps.GetValue(ref k, out v);
   string name = Marshal.PtrToStringUni(v.p);
   var iid = typeof(IAudioEndpointVolume).GUID; object o; d.Activate(ref iid, 1, IntPtr.Zero, out o); var ev = (IAudioEndpointVolume)o;
   if (setName != "" && name.Contains(setName) && level >= 0) { ev.SetMasterVolumeLevelScalar(level, Guid.Empty); ev.SetMute(false, Guid.Empty); }
   float s; ev.GetMasterVolumeLevelScalar(out s); bool m; ev.GetMute(out m);
   sb.AppendLine(string.Format("{0,-45} {1,3}%{2}", name, Math.Round(s * 100), m ? " MUTED" : "")); }
  return sb.ToString(); } }
"@
[Vol]::Run($SetName, $Level)

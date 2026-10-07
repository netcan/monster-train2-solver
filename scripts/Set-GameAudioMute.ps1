#requires -Version 7.4
param(
    [Parameter(Mandatory)] [int] $ProcessId,
    [switch] $CheckOnly
)
$ErrorActionPreference = 'Stop'
if (-not ('PojuProbeAudio.Sessions' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
namespace PojuProbeAudio {
    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")] class DeviceEnumerator {}
    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDevices {
        [PreserveSig] int EnumAudioEndpoints(int flow, uint mask, out object devices);
        [PreserveSig] int GetDefaultAudioEndpoint(int flow, int role, out IDevice device);
    }
    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IDevice {
        [PreserveSig] int Activate(ref Guid iid, uint context, IntPtr parameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
    }
    [ComImport, Guid("77AA99A0-1BD6-484F-8BC7-2C654C9A9B6F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IManager {
        [PreserveSig] int GetAudioSessionControl(ref Guid id, uint flags, out object control);
        [PreserveSig] int GetSimpleAudioVolume(ref Guid id, uint flags, out object volume);
        [PreserveSig] int GetSessionEnumerator(out ISessions sessions);
    }
    [ComImport, Guid("E2F5BB11-0570-40CA-ACDD-3AA01277DEE8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface ISessions {
        [PreserveSig] int GetCount(out int count);
        [PreserveSig] int GetSession(int index, [MarshalAs(UnmanagedType.IUnknown)] out object session);
    }
    [ComImport, Guid("BFB7FF88-7239-4FC9-8FA2-07C950BE9C6D"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IControl {
        [PreserveSig] int GetState(out int state);
        [PreserveSig] int GetDisplayName([MarshalAs(UnmanagedType.LPWStr)] out string name);
        [PreserveSig] int SetDisplayName([MarshalAs(UnmanagedType.LPWStr)] string name, ref Guid context);
        [PreserveSig] int GetIconPath([MarshalAs(UnmanagedType.LPWStr)] out string path);
        [PreserveSig] int SetIconPath([MarshalAs(UnmanagedType.LPWStr)] string path, ref Guid context);
        [PreserveSig] int GetGroupingParam(out Guid group);
        [PreserveSig] int SetGroupingParam(ref Guid group, ref Guid context);
        [PreserveSig] int RegisterAudioSessionNotification(IntPtr callback);
        [PreserveSig] int UnregisterAudioSessionNotification(IntPtr callback);
        [PreserveSig] int GetSessionIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetSessionInstanceIdentifier([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetProcessId(out uint pid);
    }
    [ComImport, Guid("87CE5498-68D6-44E5-9215-6DA47EF883D8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    interface IVolume {
        [PreserveSig] int SetMasterVolume(float volume, ref Guid context);
        [PreserveSig] int GetMasterVolume(out float volume);
        [PreserveSig] int SetMute([MarshalAs(UnmanagedType.Bool)] bool mute, ref Guid context);
        [PreserveSig] int GetMute([MarshalAs(UnmanagedType.Bool)] out bool mute);
    }
    public static class Sessions {
        public static int Mute(int processId, bool checkOnly) {
            var devices = (IDevices)new DeviceEnumerator();
            IDevice device; object manager; ISessions sessions;
            Marshal.ThrowExceptionForHR(devices.GetDefaultAudioEndpoint(0, 1, out device));
            var iid = typeof(IManager).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref iid, 23, IntPtr.Zero, out manager));
            Marshal.ThrowExceptionForHR(((IManager)manager).GetSessionEnumerator(out sessions));
            int count, matched = 0; sessions.GetCount(out count);
            for (int i = 0; i < count; i++) {
                object session; Marshal.ThrowExceptionForHR(sessions.GetSession(i, out session));
                uint pid; Marshal.ThrowExceptionForHR(((IControl)session).GetProcessId(out pid));
                if (pid != processId) continue;
                var volume = (IVolume)session; var context = Guid.Empty;
                if (!checkOnly) Marshal.ThrowExceptionForHR(volume.SetMute(true, ref context));
                bool muted; Marshal.ThrowExceptionForHR(volume.GetMute(out muted));
                if (!muted) throw new InvalidOperationException("The game's audio session is not muted.");
                matched++;
            }
            return matched;
        }
    }
}
'@
}
$matched = [PojuProbeAudio.Sessions]::Mute($ProcessId, $CheckOnly.IsPresent)
if ($matched -eq 0) { throw "No audio session found for process $ProcessId." }
[pscustomobject]@{ ProcessId = $ProcessId; MutedSessions = $matched; Muted = $true }

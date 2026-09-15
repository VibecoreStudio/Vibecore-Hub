using System.Runtime.InteropServices;

namespace VibecoreHub.Desktop.Modules.Audio;

internal static class AudioDeviceService
{
    private const int DeviceStateActive = 0x1;
    private static readonly PropertyKey FriendlyNameKey = new(new Guid("A45C254E-DF1C-4EFD-8020-67D146A850E0"), 14);

    public static IReadOnlyList<AudioDeviceInfo> GetDevices(bool input)
    {
        var result = new List<AudioDeviceInfo>();
        IMMDeviceEnumerator? enumerator = null;
        IMMDeviceCollection? collection = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumeratorComObject();
            Marshal.ThrowExceptionForHR(enumerator.EnumAudioEndpoints(input ? DataFlow.Capture : DataFlow.Render, DeviceStateActive, out collection));
            Marshal.ThrowExceptionForHR(collection.GetCount(out var count));
            for (uint index = 0; index < count; index++)
            {
                IMMDevice? device = null;
                IPropertyStore? properties = null;
                try
                {
                    Marshal.ThrowExceptionForHR(collection.Item(index, out device));
                    Marshal.ThrowExceptionForHR(device.GetId(out var id));
                    Marshal.ThrowExceptionForHR(device.OpenPropertyStore(0, out properties));
                    var key = FriendlyNameKey;
                    Marshal.ThrowExceptionForHR(properties.GetValue(ref key, out var value));
                    try
                    {
                        var name = value.GetString();
                        result.Add(new AudioDeviceInfo { Id = id, Name = string.IsNullOrWhiteSpace(name) ? id : name, IsInput = input });
                    }
                    finally { PropVariantClear(ref value); }
                }
                finally
                {
                    Release(properties);
                    Release(device);
                }
            }
        }
        finally
        {
            Release(collection);
            Release(enumerator);
        }
        return result.OrderBy(device => device.Name, StringComparer.CurrentCultureIgnoreCase).ToList();
    }

    public static string? GetDefaultDeviceId(bool input, bool communications = false)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumeratorComObject();
            var hr = enumerator.GetDefaultAudioEndpoint(input ? DataFlow.Capture : DataFlow.Render, communications ? Role.Communications : Role.Multimedia, out device);
            if (hr < 0 || device is null) return null;
            Marshal.ThrowExceptionForHR(device.GetId(out var id));
            return id;
        }
        finally { Release(device); Release(enumerator); }
    }

    public static void Apply(AudioPreset preset)
    {
        if (!string.IsNullOrWhiteSpace(preset.OutputDeviceId)) SetDefault(preset.OutputDeviceId);
        if (!string.IsNullOrWhiteSpace(preset.InputDeviceId)) SetDefault(preset.InputDeviceId);
    }

    public static double GetOutputVolume()
    {
        return WithDefaultOutputVolume(endpoint =>
        {
            Marshal.ThrowExceptionForHR(endpoint.GetMasterVolumeLevelScalar(out var level));
            return level;
        });
    }

    public static void SetOutputVolume(double level)
    {
        WithDefaultOutputVolume(endpoint =>
        {
            var context = Guid.Empty;
            Marshal.ThrowExceptionForHR(endpoint.SetMasterVolumeLevelScalar((float)Math.Clamp(level, 0, 1), ref context));
            return 0;
        });
    }

    private static T WithDefaultOutputVolume<T>(Func<IAudioEndpointVolume, T> action)
    {
        IMMDeviceEnumerator? enumerator = null;
        IMMDevice? device = null;
        object? activated = null;
        try
        {
            enumerator = (IMMDeviceEnumerator)(object)new MMDeviceEnumeratorComObject();
            Marshal.ThrowExceptionForHR(enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia, out device));
            var interfaceId = typeof(IAudioEndpointVolume).GUID;
            Marshal.ThrowExceptionForHR(device.Activate(ref interfaceId, 23, IntPtr.Zero, out activated));
            return action((IAudioEndpointVolume)activated);
        }
        finally
        {
            Release(activated);
            Release(device);
            Release(enumerator);
        }
    }

    private static void SetDefault(string deviceId)
    {
        IPolicyConfig? policy = null;
        try
        {
            policy = (IPolicyConfig)(object)new PolicyConfigClient();
            Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(deviceId, Role.Console));
            Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(deviceId, Role.Multimedia));
            Marshal.ThrowExceptionForHR(policy.SetDefaultEndpoint(deviceId, Role.Communications));
        }
        finally { Release(policy); }
    }

    private static void Release(object? value)
    {
        if (value is not null && Marshal.IsComObject(value)) Marshal.FinalReleaseComObject(value);
    }

    [DllImport("ole32.dll")]
    private static extern int PropVariantClear(ref PropVariant value);

    private enum DataFlow { Render, Capture, All }
    private enum Role { Console, Multimedia, Communications }

    [StructLayout(LayoutKind.Sequential)]
    private readonly struct PropertyKey(Guid formatId, uint propertyId)
    {
        public readonly Guid FormatId = formatId;
        public readonly uint PropertyId = propertyId;
    }

    [StructLayout(LayoutKind.Explicit)]
    private struct PropVariant
    {
        [FieldOffset(0)] private ushort _type;
        [FieldOffset(8)] private IntPtr _pointer;
        public readonly string? GetString() => _type == 31 && _pointer != IntPtr.Zero ? Marshal.PtrToStringUni(_pointer) : null;
    }

    [ComImport, Guid("BCDE0395-E52F-467C-8E3D-C4579291692E")]
    private sealed class MMDeviceEnumeratorComObject { }

    [ComImport, Guid("A95664D2-9614-4F35-A746-DE8DB63617E6"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceEnumerator
    {
        [PreserveSig] int EnumAudioEndpoints(DataFlow dataFlow, int stateMask, out IMMDeviceCollection devices);
        [PreserveSig] int GetDefaultAudioEndpoint(DataFlow dataFlow, Role role, out IMMDevice device);
        [PreserveSig] int GetDevice([MarshalAs(UnmanagedType.LPWStr)] string id, out IMMDevice device);
        [PreserveSig] int RegisterEndpointNotificationCallback(IntPtr client);
        [PreserveSig] int UnregisterEndpointNotificationCallback(IntPtr client);
    }

    [ComImport, Guid("0BD7A1BE-7A1A-44DB-8397-CC5392387B5E"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDeviceCollection
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int Item(uint index, out IMMDevice device);
    }

    [ComImport, Guid("D666063F-1587-4E43-81F1-B948E807363F"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IMMDevice
    {
        [PreserveSig] int Activate(ref Guid interfaceId, int classContext, IntPtr activationParameters, [MarshalAs(UnmanagedType.IUnknown)] out object instance);
        [PreserveSig] int OpenPropertyStore(int accessMode, out IPropertyStore properties);
        [PreserveSig] int GetId([MarshalAs(UnmanagedType.LPWStr)] out string id);
        [PreserveSig] int GetState(out int state);
    }

    [ComImport, Guid("5CDF2C82-841E-4546-9722-0CF74078229A"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IAudioEndpointVolume
    {
        [PreserveSig] int RegisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int UnregisterControlChangeNotify(IntPtr notify);
        [PreserveSig] int GetChannelCount(out uint channelCount);
        [PreserveSig] int SetMasterVolumeLevel(float levelDb, ref Guid eventContext);
        [PreserveSig] int SetMasterVolumeLevelScalar(float level, ref Guid eventContext);
        [PreserveSig] int GetMasterVolumeLevel(out float levelDb);
        [PreserveSig] int GetMasterVolumeLevelScalar(out float level);
    }

    [ComImport, Guid("886D8EEB-8CF2-4446-8D02-CDBA1DBDCF99"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPropertyStore
    {
        [PreserveSig] int GetCount(out uint count);
        [PreserveSig] int GetAt(uint index, out PropertyKey key);
        [PreserveSig] int GetValue(ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetValue(ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int Commit();
    }

    [ComImport, Guid("870AF99C-171D-4F9E-AF0D-E63DF40C2BC9")]
    private sealed class PolicyConfigClient { }

    [ComImport, Guid("F8679F50-850A-41CF-9C72-430F290290C8"), InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPolicyConfig
    {
        [PreserveSig] int GetMixFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, out IntPtr format);
        [PreserveSig] int GetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int defaultFormat, out IntPtr format);
        [PreserveSig] int ResetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId);
        [PreserveSig] int SetDeviceFormat([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr endpointFormat, IntPtr mixFormat);
        [PreserveSig] int GetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int defaultPeriod, out long defaultValue, out long minimumValue);
        [PreserveSig] int SetProcessingPeriod([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref long period);
        [PreserveSig] int GetShareMode([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr mode);
        [PreserveSig] int SetShareMode([MarshalAs(UnmanagedType.LPWStr)] string deviceId, IntPtr mode);
        [PreserveSig] int GetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref PropertyKey key, out PropVariant value);
        [PreserveSig] int SetPropertyValue([MarshalAs(UnmanagedType.LPWStr)] string deviceId, ref PropertyKey key, ref PropVariant value);
        [PreserveSig] int SetDefaultEndpoint([MarshalAs(UnmanagedType.LPWStr)] string deviceId, Role role);
        [PreserveSig] int SetEndpointVisibility([MarshalAs(UnmanagedType.LPWStr)] string deviceId, int visible);
    }
}

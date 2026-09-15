using System.Windows;
using System.Windows.Input;

namespace VibecoreHub.Desktop.Modules.Audio;

public partial class AudioPresetEditorWindow : Window
{
    private readonly AudioPreset? _original;
    public AudioPreset? Result { get; private set; }

    public AudioPresetEditorWindow(AudioPreset? preset)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => WindowAppearance.ApplyLargeCorners(this);
        _original = preset;
        Heading.Text = preset is null ? "添加音频预设" : "编辑音频预设";
        NameBox.Text = preset?.Name ?? "";
        Loaded += (_, _) => LoadDevices();
    }

    private void LoadDevices()
    {
        try
        {
            var outputs = AudioDeviceService.GetDevices(false).ToList();
            var inputs = AudioDeviceService.GetDevices(true).ToList();
            outputs.Insert(0, new AudioDeviceInfo { Id = "", Name = "不切换输出设备", IsInput = false });
            inputs.Insert(0, new AudioDeviceInfo { Id = "", Name = "不切换输入设备", IsInput = true });
            OutputBox.ItemsSource = outputs;
            InputBox.ItemsSource = inputs;
            var outputId = _original?.OutputDeviceId ?? AudioDeviceService.GetDefaultDeviceId(false);
            var inputId = _original?.InputDeviceId ?? AudioDeviceService.GetDefaultDeviceId(true);
            OutputBox.SelectedItem = outputs.FirstOrDefault(device => device.Id == outputId) ?? outputs[0];
            InputBox.SelectedItem = inputs.FirstOrDefault(device => device.Id == inputId) ?? inputs[0];
            StatusText.Text = $"已找到 {outputs.Count - 1} 个输出、{inputs.Count - 1} 个输入；输出会同步默认通信设备";
        }
        catch (Exception exception)
        {
            StatusText.Text = $"读取设备失败：{exception.Message}";
        }
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        if (OutputBox.SelectedItem is not AudioDeviceInfo output || InputBox.SelectedItem is not AudioDeviceInfo input) return;
        var name = NameBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(name)) name = output.Id.Length > 0 ? output.Name : input.Name;
        if (string.IsNullOrWhiteSpace(output.Id) && string.IsNullOrWhiteSpace(input.Id)) return;
        Result = new AudioPreset
        {
            Id = _original?.Id ?? Guid.NewGuid().ToString("N"),
            Name = name,
            OutputDeviceId = output.Id,
            OutputDeviceName = output.Id.Length == 0 ? "不切换" : output.Name,
            InputDeviceId = input.Id,
            InputDeviceName = input.Id.Length == 0 ? "不切换" : input.Name,
            Order = _original?.Order ?? int.MaxValue
        };
        DialogResult = true;
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) => LoadDevices();
    private void Cancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    private void Title_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
}

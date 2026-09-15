using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace VibecoreHub.Desktop.Modules.Audio;

public partial class AudioSwitcherModuleView : UserControl
{
    private readonly AudioPresetState _state;
    private readonly Dictionary<string, (Button Button, TextBlock Indicator)> _presetRows = [];
    private bool _updatingVolume;

    public AudioSwitcherModuleView()
    {
        InitializeComponent();
        _state = AudioPresetStore.Load();
        Loaded += (_, _) => { BuildRows(); LoadVolume(); };
        MouseEnter += (_, _) => { UpdateActiveMarkers(); LoadVolume(); };
    }

    private void BuildRows()
    {
        PresetPanel.Children.Clear();
        _presetRows.Clear();
        foreach (var preset in _state.Presets.OrderBy(item => item.Order)) PresetPanel.Children.Add(CreatePresetButton(preset));
        var add = BaseButton();
        add.Content = new TextBlock { Text = "+", FontSize = 20, FontWeight = FontWeights.Light, Foreground = (Brush)FindResource("MutedBrush"), HorizontalAlignment = HorizontalAlignment.Center };
        add.ToolTip = "添加音频预设";
        add.Click += (_, _) => EditPreset(null);
        PresetPanel.Children.Add(add);
        UpdateActiveMarkers();
    }

    private Button BaseButton() => new()
    {
        Height = 44,
        Margin = new Thickness(0, 0, 0, 4),
        Style = (Style)FindResource("AudioPresetButton")
    };

    private Button CreatePresetButton(AudioPreset preset)
    {
        var button = BaseButton();
        button.Tag = preset;
        var content = new Grid { ClipToBounds = true };
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        content.ColumnDefinitions.Add(new ColumnDefinition());
        content.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var title = new TextBlock { Text = preset.Name, FontSize = 12, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("TextBrush"), VerticalAlignment = VerticalAlignment.Center };
        var summary = new TextBlock { Text = preset.Summary, Margin = new Thickness(12, 0, 0, 0), FontSize = 11, Foreground = (Brush)FindResource("FaintBrush"), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
        Grid.SetColumn(summary, 1);
        var indicator = new TextBlock
        {
            Text = "● 当前",
            Margin = new Thickness(8, 0, 0, 0),
            FontSize = 11,
            FontWeight = FontWeights.SemiBold,
            Foreground = (Brush)FindResource("AccentBrush"),
            VerticalAlignment = VerticalAlignment.Center,
            Visibility = Visibility.Collapsed
        };
        Grid.SetColumn(indicator, 2);
        content.Children.Add(title);
        content.Children.Add(summary);
        content.Children.Add(indicator);
        button.Content = content;
        _presetRows[preset.Id] = (button, indicator);
        button.Click += (_, _) => ApplyPreset(preset);

        var menu = new ContextMenu();
        var apply = new MenuItem { Header = "立即切换" }; apply.Click += (_, _) => ApplyPreset(preset);
        var edit = new MenuItem { Header = "编辑" }; edit.Click += (_, _) => EditPreset(preset);
        var delete = new MenuItem { Header = "删除" }; delete.Click += (_, _) => { _state.Presets.Remove(preset); SaveAndRefresh(); };
        menu.Items.Add(apply); menu.Items.Add(edit); menu.Items.Add(delete);
        button.ContextMenu = menu;
        return button;
    }

    private void ApplyPreset(AudioPreset preset)
    {
        try
        {
            AudioDeviceService.Apply(preset);
            System.Media.SystemSounds.Asterisk.Play();
            UpdateActiveMarkers();
            LoadVolume();
        }
        catch (Exception exception)
        {
            MessageBox.Show(Window.GetWindow(this), $"切换失败。设备可能已断开。\n\n{exception.Message}", "音频切换", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private void LoadVolume()
    {
        try
        {
            _updatingVolume = true;
            var value = Math.Round(AudioDeviceService.GetOutputVolume() * 100);
            VolumeSlider.Value = value;
            VolumeValue.Text = $"{value:0}%";
            VolumeSlider.IsEnabled = true;
        }
        catch
        {
            VolumeValue.Text = "--%";
            VolumeSlider.IsEnabled = false;
        }
        finally { _updatingVolume = false; }
    }

    private void VolumeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_updatingVolume || VolumeValue is null) return;
        var value = Math.Round(e.NewValue);
        VolumeValue.Text = $"{value:0}%";
        try { AudioDeviceService.SetOutputVolume(value / 100); }
        catch { VolumeValue.Text = "--%"; }
    }

    private void UpdateActiveMarkers()
    {
        string? currentOutput;
        string? currentCommunicationOutput;
        string? currentInput;
        try
        {
            currentOutput = AudioDeviceService.GetDefaultDeviceId(false);
            currentCommunicationOutput = AudioDeviceService.GetDefaultDeviceId(false, true);
            currentInput = AudioDeviceService.GetDefaultDeviceId(true);
        }
        catch { return; }

        foreach (var preset in _state.Presets)
        {
            if (!_presetRows.TryGetValue(preset.Id, out var row)) continue;
            var hasDevice = !string.IsNullOrWhiteSpace(preset.OutputDeviceId) || !string.IsNullOrWhiteSpace(preset.InputDeviceId);
            var outputMatches = string.IsNullOrWhiteSpace(preset.OutputDeviceId)
                || (preset.OutputDeviceId == currentOutput && preset.OutputDeviceId == currentCommunicationOutput);
            var inputMatches = string.IsNullOrWhiteSpace(preset.InputDeviceId) || preset.InputDeviceId == currentInput;
            var isActive = hasDevice && outputMatches && inputMatches;
            row.Indicator.Visibility = isActive ? Visibility.Visible : Visibility.Collapsed;
            row.Button.BorderBrush = (Brush)FindResource(isActive ? "AccentBrush" : "LineBrush");
            row.Button.Background = (Brush)FindResource(isActive ? "RaisedBrush" : "WindowBrush");
        }
    }

    private void EditPreset(AudioPreset? preset)
    {
        var editor = new AudioPresetEditorWindow(preset) { Owner = Window.GetWindow(this) };
        if (editor.ShowDialog() != true || editor.Result is null) return;
        if (preset is null) _state.Presets.Add(editor.Result);
        else
        {
            var index = _state.Presets.IndexOf(preset);
            if (index >= 0) _state.Presets[index] = editor.Result;
        }
        SaveAndRefresh();
    }

    private void SaveAndRefresh()
    {
        for (var index = 0; index < _state.Presets.Count; index++) _state.Presets[index].Order = index;
        AudioPresetStore.Save(_state);
        BuildRows();
    }
}

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace OpenTuningTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ClearGreetingCommand))]
    private string userName = string.Empty;

    [ObservableProperty]
    private string greeting = "Enter your name and click Generate.";

    public IRelayCommand GenerateGreetingCommand { get; }

    public IRelayCommand ClearGreetingCommand { get; }

    public MainViewModel()
    {
        GenerateGreetingCommand = new RelayCommand(GenerateGreeting);
        ClearGreetingCommand = new RelayCommand(ClearGreeting, CanClearGreeting);
    }

    partial void OnUserNameChanged(string value)
    {
        ClearGreetingCommand.NotifyCanExecuteChanged();
    }

    private void GenerateGreeting()
    {
        Greeting = string.IsNullOrWhiteSpace(UserName)
            ? "Please type a name first."
            : $"Ready to tune, {UserName.Trim()}.";
    }

    private bool CanClearGreeting()
    {
        return !string.IsNullOrWhiteSpace(UserName) || Greeting != "Enter your name and click Generate.";
    }

    private void ClearGreeting()
    {
        UserName = string.Empty;
        Greeting = "Enter your name and click Generate.";
    }
}

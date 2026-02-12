using OpenTuningTool.ViewModels;

namespace OpenTuningTool;

public partial class Form1 : Form
{
    private readonly MainViewModel viewModel;

    public Form1(MainViewModel viewModel)
    {
        this.viewModel = viewModel;
        InitializeComponent();
        BindViewModel();
        WireCommands();
    }

    private void BindViewModel()
    {
        txtUserName.DataBindings.Add("Text", viewModel, nameof(MainViewModel.UserName), false, DataSourceUpdateMode.OnPropertyChanged);
        lblGreeting.DataBindings.Add("Text", viewModel, nameof(MainViewModel.Greeting), false, DataSourceUpdateMode.OnPropertyChanged);
    }

    private void WireCommands()
    {
        btnGenerate.Click += (_, _) => viewModel.GenerateGreetingCommand.Execute(null);
        btnClear.Click += (_, _) => viewModel.ClearGreetingCommand.Execute(null);

        btnClear.Enabled = viewModel.ClearGreetingCommand.CanExecute(null);
        viewModel.ClearGreetingCommand.CanExecuteChanged += (_, _) =>
        {
            btnClear.Enabled = viewModel.ClearGreetingCommand.CanExecute(null);
        };
    }
}

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using checkers.ViewModels;

namespace checkers.Views
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
#if DEBUG
            this.AttachDevTools();
#endif
            // Explicitly set the DataContext to ensure it's properly initialized
            DataContext = new GameViewModel();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
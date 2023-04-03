using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using FarmSimMissingMods.ViewModel;

namespace FarmSimMissingMods.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new MainViewModel();
            DataContext = viewModel;
            
            viewModel.ExitRequestedEvent += ViewModelOnExitRequestedEvent;
        }

        private void ViewModelOnExitRequestedEvent(object sender, EventArgs e)
        {
            Close();
        }

        private void ScrollViewer_OnPreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            var scv = (ScrollViewer)sender;
            scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
            e.Handled = true;
        }
    }
}
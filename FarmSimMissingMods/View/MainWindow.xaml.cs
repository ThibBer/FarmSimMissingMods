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
        private MainViewModel m_MainViewModel;
        
        public MainWindow()
        {
            InitializeComponent();
            m_MainViewModel = new MainViewModel();
            DataContext = m_MainViewModel;
            
            m_MainViewModel.ExitRequestedEvent += ViewModelOnExitRequestedEvent;
        }

        protected override void OnClosed(EventArgs e)
        {
            m_MainViewModel.ExitRequestedEvent -= ViewModelOnExitRequestedEvent;
            m_MainViewModel.Dispose();
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
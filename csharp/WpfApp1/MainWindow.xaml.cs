using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private string _ImageFilePath;
        public string ImageFilePath
        {
            get
            {
                return _ImageFilePath;
            }
            set
            {
                _ImageFilePath = value;
                OnPropertyChanged<string>();
            }
        }

        private string _subscriptionKey;
        public string SubscriptionKey
        {
            get
            {
                return _subscriptionKey;
            }
            set
            {
                _subscriptionKey = value;
                OnPropertyChanged<string>();
            }
        }

        private string _subscriptionEndpoint;
        public string SubscriptionEndpoint
        {
            get
            {
                return _subscriptionEndpoint;
            }
            set
            {
                _subscriptionEndpoint = value;
                OnPropertyChanged<string>();
            }
        }


        // Create the OnPropertyChanged method to raise the event
        public void OnPropertyChanged<T>([CallerMemberName]string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }


        public MainWindow()
        {
            InitializeComponent();
        }

        private void btnOpenFiles_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Multiselect = false;
            openFileDialog.Filter = "Picture files (*.jpg)|*.jpg|All files (*.*)|*.*";
            openFileDialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            if (openFileDialog.ShowDialog() == true)
            {
                ImageFilePath = openFileDialog.FileName.ToString();
                ImageFilePathTextBox.Text = ImageFilePath;
            }
        }

        private void btnSolveIt(object sender, RoutedEventArgs e)
        {
            ImageFilePath = @"C:\Users\yuqyang\Desktop\example.jpg";

            solver obj = new solver();
            obj.SolveThePuzzle(ImageFilePath, SubscriptionKey, SubscriptionEndpoint);
        }

    }
    
}

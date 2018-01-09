using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using WpfApp1.ViewModels;
using WpfApp1.Models;
using Microsoft.Win32;

using Newtonsoft.Json;

namespace WpfApp1.ViewModels
{
    public class ConfigureControlView : ObservableObject
    {
        private readonly string CfgFile = "solver.ini";

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
                RaisePropertyChangedEvent("ImageFilePath");
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
                RaisePropertyChangedEvent("SubscriptionKey");
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
                RaisePropertyChangedEvent("SubscriptionEndpoint");
            }
        }

        private string _resultMessage;
        public string ResultMessage
        {
            get
            {
                return _resultMessage;
            }
            set
            {
                _resultMessage = value;
                RaisePropertyChangedEvent("ResultMessage");
            }
        }

        public ICommand BtnOpenFiles_Click
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    OpenFileDialog openFileDialog = new OpenFileDialog
                    {
                        Multiselect = false,
                        Filter = "Picture files (*.jpg)|*.jpg|All files (*.*)|*.*",
                        InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
                    };
                    if (openFileDialog.ShowDialog() == true)
                    {
                        ImageFilePath = openFileDialog.FileName.ToString();
                    }
                });
            }
        }

        public ICommand BtnSolveIt_Click
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    StoreCfgToFile(CfgFile);
                    Solver obj = new Solver();
                    ResultMessage = obj.SolveThePuzzle(ImageFilePath, SubscriptionKey, SubscriptionEndpoint);
                });
            }
        }

        public ICommand BtnRestore_Click
        {
            get
            {
                return new DelegateCommand(() =>
                {
                    if (File.Exists(CfgFile))
                    {
                        ReStoreCfgFromFile(CfgFile);
                    }
                });
            }
        }

        public void StoreCfgToFile(string fileName)
        {
            var jsonList = new List<string>
            {
                SubscriptionKey, SubscriptionEndpoint, ImageFilePath
            };
            File.WriteAllText(fileName, JsonConvert.SerializeObject(jsonList));
        }

        public void ReStoreCfgFromFile(string fileName)
        {
            var jsonList = JsonConvert.DeserializeObject<List<string>>(File.ReadAllText(fileName));
            if (jsonList != null)
            {
                SubscriptionKey = jsonList[0];
                SubscriptionEndpoint = jsonList[1];
                ImageFilePath = jsonList[2];
            }
        }
    }
}

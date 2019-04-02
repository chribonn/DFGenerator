using System;
using System.IO;
using System.Collections.Generic;
using System.Windows;
using System.ComponentModel;
using System.Windows.Forms;
using System.Reflection;
using System.Deployment.Application;

namespace DFGenerator
{
    // ACB - 201903
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private BackgroundWorker m_oWorker;

        public MainWindow()
        {
            InitializeComponent();

            // Populate the product version number
            txtVersion.Text = GetDFGenVer();

            m_oWorker = new BackgroundWorker();

            // Create a background worker thread that ReportsProgress &
            // SupportsCancellation
            // Hook up the appropriate events.
            m_oWorker.DoWork += new DoWorkEventHandler(m_oWorker_DoWork);
            m_oWorker.ProgressChanged += new ProgressChangedEventHandler
                    (m_oWorker_ProgressChanged);
            m_oWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler
                    (m_oWorker_RunWorkerCompleted);
            m_oWorker.WorkerReportsProgress = true;
            m_oWorker.WorkerSupportsCancellation = true;
        }
        // For each file in the directory, extract the extention and ennumerate on it.
        private IDictionary<string, string> BuldDummyFiles(string dummyFileDir)
        {
            string fileExt;
            var dumFile = new Dictionary<string, string>();
            DirectoryInfo d = new DirectoryInfo(dummyFileDir.ToLower());
            FileInfo[] Files = d.GetFiles("*.*");
            foreach (FileInfo file in Files)
            {
                fileExt = System.IO.Path.GetExtension(file.Name);

                // Add if file extension doesn't already exist
                if (!dumFile.ContainsKey(fileExt))
                {
                    dumFile.Add(fileExt, file.Name.ToString());
                }
            }

            return dumFile;
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            // Create OpenFileDialog 
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();

            // Set filter for file extension and default file extension 
            dlg.DefaultExt = ".log";
            dlg.Filter = "LOG Files (*.log)|*.log|All Files (*.*)|*.*";


            // Display OpenFileDialog by calling ShowDialog method 
            Nullable<bool> result = dlg.ShowDialog();


            // Get the selected file name and display in a TextBox 
            if (result == true)
            {
                // Open document 
                string filename = dlg.FileName;
                txtLogFile.Text = filename;
            }
        }

        private void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtLogFile.Text))
            {
                System.Windows.MessageBox.Show("Specify the log file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtDummyDir.Text))
            {
                System.Windows.MessageBox.Show("Specify the location of the dummy files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (string.IsNullOrWhiteSpace(txtSrchTree.Text))
            {
                System.Windows.MessageBox.Show("The Search Path cannot be left blank.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            

            if (string.IsNullOrWhiteSpace(txtRepTree.Text))
            {
                System.Windows.MessageBox.Show("The Replace Path cannot be left blank.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            // Ensure that the last character is a slash
           if (txtSrchTree.Text.Substring(txtSrchTree.Text.Length - 1) != "\\")
                txtSrchTree.Text += "\\";

            // Ensure that the last character is a slash
            if (txtRepTree.Text.Substring(txtRepTree.Text.Length - 1) != "\\")
                 txtRepTree.Text += "\\";

            //Change the status of the buttons on the UI accordingly
            //The start button is disabled as soon as the background operation is started
            //The Cancel button is enabled so that the user can stop the operation 
            //at any point of time during the execution
            btnStart.IsEnabled = false;
            btnOpen.IsEnabled = false;
            btnCancel.IsEnabled = true;

            List<object> arguments = new List<object>();
            arguments.Add(txtLogFile.Text.Trim());
            arguments.Add(txtDummyDir.Text.Trim());
            arguments.Add(txtSrchTree.Text.Trim());
            arguments.Add(txtRepTree.Text.Trim());

            if (m_oWorker.IsBusy != true)
            {
                m_oWorker.RunWorkerAsync(arguments);
            }
            
        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            if (m_oWorker.IsBusy)
            {

                // Notify the worker thread that a cancel has been requested.
                // The cancel will not actually happen until the thread in the
                // DoWork checks the m_oWorker.CancellationPending flag. 

                m_oWorker.CancelAsync();
            }
        }

        private void BtnDummyDir_Click(object sender, RoutedEventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                DialogResult result = fbd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                {
                    txtDummyDir.Text = fbd.SelectedPath.ToString();
                }
            }
        }

        private string GetDFGenVer()
        {
                return ApplicationDeployment.IsNetworkDeployed
                       ? ApplicationDeployment.CurrentDeployment.CurrentVersion.ToString()
                       : Assembly.GetExecutingAssembly().GetName().Version.ToString();
        }
    }
}

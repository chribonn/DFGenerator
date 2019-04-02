using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.ComponentModel;

namespace DFGenerator
{
    public partial class MainWindow
    {

        /// <summary>
        /// On completed do the appropriate task
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_oWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // The background process is complete. We need to inspect
            // our response to see if an error occurred, a cancel was
            // requested or if we completed successfully.  
            if (e.Cancelled)
            {
                Output.Text = "Task Cancelled.";
            }

            // Check to see if an error occurred in the background process.

            else if (e.Error != null)
            {
                Output.Text = "Error while performing background operation.";
            }
            else
            {
                // Everything completed normally.
                //Extract the log file name
                string errFileName = e.Result.ToString();
                Output.Text = "Task Completed. Check log file " + errFileName + " for errors.";
            }

            //Change the status of the buttons on the UI accordingly
            btnStart.IsEnabled = true;
            btnCancel.IsEnabled = false;
        }

        /// <summary>
        /// Notification is performed here to the progress bar
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_oWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {

            // This function fires on the UI thread so it's safe to edit
            // the UI control directly, no funny business with Control.Invoke :)
            // Update the progressBar with the integer supplied to us from the
            // ReportProgress() function.  
            progBar.Value = e.ProgressPercentage;
            Output.Text = "Processing......" + progBar.Value.ToString() + "%";
        }

        /// <summary>
        /// Time consuming operations go here </br>
        /// i.e. Database operations,Reporting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void m_oWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            List<object> argumentList = e.Argument as List<object>;
            string logFileName = (string)argumentList[0];
            string dummyFileDir = (string)argumentList[1];
            string srchStr = (string)argumentList[2];
            string replStr = (string)argumentList[3];
            string fileName = "", fileExt = "", fileDir = "", newPath = "", dummyPath = "";
            int counter = 0;

            var dummyFileList = BuldDummyFiles(dummyFileDir);

            // Geherate the name of the log file for errors. Delete it if it already exiss
            string errFileName = Path.Combine(Path.GetDirectoryName(logFileName), DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log");
            // Push the file name to the RunWorkerCompletedEventArgs
            e.Result = errFileName;
            // Check if file already exists. If yes, delete it.     
            if (File.Exists(errFileName))
            {
                File.Delete(errFileName);
            }
            // Create a file     
            StreamWriter fs = new StreamWriter(errFileName);

            logFileName = logFileName.Trim();
            srchStr = srchStr.Trim().ToLower();
            string[] srchStrArray = new string[] { srchStr };
            int srchLen = srchStr.Length;
            replStr = replStr.Trim();

            string line;

            // Get the number of lines in the logfile
            var totLines = File.ReadLines(@logFileName).Count();

            // Read the file and display it line by line.  
            System.IO.StreamReader file =
                new System.IO.StreamReader(@logFileName);
            while ((line = file.ReadLine()) != null)
            {
                m_oWorker.ReportProgress(counter++ * 100 / totLines);
                if (line.Substring(0, srchLen).ToLower() == srchStr)
                {
                    try
                    {
                        fileName = System.IO.Path.GetFileName(line);
                        fileExt = System.IO.Path.GetExtension(line).ToLower();
                        fileDir = System.IO.Path.GetDirectoryName(line);
                    }
                    catch (Exception ex)
                    {
                        fs.Write("Line " + counter.ToString() + ": Unable to process source dir " + line + "\n");
                    }

                    if (dummyFileList.ContainsKey(fileExt))
                    {
                        string[] parts = fileDir.ToLower().Split(srchStrArray, StringSplitOptions.None);
                        newPath = replStr;
                        for (int i = 1; i < parts.Length; i++)
                        {
                            newPath += parts[i];
                        }

                        // Create the directory if necessary
                        if (!System.IO.Directory.Exists(newPath))
                        {
                            try
                            {
                                System.IO.Directory.CreateDirectory(newPath);
                            }
                            catch (Exception ex)
                            {
                                fs.Write("Line " + counter.ToString() + ": Unable to create directory " + newPath + "\n");
                            }
                        }

                        // Build the new file name
                        newPath = Path.Combine(newPath, fileName);
                        // Generate the dummy path
                        dummyPath = Path.Combine(dummyFileDir, dummyFileList[fileExt]);
                        // copy the dummy file to the new location
                        try
                        {
                            System.IO.File.Copy(dummyPath, newPath, true);
                        }
                        catch (Exception ex)
                        {
                            fs.Write("Line " + counter.ToString() + ": Unable to copy " + dummyPath +
                                " -> " + newPath + "\n");
                        }
                    }
                    else
                    {
                        fs.Write("Line " + counter.ToString() + ": " + "File extension " + fileExt + " not catered for\n");
                    }

                    // Periodically check if a cancellation request is pending.
                    // If the user clicks cancel the line
                    // m_AsyncWorker.CancelAsync(); if ran above.  This
                    // sets the CancellationPending to true.
                    // You must check this flag in here and react to it.
                    // We react to it by setting e.Cancel to true and leaving
                    if (m_oWorker.CancellationPending)
                    {
                        // Set the e.Cancel flag so that the WorkerCompleted event
                        // knows that the process was cancelled.
                        e.Cancel = true;
                        m_oWorker.ReportProgress(0);
                        return;
                    }

                }
            }

            // Close the file stream
            fs.Flush();
            fs.Close();

            //Report 100% completion on operation completed
            m_oWorker.ReportProgress(100);
        }

    }
}

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;

namespace Keep_Disk_On
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        int deley = 60000;
        bool stop = false;
        bool closing = false;
        AutoResetEvent wake = new AutoResetEvent(false);
        BackgroundWorker worker;

        FileInfo file;

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            string ex;
            if (!GetDives(out ex))
            {
                MessageBox.Show("Error\n" + ex);
                Application.Current.Shutdown();
            }
            update_tbx.Text = "1m";
        }

        private bool GetDives(out string err)
        {
            try
            {
                var drives = DriveInfo.GetDrives();
                drive_cbx.Items.Clear();
                foreach (var i in drives)
                    if (i.IsReady && (i.DriveType == DriveType.Fixed || i.DriveType == DriveType.Removable || i.DriveType == DriveType.Ram))
                        drive_cbx.Items.Add(MakeCBXItem(i));
                err = "";
                return true;
            }
            catch (Exception ex)
            {
                err = ex.Message;
                return false;
            }
        }

        private object MakeCBXItem(DriveInfo di)
        {
            ComboBoxItem it = new ComboBoxItem();
            it.Content = di.Name;
            it.Tag = di;
            return it;
        }

        private void Refresh_Btm_Click(object sender, RoutedEventArgs e)
        {
            string ex;
            if (!GetDives(out ex))
            {
                MessageBox.Show("Error\n" + ex);
            }
        }

        private void Update_Tbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            int multi = 1;
            int i;
            string x = "";
            bool has_dot = false;

            for (i = 0; i < update_tbx.Text.Length; i++)
            {
                if ((update_tbx.Text[i] > 47 && update_tbx.Text[i] < 58) || (!has_dot && update_tbx.Text[i] == 46))     //0 and 9  &   .
                {
                    x += update_tbx.Text[i];
                    if (update_tbx.Text[i] == 46)
                        has_dot = true;
                }
                else
                {
                    switch (update_tbx.Text[i])
                    {
                        case 's':
                        case 'S':
                            break;
                        case 'm':
                        case 'M':
                            multi = 60;
                            break;
                        case 'h':
                        case 'H':
                            multi = 3600;
                            break;
                        case 'd':
                        case 'D':
                            multi = 86400;
                            break;
                        default:
                            multi = -1;
                            break;
                    }
                    break;
                }
            }
            double xx;
            double.TryParse(x, NumberStyles.Float, CultureInfo.InvariantCulture, out xx);
            xx *= multi;
            deley = 0;
            if (i < update_tbx.Text.Length - 1)
                secound_count_tbx.Text = "Invalid input";
            else
            {
                if (multi > 0)
                    if (xx > 86400)
                        secound_count_tbx.Text = "Too long period.";
                    else if (xx < 1)
                        secound_count_tbx.Text = "Too short period.";
                    else
                    {
                        secound_count_tbx.Text = " = " + xx + " second" + (xx == 1 ? "" : "s");
                        deley = (int)(xx * 1000);
                    }
                else
                    secound_count_tbx.Text = "Invalid input";
            }
        }

        private void Start_Btm_Click(object sender, RoutedEventArgs e)
        {
            if (drive_cbx.SelectedIndex > -1)
            {
                if (deley <= 0)
                {
                    MessageBox.Show("Invalid update period\nuse 1 second to 1 day, for example 30s, 1m or 1.5h");
                    return;
                }
                drive_cbx.IsEnabled = refresh_btm.IsEnabled = update_tbx.IsEnabled = start_btm.IsEnabled = false;
                stop_btm.IsEnabled = true;
                BinaryWriter BW = null;
                file = null;
                try
                {
                    string file_name = GetFileName((drive_cbx.SelectedItem as ComboBoxItem).Content.ToString());
                    BW = new BinaryWriter(File.Create(file_name, 4096, FileOptions.WriteThrough));
                    file = new FileInfo(file_name);
                    file.Attributes = (FileAttributes)6;
                }
                catch (Exception ex)
                {
                    try
                    {
                        if (BW != null)
                            BW.Close();
                        if (file != null)
                            file.Delete();
                    }
                    catch (Exception) { }
                    MessageBox.Show("Error\n" + ex.Message);
                    drive_cbx.IsEnabled = refresh_btm.IsEnabled = update_tbx.IsEnabled = start_btm.IsEnabled = true;
                    stop_btm.IsEnabled = false;
                    return;
                }
                stop = false;
                wake.Reset();
                worker = new BackgroundWorker();

                worker.DoWork += (object bw_sender, DoWorkEventArgs args) =>
                {
                    byte[] block = new byte[1048576];
                    Random r = new Random();
                    for (int i = 0; i < block.Length; i++)
                        block[i] = (byte)r.Next(0, 255);
                    try
                    {
                        while (!stop)
                        {
                            for (int i = 0; i < block.Length - 1; i += 32768)
                                BW.Write(block, i, 32768);
                            BW.Flush();
                            (BW.BaseStream as FileStream).Flush(true);
                            BW.BaseStream.Position = 0;
                            wake.WaitOne(deley);
                        }
                    }
                    finally
                    {
                        try
                        {
                            BW.Close();
                        }
                        catch (Exception) { }
                        try
                        {
                            file.Delete();
                        }
                        catch (Exception) { }
                    }
                };
                worker.RunWorkerCompleted += Worker_RunWorkerCompleted;

                worker.RunWorkerAsync();
            }
            else MessageBox.Show("select disk drive first");
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            worker = null;
            if (closing)
            {
                Close();
                return;
            }
            if (e.Error != null)
                MessageBox.Show("Error\n" + e.Error.Message);
            drive_cbx.IsEnabled = refresh_btm.IsEnabled = update_tbx.IsEnabled = start_btm.IsEnabled = true;
            stop_btm.IsEnabled = false;
        }

        private void Stop_Btm_Click(object sender, RoutedEventArgs e)
        {
            stop_btm.IsEnabled = false;
            stop = true;
            wake.Set();
        }

        private string GetFileName(string v)
        {
            return Path.Combine(v, Path.GetRandomFileName());
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (worker != null)
            {
                e.Cancel = true;
                closing = true;
                stop_btm.IsEnabled = false;
                stop = true;
                wake.Set();
            }
        }

    }
}

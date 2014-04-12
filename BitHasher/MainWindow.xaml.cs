using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using MahApps.Metro.Controls;
using Microsoft.Win32;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Interop;
using System.Windows.Shell;

//test commit

namespace BitHasher
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : MetroWindow
    {
        public List<Checksum> checksums = new List<Checksum>(){
            new Checksum("Message Digest 5", "MD5", delegate(){
                return MD5.Create();
            }),
            new Checksum("Secure Hash Algorithm 160bit", "SHA-1", delegate(){
                return new SHA1Managed();
            }),
            new Checksum("Secure Hash Algorithm 256bit", "SHA-256", delegate(){
                return new SHA256Managed();
            }),
            new Checksum("Secure Hash Algorithm 384bit", "SHA-384", delegate(){
                return new SHA384Managed();
            }),
            new Checksum("Secure Hash Algorithm 512bit", "SHA-512", delegate(){
                return new SHA512Managed();
            }),
            new Checksum("Cyclic Redundancy Check 32", "CRC32", delegate(){
                return new CRC32();
            })
        };

        public MainWindow()
        {
            InitializeComponent();

            comboBoxAlgorithm.ItemsSource = checksums;
            comboBoxAlgorithm.DisplayMemberPath = "Abbreviation";
            comboBoxAlgorithm.SelectedValuePath = "Function";
            comboBoxAlgorithm.SelectedIndex = 0;
        }

        private void MetroWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop, true))
            {
                textBoxFileName.Text = ((string[])e.Data.GetData(DataFormats.FileDrop))[0];
                computeHash();
            }
        }

        private void hashIt(String file, HashAlgorithm hasher, Action<double> progress, Action<string> callback)
        {
            if (!File.Exists(file))
                return;


            new Thread(delegate()
            {
                Stream stream = File.OpenRead(file);
                byte[] buffer;

                do
                {
                    buffer = new byte[stream.Length / 100];
                    hasher.TransformBlock(buffer, 0, stream.Read(buffer, 0, buffer.Length), null, 0);

                    this.Dispatcher.Invoke(delegate() { progress((double)stream.Position / stream.Length * 100); });
                }
                while (stream.Length != stream.Position);

                hasher.TransformFinalBlock(buffer, 0, 0);

                stream.Close();
                this.Dispatcher.Invoke(delegate() { callback(BitConverter.ToString(hasher.Hash).Replace("-", "")); });
            }).Start();
        }

        private void computeHash()
        {

            taskBarItemInfo.ProgressState = TaskbarItemProgressState.Normal;
            hashIt(textBoxFileName.Text, checksums[comboBoxAlgorithm.SelectedIndex].GetHasher(), delegate(double progress)
            {
                progressBar.Value = (double)progress;
                taskBarItemInfo.ProgressValue = (double)progress / 100;
            }, delegate(string hash)
            {
                textBoxHash.Text = hash;
                comboBoxAlgorithm.IsEnabled = true;
                taskBarItemInfo.ProgressState = TaskbarItemProgressState.None;
            });
        }

        private void comboBoxChecksum_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            labelAlgorithm.Content = checksums[comboBoxAlgorithm.SelectedIndex].Algorithm;
            computeHash();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (textBoxVerifyHash.Text.Length == textBoxHash.Text.Length && textBoxVerifyHash.Text != "")
            {
                if (Regex.Replace(textBoxVerifyHash.Text, @"\W", "") == textBoxHash.Text)
                {
                    groupBox.Background = new SolidColorBrush(Color.FromArgb((byte)204, (byte)17, (byte)218, (byte)54));
                    labelResult.Content = "checksums OK";
                }
                else
                {
                    groupBox.Background = new SolidColorBrush(Color.FromArgb((byte)204, (byte)255, (byte)0, (byte)0));
                    labelResult.Content = "checksums mismatch";
                }
            }
            else
            {
                groupBox.Background = new SolidColorBrush(Color.FromArgb((byte)204, (byte)17, (byte)158, (byte)255));
                labelResult.Content = "";
            }
            groupBox.BorderBrush = groupBox.Background;
        }

        public class Checksum
        {
            public string Algorithm { get; set; }
            public string Abbreviation { get; set; }
            public Func<HashAlgorithm> GetHasher { get; set; }

            public Checksum(string Algorithm, string Abbreviation, Func<HashAlgorithm> GetHasher)
            {
                this.Algorithm = Algorithm;
                this.Abbreviation = Abbreviation;
                this.GetHasher = GetHasher;
            }
        }

        private void buttonGithub_Click(object sender, RoutedEventArgs e)
        {
            Process.Start("https://github.com/NameLess-exe");
        }

        private void buttonExport_Click(object sender, RoutedEventArgs e)
        {
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.Title = "Select directory to save hashes...";
            dialog.FileName = "checksums for " + System.IO.Path.GetFileName(textBoxFileName.Text) + ".txt";
            dialog.Filter = "Text documents (.txt)|*.txt";
            dialog.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);

            if ((bool)dialog.ShowDialog())
            {
                foreach (Checksum checksum in checksums)
                {
                    hashIt(textBoxFileName.Text, checksum.GetHasher(), delegate(double progress)
                    {
                    }, delegate(string hash)
                    {
                        StreamWriter file = File.AppendText(dialog.FileName);
                        file.WriteLine(string.Format("{0} ({1}) - {2}", checksum.Algorithm, checksum.Abbreviation, hash));
                        file.Close();
                    });
                }
            }
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace Diplom
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            
            worker.DoWork += new DoWorkEventHandler(worker_DoWork);
            worker.ProgressChanged += new ProgressChangedEventHandler(worker_ProgressChanged);
            worker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(worker_RunWorkerCompleted);
        }

        private int _index_now = -1;
        private List<Image> _images = new List<Image>();

        private void open_img_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
            openFileDialog.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.png; *.bmp; *.webp)|*.jpg; *.jpeg; *.gif; *.png; *.bmp;*.jpeg; *.webp|All files (*.*)|*.*";
            if (openFileDialog.ShowDialog() == true)
            {
                Uri fileUri = new Uri(openFileDialog.FileName);
                add_img(new BitmapImage(fileUri));
            }
        }
        private void save_img_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.InitialDirectory = System.Reflection.Assembly.GetExecutingAssembly().Location;
            saveFileDialog.Filter = "Image Files(*.jpg; *.jpeg; *.gif; *.png; *.bmp; *.webp)|*.jpg; *.jpeg; *.gif; *.png; *.bmp;*.jpeg; *.webp|All files (*.*)|*.*";
            if (saveFileDialog.ShowDialog() == true)
            {
                JpegBitmapEncoder jpegBitmapEncoder = new JpegBitmapEncoder();
                jpegBitmapEncoder.Frames.Add(BitmapFrame.Create(_images[_index_now].Source as BitmapSource));
                using (FileStream fileStream = new FileStream(saveFileDialog.FileName, FileMode.Create))
                    jpegBitmapEncoder.Save(fileStream);
            }
        }
        private void add_img(BitmapSource bitmapSource)
        {
            _index_now++;
            _images.Insert(_index_now, new Image());
            _images[_index_now].Source = bitmapSource;
            _img_now.Source = _images[_index_now].Source;

            check_index();
            check_count_is_not_null();
        }
        private void close_img_Click(object sender, RoutedEventArgs e)
        {
            _images.RemoveAt(_index_now);
            if (_index_now == _images.Count)
                _index_now--;
            if (check_count_is_not_null())
                _img_now.Source = _images[_index_now].Source;
            else
                _img_now.Source = null;
            check_index();
        }

        private void check_index()
        {
            if (_index_now < 1)
                button_last_img.IsEnabled = false;
            else button_last_img.IsEnabled = true;

            if (_index_now == _images.Count - 1)
                button_next_img.IsEnabled = false;
            else button_next_img.IsEnabled = true;
        }
        private bool check_count_is_not_null()
        {
            if (_images.Count == 0)
            {
                close_img.IsEnabled = false;
                save_img.IsEnabled = false;
                menu_transform.IsEnabled = false;
                return false;
            }
            else
            {
                close_img.IsEnabled = true;
                save_img.IsEnabled = true;
                menu_transform.IsEnabled = true;
                return true;
            }
        } 

        private void button_last_img_Click(object sender, RoutedEventArgs e)
        {
            _index_now--;
            _img_now.Source = _images[_index_now].Source;
            check_index();
        }
        private void button_next_img_Click(object sender, RoutedEventArgs e)
        {
            _index_now++;
            _img_now.Source = _images[_index_now].Source;
            check_index();
        }

        private void _img_now_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(e.ClickCount == 2)
            {
                WindowImage wi = new WindowImage(_img_now);
                wi.Show();
            }
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.O && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                open_img_Click(sender, e);
            }
            if (e.Key == Key.S && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                save_img_Click(sender, e);
            }
            if (e.Key == Key.Delete)
            {
                close_img_Click(sender, e);
            }

            if (!menu_file.IsChecked && !menu_transform.IsChecked) {
                if (e.Key == Key.Left && _index_now != 0)
                {
                    button_last_img_Click(sender, e);
                }
                else if (e.Key == Key.Right && _index_now != _images.Count - 1)
                {
                    button_next_img_Click(sender, e);
                }
            }
        }

        BackgroundWorker worker = new BackgroundWorker() { WorkerSupportsCancellation = true, WorkerReportsProgress = true };
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            System.Diagnostics.Stopwatch myStopwatch = new System.Diagnostics.Stopwatch();
            myStopwatch.Start();
            Work();
            myStopwatch.Stop();
            e.Result = myStopwatch.Elapsed.TotalSeconds;
        }
        private void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            wpb.ChangePrBar(e.ProgressPercentage);
        }
        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            wpb.WriteResults((double)e.Result);
            Finish();
        }

        private string transform;
        private double[] Params;
        private WindowProgressBar wpb;
        private ImgTransform imgTransform;
        private void Start(string _transform)
        {
            transform = _transform;

            menu_transform.IsEnabled = false;
            button_last_img.IsEnabled = false;
            button_next_img.IsEnabled = false;

            imgTransform = new ImgTransform(_img_now.Source as BitmapSource, worker);
            wpb = new WindowProgressBar(imgTransform.GetCountPixels());
            wpb.Owner = this;
            wpb.Show();
            worker.RunWorkerAsync();
        }
        private void Work()
        {
            switch (transform)
            {
                case "Fourier_freq":
                    imgTransform.Fourier_freq(Params[0]);//k brightness
                    break;

                case "change_freq":
                    imgTransform.change_freq((int)Params[0], Params[1], (int)Params[2]);//r, k, freq (0 - low, 1 - high)
                    break;

                case "Box":
                    for (int i = 0; i < Params[0]; i++)
                    {
                        imgTransform.Blur_Box((int)Params[1]);//size of mask k
                    }
                    break;

                case "Median":
                    for (int i = 0; i < Params[0]; i++)
                    {
                        imgTransform.Blur_Median();
                    }
                    break;

                case "Gauss":
                    imgTransform.SetMask(Params[1]);//sigma
                    for (int i = 0; i < Params[0]; i++)
                    {
                        imgTransform.Blur_Gauss();
                    }
                    break;

                case "Filter1":
                    for (int i = 0; i < Params[0]; i++)
                    {
                        imgTransform.Sharp_Filter1();
                    }
                    break;

                case "Filter2":
                    for (int i = 0; i < Params[0]; i++)
                    {
                        imgTransform.Sharp_Filter2();
                    }
                    break;

                case "Laplas":
                    imgTransform.Laplasian();
                    break;

                case "add_Laplas":
                    imgTransform.AddLaplasian();
                    break;

                case "Embossing":
                    imgTransform.Embossibg();
                    break;
            }
        }
        private void Finish()
        {
            add_img(imgTransform.GetImage());
        }

        private void freq_img_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                double k = Convert.ToDouble(txt_Fourier_freq_k.Text);
                if (k < 0) throw new Exception("less 0");
                Params = new double[] { k };
                Start("Fourier_freq");
            }
            catch (Exception ex)
            {
                if (ex.Message == "less 0")
                    MessageBox.Show("Коэффициент k не должен быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void change_freq_img_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n;
                double k;
                int is_high;
                if ((sender as MenuItem).Name == change_freq_high.Name)
                {
                    is_high = 1;
                    n = Convert.ToInt32(txt_change_high_freq_n.Text);
                    k = Convert.ToDouble(txt_change_high_freq_k.Text);
                }
                else
                {
                    is_high = 0;
                    n = Convert.ToInt32(txt_change_low_freq_n.Text);
                    k = Convert.ToDouble(txt_change_low_freq_k.Text);
                }

                if (n < 0) throw new Exception("n less 0");
                if (k < 0) throw new Exception("k less 0");

                Params = new double[] { n, k, is_high };
                Start("change_freq");
            }
            catch (Exception ex)
            {
                if (ex.Message == "n less 0")
                    MessageBox.Show("Значение n не должно быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else if (ex.Message == "k less 0")
                    MessageBox.Show("Коэффициент k не должен быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void blur_img_median_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = Convert.ToInt32(txt_median_n.Text);
                if (n < 0) throw new Exception("less 0");
                Params = new double[] { n };
                Start("Median");
            }
            catch (Exception ex)
            {
                if (ex.Message == "less 0")
                    MessageBox.Show("Значение n не должно быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void blur_img_box_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = Convert.ToInt32(txt_box_n.Text);
                int k = Convert.ToInt32(txt_box_k.Text);
                if (n < 0 || k < 0) throw new Exception("less 0");
                if (k %2 == 0) throw new Exception("not odd");
                Params = new double[] { n, k };
                Start("Box");
            }
            catch (Exception ex)
            {
                if (ex.Message == "less 0")
                    MessageBox.Show("Значения k и n не должны быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                if (ex.Message == "not odd")
                    MessageBox.Show("Значение k длжно быть нечетным", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void blur_img_gauss_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = Convert.ToInt32(txt_Gauss_n.Text);
                if (n < 0) throw new Exception("less 0");
                double sigma = Convert.ToDouble(txt_Gauss_sgm.Text);
                if (sigma < 0) throw new Exception("sigma less 0");
                Params = new double[] { n, sigma };
                Start("Gauss");
            }
            catch (Exception ex)
            {
                if (ex.Message == "less 0")
                    MessageBox.Show("Значение n не должно быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error); 
                if (ex.Message == "sigma less 0")
                    MessageBox.Show("Значение σ не должно быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void sharp_img_filter1_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = Convert.ToInt32(txt_sharp_filter1_n.Text);
                if (n < 0) throw new Exception("less 0");
                Params = new double[] { n };
                Start("Filter1");
            }
            catch (Exception ex)
            {
                if (ex.Message == "less 0")
                    MessageBox.Show("Значение n не должно быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void sharp_img_filter2_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int n = Convert.ToInt32(txt_sharp_filter2_n.Text);
                if (n < 0) throw new Exception("less 0"); 
                Params = new double[] { n };
                Start("Filter1");
            }
            catch (Exception ex)
            {
                if (ex.Message == "less 0")
                    MessageBox.Show("Значение n не должно быть меньше нуля", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
                else
                    MessageBox.Show("Введите корректные значения", "Введены некорректные данные", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void Laplas_img_Click(object sender, RoutedEventArgs e)
        {
            Start("Laplas");
        }
        private void add_Laplas_img_Click(object sender, RoutedEventArgs e)
        {
            Start("add_Laplas");
        }
        private void Embossing_img_Click(object sender, RoutedEventArgs e)
        {
            Start("Embossing");
        }
        private void Gray_img_Click(object sender, RoutedEventArgs e)
        {
            imgTransform = new ImgTransform(_img_now.Source as BitmapSource, worker);
            add_img(imgTransform.GetImageGray());
        }
    }
}

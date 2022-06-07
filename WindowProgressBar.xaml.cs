using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using System.Windows.Shapes;

namespace Diplom
{
    /// <summary>
    /// Логика взаимодействия для WindowProgressBar.xaml
    /// </summary>
    public partial class WindowProgressBar
    {
        int _n_pix;
        string _n_pix_str;

        public WindowProgressBar(int n_pix)
        {
            InitializeComponent();
            _n_pix = n_pix;
            _n_pix_str = n_pix.ToString();
            
        }
        public void ChangePrBar(double value)
        {
            if(value > -1)
            {
                double percent = value / _n_pix * 100;
                progressBar.Value = (int)Math.Round(percent);
                txt_progressBar.Text = "Обработано пикселей: " + $"{value,7}" + "/" + _n_pix_str + " (" + $"{percent,-3:N1}" + "%)";
            }
            else if (value == -1)
            {
                progressBar.Value = progressBar.Maximum = 0;
            }
            else if (value == -2)
            {
                txt_progressBar.Text = "Выполняется преобразование Фурье";
            }
            else if (value == -3)
            {
                txt_progressBar.Text = "Обработка частот изображения";
            }
            else if (value == -4)
            {
                txt_progressBar.Text = "Выполняется обратное преобразование Фурье";
            }
        }

        public void WriteResults(double time)
        {
            if(progressBar.Maximum == 0) 
                txt_progressBar.Text = "";
            else ChangePrBar(_n_pix);

            txt_progressBar_time.Visibility = Visibility.Visible;
            txt_progressBar_time.Text = "Изображение обработано за " + time.ToString() + " секунд.";
            button_ok.Visibility = Visibility.Visible;
        }

        private void button_ok_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

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
using System.Windows.Shapes;

namespace Diplom
{
    /// <summary>
    /// Логика взаимодействия для WindowImage.xaml
    /// </summary>
    public partial class WindowImage : Window
    {
        public WindowImage(Image img)
        {
            InitializeComponent();
            _img.Source = img.Source;
            this.Width = img.Source.Width*2;
            this.Height = img.Source.Height*2;
        }        

        private void img_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            Point centerPoint = e.GetPosition(root);
            scaleTransform.CenterX += (centerPoint.X - scaleTransform.CenterX)/2;
            scaleTransform.CenterY += (centerPoint.Y - scaleTransform.CenterY)/2;
            if (scaleTransform.ScaleX < 0.3 && scaleTransform.ScaleY < 0.3 && e.Delta < 0)
            {
                return;
            }
            scaleTransform.ScaleX += (double)e.Delta / 3500;
            scaleTransform.ScaleY += (double)e.Delta / 3500;
        }

        private Image draggedImage;
        private Point mousePosition;
        private void _img_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            var image = e.Source as Image;

            if (image != null && _img.CaptureMouse())
            {
                mousePosition = e.GetPosition(_img);
                draggedImage = image;
                Panel.SetZIndex(draggedImage, 1); // in case of multiple images
            }
        }

        private void _img_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (draggedImage != null)
            {
                _img.ReleaseMouseCapture();
                Panel.SetZIndex(draggedImage, 0);
                draggedImage = null;
            }
        }

        private void _img_MouseMove(object sender, MouseEventArgs e)
        {
            if (draggedImage != null)
            {
                var position = e.GetPosition(_img);
                var offset = position - mousePosition;
                mousePosition = position;

                translateTransform.X += offset.X;
                translateTransform.Y += offset.Y;
            }
        }
    }
}

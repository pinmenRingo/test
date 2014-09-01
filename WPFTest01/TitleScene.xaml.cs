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

namespace WPFTest01
{
    /// <summary>
    /// TitleScene.xaml の相互作用ロジック
    /// </summary>
    public partial class TitleScene : Page
    {
        public TitleScene()
        {
            InitializeComponent();
            BGM_MediaElement.Play();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            NavigationService.Navigate(new GameScene());
        }

        private void MediaElement_MediaEnded(object sender, RoutedEventArgs e)
        {
            BGM_MediaElement.Position = new TimeSpan(0, 0, 0);
            BGM_MediaElement.Play();
        }
    }
}

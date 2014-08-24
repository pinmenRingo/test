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

using System.Windows.Threading;

namespace WPFTest01
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        //他からgrid01にアクセスする用
        //コンパイルの仕様上かなんかで必要
        public static Grid grid;
        public static Canvas fieldcanvas;

        //テトリスのフィールドを管理するクラス
        Field field;

        public MainWindow()
        {
            InitializeComponent();

            grid = grid01;
            fieldcanvas = new Canvas()
            {
                Background = new SolidColorBrush(Colors.Gray),
                Visibility = Visibility.Visible,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(10,10,0,0)
            };
            field_back_canvas.Children.Add(fieldcanvas);

            field = new Field();

            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 16);
            timer.Tick += timer_Tick;
            timer.Start();
        }


        void timer_Tick(object sender, EventArgs e)
        {
            field.Proc();
        }

        private void button01_Click(object sender, RoutedEventArgs e)
        {

            field.ResetField();


        }

        public static void AddRect(Rectangle inobj)
        {
            //grid.Children.Add(inobj);
            fieldcanvas.Children.Add(inobj);
        }
        public static void DeleteRect(Rectangle inobj)
        {
            //grid.Children.Remove( inobj );
            fieldcanvas.Children.Remove(inobj);
        }

        //public static void ShowMessageaBox(string instr)
        //{
        //    MessageBox.Show(instr);
        //}
    }
}

//色の変更
/*
if( bf )
    rectangle01.Fill = new SolidColorBrush(Color.FromArgb(50, 175, 196, 255));
else
    rectangle01.Fill = new SolidColorBrush(Color.FromArgb(50,255,196,175));
bf = !bf;*/
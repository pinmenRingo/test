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

        //アクセス用
        //コンパイルの仕様上かなんかで非静的メソッドからアクセスする場合必要っぽい
        public static Grid grid;
        public static Canvas gamebackcanvas;
        public static Canvas gamecanvas;

        //テトリスのフィールドを管理するクラス
        TetrisGame tetris;

        public MainWindow()
        {
            //なんか元からあったやつ,おまじないって認識で
            InitializeComponent();

            //キャンバスの準備
            grid = grid01;
            //黒いキャンバス,あるだけ,gamecanvasだけChildlenにAddされてる
            gamebackcanvas = new Canvas()
            {
                Background = new SolidColorBrush(Colors.Black),
                Visibility = Visibility.Visible,
                Width = (TetrisGame.FIELD_WIDTH+2) * Block.BLOCK_SIZE,
                Height = (TetrisGame.FIELD_HEIGHT+2) * Block.BLOCK_SIZE,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0)
            };
            grid.Children.Add(gamebackcanvas);
            //ゲームキャンバス,ブロックは全部ここのChildlenにAddされる
            gamecanvas = new Canvas()
            {
                Background = new SolidColorBrush(Colors.Gray),
                Visibility = Visibility.Visible,
                Width = TetrisGame.FIELD_WIDTH * Block.BLOCK_SIZE,
                Height = TetrisGame.FIELD_HEIGHT * Block.BLOCK_SIZE,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(Block.BLOCK_SIZE, Block.BLOCK_SIZE, 0, 0)
            };
            gamebackcanvas.Children.Add(gamecanvas);

            //ゲームのクラスを生成する
            tetris = new TetrisGame();

            //タイマーの準備
            DispatcherTimer timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 0, 0, 16);//1秒60フレームに設定,1000/60=16.6666...
            timer.Tick += timer_Tick;
            timer.Start();
        }

        //毎フレーム呼ばれる
        void timer_Tick(object sender, EventArgs e)
        {
            //テトリスゲームを1フレーム進める
            tetris.Proc();
        }

        //ボタンが押された場合に呼ばれる
        private void button01_Click(object sender, RoutedEventArgs e)
        {
            //テトリスゲームをリセットする
            tetris.ResetGame();
        }

        public static void AddRect(Rectangle inobj)
        {
            //grid.Children.Add(inobj);
            gamecanvas.Children.Add(inobj);
        }
        public static void DeleteRect(Rectangle inobj)
        {
            //grid.Children.Remove( inobj );
            gamecanvas.Children.Remove(inobj);
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
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
    public partial class GameScene : Page
    {

        //アクセス用
        //コンパイルの仕様上かなんかで非静的メソッドからアクセスする場合必要っぽい
        public static Grid grid;
        public static Canvas gamebackcanvas;//背景とか追加予定のキャンバス
        public static Canvas gamecanvas;//テトリスのブロックが登録されるキャンバス,gamebackcanvasの子

        //テトリスのフィールドを管理するクラス
        TetrisGame tetris;

        public GameScene()
        {
            //なんか元からあったやつ,おまじないって認識で
            InitializeComponent();

            //要素に静的メソッド以外からでもアクセスできるように小細工
            grid = grid01;//gamebackcanvasの親
            gamebackcanvas = gamebackcanvas_xaml;//gamecanvasの親
            gamecanvas = gamecanvas_xaml;//ゲームキャンバス,ブロックは全部ここのChildlenにAddされる

            //ゲームのクラスを生成する
            tetris = new TetrisGame();

            //タイマーの準備
            DispatcherTimer timer = new DispatcherTimer();//タイマー生成
            timer.Interval = new TimeSpan(0, 0, 0, 0, 16);//1秒60フレームに設定,1000/60=16.6666...
            timer.Tick += timer_Tick;//デリゲートを追加？的な
            timer.Start();//タイマースタート
        }

        //毎フレーム呼ばれる関数
        void timer_Tick(object sender, EventArgs e)
        {
            //テトリスゲームを1フレーム進める
            tetris.Proc();//ゲームオーバー時はtrueが返ってくる
        }

        //ボタンが押された場合に呼ばれる
        private void button01_Click(object sender, RoutedEventArgs e)
        {
            //テトリスゲームをリセットする
            tetris.ResetGame();
        }

        //gamecanvasにRectangleを登録する
        public static void AddRect(Rectangle inobj)
        {
            gamecanvas.Children.Add(inobj);
        }
        //gamecanvasからRectangleを削除する
        public static void DeleteRect(Rectangle inobj)
        {
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
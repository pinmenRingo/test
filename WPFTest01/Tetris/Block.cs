using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;
using System.Windows;

namespace WPFTest01
{
    class Block
    {
        //ブロックのサイズ
        //public const int BLOCK_SIZE = 16;
        //キャンバスのサイズとフィールドの横幅から計算
        public static int BLOCK_WIDTH = (int)GameScene.gamecanvas.Width / TetrisGame.FIELD_WIDTH;
        public static int BLOCK_HEIGHT = (int)GameScene.gamecanvas.Height / TetrisGame.FIELD_HEIGHT;

        //ブロックの座標
        public int x, y;

        //MainWindow.fieldcanvasに登録する
        Rectangle rect;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        /// <param name="inx">ブロックの初期x座標</param>
        /// <param name="iny">ブロックの初期y座標</param>
        /// <param name="incolor">ブロックの色</param>
        public Block( int inx, int iny, Color incolor )
        {
            //Rectangleを生成

            rect = new Rectangle()
            {
                Width = BLOCK_WIDTH,
                Height = BLOCK_HEIGHT,
                Fill = new SolidColorBrush(incolor),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new Thickness(0, 0, 0, 0),
                Stroke=new SolidColorBrush(Colors.Black),
                StrokeThickness = 1.5
            };
            //座標をセット
            Move(inx, iny);
        }

        /// <summary>
        /// 渡された移動量分移動可能か判断する
        /// 0,0を渡した場合その場に設置可能か判定できる
        /// </summary>
        /// <param name="dx">x移動量</param>
        /// <param name="dy">y移動量</param>
        /// <returns>移動可能か</returns>
        public bool CanMove( int dx, int dy ) {
            //return !(Field.bUsing[y+dy, x+dx]);
            return CanMoveTo(x + dx, y + dy);
        }

        /// <summary>
        /// 渡された移動量分移動する
        /// </summary>
        /// <param name="dx">x移動量</param>
        /// <param name="dy">y移動量</param>
        public void Move(int dx, int dy)
        {
            MoveTo(x + dx, y + dy);
        }


        /// <summary>
        /// 渡された座標に移動可能か判断する
        /// </summary>
        /// <param name="dx">x座標</param>
        /// <param name="dy">y座標</param>
        /// <returns>移動可能か</returns>
        public bool CanMoveTo(int tox, int toy)
        {
            if (tox == -1 || tox == TetrisGame.FIELD_WIDTH || toy == -1 || toy == TetrisGame.FIELD_HEIGHT)
            {
                return false;
            }


            return !(TetrisGame.bUsing[toy,tox]);
        }
        /// <summary>
        /// 渡された座標に移動する
        /// </summary>
        /// <param name="dx">x座標</param>
        /// <param name="dy">y座標</param>
        public void MoveTo(int tox, int toy)
        {
            //座標の更新
            x = tox; y = toy;
            //描画座標の更新
            UpdateDrawPosition();
        }

        /// <summary>
        /// 描画座標を更新する
        /// </summary>
        public void UpdateDrawPosition()
        {
           rect.Margin = new Thickness(x * BLOCK_WIDTH, y * BLOCK_HEIGHT, 0, 0);
        }

        /// 着地時に呼ばれる
        /// Fieldに自らを登録する
        //public void RegisterToField()
        //{
        //    Field.bUsing[y, x] = true;
        //    Field.
        //}

        //public void Fall() { ++y; }
        //public void MoveRight() { ++x; }
        //public void MoveLeft() { --x; }
        public Rectangle GetRect() { return rect; }
    }
}

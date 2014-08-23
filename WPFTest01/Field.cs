using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;

namespace WPFTest01
{
    class Field
    {
        public static int FIELD_WIDTH = 12;
        public static int FIELD_HEIGHT = 20;
        static int BLOCK_MAX = FIELD_WIDTH*FIELD_HEIGHT;

        Block[,] rects     = new Block[FIELD_HEIGHT,FIELD_WIDTH];
        public static bool[,] bUsing  = new bool[FIELD_HEIGHT+1,FIELD_WIDTH];

        public Tetromino fallingTet;

        public Field(){
            //フィールドを初期化
            for (int y = 0; y < FIELD_HEIGHT; ++y)
            {
                for (int x = 0; x < FIELD_WIDTH; ++x)
                {
                    bUsing[y, x] = false;
                }
            }
            //地面には常にブロックが敷き詰められているようにしておく
            for (int x = 0; x < FIELD_WIDTH; ++x)
            {
                bUsing[FIELD_HEIGHT, x] = true;
            }

            //右の枠を追加
            MainWindow.AddRect(new Rectangle()
            {
                Width = Block.BLOCK_SIZE,
                Height = Block.BLOCK_SIZE * FIELD_HEIGHT,
                Fill = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new System.Windows.Thickness(Block.BLOCK_SIZE * FIELD_WIDTH, 0, 0, 0)
            });
            //下の枠を追加
            MainWindow.AddRect(new Rectangle()
            {
                Width = Block.BLOCK_SIZE * (FIELD_WIDTH+1),//右下の角も埋める
                Height = Block.BLOCK_SIZE,
                Fill = new SolidColorBrush(Colors.Black),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                VerticalAlignment = System.Windows.VerticalAlignment.Top,
                Margin = new System.Windows.Thickness(0, Block.BLOCK_SIZE * FIELD_HEIGHT, 0, 0)
            });

            fallingTet = new Tetromino();
        }

        /// <summary>
        /// フィールドをリセットする
        /// </summary>
        public void ResetField()
        {

            //フラグを初期化
            for (int y = 0; y < FIELD_HEIGHT; ++y)
            {
                for (int x = 0; x < FIELD_WIDTH; ++x)
                {
                    if (bUsing[y, x])
                    {
                        MainWindow.DeleteRect(rects[y, x].GetRect());
                    }
                    bUsing[y, x] = false;
                }
            }
            gaming = true;

            for (int i = 0; i < 4; ++i)
            {
                MainWindow.DeleteRect(fallingTet.blocks[i].GetRect());
            }
            fallingTet = new Tetromino();
            framecount = fallframe;
        }

        /// <summary>
        /// 渡されたブロックをフィールドに追加する
        /// </summary>
        /// <param name="inblock">追加するブロック</param>
        public void RegistTetromino( Tetromino intet ){
            for (int i = 0; i < 4; ++i)
            {
                bUsing[intet.blocks[i].y, intet.blocks[i].x] = true;
                rects[intet.blocks[i].y, intet.blocks[i].x] = intet.blocks[i];//.GetRect();
                //System.Windows.MessageBox.Show( i+":"+intet.blocks[i].y);
            }
        }

        //fallspeedフレーム経過で1マス下がる
        static int fallframe = 10;
        //fallspeed周りのカウント用
        static int framecount = fallframe;

        //テスト用,そのうち消す
        bool gaming = true;
        bool spacepushing = false;


        /// <summary>
        /// ブロックを配置するフィールドを初期化する
        /// </summary>
        public void Proc(){

            if (!gaming)
            {
                return;
            }

            //キー入力処理
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Right))
            {
                fallingTet.Move(1, 0);
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Left))
            {
                fallingTet.Move(-1, 0);
            }
            //回転は押された瞬間のみ反応するようフラグで管理(長押し無効)
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Up))
            {
                if (!spacepushing)
                {
                    fallingTet.Turn( true );
                    spacepushing = true;
                }
            }
            else
            {
                spacepushing = false;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Down))
            {
                fallingTet.Move(0, 1);
            }

            //fallspeedフレーム経過した場合
            if (--framecount == 0)
            {
                //カウンタリセット
                framecount = fallframe;

                //1マス下げる
                if (!fallingTet.Move(0,1))
                {
                    //着地成功

                    //落下地点をフィールドに登録
                    RegistTetromino(fallingTet);

                    //行の削除処理
                    DeleteFilledLine(fallingTet);

                    //着地時,新たにテトリミノを生成する
                    if (!fallingTet.GenerateNewTetromino())
                    {
                        //生成できなかった(=ゲームオーバー)
                        gaming = false;//temp,boolを返して外でやるべき
                    }
                }
            }

        }
        /// <summary>
        /// 消える行を探索,削除する
        /// </summary>
        /// <param>設置されたテトリミノ</param>
        /// <returns>消えた行数</returns>
        public int DeleteFilledLine( Tetromino intet )
        {
            int tDeletedLine = 0;

            int max=0, min=FIELD_HEIGHT;

            //テトリミノが設置されたy座標の最大から最小までの行を取得し
            //削除するかの探索対象とする
            for (int i = 0; i < 4; ++i)
            {
                max = max < intet.blocks[i].y ? intet.blocks[i].y : max;
                min = min > intet.blocks[i].y ? intet.blocks[i].y : min;
            }
            for (int y = min; y <= max; ++y)
            {
                bool tbFull = true;
                for (int x = 0; x < FIELD_WIDTH&&tbFull; ++x)
                {
                    tbFull = bUsing[y, x];
                }
                if( tbFull )
                {
                    //y行目が詰まっていた(消える)場合
                    //削除行を覚えての最適化はしない,バグやら手間のため
                    DeleteLine(y);
                    tDeletedLine++;
                }
            }


            return tDeletedLine;
        }
        /// <summary>
        /// 指定された行を削除し上の行すべてを一段下げる
        /// </summary>
        /// <param name="deleteline">削除する行</param>
        public void DeleteLine(int inline)
        {
            //指定行のRectangleをウィンドウの描画登録から削除する
            for (int x = 0; x < FIELD_WIDTH; ++x)
            {
                MainWindow.DeleteRect(rects[inline, x].GetRect());
            }
            //削除行より上のすべての段を1マスずつ下げる
            for (int y = inline; y!=0; --y)
            {
                for (int x = 0; x < FIELD_WIDTH; ++x)
                {
                    bUsing[y, x] = bUsing[y - 1, x];
                    if (bUsing[y, x])
                    {
                        rects[y, x] = rects[y - 1, x];
                        rects[y, x].Move(0, 1);
                        //System.Windows.MessageBox.Show( "("+x+","+y+")" );
                    }
                }
            }
            //一番上の行を空にする
            for (int x = 0; x < FIELD_WIDTH; ++x)
            {
                bUsing[0, x] = false;
            }

        }
    }
}

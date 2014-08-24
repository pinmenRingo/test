using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Shapes;
using System.Windows.Media;

namespace WPFTest01
{
    /// <summary>
    /// テトリス全体を司るクラス
    /// </summary>
    class TetrisGame
    {
        //横方向のマス数
        public static int FIELD_WIDTH = 12;
        //縦方向のマス数
        public static int FIELD_HEIGHT = 20;
        //全マス数
        static int BLOCK_MAX = FIELD_WIDTH*FIELD_HEIGHT;

        
        //[HEIGHT,WIDTH]で配列を作る
        //着地したブロックを登録するクラス
        Block[,] rects     = new Block[FIELD_HEIGHT,FIELD_WIDTH];
        //そこにブロックが置いてあるかどうかのフラグ
        public static bool[,] bUsing  = new bool[FIELD_HEIGHT+1,FIELD_WIDTH];

        //落下中のテトリミノ
        //プレイヤーが操作しているやつ
        public Tetromino fallingTet;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public TetrisGame(){
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

            fallingTet = new Tetromino();
        }

        /// <summary>
        /// ゲームをリセットする
        /// </summary>
        public void ResetGame()
        {

            //フィールド上のブロックやフラグを初期化
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
            //ゲームオーバー状態をリセット
            gameover = false;
            MainWindow.gamecanvas.Background = new SolidColorBrush(Colors.Gray);

            //落下中のテトリミノをリセット
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
        public void RegisterTetromino( Tetromino intet ){
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
        bool gameover = false;//ゲームオーバーフラグ
        bool spacepushing = false;//一回のキープッシュで一度だけ反応するようにするフラグ


        /// <summary>
        /// ブロックを配置するフィールドを初期化する
        /// </summary>
        public void Proc(){

            //ゲームオーバー時は何もせずリターン
            if (gameover)
            {
                return;
            }

            //キー入力処理
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Right))
            {
                //落下中のテトリミノを右へ
                fallingTet.Move(1, 0);
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Left))
            {
                //落下中のテトリミノを左へ
                fallingTet.Move(-1, 0);
            }
            //右回転：押された瞬間のみ反応するようフラグで管理(長押し無効)
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Up) || System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Z))
            {
                if (!spacepushing)
                {
                    fallingTet.Turn(true);
                    spacepushing = true;
                }
            }
            //左回転：押された瞬間のみ反応するようフラグで管理(長押し無効)
            else if ( System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.X))
            {
                if (!spacepushing)
                {
                    fallingTet.Turn(false);
                    spacepushing = true;
                }
            }
            else
            {
                //キーが話されたらフラグをリセット
                spacepushing = false;
            }
            if (System.Windows.Input.Keyboard.IsKeyDown(System.Windows.Input.Key.Down))
            {
                //下に移動する
                fallingTet.Move(0, 1);
            }

            //カウントを進める
            if (--framecount == 0)
            {
                //fallspeedフレーム経過した

                //カウンターリセット
                framecount = fallframe;

                //1マス下がる
                if (!fallingTet.Move(0,1))
                {
                    //着地した場合

                    //落下地点をフィールドに登録
                    RegisterTetromino(fallingTet);

                    //行の削除処理(埋まった行があれば削除する)
                    DeleteFilledLine(fallingTet);

                    //着地時,新たにテトリミノを生成する
                    if (!fallingTet.GenerateNewTetromino())
                    {
                        //生成できなかった(=ゲームオーバー)
                        gameover = true;//temp,boolを返して外でやるべき
                        //背景を真っ赤に
                        MainWindow.gamecanvas.Background = new SolidColorBrush(Colors.Red);
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

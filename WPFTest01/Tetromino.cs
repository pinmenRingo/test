using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace WPFTest01
{
    class Tetromino
    {

        //テトリミノの座標の中心
        public int x, y;

        //テトリミノを構成するブロック達
        public Block[] blocks = new Block[4];

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public Tetromino()
        {
            GenerateNewTetromino();
        }

        const int TETROMINO_NUM = 6;//棒,括弧x2,四角,くねくねx2

        //テトリミノテンプレート[型(TETROMINO_NUM),ブロック番号(4),座標xy(2)]
        Color[] TETROMINO_COLOR = { Colors.Red, Colors.Aqua, Colors.Yellow, Colors.Violet, Colors.Pink, Colors.PaleGreen };
        //テトリミノの形を配列で定義
        //値はテトリミノの座標からの相対距離
        int[, ,] TETROMINO_TEMPLATE ={
                                              {
                                                  {-1,0},
                                                  {0,0},
                                                  {0,1},
                                                  {1,1}
                                              },
                                              {
                                                  {1,0},
                                                  {0,0},
                                                  {0,1},
                                                  {-1,1}
                                              },
                                              {
                                                  {-1,1},
                                                  {-1,0},
                                                  {0,0},
                                                  {1,0}
                                              },
                                              {
                                                  {1,1},
                                                  {1,0},
                                                  {0,0},
                                                  {-1,0}
                                              },
                                              {
                                                  {-1,0},
                                                  {0,0},
                                                  {1,0},
                                                  {2,0}
                                              },
                                              {
                                                  {1,0},
                                                  {0,0},
                                                  {0,1},
                                                  {1,1}
                                              }
                                          };
        Random rand = new Random();

        /// <summary>
        /// 新たにテトリミノを生成する
        /// </summary>
        /// <returns>生成の成否を返す</returns>
        public bool GenerateNewTetromino()
        {
            //生成フラグ
            bool generated = true;
            //テトリミノのx座標を真ん中,y座標を一番上にとる
            int centerx = TetrisGame.FIELD_WIDTH / 2;
            x = centerx;
            y = 0;

            //生成するテトリミノの番号をランダムに設定
            int tetrominonum = rand.Next(TETROMINO_NUM);

            //System.Windows.MessageBox.Show(tetrominonum.ToString());
            //各ブロック生成可能か判定する
            for (int i = 0; i < 4; ++i)
            {
                blocks[i] = new Block(TETROMINO_COLOR[tetrominonum]);
                if (!blocks[i].CanMoveTo(centerx + TETROMINO_TEMPLATE[tetrominonum, i, 0], TETROMINO_TEMPLATE[tetrominonum, i, 1]))
                {
                    generated = false;
                    break;
                }
                blocks[i].MoveTo(centerx + TETROMINO_TEMPLATE[tetrominonum, i, 0], TETROMINO_TEMPLATE[tetrominonum, i, 1]);
            }
            if (generated)
            {
                //生成に成功していた場合ブロックをgridにAddChildする
                for (int i = 0; i < 4; ++i)
                {
                    MainWindow.AddRect(blocks[i].GetRect());
                }
            }

            return generated;
        }

        /// <summary>
        /// テトリミノを移動する
        /// </summary>
        /// <param name="dx">x移動量</param>
        /// <param name="dy">y移動量</param>
        /// <returns>移動の成否を返す</returns>
        public bool Move(int dx, int dy)
        {
            //移動可能か判定する
            bool bCanMove = true;
            for ( int i = 0; i < 4 && bCanMove; ++i )
            {
                bCanMove = blocks[i].CanMove(dx,dy);
            }

            //移動可能な場合移動させる
            if (bCanMove)
            {
                //ブロックの座標を移動させる
                for (int i = 0; i < 4; ++i)
                {
                    blocks[i].Move(dx, dy);
                }
                //テトリミノの座標を移動させる
                x += dx;
                y += dy;
            }

            return bCanMove;

        }

        /// <summary>
        /// 回転
        /// </summary>
        /// <param name="right">右回転の場合true,左回転の場合false</param>
        /// <returns>成否を返す</returns>
        public bool Turn( bool right )
        {
            //回転可能かのフラグ
            bool suc = true;

            //回転前の座標を保存してやる
            int[] prex = { blocks[0].x, blocks[1].x, blocks[2].x, blocks[3].x };
            int[] prey = { blocks[0].y, blocks[1].y, blocks[2].y, blocks[3].y };

            //回転可能か判定しながら可能なら移動していってやる
            for (int i = 0; i < 4; ++i)
            {
                //テトリミノの中心からブロックへの相対座標を得る
                int dx = blocks[i].x - x;
                int dy = blocks[i].y - y;
                //回転行列のあれを用いて90度回転
                //( cosθ , sinθ )
                //( -sinθ, cosθ )
                int tx = x + (right ? -dy : +dy);
                int ty = y + (right ? +dx : -dx);
                if (!blocks[i].CanMoveTo(tx, ty))
                {
                    suc = false;
                    break;
                }
                blocks[i].MoveTo(tx, ty);
            }
            //失敗時は元の場所に戻す(回転を取り消す的な)
            if (!suc)
            {
                for (int i = 0; i < 4; ++i)
                {
                    blocks[i].MoveTo(prex[i], prey[i]);
                }
            }

            return suc;
        }
        
    }
}

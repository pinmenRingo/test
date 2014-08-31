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
        public Tetromino(int index)
        {
            //最初落下するテトリミノ(ブロック4つ)を生成する
            GenerateNewTetromino(index);
        }

        //テトリミノの種類
        const int TETROMINO_NUM = 19;//棒,括弧x2,四角,くねくねx2
        //テトリミノの色[型(TETROMINO_NUM)]
        Color[] TETROMINO_COLOR = { Colors.Red, Colors.Aqua, Colors.Yellow, Colors.Violet, Colors.Pink, Colors.PaleGreen,Colors.Azure };
        //テトリミノテンプレート[型(TETROMINO_NUM),ブロック番号(4),座標xy(2)]
        //テトリミノの形を配列で定義
        //値はテトリミノの座標からの相対距離
        int[, ,] TETROMINO_TEMPLATE ={
                                              //T
                                              {
                                                  {0,0},
                                                  {0,1},
                                                  {-1,1},
                                                  {1,1}
                                              },

                                              {
                                                  {0,0},
                                                  {0,1},
                                                  {1,1},
                                                  {0,2}
                                              },

                                              {
                                                 {0,0},
                                                 {0,1},
                                                 {-1,1},
                                                 {0,2}
                                              },

                                              {
                                                  {0,0},
                                                  {-1,0},
                                                  {1,0},
                                                  {0,1}
                                              },

                                               //J
                                              {
                                                  {0,0},
                                                  {0,1},
                                                  {1,1},
                                                  {2,1}
                                              },

                                              {
                                                  {0,0},
                                                  {1,0},
                                                  {0,1},
                                                  {0,2}
                                              },

                                              {
                                                  {0,0},
                                                  {-1,0},
                                                  {1,0},
                                                  {1,1}
                                              },
                                              {
                                                  {0,0},
                                                  {0,1},
                                                  {0,2},
                                                  {1,2}
                                              },

                                               //L
                                              {
                                                  
                                                  {0,0},
                                                  {0,1},
                                                  {0,2},
                                                  {1,2}
                                              },

                                              {
                                                 {0,0},
                                                 {-1,0},
                                                 {1,0},
                                                 {-1,1}
                                              },

                                              {
                                                 {0,0},
                                                 {-1,0},
                                                 {0,1},
                                                 {0,2}
                                              },

                                              {
                                                 {0,0},
                                                 {0,1},
                                                 {-1,1},
                                                 {-2,1}
                                              },

                                               //S
                                              {
                                                  {0,0},
                                                  {1,0},
                                                  {0,1},
                                                  {-1,1}
                                              },

                                              {
                                                  {0,0},
                                                  {0,1},
                                                  {1,1},
                                                  {1,2}
                                              },

                                              //Z                                    
                                              {
                                                  {0,0},
                                                  {-1,0},
                                                  {0,1},
                                                  {1,1}
                                              },

                                              {
                                                 {0,0},
                                                 {0,1},
                                                 {-1,1},
                                                 {-1,2}
                                              },

                                              // - 
                                              {
                                                  {0,0},
                                                  {0,1},
                                                  {0,2},
                                                  {0,3}
                                              },

                                              {
                                                  {-1,0},
                                                  {0,0},
                                                  {1,0},
                                                  {2,0}
                                              },

                                              //□
                                              {
                                                  {0,0},
                                                  {1,0},
                                                  {0,1},
                                                  {1,1}
                                              }
                                          };
        //乱数を得るためのクラス
        Random rand = new Random();//引数無しなのでシード値は時間からいい感じにやってくれるそうです

        /// <summary>
        /// 新たにテトリミノ(ブロック4つ)を生成する
        /// </summary>
        /// <returns>生成の成否を返す</returns>
        public bool GenerateNewTetromino(int index)
        {
            //生成フラグ
            bool generated = true;
            //テトリミノのx座標を真ん中,y座標を一番上にとる
            int centerx = TetrisGame.FIELD_WIDTH / 2;
            x = centerx; 

            //生成するテトリミノの番号をランダムに設定
            //生成するテトリミノの番号 = マッチングテンプレートのインデックス
            //int tetrominonum = rand.Next(TETROMINO_NUM);
            int tetrominonum = index;

            //回転の際にテトリミノの中心で回すため、
            //特定のテトリミノは座標を調整
            //闇
            
            if (tetrominonum == 3 || 
                tetrominonum == 17||
                tetrominonum == 6)
            {
                y = 0;
            }
            else if (tetrominonum == 11)
            {
                y = 1;
                x--;
            }
            else if (tetrominonum == 4)
            {
                y = 1;
                x++;
            }
            else
            {
                y = 1;
            }

            

            //System.Windows.MessageBox.Show(tetrominonum.ToString());
            //各ブロック生成可能か判定する
            for (int i = 0; i < 4; ++i)
            {
                //TETROMINO_TEMPLATEのtetrominonum番目のテトリミノのi番目のブロックの座標を得る
                int tx = centerx + TETROMINO_TEMPLATE[tetrominonum, i, 0];
                int ty = TETROMINO_TEMPLATE[tetrominonum, i, 1];

                //ブロックを生成
                blocks[i] = new Block( tx, ty, TETROMINO_COLOR[tetrominonum%7]);

                //他のブロックに被っていないか確かめる
                if (!blocks[i].CanMove(0, 0))
                {
                    //ブロックを生成したい場所に既にブロックがおいてあった場合
                    generated = false;//フラグを折る
                    break;
                }
            }
            if (generated)
            {
                //生成に成功していた場合ブロックをgridにAddChildする
                for (int i = 0; i < 4; ++i)
                {
                    GameScene.AddRect(blocks[i].GetRect());
                }
            }

            return generated;
        }

        public bool isGenerateNextTet(int index)
        {
            //生成フラグ
            bool generated = true;
            //テトリミノのx座標を真ん中,y座標を一番上にとる
            int centerx = TetrisGame.FIELD_WIDTH / 2;
            x = centerx;

            //各ブロック生成可能か判定する
            for (int i = 0; i < 4; ++i)
            {
                //TETROMINO_TEMPLATEのtetrominonum番目のテトリミノのi番目のブロックの座標を得る
                int tx = centerx + TETROMINO_TEMPLATE[index, i, 0];
                int ty = TETROMINO_TEMPLATE[index, i, 1];

                //ブロックを生成
                blocks[i] = new Block(tx, ty, TETROMINO_COLOR[index % 7]);

                //他のブロックに被っていないか確かめる
                if (!blocks[i].CanMove(0, 0))
                {
                    //ブロックを生成したい場所に既にブロックがおいてあった場合
                    generated = false;//フラグを折る
                    break;
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
            for ( int i = 0; i < 4 && bCanMove; ++i )//canmoveがfalseになったらループを抜ける(条件部参照)
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
                x += dx; y += dy;
            }

            return bCanMove;

        }

        public bool canMove(int dx, int dy)
        {
            bool canMove = true;

            for (int i = 0; i < 4; ++i)
            {
               canMove =  blocks[i].CanMove(dx, dy);
            }

            return canMove;
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
                //テトリミノの中心からブロックへの相対座標を得る(ブロック座標-テトリミノ座標=相対座標)
                int dx = blocks[i].x - x;
                int dy = blocks[i].y - y;
                //回転行列のあれを用いて90度回転
                //( cosθ , sinθ )
                //( -sinθ, cosθ )
                int tx =  (right ? -dy : +dy);
                int ty =  (right ? +dx : -dx);
                //相対座標から絶対座標に直す(回転後相対座標+テトリミノ座標=回転後ブロック座標)
                tx += x; ty += y;

                //回転後の座標にブロックが無いかチェック
                if (!blocks[i].CanMoveTo(tx, ty))
                {
                    //あったら失敗
                    suc = false;
                    break;
                }
                //そのブロックが移動できそうならとりあえず移動しとく
                blocks[i].MoveTo(tx, ty);
            }
            //失敗していた時は全部ロック元の場所に戻す(回転を取り消す的な)
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

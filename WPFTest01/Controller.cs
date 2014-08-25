using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace WPFTest01
{
    struct Face
    {
    //    public Face()
    //    {
    //        pos.X = 0.0;
    //        pos.Y = 0.0;
    //        ID = -1;
    //        updated = false;
    //    }
        public Point pos;//座標
        public int ID;//スケルトンのID,-1の時未使用
        public bool updated;//そのフレームでposが更新されたか
    };

    /// <summary>
    /// テトリミノの座標と顔4つの座標からテトリミノを操作する
    /// UpdatePositionメソッドに認識されたスケルトンのインデックス(添え字?ID?)と顔の座標を渡すだけでおｋ,3人でも10人でも問題無い
    /// </summary>
    class Controller
    {
        //pos[i]にはKinectで割り振られたID[i]の人の座標が入る
        //Point[] pos = new Point[4];//4人の座標
        //int[] ID = new int[4];//4人分,-1の時未使用
        //bool[] updated = new bool[4];//4人分,そのフレームでpos[i]が更新されたか

        //face[i]にはKinectで割り振られたface[i].IDの人の座標が入る
        Face[] face = new Face[4];

        //見知らぬIDが渡された時ここに一時保存する
        /// Face[0～3]に登録されていない顔がUpdatePositionで渡された場合1フレームの間だけここに保存される
        int newbieID = -1;
        Point newbiePos;

        public Controller()
        {
            for (int i = 0; i < 4; ++i)
            {
                face[i] = new Face();
                //コンストラクタで初期化したかったけど分からなかったのでこっちで
                face[i].ID = -1;
                face[i].pos = new Point(0.0, 0.0);
                face[i].updated = false;
            }
        }

        //現在の顔の座標を更新しその座標からテトリミノの操作を行う
        void Proc(Tetromino intet)
        {

            //newbieの登録処理(あれば)
            RegisterNewbieFace();

            //顔の座標からテトリミノを操作する
            ControllTetromino(intet);

            //色々初期化
            for (int i = 0; i < 4; ++i)
            {
                face[i].updated = false;
            }
            newbieID = -1;
        }

        /// <summary>
        /// 顔の座標とテトロミノの情報を用いてテトロミノを操作する
        /// </summary>
        /// <param name="intet">操作するテトリミノ</param>
        /// <returns>操作したか否か(要らないと思うけど一応)</returns>
        bool ControllTetromino( Tetromino intet )
        {
            bool controlled = false;

            //4人に満たない場合何もせずfalseを返す
            for (int i = 0; i < 4; ++i)
            {
                if (face[i].ID == -1)
                {
                    return false;
                }
            }

            //TODO:ここで顔認識

            //intet.Turn(true)とかするあれ




            return controlled;
        }

        /// <summary>
        /// newbieがあり、Face[0～3]に動いていない(認識されていない)顔があればそのiにnewbieを登録する
        /// </summary>
        /// <returns>登録の成否</returns>
        bool RegisterNewbieFace()
        {
            //newbieな顔が無ければ何もせずリターン
            if( newbieID == -1 ){
                return false;
            }

            //newbieな顔があった場合
            //前フレーム更新が無い顔を捜してそれと入れ替え
            for (int i = 0; i < 4; ++i)
            {
                if (!face[i].updated)
                {
                    face[i].ID = newbieID;
                    face[i].pos = newbiePos;
                    return true;
                }
            }

            //登録されている顔がすべて更新されていた場合諦めて登録せずにreturn
            return false;
        }


        /// <summary>
        /// 顔の座標を更新する
        /// </summary>
        /// <param name="inID">顔のスケルトン番号</param>
        /// <param name="inposx">顔のx座標</param>
        /// <param name="inposy">顔のy座標</param>
        /// <returns>既存の顔の座標が更新されたか否か(多分要らないけど一応)</returns>
        bool UpdatePosition(int inID, double inposx, double inposy)
        {
            for (int i = 0; i < 4; ++i)
            {
                //該当IDを見つけた場合その座標を更新する
                if (inID == face[i].ID)
                {
                    //見つけたので更新してreturn
                    face[i].pos.X = inposx;
                    face[i].pos.Y = inposy;
                    face[i].updated = true;
                    return true;
                }
            }
            //登録されていないIDなのでnewbieに保存しておく
            newbieID = inID;
            newbiePos.X = inposx;
            newbiePos.Y = inposy;

            return false;
        }

    }
}

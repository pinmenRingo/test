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
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows.Threading;
using Microsoft.Kinect;
using Microsoft.Kinect.Face;

namespace WPFTest01
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class GameScene : Page, INotifyPropertyChanged
    {

        //アクセス用
        //コンパイルの仕様上かなんかで非静的メソッドからアクセスする場合必要っぽい
        public static Grid grid;
        public static Canvas gamebackcanvas;//背景とか追加予定のキャンバス
        public static Canvas gamecanvas;//テトリスのブロックが登録されるキャンバス,gamebackcanvasの子
        public static Canvas fallingbmpcanvas;

        //テトリスのフィールドを管理するクラス
        TetrisGame tetris;

        static bool fallingbmpupdated = false;

        /// キネクト用>
     
        private const double HandSize = 30;

        private const double JointThickness = 3;

        private const double ClipBoundsThickness = 10;

        /// <summary>
        /// Constant for clamping Z values of camera space points from being negative
        /// </summary>
        private const float InferredZPositionClamp = 0.1f;

        /// <summary>
        /// Brush used for drawing joints that are currently tracked
        /// </summary>
        private readonly Brush trackedJointBrush = new SolidColorBrush(Color.FromArgb(255, 68, 192, 68));

        /// <summary>
        /// Brush used for drawing joints that are currently inferred
        /// </summary>        
        private readonly Brush inferredJointBrush = Brushes.Yellow;

        /// <summary>
        /// Pen used for drawing bones that are currently inferred
        /// </summary>        
        private readonly Pen inferredBonePen = new Pen(Brushes.Gray, 1);

        /// <summary>
        /// Drawing group for body rendering output
        /// </summary>
        private DrawingGroup drawingGroup;

        DrawingImage imageSource; //ここにKIENCT用画像が表示される
        private KinectSensor kinectSensor = null;
        private CoordinateMapper coordinateMapper = null; //座標系変換を扱ってるクラスっぽい
        //private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;

        /// <summary>
        /// definition of bones
        /// </summary>
        private List<Tuple<JointType, JointType>> bones;

        private int displayWidth;

        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;
        private string matchStatus = null;
        private string headPos = null;
        private string leftHandPos = null;
        private string rightHandPos = null;
        private string rightKneePos = null;
        private string leftKneePos = null;
        private string widthheight = null;
        private SolidColorBrush matchAlertColor;
        private Dictionary<JointType, Brush> jointColors;
        private Label[,] matchControl;

        private int[] useJoints;

        /// <summary>
        /// マッチング用のテンプレート
        /// </summary>
        private int[,,] matchingTemplets;
        
        //スケール
        double kScaleY = 0.008;
        double kScaleX = 0.008;

        //各Gridのサイズ
        double gridWidth;
        double gridHeight;

        static Image imgfalling = null;

        //以下画像制御
        public static WriteableBitmap[] rows = new WriteableBitmap[20];
        public static WriteableBitmap fallingbmp = null;//new WriteableBitmap(64, 64, 96.0, 96.0, PixelFormats.Bgra32, null)
        
        //カラーセンサーから取得するサイズ
        const int COLOR_PIXELS_WIDTH = 1380;
        const int COLOR_PIXELS_HEIGHT = 1080;

        const int BLOCK_WIDTH_PIX = COLOR_PIXELS_WIDTH / 4;//16;//512/TetrisGame.FIELD_WIDTH;
        const int FIELD_WIDTH_PIX = BLOCK_WIDTH_PIX*TetrisGame.FIELD_WIDTH;
        const int BLOCK_HEIGHT_PIX = COLOR_PIXELS_HEIGHT/4;//16;//22;
        const int FIELD_HEIGHT_PIX = BLOCK_HEIGHT_PIX*TetrisGame.FIELD_HEIGHT;
        const int BYTES_PER_PIX = 4;
        static byte[] fallingbytes = new byte[COLOR_PIXELS_WIDTH*COLOR_PIXELS_WIDTH * BYTES_PER_PIX];
        static byte[] fieldbytes = null;
        static WriteableBitmap fieldbmp = null;

        static byte[] temprowbytes = new byte[BLOCK_WIDTH_PIX * 4 * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];
        //static byte[] tempfallingbytes = new byte[BLOCK_WIDTH_PIX * 4 * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];

        static Random rand = new Random();//引数無しなのでシード値は時間からいい感じにやってくれるそうです


        //追加のキネクト要素
        private MultiSourceFrameReader multiFrameSourceReader = null;
        //private WriteableBitmap bitmap = null;
        private int depthWidth, depthHeight;
        private ushort[] depthFrameData = null;
        private byte[] colorFrameData = null;
        private byte[] bodyIndexFrameData = null;
        private byte[] displayPixels = null;
        private DepthSpacePoint[] depthPoints = null;
        private WriteableBitmap colorbitmap = null;

        //public byte[] testbmp = new byte[512 * 424 * 4];

        //落下時にクロマキーをfallingbmpに保存する周りで利用するフラグ
        public static bool waitingforclip = false;//ブロックが落下した際にtrueになり次のキネクトフレーム更新時にfallingbmpにクリップ画像を渡す
        //static bool completeclip = false;//クリップ画像が渡されたらtrueになる

        public GameScene()
        {
            //なんか元からあったやつ,おまじないって認識で
            InitializeComponent();

            //kinect
            this.kinectSensor = KinectSensor.GetDefault();
            this.coordinateMapper = this.kinectSensor.CoordinateMapper;
   
            //フレームサイズ
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
            this.gridWidth = matchGrid.Width / 4;
            this.gridHeight = matchGrid.Height / 4;

            viewBox.Width = this.displayWidth;
            viewBox.Height = this.displayHeight;
            //viewBox_color.Width = this.displayWidth;
            //viewBox_color.Height = this.displayHeight;

            // ここうまいこと触る
            matchGrid.Width = this.displayWidth;
            matchGrid.Height = this.displayHeight;

            this.InitMatchGrid();

            //this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

            #region Boneの初期化

            this.bones = new List<Tuple<JointType, JointType>>();
            // Torso
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Head, JointType.Neck));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.Neck, JointType.SpineShoulder));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.SpineMid));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineMid, JointType.SpineBase));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineShoulder, JointType.ShoulderLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.SpineBase, JointType.HipLeft));

            // Right Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderRight, JointType.ElbowRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowRight, JointType.WristRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.HandRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandRight, JointType.HandTipRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristRight, JointType.ThumbRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            #endregion

            #region Bone Color
            this.bodyColors = new List<Pen>();
            this.bodyColors.Add(new Pen(Brushes.Red, 6)); //Pen = 図形を中抜き？
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));

            #endregion



            #region マルチセンサーの初期化

            //マルチキネクトのあれで追加
            this.multiFrameSourceReader = this.kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Depth | FrameSourceTypes.Color | FrameSourceTypes.BodyIndex | FrameSourceTypes.Body);

            // 深度センサーの解像度を取得
            FrameDescription depthFrameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            int depthWidth = depthFrameDescription.Width; int depthHeight = depthFrameDescription.Height;
            this.depthWidth = depthWidth; this.depthHeight = depthHeight;

            // カラーセンサーの解像度を取得
            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.FrameDescription;
            int colorWidth = colorFrameDescription.Width;
            int colorHeight = colorFrameDescription.Height;

            //描画用バッファの準備
            this.displayPixels = new byte[colorWidth * colorHeight * BYTES_PER_PIX];
            //ようわからん中間バッファの準備
            this.depthPoints = new DepthSpacePoint[colorWidth * colorHeight];

            //メモリ確保
            this.depthFrameData = new ushort[depthWidth * depthHeight];
            this.bodyIndexFrameData = new byte[depthWidth * depthHeight];
            this.colorFrameData = new byte[colorWidth * colorHeight * BYTES_PER_PIX];
            this.colorbitmap = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            

            #endregion


            this.kinectSensor.Open();

            this.drawingGroup = new DrawingGroup();

            this.imageSource = new DrawingImage(this.drawingGroup);

            this.DataContext = this;
            


            

            //マッチング関係
            this.useJoints = new int[5] { (int)JointType.Head, (int)JointType.HandLeft,(int)JointType.HandRight,(int)JointType.KneeRight,(int)JointType.KneeLeft};

            #region MatchTemplate
            this.matchingTemplets = new int[,,]{
                                           //T型
                                           {
                                               
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {0,7,13,11},
                                               {0,0,0,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {0,0,7,11},
                                               {0,0,13,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {0,7,11,0},
                                               {0,0,13,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {0,7,3,11},
                                               {0,0,13,0}
                                           },

                                           //J
                                           {
                                               {0,0,0,0},
                                               {0,3,0,0},
                                               {0,7,13,11},
                                               {0,0,0,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,3,11,0},
                                               {0,7,0,0},
                                               {0,13,0,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {7,3,11,0},
                                               {0,0,17,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,7,0,0},
                                               {0,3,0,0},
                                               {13,11,0,0}
                                           },

                                           //L
                                           {
                                               {0,0,0,0},
                                               {0,11,0,0},
                                               {0,3,0,0},
                                               {0,7,17,0},
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {0,7,3,11},
                                               {0,13,0,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,3,11,0},
                                               {0,0,7,0},
                                               {0,0,13,0}
                                           },
                                           {
                                               {0,0,0,0},
                                               {0,0,3,0},
                                               {7,13,11,0},
                                               {0,0,0,0 }
                                           },

                                           //S
                                           {
                                               {0,0,0,0},
                                               {0,0,3,11},
                                               {0,7,17,0},
                                               {0,0,0,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,3,0,0},
                                               {0,7,11,0},
                                               {0,0,17,0}
                                           },

                                           //逆S
                                           {
                                               {0,0,0,0},
                                               {0,7,3,0},
                                               {0,0,17,11},
                                               {0,0,0,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,11,0},
                                               {0,7,3,0},
                                               {0,13,0,0}
                                           },

                                           // l
                                           {
                                               {0,0,11,0},
                                               {0,0,3,0},
                                               {0,0,7,0},
                                               {0,0,17,0}
                                           },

                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {7,3,17,11},
                                               {0,0,0,0}
                                           },

                                           //□
                                           {
                                               {0,0,0,0},
                                               {0,0,0,0},
                                               {0,7,3,0,},
                                               {0,13,11,0}
                                           }

                                       };
            #endregion


            //要素に静的メソッド以外からでもアクセスできるように小細工
            grid = grid01;//gamebackcanvasの親
            gamebackcanvas = gamebackcanvas_xaml;//gamecanvasの親
            gamecanvas = gamecanvas_xaml;//ゲームキャンバス,ブロックは全部ここのChildlenにAddされる
            fallingbmpcanvas = fallingbmpcanvas_xaml;
            //imgfalling = image_falling;

            #region BitMap

            //rowsを初期化
            byte[] bytes = new byte[FIELD_WIDTH_PIX * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];
            for (int i = 0; i < BLOCK_HEIGHT_PIX*FIELD_WIDTH_PIX; ++i)
            {
                bytes[i * 4] = 0x00;                bytes[i * 4+1] = 0x00;                bytes[i * 4+2] = 0x00;                bytes[i * 4+3] = 0x00;
            }
            for (int i = 0; i < 20; ++i)
            {
                rows[i] = new WriteableBitmap(FIELD_WIDTH_PIX, BLOCK_HEIGHT_PIX, 96.0, 96.0, PixelFormats.Bgra32, null);
                rows[i].WritePixels(
                    new Int32Rect(0, 0, FIELD_WIDTH_PIX, BLOCK_HEIGHT_PIX),bytes,FIELD_WIDTH_PIX * 4,0);
            }

            fieldbytes = new byte[FIELD_WIDTH_PIX * FIELD_HEIGHT_PIX * BYTES_PER_PIX];
            fieldbmp = new WriteableBitmap(FIELD_WIDTH_PIX, FIELD_HEIGHT_PIX, 96.0, 96.0, PixelFormats.Bgra32, null);
            for (int i = fieldbytes.Length - 1; i >= 0; --i)
            {
                fieldbytes[i] = 0x00;
            }
            fieldbmp.WritePixels(
                new Int32Rect(0,0,FIELD_WIDTH_PIX,FIELD_HEIGHT_PIX),
                fieldbytes,
                FIELD_WIDTH_PIX*BYTES_PER_PIX,
                0
                );

            //fallingbmp = new WriteableBitmap(BLOCK_WIDTH_PIX * 4, BLOCK_HEIGHT_PIX * 4, 96.0, 96.0, PixelFormats.Bgra32, null);
            //fallingbmp = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
            //fallingbmp = new WriteableBitmap(COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT, 96.0, 96.0, PixelFormats.Bgra32, null);
            //なんと！それは正方形！
            fallingbmp = new WriteableBitmap(COLOR_PIXELS_WIDTH, COLOR_PIXELS_WIDTH, 96.0, 96.0, PixelFormats.Bgra32, null);

            //80*80の画像を生成
            //for (int i = 0; i < COLOR_PIXELS_WIDTH*COLOR_PIXELS_HEIGHT; ++i)
            //{
            //    byte t = 0x00;
            //    if(rand.Next(10)==1){
            //        t = 0xff; fallingbytes[i * 4] = (byte)rand.Next(256); fallingbytes[i * 4 + 1] = (byte)rand.Next(256); fallingbytes[i * 4 + 2] = (byte)rand.Next(256);
            //    }
            //    else
            //    {
            //        fallingbytes[i * 4] = 0x00; fallingbytes[i * 4 + 1] = 0x00; fallingbytes[i * 4 + 2] = 0x00;
            //    }
            //    fallingbytes[i * 4 + 3] = t;//(rand.Next(3) == 1) ? 0xff : 0x00;
            //}
            //fallingbmp.WritePixels(
            //    new Int32Rect(0,0,COLOR_PIXELS_WIDTH,COLOR_PIXELS_HEIGHT),
            //    fallingbytes,
            //    COLOR_PIXELS_WIDTH*BYTES_PER_PIX,
            //    0);


            //NotifyPropertyChanged("image_falling");
            //PropertyChanged(this,)
            PropertyChanged(this, new PropertyChangedEventArgs("image_falling"));

            #endregion

            //ゲームのクラスを生成する
            tetris = new TetrisGame();

            //タイマーの準備
            DispatcherTimer timer = new DispatcherTimer();//タイマー生成
            timer.Interval = new TimeSpan(0, 0, 0, 0, 16);//1秒60フレームに設定,1000/60=16.6666...
            timer.Tick += timer_Tick;//デリゲートを追加？的な
            timer.Start();//タイマースタート
         
        }

        /// <summary>
        /// rows[line]にfallingbmpに(BLOCK_HEIGHT_PIX*line,0)(BLOCK_HEIGHT_PIX*(line+1),BLOCK_WIDTH_PIX*4)の部分を合成する
        /// </summary>
        /// <param name="line"></param>
        /// <param name="bmpline"></param>
        //public static void AddBMP(int x,int line, int bmpline)
        //{
        //    Array.Clear(temprowbytes, 0, temprowbytes.Length);
        //    Array.Clear(tempfallingbytes, 0, tempfallingbytes.Length);

        //    int tx = (x - 2) < 0 ? 0 : x - 2;
        //    tx = tx > TetrisGame.FIELD_WIDTH - 4 ? TetrisGame.FIELD_WIDTH - 4 : tx;

        //    //fallingbmp.CopyPixels(new Int32Rect(0,bmpline*BLOCK_HEIGHT_PIX,BLOCK_WIDTH_PIX*4,BLOCK_HEIGHT_PIX),tempfallingbytes, BLOCK_WIDTH_PIX * 4 * BYTES_PER_PIX, 0);
        //    fallingbmp.CopyPixels(new Int32Rect(256, bmpline * BLOCK_HEIGHT_PIX, BLOCK_WIDTH_PIX * 4, BLOCK_HEIGHT_PIX), tempfallingbytes, BLOCK_WIDTH_PIX * 4 * BYTES_PER_PIX, 0);
        //    //fallingbmp.AddDirtyRect(new Int32Rect(256, 0, 1370, 1080));

        //    Array.Clear(temprowbytes,0, temprowbytes.Length);

        //    rows[line].CopyPixels(new Int32Rect(tx * BLOCK_WIDTH_PIX, 0, BLOCK_WIDTH_PIX * 4, BLOCK_HEIGHT_PIX), temprowbytes, BLOCK_WIDTH_PIX*4 * BYTES_PER_PIX, 0);

        //    for (int i = BLOCK_WIDTH_PIX * 4 * BLOCK_HEIGHT_PIX - 1; i >= 0; --i)
        //    {
        //        if (/*temprowbytes[i * 4 + 3] == 0x00 &&*/ tempfallingbytes[i * 4 + 3] == 0xff)
        //        //if ( tempfallingbytes[i*4+3] == 0xff)
        //        {
        //            temprowbytes[i * 4] = tempfallingbytes[i * 4];
        //            temprowbytes[i * 4 + 1] = tempfallingbytes[i * 4 + 1];
        //            temprowbytes[i * 4 + 2] = tempfallingbytes[i * 4 + 2];
        //            temprowbytes[i * 4 + 3] = 0xff;
        //        }
        //    }

        //    rows[line].WritePixels(
        //        new Int32Rect(tx*BLOCK_WIDTH_PIX,0,BLOCK_WIDTH_PIX*4,BLOCK_HEIGHT_PIX),
        //        temprowbytes,
        //        BLOCK_WIDTH_PIX*4*BYTES_PER_PIX,
        //        0
        //        );

        //    //fallingを更新
        //    //for (int i = 0; i < BLOCK_WIDTH_PIX * BLOCK_HEIGHT_PIX * 4 * 4; ++i)
        //    //{
        //    //    byte t = 0x00;
        //    //    if (rand.Next(10) == 1)
        //    //    {
        //    //        t = 0xff;
        //    //        fallingbytes[i * 4] = (byte)rand.Next(256);
        //    //        fallingbytes[i * 4 + 1] = (byte)rand.Next(256);
        //    //        fallingbytes[i * 4 + 2] = (byte)rand.Next(256);
        //    //    }
        //    //    else
        //    //    {
        //    //        fallingbytes[i * 4] = 0x00;
        //    //        fallingbytes[i * 4 + 1] = 0x00;
        //    //        fallingbytes[i * 4 + 2] = 0x00;
        //    //    }
        //    //    fallingbytes[i * 4 + 3] = t;//(rand.Next(3) == 1) ? 0xff : 0x00;
        //    //}
        //    //fallingbmp.WritePixels(
        //    //    new Int32Rect(0, 0, BLOCK_WIDTH_PIX * 4, BLOCK_HEIGHT_PIX * 4),
        //    //    fallingbytes,
        //    //    BLOCK_WIDTH_PIX * 4 * 4,
        //    //    0);


        //}

        public static void AddBMPField( int x, int y )
        {
            int length = COLOR_PIXELS_WIDTH * COLOR_PIXELS_WIDTH;

            x -= 2;//無回転
            int sx = x * BLOCK_WIDTH_PIX;
            int getx = FIELD_WIDTH_PIX - x * BLOCK_WIDTH_PIX;
            if (getx > BLOCK_WIDTH_PIX * 4)
            {
                getx = BLOCK_WIDTH_PIX * 4;
            }
            if (sx < 0)
            {
                getx += sx;
                sx = 0;
            }

            byte[] tbytes1 = new byte[length * BYTES_PER_PIX];
            byte[] tbytes2 = new byte[length * BYTES_PER_PIX];
            //y -= 1;//無回転
            if (y < 0) y = 0;
            int gety = FIELD_HEIGHT_PIX - y * BLOCK_HEIGHT_PIX;
            if (gety > BLOCK_HEIGHT_PIX * 4)
            {
                gety = BLOCK_HEIGHT_PIX*4;
            }

            fallingbmp.CopyPixels(new Int32Rect(0, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_WIDTH), tbytes1, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);
            //fallingbmp.CopyPixels(new Int32Rect(0, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT), tbytes1, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);
            ////fieldbmp.CopyPixels(new Int32Rect(sx, y * BLOCK_HEIGHT_PIX, COLOR_PIXELS_WIDTH/*FIELD_WIDTH_PIX - sx*/, COLOR_PIXELS_HEIGHT/*FIELD_HEIGHT_PIX - y * BLOCK_HEIGHT_PIX*/), tbytes2, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);
            ////fieldbmp.CopyPixels(new Int32Rect(sx, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT), tbytes2, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);
            fieldbmp.CopyPixels(new Int32Rect(sx, y * BLOCK_HEIGHT_PIX, getx, gety), tbytes2, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);

            //1に落ちてきたの入ってる
            int ti;
            //合成する
            for( int i=length-1; i>=0; --i ){
                ti = i*4;
                if( tbytes1[ti+3] != 0x00 ){
                    tbytes2[ti] = tbytes1[ti];
                    tbytes2[ti+1] = tbytes1[ti+1];
                    tbytes2[ti+2] = tbytes1[ti+2];
                    tbytes2[ti+3] = tbytes1[ti+3];
                }
            }
            
            fieldbmp.WritePixels(
                //new Int32Rect(sx, y * BLOCK_HEIGHT_PIX, FIELD_WIDTH_PIX - sx, FIELD_HEIGHT_PIX - y * BLOCK_HEIGHT_PIX),
                new Int32Rect(sx, y * BLOCK_HEIGHT_PIX, getx, gety),
                tbytes2,
                COLOR_PIXELS_WIDTH * BYTES_PER_PIX,
                //fallingbmp.PixelWidth * BYTES_PER_PIX,
                0
                );


            //fallingbmp.CopyPixels(new Int32Rect(256, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT), fieldbytes, FIELD_WIDTH_PIX * BYTES_PER_PIX, 0);
            //fieldbmp.WritePixels(
            //     new Int32Rect(sx, y * BLOCK_HEIGHT_PIX, FIELD_WIDTH_PIX - sx, FIELD_HEIGHT_PIX - y * BLOCK_HEIGHT_PIX),
            //    //new Int32Rect(x * BLOCK_WIDTH_PIX, y * BLOCK_HEIGHT_PIX, COLOR_PIXELS_WIDTH - x * BLOCK_WIDTH_PIX, COLOR_PIXELS_HEIGHT - y * BLOCK_HEIGHT_PIX),
            //     fieldbytes,
            //     FIELD_WIDTH_PIX * BYTES_PER_PIX,
            //     0
                 //);
        }

        public static void TurnFallingBMP(bool right)
        {;
            int tlength = COLOR_PIXELS_WIDTH * COLOR_PIXELS_WIDTH;

            fallingbmp.CopyPixels(fallingbytes, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);


            byte[] tbytes = new byte[tlength * BYTES_PER_PIX];

            Array.Copy(fallingbytes, tbytes, fallingbytes.Length);


            if (right)
            {
                for (int y = 0; y < COLOR_PIXELS_WIDTH; ++y)
                {
                    for (int x = 0; x < COLOR_PIXELS_WIDTH; ++x)
                    {
                        int t1 = x * (COLOR_PIXELS_WIDTH) + ((COLOR_PIXELS_WIDTH - 1) - y);
                        int t2 = y * (COLOR_PIXELS_WIDTH) + x;
                        t1 *= 4;
                        t2 *= 4;
                        fallingbytes[t1] = tbytes[t2];
                        fallingbytes[t1+1] = tbytes[t2+1];
                        fallingbytes[t1+2] = tbytes[t2+2];
                        fallingbytes[t1+3] = tbytes[t2+3];
                    }
                }
            }
            else
            {
                for (int y = 0; y < COLOR_PIXELS_WIDTH; ++y)
                {
                    for (int x = 0; x < COLOR_PIXELS_WIDTH; ++x)
                    {
                        int t1 = (COLOR_PIXELS_WIDTH-1-x)* (COLOR_PIXELS_WIDTH) + y;
                        int t2 = y * (COLOR_PIXELS_WIDTH) + x;
                        t1 *= 4;
                        t2 *= 4;
                        fallingbytes[t1] = tbytes[t2];
                        fallingbytes[t1 + 1] = tbytes[t2 + 1];
                        fallingbytes[t1 + 2] = tbytes[t2 + 2];
                        fallingbytes[t1 + 3] = tbytes[t2 + 3];
                    }
                }
            }



            //fallingbmp = new WriteableBitmap(theight, twidth, 96.0, 96.0, PixelFormats.Bgra32, null);

            fallingbmp.WritePixels(new Int32Rect(0, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_WIDTH), fallingbytes, COLOR_PIXELS_WIDTH * BYTES_PER_PIX, 0);
            //PropertyChanged(this, new PropertyChangedEventArgs("image_falling"));
            fallingbmpupdated = true;


        }

        public static void ResetRows()
        {
            byte[] bytes = new byte[FIELD_WIDTH_PIX * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];
            for (int i = 0; i < BLOCK_HEIGHT_PIX * FIELD_WIDTH_PIX; ++i)
            {
                bytes[i * 4] = 0x00; bytes[i * 4 + 1] = 0x00; bytes[i * 4 + 2] = 0x00; bytes[i * 4 + 3] = 0x00;
            }
            for (int i = 0; i < 20; ++i)
            {
                rows[i].WritePixels(
                    new Int32Rect(0, 0, FIELD_WIDTH_PIX, BLOCK_HEIGHT_PIX), bytes, FIELD_WIDTH_PIX * 4, 0);
            }

            Array.Clear(fieldbytes,0,fieldbytes.Length);
            fieldbmp.WritePixels(
                 new Int32Rect(0, 0, FIELD_WIDTH_PIX, FIELD_HEIGHT_PIX),
                //new Int32Rect(x * BLOCK_WIDTH_PIX, y * BLOCK_HEIGHT_PIX, COLOR_PIXELS_WIDTH - x * BLOCK_WIDTH_PIX, COLOR_PIXELS_HEIGHT - y * BLOCK_HEIGHT_PIX),
                 fieldbytes,
                 FIELD_WIDTH_PIX * BYTES_PER_PIX,
                 0
            );
        }

        #region Kienct
        public event PropertyChangedEventHandler PropertyChanged;

#region バインディング

        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        public string StatusText
        {
            get
            {
                return this.statusText;
            }

            set
            {
                if (this.statusText != value)
                {
                    this.statusText = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusText"));
                    }
                }
            }
        }

        public string MatchStatus
        {
            get
            {
                return this.matchStatus;
            }

            set
            {
                if (this.matchStatus != value)
                {
                    this.matchStatus = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("MatchStatus"));
                    }
                }
            }
        }

        public string HeadPos
        {
            get
            {
                return this.headPos;
            }

            set
            {
                if (this.headPos != value)
                {
                    this.headPos = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("HeadPos"));
                    }
                }
            }
        }

        public string LeftHandPos
        {
            get
            {
                return this.leftHandPos;
            }

            set
            {
                if (this.leftHandPos != value)
                {
                    this.leftHandPos = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("LeftHandPos"));
                    }
                }
            }
        }

        public string RightHandPos
        {
            get
            {
                return this.rightHandPos;
            }

            set
            {
                if (this.rightHandPos != value)
                {
                    this.rightHandPos = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("RightHandPos"));
                    }
                }
            }
        }

        public string RightKneePos
        {
            get
            {
                return this.rightKneePos;
            }

            set
            {
                if (this.rightKneePos != value)
                {
                    this.rightKneePos = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("RightKneePos"));
                    }
                }
            }
        }

        public string LeftKneePos
        {
            get
            {
                return this.leftKneePos;
            }

            set
            {
                if (this.leftKneePos != value)
                {
                    this.leftKneePos = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("LeftKneePos"));
                    }
                }
            }
        }

        public SolidColorBrush MatchAlertColor
        {
            get
            {
                return this.matchAlertColor;
            }

            set
            {
                if (this.matchAlertColor != value)
                {
                    this.matchAlertColor = value;
                }

                if (this.PropertyChanged != null)
                {
                    this.PropertyChanged(this, new PropertyChangedEventArgs("MatchAlertColor"));
                }

            }
        }

#endregion


        private void GameSceneLoaded(object sender, RoutedEventArgs e)
        {
            if (this.multiFrameSourceReader != null)
            {
                this.multiFrameSourceReader.MultiSourceFrameArrived += multiFrameSourceReader_FrameArrived;
            }
            //if (this.bodyFrameReader != null)
            //{
            //    this.bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;
            //}

            //matchGridの初期化
            this.InitMatchGrid();
        }

        private void GameSceneClosing(object sender, RoutedEventArgs e)
        {
            if (this.multiFrameSourceReader != null)
            {
                this.multiFrameSourceReader.Dispose();
                this.multiFrameSourceReader = null;
            }
            //if (this.bodyFrameReader != null)
            //{
            //    this.bodyFrameReader.Dispose();
            //    this.bodyFrameReader = null;
            //}

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        void multiFrameSourceReader_FrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            MultiSourceFrame multiSourceFrame = e.FrameReference.AcquireFrame();

            bool dataReceived = false;

            //ここボーン

            #region スケルトンの描画やら
            if (!waitingforclip){

            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
            //using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                using (DrawingContext dc = this.drawingGroup.Open())
                {
                    //背景の黒い画面を描画
                    dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(0x11,0x00,0x00,0x00)), null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

                    int penIndex = 0;

                    foreach (Body body in this.bodies)
                    {
                        Pen drawPen = this.bodyColors[penIndex++];

                        if (body.IsTracked)
                        {
                            this.DrawClippedEdges(body, dc);

                            //ジョイントの辞書配列
                            //joint名:key
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            //CameraSpacePoint point =  body.Joints[JointType.HandLeft].Position;

                            //jointのポイントをディスプレイに変換

                            //ジョイントごとの座標をいれる辞書配列
                            //key:ジョント名
                            Dictionary<JointType, Point> jointPoint = new Dictionary<JointType, Point>();


                            foreach (JointType jointType in joints.Keys)
                            {
                                CameraSpacePoint position = joints[jointType].Position;
                                if (position.Z < 0)
                                {
                                    position.Z = InferredZPositionClamp;
                                }

                                DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);

                                jointPoint[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);

                                switch (jointType)
                                {

                                    case JointType.Head:
                                        double head_x = jointPoint[JointType.Head].X * kScaleX;
                                        double head_y = jointPoint[JointType.Head].Y * kScaleY;
                                        this.HeadPos = "head_x:" + head_x.ToString() + " " + "head_y" + head_y.ToString();

                                        break;


                                    case JointType.HandLeft:

                                        double leftHand_x = jointPoint[JointType.HandLeft].X * kScaleX;
                                        double leftHand_y = jointPoint[JointType.HandLeft].Y * kScaleY;
                                        this.LeftHandPos = "leftHand_x:" + leftHand_x.ToString() + " " + "leftHand_y:" + leftHand_y.ToString();
                                        break;

                                    case JointType.HandRight:
                                        double rightHand_x = jointPoint[JointType.HandRight].X * kScaleX;
                                        double rightHand_y = jointPoint[JointType.HandRight].Y * kScaleY;
                                        this.RightHandPos = "rightHand_x:" + rightHand_x.ToString() + " " + "rightHand_y:" + rightHand_y.ToString();

                                        break;

                                    case JointType.KneeRight:
                                        double rightKnee_x = jointPoint[JointType.KneeRight].X * kScaleX;
                                        double rightKnee_y = jointPoint[JointType.KneeRight].Y * kScaleY;
                                        this.RightKneePos = "rightKnee_x:" + rightKnee_x.ToString() + " " + "rightKnee_y" + rightKnee_y.ToString();
                                        break;

                                    case JointType.KneeLeft:
                                        double leftKnee_x = jointPoint[JointType.KneeLeft].X * kScaleX;
                                        double leftKnee_y = jointPoint[JointType.KneeLeft].X * kScaleY;
                                        this.LeftKneePos = "leftKnee_x:" + leftKnee_x.ToString() + " " + "leftKnee_y" + leftKnee_y.ToString();
                                        break;

                                    default:
                                        break;
                                }

                            }

                            //体を描画
                            this.DrawBody(joints, jointPoint, dc, drawPen);

                            //マッチング処理
                            bool isMatched = this.isMatching(jointPoint, 0);
                            if (isMatched) this.MatchAlertColor = new SolidColorBrush(Colors.Blue);
                            else this.MatchAlertColor = new SolidColorBrush(Colors.Red);


                        }

                    }

                    //レンダーエリア外に描画しないように防ぐ
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


                }
            }}

            #endregion

            #region カラーの描画

            if (!waitingforclip)
            {
                using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                {
                    if (colorFrame != null)
                    {
                        FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                        using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                        {
                            this.colorbitmap.Lock();

                            // verify data and write the new color frame data to the display bitmap
                            if ((colorFrameDescription.Width == this.colorbitmap.PixelWidth) && (colorFrameDescription.Height == this.colorbitmap.PixelHeight))
                            {
                                colorFrame.CopyConvertedFrameDataToIntPtr(
                                    this.colorbitmap.BackBuffer,
                                    (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                    ColorImageFormat.Bgra);

                                this.colorbitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorbitmap.PixelWidth, this.colorbitmap.PixelHeight));
                            }

                            this.colorbitmap.Unlock();
                        }
                    }
                }
            }
            #endregion

            #region コーディネート系

            //必要に応じて行う
            if (waitingforclip)
            {

                int depthWidth = 0;
                int depthHeight = 0;

                bool multiSourceFrameProcessed = false;
                bool colorFrameProcessed = false;
                bool depthFrameProcessed = false;
                bool bodyIndexFrameProcessed = false;
                //こっからコーディネート
                if (multiSourceFrame != null)
                {
                    // Frame Acquisition should always occur first when using multiSourceFrameReader
                    using (DepthFrame depthFrame = multiSourceFrame.DepthFrameReference.AcquireFrame())
                    {
                        if (depthFrame != null)
                        {
                            FrameDescription depthFrameDescription = depthFrame.FrameDescription;
                            depthWidth = depthFrameDescription.Width;
                            depthHeight = depthFrameDescription.Height;

                            if ((depthWidth * depthHeight) == this.depthFrameData.Length)
                            {
                                depthFrame.CopyFrameDataToArray(this.depthFrameData);
                                depthFrameProcessed = true;
                            }
                        }
                    }

                    using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
                    {
                        if (colorFrame != null)
                        {
                            FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                            if ((colorFrameDescription.Width * colorFrameDescription.Height * BYTES_PER_PIX) == this.colorFrameData.Length)
                            {
                                if (colorFrame.RawColorImageFormat == ColorImageFormat.Bgra)
                                {
                                    colorFrame.CopyRawFrameDataToArray(this.colorFrameData);
                                }
                                else
                                {
                                    colorFrame.CopyConvertedFrameDataToArray(this.colorFrameData, ColorImageFormat.Bgra);
                                }

                                colorFrameProcessed = true;
                            }
                        }
                    }

                    using (BodyIndexFrame bodyIndexFrame = multiSourceFrame.BodyIndexFrameReference.AcquireFrame())
                    {
                        if (bodyIndexFrame != null)
                        {
                            FrameDescription bodyIndexFrameDescription = bodyIndexFrame.FrameDescription;

                            if ((bodyIndexFrameDescription.Width * bodyIndexFrameDescription.Height) == this.bodyIndexFrameData.Length)
                            {
                                bodyIndexFrame.CopyFrameDataToArray(this.bodyIndexFrameData);
                                bodyIndexFrameProcessed = true;
                            }
                        }

                        multiSourceFrameProcessed = true;
                    }
                }

                // we got all frames
                if (multiSourceFrameProcessed && depthFrameProcessed && colorFrameProcessed && bodyIndexFrameProcessed)
                {
                    /*this.coordinateMapper.MapColorFrameToDepthSpace(this.depthFrameData, this.depthPoints);

                    Array.Clear(this.displayPixels, 0, this.displayPixels.Length);

                    int length = this.bodyIndexFrameData.Length;

                    DepthSpacePoint depthPoint;

                    var negativeinf = float.NegativeInfinity;

                    int depthX, depthY;

                    int colorIndex = 0;

                    length = this.depthPoints.Length;
                    // loop over each row and column of the depth
                    for (colorIndex = 0; colorIndex < length; colorIndex += 2)
                    //for (int colorIndex = length - 2; colorIndex >= 1000000; colorIndex -= 3)
                    //for (int colorIndex = length - 2; colorIndex >= 0; colorIndex -= 3)
                    {

                        //一時変数
                        depthPoint = this.depthPoints[colorIndex];

                        //if (float.IsNegativeInfinity(depthPoint.X) && !float.IsNegativeInfinity(depthPoint.Y)) ←ゴミ　ゴミゴミゴミゴミゴミゴミゴミゴミゴミゴミゴミゴミ
                        if (depthPoint.X != negativeinf && depthPoint.Y != negativeinf)
                        {
                            // make sure the depth pixel maps to a valid point in color space
                            depthX = (int)(depthPoint.X + 0.5f);
                            depthY = (int)(depthPoint.Y + 0.5f);

                            if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                            {
                                int depthIndex = (depthY * depthWidth) + depthX;
                                byte player = this.bodyIndexFrameData[depthIndex];

                                // if we're tracking a player for the current pixel, sets its color and alpha to full
                                if (player != 0xff)
                                {
                                    // set source for copy to the color pixel
                                    int sourceIndex = colorIndex * BYTES_PER_PIX;

                                    int nextpixIndex = sourceIndex + BYTES_PER_PIX;
                                    int nextnextpixIndex = nextpixIndex + BYTES_PER_PIX;
                                    this.displayPixels[nextpixIndex] = this.colorFrameData[nextpixIndex++];
                                    this.displayPixels[nextpixIndex] = this.colorFrameData[nextpixIndex++];
                                    this.displayPixels[nextpixIndex] = this.colorFrameData[nextpixIndex++];
                                    this.displayPixels[nextpixIndex] = 0xff;

                                    this.displayPixels[nextnextpixIndex] = this.colorFrameData[nextnextpixIndex++];
                                    this.displayPixels[nextnextpixIndex] = this.colorFrameData[nextnextpixIndex++];
                                    this.displayPixels[nextnextpixIndex] = this.colorFrameData[nextnextpixIndex++];
                                    this.displayPixels[nextnextpixIndex] = 0xff;

                                    // write out blue byte
                                    //this.displayPixels[sourceIndex + BYTES_PER_PIX] = this.colorFrameData[sourceIndex + BYTES_PER_PIX];
                                    this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];

                                    // write out green byte
                                    //this.displayPixels[sourceIndex + BYTES_PER_PIX] = this.colorFrameData[sourceIndex + BYTES_PER_PIX];
                                    this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];

                                    // write out red byte
                                    //this.displayPixels[sourceIndex + BYTES_PER_PIX] = this.colorFrameData[sourceIndex + BYTES_PER_PIX];
                                    this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];

                                    // write out alpha byte
                                    //this.displayPixels[sourceIndex + BYTES_PER_PIX] = 0xff;
                                    this.displayPixels[sourceIndex] = 0xff;
                                }
                            }
                        }
                    }*/

                    //int tlength = displayPixels.Length/4;
                    //for (int i = 0; i < tlength; ++i)
                    //{
                    //    displayPixels[i * 4] = 0x00;
                    //    displayPixels[i * 4+1] = 0xff;
                    //    displayPixels[i * 4+2] = 0x00;
                    //    displayPixels[i * 4+3] = 0xff;
                    //}



                        //this.colorbitmap.WritePixels(
                        //    new Int32Rect(0, 0, this.colorbitmap.PixelWidth, this.colorbitmap.PixelHeight),
                        //    this.displayPixels,
                        //    this.colorbitmap.PixelWidth * BYTES_PER_PIX,
                        //    0);


                    this.coordinateMapper.MapColorFrameToDepthSpace(this.depthFrameData, this.depthPoints);

                    Array.Clear(this.displayPixels, 0, this.displayPixels.Length);

                    // loop over each row and column of the depth
                    for (int colorIndex = 0; colorIndex < this.depthPoints.Length; ++colorIndex)
                    {
                        DepthSpacePoint depthPoint = this.depthPoints[colorIndex];

                        if (!float.IsNegativeInfinity(depthPoint.X) && !float.IsNegativeInfinity(depthPoint.Y))
                        {
                            // make sure the depth pixel maps to a valid point in color space
                            int depthX = (int)(depthPoint.X + 0.5f);
                            int depthY = (int)(depthPoint.Y + 0.5f);

                            if ((depthX >= 0) && (depthX < depthWidth) && (depthY >= 0) && (depthY < depthHeight))
                            {
                                int depthIndex = (depthY * depthWidth) + depthX;
                                byte player = this.bodyIndexFrameData[depthIndex];

                                // if we're tracking a player for the current pixel, sets its color and alpha to full
                                if (player != 0xff)
                                {
                                    // set source for copy to the color pixel
                                    int sourceIndex = colorIndex * BYTES_PER_PIX;

                                    // write out blue byte
                                    this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];

                                    // write out green byte
                                    this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];

                                    // write out red byte
                                    this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];

                                    // write out alpha byte
                                    this.displayPixels[sourceIndex] = 0xff;
                                }
                            }
                        }
                    }


                        fallingbmp.WritePixels(
                            new Int32Rect(0, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT),
                            this.displayPixels,
                            COLOR_PIXELS_WIDTH * BYTES_PER_PIX,
                            0);
                    //new Int32Rect(0, 0, this.colorbitmap.PixelWidth, this.colorbitmap.PixelHeight),
                    //COLOR_PIXELS_WIDTH*BYTES_PER_PIX,
                    //this.colorbitmap.PixelWidth * BYTES_PER_PIX,
                    //fallingbmp.PixelWidth * BYTES_PER_PIX,



                    //テスト
                    //fallingbmp.WritePixels(
                    //        new Int32Rect(0, 0, fallingbmp.PixelWidth, fallingbmp.PixelHeight),
                    //        this.displayPixels,
                    //        fallingbmp.PixelWidth * BYTES_PER_PIX,
                    //        0);


                    PropertyChanged(this, new PropertyChangedEventArgs("image_falling"));
                

                    //成功！
                    waitingforclip = false;

                    //new Int32Rect(0, 0, fallingbmp.PixelWidth, fallingbmp.PixelHeight),
                    //this.colorbitmap.PixelWidth * BYTES_PER_PIX,
                }
            }

            #endregion


        }

        private bool isMatching(Dictionary<JointType, Point> jointPoint, int templateNum)
        {
            bool ret = false;
            int matchingCount = 0;

            foreach (JointType i in this.useJoints)
            {
                int x = (int)(jointPoint[i].X * kScaleX);
                int y = (int)(jointPoint[i].Y * kScaleY);

                //validation
                if (x <= 0) x = 0;
                if (x >= 3) x = 3;
                if (y <= 0) y = 0;
                if (y >= 3) y = 3;

                if (this.matchingTemplets[templateNum,y,x] == (int)i)
                {
                    matchingCount++;
                }

                if (matchingCount == 4)
                {
                    ret = true;
                    break;
                }
                else
                {
                    ret = false;
                }

            }

            return ret;
        }

        //matchGrid初期化（Labelを配置）
        private void InitMatchGrid()
        {
            int rows = matchGrid.RowDefinitions.Count;
            int cols = matchGrid.ColumnDefinitions.Count;
            matchControl = new Label[cols,rows];

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {
                    matchControl[y, x] = new Label();
                    matchControl[y, x].Background = Brushes.Blue;
                    matchControl[y, x].BorderThickness = new Thickness(1, 1, 1, 1);
                    matchGrid.Children.Add(matchControl[y, x]);

                }
            }


        }


        /// <summary>
        /// 体を描画する
        /// </summary>
        /// <param name="joints"></param>
        /// <param name="jointPoints"></param>
        /// <param name="dc"></param>
        /// <param name="drawPen"></param>
        private void DrawBody(IReadOnlyDictionary<JointType, Joint> joints, IDictionary<JointType, Point> jointPoints, DrawingContext dc, Pen drawPen)
        {
            //Draw Bone
            foreach (var bone in this.bones)
            {
                this.DrawBones(joints, jointPoints, bone.Item1, bone.Item2, dc, drawPen);
            }

            //Draw Joint
            foreach (JointType jointType in joints.Keys)
            {
                Brush drawBrush = null;

                TrackingState trackingState = joints[jointType].TrackingState;

                switch (trackingState)
                {
                    case TrackingState.Inferred:
                        drawBrush = this.inferredJointBrush;
                        break;
                    case TrackingState.NotTracked:
                        break;
                    case TrackingState.Tracked:
                        drawBrush = this.trackedJointBrush;
                        break;
                    default:
                        break;
                }

                if (drawBrush != null)
                {
                    dc.DrawEllipse(drawBrush, null, jointPoints[jointType], JointThickness, JointThickness);
                }
            }
        }


        private void DrawBones(IReadOnlyDictionary<JointType,Joint>joints,
            IDictionary<JointType,Point>jointPoints,
            JointType jointType0,
            JointType jointType1,
            DrawingContext dc,
            Pen drawingPen)
        {

            Joint joint0 = joints[jointType0];
            Joint joint1 = joints[jointType1];

            if (joint0.TrackingState == TrackingState.NotTracked || joint0.TrackingState == TrackingState.NotTracked)
	        {
                return;
		 
	        }

            Pen drawPen = this.inferredBonePen;
            if (joint0.TrackingState == TrackingState.Tracked && joint1.TrackingState == TrackingState.Tracked)
            {
                drawPen = drawingPen;
            }

            dc.DrawLine(drawPen,jointPoints[jointType0],jointPoints[jointType1]);


	
        }

        private void DrawClippedEdges(Body body, DrawingContext drawingContext)
        {
            FrameEdges clippedEdge = body.ClippedEdges;
            if (clippedEdge.HasFlag(FrameEdges.Bottom))
            {
                drawingContext.DrawRectangle(Brushes.Red,null,new Rect(0,this.displayHeight-ClipBoundsThickness,this.displayWidth,ClipBoundsThickness));
            }

            if (clippedEdge.HasFlag(FrameEdges.Top))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, this.displayWidth, ClipBoundsThickness));
            }

            if (clippedEdge.HasFlag(FrameEdges.Left))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(0, 0, ClipBoundsThickness, this.displayHeight));
            }

            if (clippedEdge.HasFlag(FrameEdges.Right))
            {
                drawingContext.DrawRectangle(Brushes.Red, null, new Rect(this.displayWidth - ClipBoundsThickness, 0, ClipBoundsThickness, this.displayHeight));
            }
        }


        #endregion




        //毎フレーム呼ばれる関数
        void timer_Tick(object sender, EventArgs e)
        {
            //テトリスゲームを1フレーム進める
            tetris.Proc();//ゲームオーバー時はtrueが返ってくる

            this.WIDTHHEIGHT = "( " + fallingbmp.PixelWidth + ", " + fallingbmp.PixelHeight + " )";

            if (fallingbmpupdated)
            {
                fallingbmpupdated=false;
                //PropertyChanged(this, new PropertyChangedEventArgs("image_falling"));
            }

            //キネクト無い時用
//            if (waitingforclip)
//            {
//                waitingforclip = false;


//                byte color1 = (byte)rand.Next(256);
//                byte color2 = (byte)rand.Next(256);
//                byte color3 = (byte)rand.Next(256);
//                int length = fallingbytes.Length/4;
//                for (int i = 0; i < length; ++i)
//                {
//                    fallingbytes[i * 4] = color1;
//                    fallingbytes[i * 4 + 1] = color2;
//                    fallingbytes[i * 4 + 2] = color3;
//                    fallingbytes[i * 4 + 3] = 0xff;
//                }
//                for (int y = 0; y < 30; ++y)
//                {
//                    for (int x = 0; x < 30; ++x)
//                    {
//                        int index = y * fallingbmp.PixelWidth + x;
//                        fallingbytes[index * 4] = 0x00;
//                        fallingbytes[index * 4 + 1] = 0x00;
//                        fallingbytes[index * 4 + 2] = 0x00;
//                        fallingbytes[index * 4 + 3] = 0xff;
//                    }
//                }
//                fallingbmp.WritePixels(
//                     new Int32Rect(0, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_WIDTH),
//                     fallingbytes,
//                     COLOR_PIXELS_WIDTH* BYTES_PER_PIX,
//                     0);


///*
//                byte color1 = (byte)rand.Next(256);
//                byte color2 = (byte)rand.Next(256);
//                byte color3 = (byte)rand.Next(256);
//                int length = displayPixels.Length / 4;
//                for (int i = 0; i < length; ++i)
//                {
//                    displayPixels[i * 4] = color1;
//                    displayPixels[i * 4 + 1] = color2;
//                    displayPixels[i * 4 + 2] = color3;
//                    displayPixels[i * 4 + 3] = 0xff;
//                }

//                for (int y = 0; y < 30; ++y)
//                {
//                    for (int x = 0; x < 30; ++x)
//                    {
//                        int index = y * fallingbmp.PixelWidth + x;
//                        displayPixels[index * 4] = 0x00;
//                        displayPixels[index * 4+1] = 0x00;
//                        displayPixels[index * 4+2] = 0x00;
//                        displayPixels[index * 4+3] = 0xff;
//                    }
//                }

//                    //落下するブロックの画像
//                    fallingbmp.WritePixels(
//                            new Int32Rect(0, 0, fallingbmp.PixelWidth, fallingbmp.PixelHeight),
//                        //new Int32Rect(0, 0, this.colorbitmap.PixelWidth, this.colorbitmap.PixelHeight),
//                            this.displayPixels,
//                            fallingbmp.PixelWidth * BYTES_PER_PIX,
//                            0);
//                    //PropertyChanged(this, new PropertyChangedEventArgs("image_falling"));

//                //fallingbmp = new WriteableBitmap(COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT, 96.0, 96.0, PixelFormats.Bgra32, null);
//                ////落下後登録される画像
//                //fallingbmp.WritePixels(
//                //    new Int32Rect(0, 0, fallingbmp.PixelWidth, fallingbmp.PixelHeight),
//                //    //new Int32Rect(0, 0, this.colorbitmap.PixelWidth, this.colorbitmap.PixelHeight),
//                //    this.displayPixels,
//                //    fallingbmp.PixelWidth * BYTES_PER_PIX,
//                //    0);*/
//            }
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

        #region ぴんめんのばいでぃんぐ

        public ImageSource CutImageSource
        {
            get
            {
                return this.colorbitmap;
            }
        }
        public ImageSource CutImageSource0
        {
            get
            {
                return rows[0];
            }
        }
        public ImageSource CutImageSource1
        {
            get
            {
                return rows[1];
            }
        }
        public ImageSource CutImageSource2
        {
            get
            {
                return rows[2];
            }
        }
        public ImageSource CutImageSource3
        {
            get
            {
                return rows[3];
            }
        }
        public ImageSource CutImageSource4
        {
            get
            {
                return rows[4];
            }
        }
        public ImageSource CutImageSource5
        {
            get
            {
                return rows[5];
            }
        }
        public ImageSource CutImageSource6
        {
            get
            {
                return rows[6];
            }
        }
        public ImageSource CutImageSource7
        {
            get
            {
                return rows[7];
            }
        }
        public ImageSource CutImageSource8
        {
            get
            {
                return rows[8];
            }
        }
        public ImageSource CutImageSource9
        {
            get
            {
                return rows[9];
            }
        }
        public ImageSource CutImageSource10
        {
            get
            {
                return rows[10];
            }
        }
        public ImageSource CutImageSource11
        {
            get
            {
                return rows[11];
            }
        }
        public ImageSource CutImageSource12
        {
            get
            {
                return rows[12];
            }
        }
        public ImageSource CutImageSource13
        {
            get
            {
                return rows[13];
            }
        }
        public ImageSource CutImageSource14
        {
            get
            {
                return rows[14];
            }
        }
        public ImageSource CutImageSource15
        {
            get
            {
                return rows[15];
            }
        }
        public ImageSource CutImageSource16
        {
            get
            {
                return rows[16];
            }
        }
        public ImageSource CutImageSource17
        {
            get
            {
                return rows[17];
            }
        }
        public ImageSource CutImageSource18
        {
            get
            {
                return rows[18];
            }
        }
        public ImageSource CutImageSource19
        {
            get
            {
                return rows[19];
            }
        }
        public ImageSource FallingBitmap
        {
            get
            {
                return fallingbmp;
            }
        }
        public ImageSource FieldImageSource
        {
            get
            {
                return fieldbmp;
            }
            //set
            //{
            //    if (fallingbmp != value)
            //    {
            //        fallingbmp = (WriteableBitmap)value;

            //        if (this.PropertyChanged != null)
            //        {
            //            this.PropertyChanged(this, new PropertyChangedEventArgs("image_falling"));
            //        }
            //    }
            //}
        }

        public string WIDTHHEIGHT
        {
            get
            {
                return this.widthheight;
            }
            set
            {
                if (this.widthheight != value)
                {
                    this.widthheight = value;

                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("widthheight"));
                    }
                }
            }

        }
#endregion

        //public static void ShowMessageaBox(string instr)
        //{
        //    MessageBox.Show(instr);
        //}
    }
}

#region ゴミ箱


//色の変更
/*
if( bf )
    rectangle01.Fill = new SolidColorBrush(Color.FromArgb(50, 175, 196, 255));
else
    rectangle01.Fill = new SolidColorBrush(Color.FromArgb(50,255,196,175));
bf = !bf;*/



/*
void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
{
    bool dataReceived = false;

    //ここボーン


    using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
    //using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
    {
        if (bodyFrame != null)
        {
            if (this.bodies == null)
            {
                this.bodies = new Body[bodyFrame.BodyCount];
            }

            bodyFrame.GetAndRefreshBodyData(this.bodies);
            dataReceived = true;
        }
    }

    if (dataReceived)
    {
        using (DrawingContext dc = this.drawingGroup.Open())
        {
            //背景の黒い画面を描画
            dc.DrawRectangle(new SolidColorBrush(Color.FromArgb(0x11,0x00,0x00,0x00)), null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

            int penIndex = 0;

            foreach (Body body in this.bodies)
            {
                Pen drawPen = this.bodyColors[penIndex++];

                if (body.IsTracked)
                {
                    this.DrawClippedEdges(body, dc);

                    //ジョイントの辞書配列
                    //joint名:key
                    IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                    //CameraSpacePoint point =  body.Joints[JointType.HandLeft].Position;

                    //jointのポイントをディスプレイに変換

                    //ジョイントごとの座標をいれる辞書配列
                    //key:ジョント名
                    Dictionary<JointType, Point> jointPoint = new Dictionary<JointType, Point>();


                    foreach (JointType jointType in joints.Keys)
                    {
                        CameraSpacePoint position = joints[jointType].Position;
                        if (position.Z < 0)
                        {
                            position.Z = InferredZPositionClamp;
                        }

                        DepthSpacePoint depthSpacePoint = this.coordinateMapper.MapCameraPointToDepthSpace(position);

                        jointPoint[jointType] = new Point(depthSpacePoint.X, depthSpacePoint.Y);

                        switch (jointType)
                        {

                            case JointType.Head:
                                double head_x = jointPoint[JointType.Head].X * kScaleX;
                                double head_y = jointPoint[JointType.Head].Y * kScaleY;
                                this.HeadPos = "head_x:" + head_x.ToString() + " " + "head_y" + head_y.ToString();

                                break;


                            case JointType.HandLeft:

                                double leftHand_x = jointPoint[JointType.HandLeft].X * kScaleX;
                                double leftHand_y = jointPoint[JointType.HandLeft].Y * kScaleY;
                                this.LeftHandPos = "leftHand_x:" + leftHand_x.ToString() + " " + "leftHand_y:" + leftHand_y.ToString();
                                break;

                            case JointType.HandRight:
                                double rightHand_x = jointPoint[JointType.HandRight].X * kScaleX;
                                double rightHand_y = jointPoint[JointType.HandRight].Y * kScaleY;
                                this.RightHandPos = "rightHand_x:" + rightHand_x.ToString() + " " + "rightHand_y:" + rightHand_y.ToString();

                                break;

                            case JointType.KneeRight:
                                double rightKnee_x = jointPoint[JointType.KneeRight].X * kScaleX;
                                double rightKnee_y = jointPoint[JointType.KneeRight].Y * kScaleY;
                                this.RightKneePos = "rightKnee_x:" + rightKnee_x.ToString() + " " + "rightKnee_y" + rightKnee_y.ToString();
                                break;

                            case JointType.KneeLeft:
                                double leftKnee_x = jointPoint[JointType.KneeLeft].X * kScaleX;
                                double leftKnee_y = jointPoint[JointType.KneeLeft].X * kScaleY;
                                this.LeftKneePos = "leftKnee_x:" + leftKnee_x.ToString() + " " + "leftKnee_y" + leftKnee_y.ToString();
                                break;

                            default:
                                break;
                        }

                    }

                    //体を描画
                    this.DrawBody(joints, jointPoint, dc, drawPen);

                    //マッチング処理
                    bool isMatched = this.isMatching(jointPoint, 0);
                    if (isMatched) this.MatchAlertColor = new SolidColorBrush(Colors.Blue);
                    else this.MatchAlertColor = new SolidColorBrush(Colors.Red);


                }

            }

            //レンダーエリア外に描画しないように防ぐ
            this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


        }
    }
}*/

//for (int bodyIndex = 0; bodyIndex < length; ++bodyIndex)
//{

//    if (cnt == 512)
//    {
//        cnt = 0;
//        ++line;
//        //colorIndex += 100;
//        bodyx = 0;
//        //++bodyline;
//        bodyline += procline;
//        procline = (float)(((bodyline + 1) * scaleY) - (bodyline * scaleY));
//        iprocline = (int)procline;
//        colorIndex = line * (int)procline * 1920;
//    }
//    ++cnt;

//    float fprocpix = (float)(((bodyx + 1) * scaleX) - (bodyx * scaleX));
//    int procpix = (int)(fprocpix);


//    byte player = this.bodyIndexFrameData[bodyIndex];

//    // if we're tracking a player for the current pixel, sets its color and alpha to full
//    if (player != 0xff)
//    {
//        // set source for copy to the color pixel
//        int sourceIndex = ((int)bodyx + (int)(bodyline) * 1920) * BYTES_PER_PIX;//colorIndex * BYTES_PER_PIX;

//        for (int t = 0; t < iprocline; ++t)
//        {

//            for (int i = 0; i < procpix; ++i)
//            {
//                this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];//b
//                this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];//g
//                this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];//r
//                this.displayPixels[sourceIndex++] = 0xff;//a
//            }
//            sourceIndex += 1920 * BYTES_PER_PIX;
//            sourceIndex -= procpix * BYTES_PER_PIX;
//        }

//    }
//    colorIndex += procpix;
//    bodyx += fprocpix;
//}



//using (ColorFrame colorFrame = multiSourceFrame.ColorFrameReference.AcquireFrame())
//{
//    if (colorFrame != null)
//    {
//        FrameDescription colorFrameDescription = colorFrame.FrameDescription;

//        using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
//        {
//            fallingbmp.Lock();

//            // verify data and write the new color frame data to the display bitmap
//            if ((colorFrameDescription.Width == fallingbmp.PixelWidth) && (colorFrameDescription.Height == fallingbmp.PixelHeight))
//            {
//                colorFrame.CopyConvertedFrameDataToIntPtr(
//                    fallingbmp.BackBuffer,
//                    (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
//                    ColorImageFormat.Bgra);

//                fallingbmp.AddDirtyRect(new Int32Rect(256, 0, COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT));
//            }

//            fallingbmp.Unlock();
//            waitingforclip = false;
//        }
//    }
//}


//for (int bodyIndex = 0; bodyIndex < length; ++bodyIndex)
//{

//    if (cnt == 512)
//    {
//        cnt = 0;
//        ++line;
//        //colorIndex += 100;
//        bodyx = 0;
//        //++bodyline;
//        bodyline += procline;
//        procline = (float)(((bodyline + 1) * scaleY) - (bodyline * scaleY));
//        iprocline = (int)procline;
//        colorIndex = line * (int)procline * 1920;
//    }
//    ++cnt;

//    float fprocpix = (float)(((bodyx + 1) * scaleX) - (bodyx * scaleX));
//    int procpix = (int)(fprocpix);


//    byte player = this.bodyIndexFrameData[bodyIndex];

//    // if we're tracking a player for the current pixel, sets its color and alpha to full
//    if (player != 0xff)
//    {
//        // set source for copy to the color pixel
//        int sourceIndex = ((int)bodyx + (int)(bodyline) * 1920) * BYTES_PER_PIX;//colorIndex * BYTES_PER_PIX;

//        for (int t = 0; t < iprocline; ++t)
//        {

//            for (int i = 0; i < procpix; ++i)
//            {
//                this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];//b
//                this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];//g
//                this.displayPixels[sourceIndex] = this.colorFrameData[sourceIndex++];//r
//                this.displayPixels[sourceIndex++] = 0xff;//a
//            }
//            sourceIndex += 1920 * BYTES_PER_PIX;
//            sourceIndex -= procpix * BYTES_PER_PIX;
//        }

//    }
//    colorIndex += procpix;
//    bodyx += fprocpix;
//}


////fallingbmp = new WriteableBitmap(colorWidth, colorHeight, 96.0, 96.0, PixelFormats.Bgra32, null);
//fallingbmp = new WriteableBitmap(COLOR_PIXELS_WIDTH, COLOR_PIXELS_HEIGHT, 96.0, 96.0, PixelFormats.Bgra32, null);
                    
#endregion

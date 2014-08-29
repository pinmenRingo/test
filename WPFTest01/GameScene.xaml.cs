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

        //テトリスのフィールドを管理するクラス
        TetrisGame tetris;

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
        private CoordinateMapper coorinateMapper = null; //座標系変換を扱ってるクラスっぽい
        //private multiFrameSourceReader multiFrameSourceReader = null;
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


        //以下画像制御
        static WriteableBitmap[] rows = new WriteableBitmap[20];
        static WriteableBitmap fallingbmp = null;//new WriteableBitmap(64, 64, 96.0, 96.0, PixelFormats.Bgra32, null)
        
        const int BLOCK_WIDTH_PIX = 16;
        const int FIELD_WIDTH_PIX = BLOCK_WIDTH_PIX*TetrisGame.FIELD_WIDTH;
        const int BLOCK_HEIGHT_PIX = 16;
        const int BYTES_PER_PIX = 4;
        static byte[] bytes = new byte[FIELD_WIDTH_PIX * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];
        static byte[] fallingbytes = new byte[BLOCK_HEIGHT_PIX * 4 * BLOCK_WIDTH_PIX * 4 * BYTES_PER_PIX];

        static byte[] temprowbytes = new byte[BLOCK_WIDTH_PIX * 4 * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];
        static byte[] tempfallingbytes = new byte[BLOCK_WIDTH_PIX * 4 * BLOCK_HEIGHT_PIX * BYTES_PER_PIX];

        static Random rand = new Random();//引数無しなのでシード値は時間からいい感じにやってくれるそうです


        //追加のキネクト要素
        private MultiSourceFrameReader multiFrameSourceReader = null;
        private WriteableBitmap bitmap = null;
        private int depthWidth, depthHeight;
        private ushort[] depthFrameData = null;
        private byte[] colorFrameData = null;
        private byte[] bodyIndexFrameData = null;
        private byte[] displayPixels = null;
        private DepthSpacePoint[] depthPoints = null;

        


        public GameScene()
        {
            //なんか元からあったやつ,おまじないって認識で
            InitializeComponent();

            //kinect
            this.kinectSensor = KinectSensor.GetDefault();
            this.coorinateMapper = this.kinectSensor.CoordinateMapper;
   
            //フレームサイズ
            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;
            this.gridWidth = matchGrid.Width / 4;
            this.gridHeight = matchGrid.Height / 4;

            viewBox.Width = this.displayWidth;
            viewBox.Height = this.displayHeight;

            matchGrid.Width = this.displayWidth;
            matchGrid.Height = this.displayHeight;

            this.InitMatchGrid();

            //this.multiFrameSourceReader = this.kinectSensor.BodyFrameSource.OpenReader();

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

            #region BitMap

            //32*16の画像を生成
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

            fallingbmp = new WriteableBitmap(64, 64, 96.0, 96.0, PixelFormats.Bgra32, null);
            //80*80の画像を生成
            for (int i = 0; i < BLOCK_WIDTH_PIX*BLOCK_HEIGHT_PIX*4*4; ++i)
            {
                byte t = 0x00;
                if(rand.Next(10)==1){
                    t = 0xff; fallingbytes[i * 4] = (byte)rand.Next(256); fallingbytes[i * 4 + 1] = (byte)rand.Next(256); fallingbytes[i * 4 + 2] = (byte)rand.Next(256);
                }
                else
                {
                    fallingbytes[i * 4] = 0x00; fallingbytes[i * 4 + 1] = 0x00; fallingbytes[i * 4 + 2] = 0x00;
                }
                fallingbytes[i * 4 + 3] = t;//(rand.Next(3) == 1) ? 0xff : 0x00;
            }
            fallingbmp.WritePixels(
                new Int32Rect(0,0,BLOCK_WIDTH_PIX*4,BLOCK_HEIGHT_PIX*4), fallingbytes, BLOCK_WIDTH_PIX*4*4, 0);

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
        public static void AddBMP(int x,int line, int bmpline)
        {
            Array.Clear(temprowbytes, 0, temprowbytes.Length);
            Array.Clear(tempfallingbytes, 0, tempfallingbytes.Length);

            int tx = (x - 2) < 0 ? 0 : x - 2;
            tx = tx > TetrisGame.FIELD_WIDTH - 4 ? TetrisGame.FIELD_WIDTH - 4 : tx;

            fallingbmp.CopyPixels(new Int32Rect(0,bmpline*BLOCK_HEIGHT_PIX,BLOCK_WIDTH_PIX*4,BLOCK_HEIGHT_PIX),tempfallingbytes, BLOCK_WIDTH_PIX * 4 * BYTES_PER_PIX, 0);

            Array.Clear(temprowbytes,0, temprowbytes.Length);

            rows[line].CopyPixels(new Int32Rect(tx * BLOCK_WIDTH_PIX, 0, BLOCK_WIDTH_PIX * 4, BLOCK_HEIGHT_PIX), temprowbytes, BLOCK_WIDTH_PIX*4 * BYTES_PER_PIX, 0);

            for (int i = BLOCK_WIDTH_PIX * 4 * BLOCK_HEIGHT_PIX - 1; i >= 0; --i)
            {
                if (temprowbytes[i * 4 + 3] == 0x00 && tempfallingbytes[i * 4 + 3] == 0xff)
                //if ( tempfallingbytes[i*4+3] == 0xff)
                {
                    temprowbytes[i * 4] = tempfallingbytes[i * 4];
                    temprowbytes[i * 4 + 1] = tempfallingbytes[i * 4 + 1];
                    temprowbytes[i * 4 + 2] = tempfallingbytes[i * 4 + 2];
                    temprowbytes[i * 4 + 3] = 0xff;
                }
            }

            rows[line].WritePixels(
                new Int32Rect(tx*BLOCK_WIDTH_PIX,0,BLOCK_WIDTH_PIX*4,BLOCK_HEIGHT_PIX),
                temprowbytes,
                BLOCK_WIDTH_PIX*4*BYTES_PER_PIX,
                0
                );

            //fallingを更新
            for (int i = 0; i < BLOCK_WIDTH_PIX * BLOCK_HEIGHT_PIX * 4 * 4; ++i)
            {
                byte t = 0x00;
                if (rand.Next(10) == 1)
                {
                    t = 0xff;
                    fallingbytes[i * 4] = (byte)rand.Next(256);
                    fallingbytes[i * 4 + 1] = (byte)rand.Next(256);
                    fallingbytes[i * 4 + 2] = (byte)rand.Next(256);
                }
                else
                {
                    fallingbytes[i * 4] = 0x00;
                    fallingbytes[i * 4 + 1] = 0x00;
                    fallingbytes[i * 4 + 2] = 0x00;
                }
                fallingbytes[i * 4 + 3] = t;//(rand.Next(3) == 1) ? 0xff : 0x00;
            }
            fallingbmp.WritePixels(
                new Int32Rect(0, 0, BLOCK_WIDTH_PIX * 4, BLOCK_HEIGHT_PIX * 4),
                fallingbytes,
                BLOCK_WIDTH_PIX * 4 * 4,
                0);


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

            //using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            using (BodyFrame bodyFrame = multiSourceFrame.BodyFrameReference.AcquireFrame())
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
                    dc.DrawRectangle(Brushes.Black, null, new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));

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

                                DepthSpacePoint depthSpacePoint = this.coorinateMapper.MapCameraPointToDepthSpace(position);

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

        #region binding_gamefield

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

#endregion

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
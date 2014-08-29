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
        private BodyFrameReader bodyFrameReader = null;
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

        //ブラシ
        private Brush headBrush = Brushes.Yellow;
        private Brush leftHandBrush = Brushes.Violet;
        private Brush rightHandBrush = Brushes.Turquoise;
        private Brush rightKneeBrush = Brushes.Tomato;
        private Brush leftKneeBrush = Brushes.Orange;


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

            //マッチングの際に表示する各Jointと色の辞書配列
           
            this.jointColors = new Dictionary<JointType,Brush>{
                {JointType.Head,headBrush},
                {JointType.HandLeft,leftHandBrush},
                {JointType.HandRight,rightHandBrush},
                {JointType.KneeRight,rightKneeBrush},
                {JointType.KneeLeft,leftKneeBrush}
            };


            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

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

            //マッチング用Gridの初期化（Labelを配置）
            this.InitMatchGrid();

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
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;
            }

            //matchGridの初期化
            this.InitMatchGrid();
            //matchGridに次落とすブロックの描画
            this.SetColorToMatchGrid();
        }

        private void GameSceneClosing(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
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
                    matchControl[y, x].Background = Brushes.Transparent;
                    matchControl[y, x].Opacity = 0.5;
                    matchControl[y, x].BorderThickness = new Thickness(1, 1, 1, 1);
                    matchGrid.Children.Add(this.matchControl[y, x]);

                    matchControl[y, x].SetValue(Grid.RowProperty, y);
                    matchControl[y, x].SetValue(Grid.ColumnProperty, x);

                }
            }


        }

        //今回落とすブロックをmatchGridに描画（Labelに色を付ける）
        private void SetColorToMatchGrid(int matchTemplateNum)
        {
            int rows = matchGrid.RowDefinitions.Count;
            int cols = matchGrid.ColumnDefinitions.Count;

            for (int y = 0; y < rows; y++)
            {
                for (int x = 0; x < cols; x++)
                {

                    switch (this.matchingTemplets[matchTemplateNum, y, x])
                    {
                        case (int)JointType.Head:
                            matchControl[y, x].Background = this.jointColors[JointType.Head];
                            matchControl[y, x].SetValue(Grid.RowProperty, y);
                            matchControl[y, x].SetValue(Grid.ColumnProperty, x);
                            break;

                        case (int)JointType.HandLeft:
                            matchControl[y,x].Background = this.jointColors[JointType.HandLeft];
                            matchControl[y, x].SetValue(Grid.RowProperty, y);
                            matchControl[y, x].SetValue(Grid.ColumnProperty, x);
                            break;

                        case (int)JointType.HandRight:
                            matchControl[y,x].Background = this.jointColors[JointType.HandRight];
                            matchControl[y, x].SetValue(Grid.RowProperty, y);
                            matchControl[y, x].SetValue(Grid.ColumnProperty, x);
                            break;

                        case (int)JointType.KneeLeft:
                            matchControl[y,x].Background = this.jointColors[JointType.KneeLeft];
                            matchControl[y, x].SetValue(Grid.RowProperty, y);
                            matchControl[y, x].SetValue(Grid.ColumnProperty, x);
                            break;

                        case (int)JointType.KneeRight:
                            matchControl[y,x].Background = this.jointColors[JointType.KneeRight];
                            matchControl[y, x].SetValue(Grid.RowProperty, y);
                            matchControl[y, x].SetValue(Grid.ColumnProperty, x);
                            break;
                    }

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
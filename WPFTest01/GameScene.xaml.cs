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
     
        /// <summary>
        /// Radius of drawn hand circles
        /// </summary>
        private const double HandSize = 30;

        /// <summary>
        /// Thickness of drawn joint lines
        /// </summary>
        private const double JointThickness = 3;

        /// <summary>
        /// Thickness of clip edge rectangles
        /// </summary>
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

        /// <summary>
        /// Width of display (depth space)
        /// </summary>
        private int displayWidth;

        /// <summary>
        /// Height of display (depth space)
        /// </summary>
        private int displayHeight;

        /// <summary>
        /// List of colors for each body tracked
        /// </summary>
        private List<Pen> bodyColors;

        /// <summary>
        /// Current status text to display
        /// </summary>
        private string statusText = null;


        public GameScene()
        {
            //なんか元からあったやつ,おまじないって認識で
            InitializeComponent();

            //kinect
            this.kinectSensor = KinectSensor.GetDefault();
            this.coorinateMapper = this.kinectSensor.CoordinateMapper;

            FrameDescription frameDescription = this.kinectSensor.DepthFrameSource.FrameDescription;
            this.displayWidth = frameDescription.Width;
            this.displayHeight = frameDescription.Height;

            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();

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

            // Left Arm
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ShoulderLeft, JointType.ElbowLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.ElbowLeft, JointType.WristLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.HandLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HandLeft, JointType.HandTipLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.WristLeft, JointType.ThumbLeft));

            // Right Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipRight, JointType.KneeRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeRight, JointType.AnkleRight));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleRight, JointType.FootRight));

            // Left Leg
            this.bones.Add(new Tuple<JointType, JointType>(JointType.HipLeft, JointType.KneeLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.KneeLeft, JointType.AnkleLeft));
            this.bones.Add(new Tuple<JointType, JointType>(JointType.AnkleLeft, JointType.FootLeft));

            this.bodyColors = new List<Pen>();
            this.bodyColors.Add(new Pen(Brushes.Red, 6)); //Pen = 図形を中抜き？
            this.bodyColors.Add(new Pen(Brushes.Orange, 6));
            this.bodyColors.Add(new Pen(Brushes.Green, 6));
            this.bodyColors.Add(new Pen(Brushes.Blue, 6));
            this.bodyColors.Add(new Pen(Brushes.Indigo, 6));
            this.bodyColors.Add(new Pen(Brushes.Violet, 6));


            this.kinectSensor.Open();

            this.drawingGroup = new DrawingGroup();

            this.imageSource = new DrawingImage(this.drawingGroup);

            this.DataContext = this; //バインディング関係


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

        public ImageSource ImageSource
        {
            get
            {
                return this.imageSource;
            }
        }

        private void GameSceneLoaded(object sender, RoutedEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;
            }
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
                            IReadOnlyDictionary<JointType, Joint> joints = body.Joints;

                            //jointのポイントをディスプレイに変換
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
                            }

                            //体を描画
                            this.DrawBody(joints, jointPoint, dc, drawPen);

                          
                        }
                      
                    }

                    //レンダーエリア外に描画しないように防ぐ
                    this.drawingGroup.ClipGeometry = new RectangleGeometry(new Rect(0.0, 0.0, this.displayWidth, this.displayHeight));


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
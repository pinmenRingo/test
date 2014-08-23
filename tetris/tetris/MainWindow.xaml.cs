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
using System.Windows.Threading;

namespace tetris
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    /// 

    public class Board
    {
        private int rows;
        private int cols;
        private int score;
        private int linesFilled;
        private Tetrimino currTetrimino;
        private Label[,] blockControls;

        static private Brush NoBrush = Brushes.Transparent;
        static private Brush SilverBush = Brushes.Gray;

        public Board(Grid TetrisGrid)
        {
            rows = TetrisGrid.RowDefinitions.Count;
            cols = TetrisGrid.ColumnDefinitions.Count;
            score = 0;
            linesFilled = 0;

            blockControls = new Label[cols, rows];

            //GridにLabelを配置していく
            for (int i = 0; i < cols; i++)
            {
                for (int j = 0; j < rows; j++)
                {
                    blockControls[i, j] = new Label();
                    blockControls[i, j].Background = NoBrush;
                    blockControls[i, j].BorderThickness = new Thickness(1, 1, 1, 1);

                    Grid.SetColumn(blockControls[i, j], i);
                    Grid.SetRow(blockControls[i, j], j);
                    TetrisGrid.Children.Add((blockControls[i, j]));
                }
            }

            currTetrimino = new Tetrimino();
            currTetriminoDraw();
        }

        public int getScore()
        {
            return score;
        }

        public int getLines()
        {
            return linesFilled;
        }

        private void currTetriminoDraw()
        {
            Point position = currTetrimino.getCurrPosition();
            Point[] shape = currTetrimino.getCurrentShape();
            Brush Color = currTetrimino.getCurrColor();

            foreach (Point s in shape)
            {
                blockControls[(int)(s.X + position.X),
                    (int)(s.Y + position.Y)].Background = Color;
            }
        }

        private void currTetriminoErase()
        {
            Point position = currTetrimino.getCurrPosition();
            Point[] shape = currTetrimino.getCurrentShape();

            foreach (Point s in shape)
            {
                blockControls[(int)(s.X + position.X),
                   (int)(s.Y + position.Y)].Background = NoBrush;
            }
        }

        private void CheckRows()
        {
            bool full;
            for (int i = rows - 1; i >  0; i--)
            {
                full = true;
                for (int j = 0; j < cols; j++)
                {
                    if (blockControls[j,i].Background == NoBrush)
                    {
                        full = false;
                    }
                }

                if (full)
                {
                    RemoveRows(i);
                    score += 100;
                    linesFilled += 1;
                }
            }

           
        }

        private void RemoveRows(int row)
        {
            for (int i = row; i > 2; i--)
            {
                for (int j = 0; j < cols; j++)
                {
                    blockControls[j, i].Background = blockControls[j, i - 1].Background;
                }
            }
        }

        #region 次のポジションチェック


        public void CurrTetriminoMoveLeft()
        {
            Point position = currTetrimino.getCurrPosition();
            Point[] shape = currTetrimino.getCurrentShape();
            bool move = true;
            currTetriminoErase();

            foreach (Point s in shape)
            {
                if(((int)(s.X + position.X) -1 < 0))
                {
                    move = false;
                }
                else if (blockControls[((int)(s.X + position.X) -1), (int)(s.Y + position.Y)].Background != NoBrush)
                {
                    move = false;
                }
            }

            if (move)
            {
                currTetrimino.moveLeft();
                currTetriminoDraw();
            }
            else
            {
                currTetriminoDraw();
            }
        }
        public void CurrTetriminoMoveRight()
        {
            Point position = currTetrimino.getCurrPosition();
            Point[] shape = currTetrimino.getCurrentShape();
            bool move = true;
            currTetriminoErase();

            foreach (Point s in shape)
            {
                if (((int)(s.X + position.X) + 1) >= cols)
                {
                    move = false;
          
                    
                }
                else if (blockControls[((int)(s.X + position.X)+1), (int)(s.Y + position.Y)].Background != NoBrush)
                {
                    move = false;
                }
            }

            if (move)
            {
                currTetrimino.moveRight();
                currTetriminoDraw();
            }
            else
            {
                currTetriminoDraw();
            }
        }
        public void CurrTetriminoMoveDown()
        {
            Point position = currTetrimino.getCurrPosition();
            Point[] shape = currTetrimino.getCurrentShape();
            bool move = true;
            currTetriminoErase();

            foreach (Point s in shape)
            {
                if (((int)(s.Y + position.Y) + 1) >= rows)
                {
                    move = false;
                }
                else if (blockControls[((int)(s.X + position.X)), (int)(s.Y + position.Y)+1].Background != NoBrush)
                {
                    move = false;
                }
            }

            if (move)
            {
                currTetrimino.moveDown();
                currTetriminoDraw();
            }
            else
            {
                currTetriminoDraw();
                CheckRows();
                currTetrimino = new Tetrimino();
            }



        }
        public void CurrTetriminoMoveRotate()
        {
            Point position = currTetrimino.getCurrPosition();
            Point[] s = new Point[4];
            Point[] shape = currTetrimino.getCurrentShape();
            bool move = true;
            shape.CopyTo(s,0);
            currTetriminoErase();

            for (int i = 0; i < s.Length; i++)
            {
                double x = s[i].X;
                s[i].X = s[i].Y * -1;
                s[i].Y = x;

                if (((int)((s[i].Y + position.Y) + 1)) >= rows)
                {
                    move = false;
                }
                else if (((int)(s[i].X + position.X) -1) < 0)
                {
                    move = false;
                }
                else if (((int)(s[i].X + position.X) + 1) >= cols)
                {
                    move = false;
                }
                else if (blockControls[(int)((s[i].X + position.X)+1), (int)(s[i].Y + position.Y) + 1].Background != NoBrush)
                {
                    move = false;
                }
                else if (blockControls[(int)((s[i].X + position.X) -1), (int)(s[i].Y + position.Y) + 1].Background != NoBrush)
                {
                    move = false;
                }
            
            }

            if (move)
            {
                currTetrimino.moveRotate();
                currTetriminoDraw();
            }
            else
            {
                currTetriminoDraw();
            }
        }

        #endregion


    }


    public class Tetrimino
    {

        private Point currPosition; //中心のブロックの位置
        private Point[] currShape;
        private Brush currColor;
        private bool rotate;

        public Tetrimino()
        {
            currPosition = new Point(4, 1);
            currColor = Brushes.Transparent;
            currShape = setRandomShape();
        }

        #region Getter

        public Brush getCurrColor()
        {
            return currColor;
        }

        public  Point getCurrPosition()
        {
            return currPosition;
        }

        public Point[] getCurrentShape()
        {
            return currShape;
        }

        #endregion

        #region Move
        public void moveLeft()
        {
            currPosition.X -= 1;
        }

        public void moveRight()
        {
            currPosition.X += 1;
        }

        public void moveDown()
        {
            currPosition.Y += 1;
        }

        public void moveRotate()
        {
            if (rotate)
            {
                for (int i = 0; i < currShape.Length; i++)
                {
                    double x = currShape[i].X;
                    currShape[i].X = currShape[i].Y * -1;
                    currShape[i].Y = x;
                }
            }
        }

        #endregion

        private Point[] setRandomShape()
        {
            Random rand = new Random();
            switch (rand.Next() % 7)
            {
                case 0: // I
                    rotate = true;
                    currColor = Brushes.Cyan;
                    return new Point[]{
                        new Point(0,0),
                        new Point(-1,0),
                        new Point(1,0),
                        new Point(2,0)
                    };
                    

                 case 1: // J
                    rotate = true;
                    currColor = Brushes.Blue;
                    return new Point[]{
                       new Point(0,0),
                       new Point(0,-1),
                       new Point(1,0),
                       new Point(2,0)
                    };
                     

                 case 2: // L
                    rotate = true;
                    currColor = Brushes.Orange;
                    return new Point[]{
                        new Point(0,0),
                        new Point(0,-1),
                        new Point(-1,0),
                        new Point(-2,0)
                    };
                      

                 case 3: // O
                    rotate = false;
                    currColor = Brushes.Yellow;
                    return new Point[]{
                        new Point(0,0),
                        new Point(0,1),
                        new Point(1,0),
                        new Point(1,1)
                    };
                      

                 case 4: // S
                    rotate = true;
                    currColor = Brushes.Green;
                    return new Point[]{
                        new Point(0,0),
                        new Point(-1,0),
                        new Point(0,-1),
                        new Point(1,0)
                    };
                      

                 case 5: //T
                    rotate = true;
                    currColor = Brushes.Purple;
                    return new Point[]{
                        new Point(0,0),
                        new Point(0,-1),
                        new Point(1,0),
                        new Point(-1,0)
                    };
                      

                 case 6: // Z
                    rotate = true;
                    currColor = Brushes.Red;
                    return new Point[]{
                        new Point(0,0),
                        new Point(-1,0),
                        new Point(0,1),
                        new Point(1,1)
                    };

                default:
                    return null;
            }
        }
    }


    public partial class MainWindow : Window
    {
        DispatcherTimer timer;
        Board myBoard;

        public MainWindow()
        {
            InitializeComponent();
        }

        void MainWindow_Initilized(object sender, EventArgs e)
        {
            timer = new DispatcherTimer();
            timer.Tick += new EventHandler(GameTick);
            timer.Interval = new TimeSpan(0, 0, 0, 0, 400);
            GameStart();
        }
        private void GameStart()
        {
            MainGrid.Children.Clear();
            myBoard = new Board(MainGrid);
            timer.Start();
        }

        void GameTick(object sender,EventArgs e)
        {
            Score.Content = myBoard.getScore().ToString("0000000000");
            Lines.Content = myBoard.getLines().ToString("0000000000");
            myBoard.CurrTetriminoMoveDown();
        }

        private void GamePause()
        {
            if (timer.IsEnabled) timer.Stop();
            else timer.Start();
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Left:
                    if (timer.IsEnabled) myBoard.CurrTetriminoMoveLeft();
                    break;

                case Key.Right:
                    if (timer.IsEnabled) myBoard.CurrTetriminoMoveRight();
                    break;

                case Key.Down:
                    if (timer.IsEnabled) myBoard.CurrTetriminoMoveDown();
                    break;

                case Key.Up:
                    if (timer.IsEnabled) myBoard.CurrTetriminoMoveRotate();
                    break;

                case Key.F2:
                    GameStart();
                    break;
                case Key.F3:
                    GamePause();
                    break;


            }
        }


    }
}

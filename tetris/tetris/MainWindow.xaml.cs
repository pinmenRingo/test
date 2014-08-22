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
using System.Drawing;

namespace tetris
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
    }

    public class Piece
    {
        private bool[,] piece;
        private Point location;

        public const int WIDTH = 5;
        public const int HEIGHT = 5;

        public int X
        {
            get { return (int)location.X ; }
            set { location.X = value; }

        }

        public int Y
        {
            get { return (int)location.Y; }
            set { location.Y = value; }
        }

        public int MarginTop
        {
            get
            {
                for (int y = 0; y < HEIGHT; y++)
                {
                    for (int x = 0; x <WIDTH; x++)
                    {
                        if (this[x, y]) return y;
                    }
                }
                return HEIGHT - 1;
            }
        }

        public int MarginBottom
        {
            get
            {
                for (int y = HEIGHT - 1; y > 0; y--)
                {
                    for (int x = 0; x < WIDTH; x++)
                    {
                        if (this[x, y]) return y;
                         
                    }
                }

                return 0;
            }
        }

        public int MarginLeft
        {
            get
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    for (int y = 0; y < HEIGHT; y++)
                    {
                        if (this[x, y]) return x;
                    }
                }

                return WIDTH - 1;
            }
        }

        public int MarginRight
        {
            get
            {
                for (int x = WIDTH - 1; x > 0; x--)
                {
                    for (int y = 0; y < HEIGHT; y++)
                    {
                        if (this[x, y]) return x;
                    }
                }

                return 0;
            }
        }

        public bool this[int x, int y]
        {
            get { return piece[x,y];}
            set { piece[x, y] = value; }
        }

        public static Piece Random()
        {
            Piece result = new Piece();
            Random random = new Random();

            result[2, 2] = true;
            result[2, 3] = true;
            switch (random.Next(7))
            {
                case 0:
                    result[2, 1] = true;
                    result[2, 4] = true;
                    break;

                case 1:
                    result[2, 1] = true;
                    result[3, 3] = true;
                    break;

                case 2:
                    result[2, 1] = true;
                    result[1, 3] = true;
                    break;

                case 3:
                    result[3, 2] = true;
                    result[1, 3] = true;
                    break;

                case 4:
                    result[1, 2] = true;
                    result[3, 3] = true;
                    break;

                case 5:
                    result[1, 2] = true;
                    result[1, 3] = true;
                    break;

                case 6:
                    result[1, 3] = true;
                    result[3, 3] = true;
                    break;
            }

            return result;
        }

        public Piece TurnPiece()
        {
            Piece result = new Piece();
            result.X = X;
            result.Y = Y;

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    result[HEIGHT-1-y,x] = this[x,y];
                }
            }

            return result;
        }

        public Piece Copy()
        {
            Piece result = new Piece();
            result.X = X;
            result.Y = Y;

            for (int y = 0; y < HEIGHT; y++)
            {
                for (int x = 0; x < WIDTH; x++)
                {
                    result[x, y] = this[x, y];
                }
            }

            return result;
        }

        public Piece()
        {
            piece = new bool[WIDTH, HEIGHT];
            location = new Point(0, 0);
        }
    }

    public class Field
    {
        private int width;
        private int height;
        private bool[,] field;

        public int Width
        {
            get
            { return width;}
            set
            { width = value;}
        }

        public int Height
        {
            get { return height; }
            set { height = value; }
        }

        public bool this[int x, int y]
        {
            get { return field[x, y]; }
            set { field[x, y] = value; }
        }

        public void PieceToField(Piece piece)
        {
            for (int y = 0; y < Piece.WIDTH; y++)
            {
                int ty = piece.Y + y;

                if (ty < 0)
                {
                    continue;
                }
                else if (ty >= Height)
                {
                    break;
                }

                for (int x = 0; x < Piece.HEIGHT; x++)
                {
                    if (piece[x,y])
                    {
                        int tx = piece.X + x;
                        if (tx < 0)
                        {
                            continue;
                        }
                        else if (tx >= Width)
                        {
                            break;
                        }

                        this[tx, ty] = piece[x, y];
                    }
                }
            }
        }

        public bool Contains(Piece piece)
        {
            if (piece.X + piece.MarginLeft < 0) return false;
            else if (piece.X + piece.MarginRight >= Width) return false;
            else if (piece.Y + piece.MarginBottom >= Height) return false;
            else if (piece.Y + piece.MarginTop < 0) return false;

            return true;
        }

        public bool IntersectsWith(Piece piece)
        {
            for (int y = 0; y < Piece.HEIGHT; y++)
            {
                int ty = piece.Y + y;
                if (ty < 0)
                {
                    continue;
                }
                else if (ty >= Height) break;

                for (int x = 0; x < Piece.WIDTH; x++)
                {
                    int tx = piece.X + x;
                    if (tx < 0) continue;
                    else if (tx >= Width) break;

                    if (piece[x, y] && this[tx, ty]) return true;
                }
            }

            return false;
        }

        public int AdjustLine()
        {
            int delCount = 0;
            for (int y = Height - 1; y >= 0; )
            {
                int lineCount = 0;
                for (int x = 0; x < this.Width; x++)
                {
                    if (this[x, y]) lineCount++;
                }

                if (lineCount != Width)
                {
                    y--;
                    continue;
                }

                delCount++;
                for (int iy = y; iy >= 0; iy--)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (iy > 0) this[x, iy] = this[x, iy - 1];
                        else this[x, 0] = false;
                    }
                }
            }

            return delCount;
        }

        public Field(int width, int height)
        {
            this.width = width;
            this.height = height;
            field = new bool[width, height];
        }

    }
}

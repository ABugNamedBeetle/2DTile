using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using BugTools.OpenGl;

namespace Application
{
    public class Tile2D
    {

        public static void Setup(TContext tr, int canvasW, int canvasH, int tileWidth, int tileHeight)
        {
            tr.CanvWidth = canvasW; tr.CanvHeight = canvasH;
            tr.TileWidth = tileWidth; tr.TileHeight = tileHeight;
            
        }
        static uint tex;
        static BitmapData Data;
        public static void UpdateTexture(TContext tr,  Bitmap texImage)
        {
            Gl.glGenTextures(1, out tex);
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, tex);
            Data = texImage.LockBits(new Rectangle(0, 0, texImage.Width, texImage.Height), ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            tr.TexImage = Data.Scan0;
            Gl.glTexImage2D(Gl.GL_TEXTURE_2D, 0, Gl.GL_RGBA, texImage.Width, texImage.Height, 0, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, tr.TexImage);
            texImage.UnlockBits(Data);
            texImage.Dispose();
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MIN_FILTER, Gl.GL_LINEAR);
            Gl.glTexParameteri(Gl.GL_TEXTURE_2D, Gl.GL_TEXTURE_MAG_FILTER, Gl.GL_LINEAR);

        }

        public static void ComputeTileRange(TContext tr)
        {
            tr.TileRange.LLx = tr.Texture.LLx / tr.TileWidth;
            tr.TileRange.LLy = tr.Texture.LLy / tr.TileHeight;

            tr.TileRange.TRx = tr.Texture.TRx / tr.TileWidth;
            tr.TileRange.TRy = tr.Texture.TRy / tr.TileHeight;

        }

        public static bool DrawTexture(TContext tr, TexCod texture)
        {
            tr.Texture = texture;
            ComputeTileRange(tr);
            // calculate tile statrting address
            tr.TileAdrs.X = texture.LLx / tr.TileWidth;
            tr.TileAdrs.Y = texture.LLy / tr.TileHeight;
            if (TileDraw(tr)) { return (bool)true; }
            return false; 
        }

        private static bool TileDraw(TContext tr)
        {
            // choose tile lowleft coordinate

            Gl.glBindTexture(Gl.GL_TEXTURE_2D, tex);
            Gl.glReadBuffer(Gl.GL_BACK);
            while (tr.TileRange.LLy <= tr.TileAdrs.Y && tr.TileAdrs.Y <= tr.TileRange.TRy)
            {
                while (tr.TileRange.LLx <= tr.TileAdrs.X && tr.TileAdrs.X <= tr.TileRange.TRx)
                {

                    // Console.WriteLine(tr.TileAdrs.ToString());
                    tr.LLTileCod.X = tr.TileAdrs.X * tr.TileWidth;
                    tr.LLTileCod.Y = tr.TileAdrs.Y * tr.TileHeight;
                    tr.TRTileCod.X = tr.LLTileCod.X + tr.TileWidth - 1;
                    tr.TRTileCod.Y = tr.LLTileCod.Y + tr.TileHeight - 1;
                    // overalpping coorfinate
                    tr.OverlapLL = CalOverlap(tr.LLTileCod, tr.Texture);
                    tr.OverlapTR = CalOverlap(tr.TRTileCod, tr.Texture);
                    //  Console.WriteLine("overlap " + tr.OverlapLL.ToString() +"*"+ tr.OverlapTR.ToString());

                    // calulate texmap coordinate
                    CalTexMap(tr);
                    //  Console.WriteLine(tr.TexMapCod.ToString());
                    // re- calculate overlap as per rendering viewport
                    RecalView(tr);
                    //  Console.WriteLine("viewport " + tr.OverlapLL.ToString() + "*" + tr.OverlapTR.ToString());

                    TileRender(tr);
                    // reading into PBO genrated before
                    Gl.glPixelStorei(Gl.GL_PACK_ROW_LENGTH, tr.CanvWidth);
                    Gl.glPixelStorei(Gl.GL_PACK_SKIP_ROWS, tr.TileAdrs.Y * tr.TileHeight);
                    Gl.glPixelStorei(Gl.GL_PACK_SKIP_PIXELS, tr.TileAdrs.X * tr.TileWidth);
                    Gl.glReadPixels(0, 0, tr.TileWidth,tr.TileWidth, Gl.GL_BGRA, Gl.GL_UNSIGNED_BYTE, IntPtr.Zero);
                    // choose the next tile to be rendered
                    // shift right
                    tr.TileAdrs.X = tr.TileAdrs.X + 1;
                }

                tr.TileAdrs.X = tr.TileRange.LLx;
                tr.TileAdrs.Y = tr.TileAdrs.Y + 1;
            }
                    
            
            Gl.glBindTexture(Gl.GL_TEXTURE_2D, 0);
            return (bool)true;

        }

        static float x0, y0, x1, y1;
        public static void TileRender(TContext tr) // render and store
        {
              
            Gl.glClearColor(0f,0f,0f,0f);
            Gl.glClear(Gl.GL_COLOR_BUFFER_BIT);
            Gl.glViewport(0, 0, tr.TileWidth,tr.TileHeight);
            Gl.glEnable(Gl.GL_TEXTURE_2D);
            Gl.glBegin(Gl.GL_QUADS);
           
             x0 = Utility.Map((float)tr.OverlapLL.X,0f,(float)tr.TileWidth,-1f,1f);
             y0 = Utility.Map((float)tr.OverlapLL.Y,0f,(float)tr.TileHeight,-1f,1f);
             x1 = Utility.Map((float)tr.OverlapTR.X, 0f, (float)tr.TileWidth, -1f, 1f);
             y1 = Utility.Map((float)tr.OverlapTR.Y, 0f, (float)tr.TileHeight, -1f, 1f);
            Gl.glTexCoord2f( tr.TexMapCod.X0, tr.TexMapCod.Y0); Gl.glVertex2f(x0,y1);
            Gl.glTexCoord2f( tr.TexMapCod.X0, tr.TexMapCod.Y1); Gl.glVertex2f(x0,y0);
            Gl.glTexCoord2f( tr.TexMapCod.X1, tr.TexMapCod.Y1); Gl.glVertex2f(x1,y0);
            Gl.glTexCoord2f( tr.TexMapCod.X1, tr.TexMapCod.Y0); Gl.glVertex2f(x1,y1);
            Gl.glEnd();
            
            Gl.glFlush(); 
            

        }

        public static TileCod CalOverlap(TileCod tile, TexCod texture)
        {
            TileCod Overlap = new TileCod();

            if (texture.LLx <= tile.X && tile.X <= texture.TRx && texture.LLy <= tile.Y && tile.Y <= texture.TRy) //in X and Y
            {
                Overlap = tile;
                return Overlap;
            }
            else if (texture.LLx <= tile.X && tile.X <= texture.TRx) // only X 
            {
                Overlap.X = tile.X;

                if (tile.Y <= texture.LLy) { Overlap.Y = texture.LLy; }
                else if (tile.Y >= texture.TRy) { Overlap.Y = texture.TRy; }
                return Overlap;
            }
            else if (texture.LLy <= tile.Y && tile.Y <= texture.TRy)// only in Y
            {
                if (tile.X <= texture.LLx) { Overlap.X = texture.LLx; }
                else if (tile.X >= texture.TRx) { Overlap.X = texture.TRx; }
                Overlap.Y = tile.Y;
                return Overlap;
            }
            else
            {
                if (tile.X <= texture.LLx && tile.Y <= texture.LLy) { Overlap.X = texture.LLx; Overlap.Y = texture.LLy; } // nor
                else if (tile.X >= texture.TRx && tile.Y >= texture.TRy) { Overlap.X = texture.TRx; Overlap.Y = texture.TRy; }
                return Overlap;
            }


        }
        public static void CalTexMap(TContext tr)
        {
            tr.TexMapCod.X0 = Utility.Map((float)tr.OverlapLL.X, (float)tr.Texture.LLx, (float)tr.Texture.TRx, 0f, 1f);
            tr.TexMapCod.Y0 = Utility.Map((float)tr.OverlapTR.Y, (float)tr.Texture.TRy, (float)tr.Texture.LLy, 0f, 1f);

            tr.TexMapCod.X1 = Utility.Map((float)tr.OverlapTR.X, (float)tr.Texture.LLx, (float)tr.Texture.TRx, 0f, 1f);
            tr.TexMapCod.Y1 = Utility.Map((float)tr.OverlapLL.Y, (float)tr.Texture.TRy, (float)tr.Texture.LLy, 0f, 1f);
        }
        public static void RecalView(TContext tr)
        {
            tr.OverlapLL.X = tr.OverlapLL.X - tr.LLTileCod.X;
            tr.OverlapLL.Y = tr.OverlapLL.Y - tr.LLTileCod.Y;
            tr.OverlapTR.X = tr.OverlapTR.X - tr.LLTileCod.X+1;
            tr.OverlapTR.Y = tr.OverlapTR.Y - tr.LLTileCod.Y+1;
        }

    }
    public class TContext
    {
        // canvas parameters
        public int CanvHeight = new int();
        public int CanvWidth = new int();
        //texture parameters

        public TexCod Texture = new TexCod(); // coordinate of Lower Left tile containing Lower Left tex cod
        public IntPtr TexImage = IntPtr.Zero;


        // tile parameters
        public int TileWidth = new int();
        public int TileHeight = new int();
       
        public IntPtr TileBuffer = IntPtr.Zero;

        public TileCod LLTileCod = new TileCod(); //pixel coordinate of Lower Left tile 
        public TileCod TRTileCod = new TileCod(); //pixel coordinate of Top Right tile
        public TileCod TileAdrs = new TileCod();
        
        public TileCod OverlapLL = new TileCod();
        public TileCod OverlapTR = new TileCod();
        public TexMap TexMapCod = new TexMap(); // text cord on texture map (float x0y0 x1y1)
        public TexCod TileRange = new TexCod();



        //miscellaneous parameter
        public int tileCount = -1;
       
        public int[] viewSave = new int[4];
    }
    public struct TexCod
    {
        int _LLx, _LLy, _TRx, _TRy;
         
        public TexCod(int Llx, int Lly, int Trx, int Try)
        {
            _LLx = Llx;
            _LLy = Lly;
            _TRx = Trx;
            _TRy = Try;
        }
        public int LLx { get { return _LLx; } set { _LLx = value; } }
        public int LLy { get { return _LLy; } set { _LLy = value; } }
        public int TRx { get { return _TRx; } set { _TRx = value; } }
        public int TRy { get { return _TRy; } set { _TRy = value; } }
        
        public override string ToString()
        {
            string myString = String.Format("LL=({0},{1}), TR=({2},{3})", _LLx, _LLy,_TRx,_TRy);
            return myString;
        }
    }
    public struct TileCod
    {
        int _X,_Y;
        public TileCod(int x, int y)
        {
            _X = x;
            _Y = y;

        }

        public int X { get { return _X; } set { _X = value; } }
        public int Y { get { return _Y; } set { _Y = value; } }

        public override string ToString()
        {
            string myString = String.Format("X = {0}, Y = {1}", _X, _Y);
            return myString;
        }
        



    }
    public struct TexMap
    {
         float _X0, _Y0, _X1, _Y1;
       
        public TexMap(float x0, float y0, float x1, float y1)
        {
            _X0 = x0;
            _Y0 = y0;
            _X1 = x1;
            _Y1 = y1;
        }
        public float X0 { get { return _X0; } set{_X0 = value;} }
        public float Y0 { get{return _Y0;}    set{_Y0 = value; }}
        public float X1 { get{return _X1;}    set{_X1 = value; }}
        public float Y1 { get { return _Y1; } set { _Y1 = value; } }
        public override string ToString()
        {
            string myString = String.Format("T0=({0},{1}), T1=({2},{3})", _X0, _Y0, _X1, _Y1);
            return myString;
        }

    }

}

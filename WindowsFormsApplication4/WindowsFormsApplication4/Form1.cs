using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Windows.Forms;
//Deteccion de camara
using AForge.Video;
using AForge.Video.DirectShow;

//Procesamiento de video y imagen
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Vision.Motion;



namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        //LISTA DE DISPOSITIVOS
        private FilterInfoCollection Dispositivos;
        //DISPOSITIVO QUE USAREMOS COMO FUENTE
        private VideoCaptureDevice FuenteDeVideo;
        //Detector.
        MotionDetector Detector;
        float NivelDeDeteccion;
        bool filtro = false;
        int red, green, blue;
        //carga todo.
        private void Form1_Load(object sender, EventArgs e)
        {

            //LISTAR DISPOSITIVOS DE ENTRADAS DE VIDEO
            Dispositivos = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            //Dispositivos tiene el array, con todos los dispositivos disponibles
            foreach (FilterInfo x in Dispositivos)
            {
                comboBox1.Items.Add(x.Name);
            }
            comboBox1.SelectedIndex = 0;
        }

        //Botón comenzar.
        private void button1_Click(object sender, EventArgs e)
        {
            //ESTABLECER EL DISPOSITIVO SELECCIONADO COMO FUENTE DE VIDEO
            FuenteDeVideo = new VideoCaptureDevice(Dispositivos[comboBox1.SelectedIndex].MonikerString);
            //INICIALIZAR EL CONTROL
            videoSourcePlayer1.VideoSource = FuenteDeVideo;
            //INICIAR LA RECEPCIÓN DE IMAGENES
            videoSourcePlayer1.Start();

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        //Detener video.
        private void button2_Click(object sender, EventArgs e)
        {
            videoSourcePlayer1.SignalToStop();
        }

        //Boton iniciar detector.
        private void button3_Click(object sender, EventArgs e)
        {
            Detector = new MotionDetector(new TwoFramesDifferenceDetector(), new MotionBorderHighlighting());
            NivelDeDeteccion = 0;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            videoSourcePlayer2.VideoSource = FuenteDeVideo;
            videoSourcePlayer2.Start();
            filtro = true;
        }

        //Frames de video.
        private void videoSourcePlayer1_NewFrame(object sender, ref Bitmap image)
        {

            if (Detector != null)
            {
                Bitmap grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);
                NivelDeDeteccion = Detector.ProcessFrame(image);
                while (NivelDeDeteccion > 0.02)
                {
                    //Controla que si la detección es mayor a 0.02 se guarda automáticamente en mi escritorio. 
                    // Comentar para mostar funcionamiento del Detector Ctrl+k+c
                    Bitmap img = videoSourcePlayer1.GetCurrentVideoFrame();
                    SaveFileDialog sf = new SaveFileDialog();
                    
                    img.Save("C:\\Users\\Mauro\\Desktop\\Deteccion.bmp");
                    //MessageBox.Show("Movimientos detectados guardando captura en escritorio.");
                    NivelDeDeteccion = 0;
                    img.Dispose();
                }

            }


        }

        //Botón Detener detección de movimientos.
        private void button4_Click(object sender, EventArgs e)
        {
            Detector = null;
        }

        private void videoSourcePlayer2_NewFrame_1(object sender, ref Bitmap image)
        {
            if (filtro == true)
            {
                //Threshold filter = new Threshold(5000);
                //filter.ApplyInPlace(image);
                //IterativeThreshold filter = new IterativeThreshold(2, 128);
                //Bitmap newImage = filter.Apply(image);
                //Bitmap grayImage = Grayscale.CommonAlgorithms.BT709.Apply(image);

                //Bitmap video = (Bitmap)eventArgs.Frame.Clone();
                //Bitmap video2 = (Bitmap)eventArgs.Frame.Clone();
                //Bitmap objectsImage = null;
                //EuclideanColorFiltering filter = new EuclideanColorFiltering();
                //filter.CenterColor = Color.FromArgb(5,5,5);
                // filter.Radius = 100;
                  ColorFiltering filtrado = new ColorFiltering();
                 filtrado.Red = new AForge.IntRange(red, (int)numericUpDown1.Value);
                 filtrado.Green = new AForge.IntRange(green, (int)numericUpDown2.Value);
                 filtrado.Blue = new AForge.IntRange(blue, (int)numericUpDown3.Value);
                 filtrado.ApplyInPlace(image);
                // Creacion de la escala blanco y negro
                // BitmapData objectsData = image.LockBits (new Rectangle (0, 0, image.Width, image.Height), ImageLockMode.ReadOnly, image.PixelFormat);
                Grayscale grayscaleFilter = new Grayscale(0.2125, 0.7154, 0.0721);
                //UnmanagedImage grayImage = grayscaleFilter.Apply (new UnmanagedImage (objectsData));
                // image.UnlockBits (objectsData);

                //GrayscaleBT709 grayscaleFilter = new GrayscaleBT709( ); OBSOLETO.
                // Aplico filtro blanco y negro al cuadro de la webcam
                Bitmap grayImage = grayscaleFilter.Apply(image);
                // BitmapData objectsData = objectsImage.LockBits(new Rectangle(0, 0, image.Width, image.Height),

                //  ImageLockMode.ReadOnly, image.PixelFormat);

                // UnmanagedImage grayImage = grayscaleFilter.Apply(new UnmanagedImage(objectsData));

                // contador blob
                BlobCounter blobCounter = new BlobCounter();
                //// configuración de los filtros del rectangulo
                blobCounter.MinWidth = 10;
                blobCounter.MinHeight = 10;
                blobCounter.FilterBlobs = true;
                // // Ordena por objeto mayor
                blobCounter.ObjectsOrder = ObjectsOrder.Size;

                //// blobs
                blobCounter.ProcessImage(grayImage);
               Rectangle[] rects = blobCounter.GetObjectsRectangles();

                //// Captura el objeto mayor
                if (rects.Length > 0)
                {
                    
                    Rectangle objectRect = rects[0];
                   Graphics g = Graphics.FromImage(image);

                    using (Pen pen = new Pen(Color.FromArgb(160, 255, 160), 3))
                   {
                        //Creación de estadísticas X e Y como tambien estadísticas de tamaño.
                        //Dibujo del rectangulo
                        g.DrawRectangle(pen, objectRect);
                        PointF drawPoin = new PointF(objectRect.X, objectRect.Y);
                        //posición del rectánculo
                        int objectX = objectRect.X + objectRect.Width / 2 - image.Width / 2;
                        int objectY = image.Height / 2 - (objectRect.Y + objectRect.Height / 2);
                        //Estadísticas 
                        String Blobinformation = "Eje X= " + objectX.ToString() + "\nEje Y= " + objectY.ToString() + "\nTamaño=" + objectRect.Size.ToString() +"\nLocalización=" + objectRect.Location.ToString(); 
                        g.DrawString(Blobinformation, new Font("Arial", 16), new SolidBrush(Color.Red), drawPoin);
                      
                    
                    }

                    g.Dispose();


                //}
                // else
                //{

                //     if(!showObjectsOnly)
                //    {

                //        objectsImage.Dispose();
                //    }
                //  grayImage.Dispose();

                //}

                //        }

                //    }

                //}


                //Detección de objetos

            }





        }
    }
        //Barras scroll de tamaño mínimo (Recordar como cambie los valores el propiedades, horas y horas.)
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            red = (int)trackBar1.Value;
        }

        private void trackBar2_Scroll(object sender, EventArgs e)
        {
            green = (int)trackBar2.Value;
        }

        private void trackBar3_Scroll(object sender, EventArgs e)
        {
            blue = (int)trackBar3.Value;
        }



    }
}

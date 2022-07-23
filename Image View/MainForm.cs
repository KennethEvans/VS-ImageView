#define USE_STARTUP_FILE
//#define DEBUG_AVAILABLE_FORMATS

using KEUtils.About;
using KEUtils.Utils;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Media;
using System.Net;
using System.Reflection;
using System.Windows.Forms;

namespace Image_View {
    public partial class MainForm : Form {
        public static readonly String NL = Environment.NewLine;
        public static readonly float MOUSE_WHEEL_ZOOM_FACTOR = 0.001F;
        public static readonly float KEY_ZOOM_FACTOR = 1.1F;
        public static readonly float ZOOM_MIN = 0.1F;
        public static readonly int MOVE_UP = 1;
        public static readonly int MOVE_DOWN = 2;
        public static readonly int MOVE_LEFT = 4;
        public static readonly int MOVE_RIGHT = 8;
        public static readonly int SHRINK_HEIGHT = 16;
        public static readonly int EXPAND_HEIGHT = 32;
        public static readonly int SHRINK_WIDTH = 64;
        public static readonly int EXPAND_RIGHT = 128;
        public enum ClipboardDataTypes { NULL, BITMAP, URL, CLIPBOARD };

        public Image Image { get; set; }
        public Image ImageOrig { get; set; }
        public Image ImageCrop { get; set; }
        public bool Panning { get; set; }
        public bool KeyPanning { get; set; }
        public Point PanStart { get; set; }
        float ZoomFactor { get; set; }
        public RectangleF ViewRectangle { get; set; }
        public Rectangle CropRectangle { get; set; }
        public string FileName { get; set; }

        public bool Cropping { get; set; }
        public int CropX { get; set; }
        public int CropY { get; set; }
        public int CropWidth { get; set; }
        public int CropHeight { get; set; }
        public Pen CropPen { get; set; }



        public float LeftMargin { get; set; } = 1f;
        public float RightMargin { get; set; } = 1f;
        public float TopMargin { get; set; } = 1f;
        public float BottomMargin { get; set; } = 1f;

        public PointF DPI { get; set; }
        public ImageList ToolsImageList { get; set; }
        public Size ToolsImageSize { get; set; }


        public MainForm() {
            InitializeComponent();

            ZoomFactor = 1.0F;
            pictureBox.MouseWheel += new MouseEventHandler(OnPictureBoxMouseWheel);
            this.MouseWheel += new MouseEventHandler(OnPictureBoxMouseWheel);
        }

        private void zoomImage() {
            Size clientSize = pictureBox.ClientSize;
            float newWidth = clientSize.Width * ZoomFactor;
            float newHeight = clientSize.Height * ZoomFactor;
            // Make it appear as if the zoom were at the center
            float newX = ViewRectangle.X - .5F * (newWidth - ViewRectangle.Width);
            float newY = ViewRectangle.Y - .5F * (newHeight - ViewRectangle.Height);
            ViewRectangle = new RectangleF(newX, newY, newWidth, newHeight);
            pictureBox.Invalidate();
        }

        private void resetViewToFit() {
            if (Image == null || Image.Width <= 0 || Image.Height <= 0) {
                return;
            }
            Size clientSize = pictureBox.ClientSize;
            float aspect = (float)Image.Height / Image.Width;
            float clientAspect = (float)clientSize.Height / clientSize.Width;
            if (aspect < clientAspect) {
                ZoomFactor = (float)Image.Width / clientSize.Width;
            } else {
                ZoomFactor = (float)Image.Height / clientSize.Height;
            }
            float newWidth = clientSize.Width * ZoomFactor;
            float newHeight = clientSize.Height * ZoomFactor;
            // Center it
            float newX = .5F * (Image.Width - newWidth);
            float newY = .5F * (Image.Height - newHeight);
            ViewRectangle = new RectangleF(newX, newY, newWidth, newHeight);
            pictureBox.Invalidate();
        }

        private void resetImage() {
            resetImage(null, false, null);
        }

        private void resetImage(string fileName, bool replace) {
            resetImage(fileName, true, null);
        }

        /// <summary>
        /// Main method to create or reset the image/
        /// </summary>
        /// <param name="fileName">If replace is true, gets the image from this filename.</param>
        /// <param name="replace">Whether to replace the current image or not.</param>
        /// <param name="newImage">If non-null, use this image.</param>
        private void resetImage(string fileName, bool replace, Image newImage) {
            if (replace) {
                if (fileName != null) {
                    if (Image != null) {
                        Image.Dispose();
                        Image = null;
                    }
                    if (ImageOrig != null) {
                        ImageOrig.Dispose();
                        ImageOrig = null;
                    }
                    // Check for sources that are not file names
                    bool found = false;
                    foreach (string name in Enum.GetNames(typeof(ClipboardDataTypes))) {
                        if (fileName.StartsWith(name)) {
                            found = true;
                            if (newImage != null) {
                                Image = newImage;
                                ImageOrig = (Image)Image.Clone();
                                FileName = fileName;
                            }
                            break;
                        }
                    }
                    if (!found) {
                        if (Image != null) Image.Dispose();
                        if (ImageOrig != null) Image.Dispose();
                        Image = new Bitmap(fileName);
                        if (Image != null) {
                            ImageOrig = (Image)Image.Clone();
                        }
                        FileName = fileName;
                    }
                } else {
                    // fileName is null
                    if (newImage != null) {
                        Image = newImage;
                        // This case is used for resetting the same image
                        // Do not change the FileName
                        //FileName = ClipboardDataTypes.NULL.ToString();
                    }
                }
            } // End of if(replace)
            ZoomFactor = 1.0F;
            Size clientSize = pictureBox.ClientSize;
            ViewRectangle = new RectangleF(0, 0, clientSize.Width, clientSize.Height);
            //ViewImage = new Bitmap(clientSize.Width, clientSize.Height);
            //pictureBox.Image = ViewImage;
            pictureBox.Invalidate();
            if (replace) {
                resetViewToFit();
            }
        }

        private void resetCropping() {
            Cropping = false;
            if (Panning) {
                Cursor = Cursors.Hand;
            } else {
                Cursor = Cursors.Default;
            }
            if (CropPen != null) {
                CropPen.Dispose();
                CropPen = null;
            }
            if (ImageCrop != null) {
                ImageCrop.Dispose();
                ImageCrop = null;
            }
            pictureBox.Image = Image;
            pictureBox.Invalidate();
        }

        private void Crop() {
            resetCropping();
            if (CropWidth < 1 || CropHeight < 1) {
                return;
            }
            //First we define a rectangle with the help of already calculated points  
            Bitmap newImage = new Bitmap(CropRectangle.Width, CropRectangle.Height);
            // For croping image  
            Graphics g = Graphics.FromImage(newImage);
            // create graphics  
            g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
            g.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            //set image attributes  
            g.DrawImage(Image, 0, 0, CropRectangle, GraphicsUnit.Pixel);
            pictureBox.Image = newImage;
            pictureBox.Width = newImage.Width;
            pictureBox.Height = newImage.Height;
            resetImage(null, true, newImage);
        }

        private void resetCropRectangle(int flags, int value) {
            if (CropRectangle == null) return;
            int x = CropRectangle.X;
            int y = CropRectangle.Y;
            int width = CropRectangle.Width;
            int height = CropRectangle.Height;
            if ((flags & MOVE_UP) != 0) {
                y -= value;
            } else if ((flags & MOVE_DOWN) != 0) {
                y += value;
            } else if ((flags & MOVE_LEFT) != 0) {
                x -= value;
            } else if ((flags & MOVE_RIGHT) != 0) {
                x += value;
            } else if ((flags & SHRINK_HEIGHT) != 0) {
                height -= value;
            } else if ((flags & EXPAND_HEIGHT) != 0) {
                height += value;
            } else if ((flags & SHRINK_WIDTH) != 0) {
                width -= value;
            } else if ((flags & EXPAND_RIGHT) != 0) {
                width += value;
            }

            CropRectangle = new Rectangle(x, y, width, height);
            using (Graphics g = Graphics.FromImage(ImageCrop)) {
                g.Clear(Color.Transparent);
                g.DrawRectangle(CropPen, CropRectangle);
            }
            pictureBox.Invalidate();
        }

        private float getDpiAdjustedCropLineWidth() {
            return (float)Math.Round(DPI.X / 96 * 1.5);
        }

        private Image getImageFromUrl(string url) {
            Image image0 = null;
            try {
                byte[] originalData;
                using (WebClient wc = new WebClient()) {
                    originalData = wc.DownloadData(url);
                }
                using (MemoryStream ms = new MemoryStream(originalData)) {
                    image0 = Bitmap.FromStream(ms);
                }
            } catch (Exception) {
                //SystemSounds.Exclamation.Play(); // Same as Beep?
                SystemSounds.Beep.Play();
                string msg = "Error getting image from URL:" + NL;
                if (url.Length > 512) {
                    msg += url.Substring(0, 512) + NL + "...";
                } else {
                    msg += url;
                }
                Utils.errMsg(msg);
            }
            return image0;
        }

        private void showAvailableDataFormats(EventArgs e) {
            // Debug
            IDataObject dataObject;
            object data;
            string[] formats;
            string name;
            DragEventArgs dev = e as DragEventArgs;
            if (dev != null) {
                dataObject = dev.Data;
                name = "DragDrop";
            } else {
                dataObject = Clipboard.GetDataObject();
                name = "Clipboard";
            }
            formats = dataObject.GetFormats();
            string msg = name + " Data Formats" + NL + NL;
            foreach (string format in formats) {
                if (dataObject.GetDataPresent(format)) {
                    msg += format + NL;
                    data = dataObject.GetData(format);
                    if (data != null) {
                        msg += "    " + data.GetType() + NL;
                        if (data is String) {
                            msg += "    " + dataObject.GetData(format) + NL;
                        }
                    }
                }
            }
            Utils.infoMsg(msg);
        }

        private void OnFormLoad(object sender, EventArgs e) {
            
            // DPI
            float dpiX, dpiY;
            using (Graphics g = this.CreateGraphics()) {
                dpiX = g.DpiX;
                dpiY = g.DpiY;
            }
            DPI = new PointF(dpiX, dpiY);
            ToolsImageSize = new Size((int)(16 * dpiX / 96), (int)(16 * dpiY / 96));
            // Handle the custom icons for DPI
            // Make ToolsImageList
            ToolsImageList = new ImageList();
            ToolsImageList.ImageSize = ToolsImageSize;
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = GetType().Assembly.GetManifestResourceNames();
            Stream imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.crop-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("crop", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
               "Image_View.icons.fit-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("fit", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.fullscreen-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("fullscreen", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.hand-cursor-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("hand", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.landscape-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("landscape", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.portrait-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("portrait", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.refresh-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("reset", Image.FromStream(imageStream));
            }
            imageStream = assembly.GetManifestResourceStream(
                "Image_View.icons.zoom-icon.png");
            if (imageStream != null) {
                ToolsImageList.Images.Add("zoom", Image.FromStream(imageStream));
            }
            toolStrip1.ImageList = ToolsImageList;
            cropToolStripButton.ImageKey = "crop";
            fitToolStripButton.ImageKey = "fit";
            fullscreenToolStripButton.ImageKey = "fullscreen";
            handToolStripButton.ImageKey = "hand";
            landscapeToolStripButton.ImageKey =
                "landscape";
            portraitToolStripButton.ImageKey = "portrait";
            resetToolStripButton.ImageKey = "reset";
            zoomToolStripDropDownButton.ImageKey = "zoom";
        }

        private void OnFormShown(object sender, EventArgs e) {
#if USE_STARTUP_FILE
            // Load initial image
            string fileName = @"C:\Users\evans\Documents\Map Lines\Proud Lake\Proud Lake Hiking-Biking-Bridle Trails Map.png";
            resetImage(fileName, true);
#endif
        }

        private void OnFormResize(object sender, EventArgs e) {
            Size clientSize = pictureBox.ClientSize;
            float newWidth = clientSize.Width * ZoomFactor;
            float newHeight = clientSize.Height * ZoomFactor;
            ViewRectangle = new RectangleF(ViewRectangle.X, ViewRectangle.Y,
                newWidth, newHeight);
            pictureBox.Invalidate();
        }

        private void OnKeyDown(object sender, KeyEventArgs e) {
            if (e.KeyCode == Keys.Space) {
                KeyPanning = true;
                if (!Panning) {
                    Panning = true;
                    pictureBox.Cursor = Cursors.Hand;
                }
            } else if (e.KeyCode == Keys.Oemplus) {
                ZoomFactor /= KEY_ZOOM_FACTOR;
                zoomImage();
            } else if (e.KeyCode == Keys.OemMinus) {
                ZoomFactor *= KEY_ZOOM_FACTOR;
                zoomImage();
            } else if (e.KeyCode == Keys.D0) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    resetViewToFit();
                }
            } else if (e.KeyCode == Keys.D1) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    resetImage();
                }
            } else if (e.KeyCode == Keys.Up) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    resetCropRectangle(MOVE_UP, 1);
                } else {
                    resetCropRectangle(SHRINK_HEIGHT, 1);
                }
            } else if (e.KeyCode == Keys.Down) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    resetCropRectangle(MOVE_DOWN, 1);
                } else {
                    resetCropRectangle(EXPAND_HEIGHT, 1);
                }
            } else if (e.KeyCode == Keys.Left) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    resetCropRectangle(MOVE_LEFT, 1);
                } else {
                    resetCropRectangle(SHRINK_WIDTH, 1);
                }
            } else if (e.KeyCode == Keys.Right) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    resetCropRectangle(MOVE_RIGHT, 1);
                } else {
                    resetCropRectangle(EXPAND_RIGHT, 1);
                }
            } else if (e.KeyCode == Keys.X) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    OnCutClick(null, null);
                }
            } else if (e.KeyCode == Keys.C) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    OnCopyClick(null, null);
                }
            } else if (e.KeyCode == Keys.V) {
                if ((Control.ModifierKeys & Keys.Control) == Keys.Control) {
                    OnPasteClick(null, null);
                }
            }
        }

        private void OnKeyUp(object sender, KeyEventArgs e) {
            if (KeyPanning) {
                Panning = false;
                pictureBox.Cursor = Cursors.Default;
            }
            KeyPanning = false;
        }

        private void OnOpenImageClick(object sender, EventArgs e) {
            OpenFileDialog openFileDialog = new OpenFileDialog();

            //openFileDialog.InitialDirectory = "c:\\GIF";
            openFileDialog.Filter = "Image Files|*.png;*.bmp;*.jpg;*.jpeg;*.jpe;*.jfif;*.tif;*.tiff;*.gif"
                + "|JPEG|*.jpg;*.jpeg;*.jpe"
                + "|PNG|*.png"
                + "|All files|*.*";
            openFileDialog.FilterIndex = 1;
            openFileDialog.RestoreDirectory = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;

            if (openFileDialog.ShowDialog() == DialogResult.OK) {
                string fileName = openFileDialog.FileName;
                try {
                    resetImage(fileName, true);
                } catch (Exception ex) {
                    Utils.excMsg("Error opening file:" + NL + fileName, ex);
                    return;
                }
                //refresh();
            }
        }

        private void OnSaveClick(object sender, EventArgs e) {
            string title = "OnSaveClick";
            string message = "Save: Not implemented yet";
            MessageBox.Show(message, title);
        }

        private void OnSaveAsClick(object sender, EventArgs e) {
            string title = "OnSaveAsClick";
            string message = "Save As: Not implemented yet";
            MessageBox.Show(message, title);
        }

        private void OnPrintClick(object sender, EventArgs e) {
            PrintPictureBox ppb = new PrintPictureBox(Image);
            ppb.showPrintDialog();
        }

        private void OnPrintPreviewClick(object sender, EventArgs e) {
            PrintPictureBox ppb = new PrintPictureBox(Image);
            ppb.showPrintPreview();
        }

        private void OnPageSetupClick(object sender, EventArgs e) {
            PrintPictureBox ppb = new PrintPictureBox(Image);
            ppb.showPageSetupDialog();
        }

        private void OnExitClick(object sender, EventArgs e) {
            Close();
        }

        private void OnZoomClick(object sender, EventArgs e) {
            if (sender == toolStripMenuItem200) ZoomFactor = 0.5F;
            else if (sender == toolStripMenuItem100) ZoomFactor = 1.0F;
            else if (sender == toolStripMenuItem50) ZoomFactor = 2.0F;
            else if (sender == toolStripMenuItem25) ZoomFactor = 4.0F;
            else if (sender == zoom200ToolStripMenuItem) ZoomFactor = 0.5F;
            else if (sender == zoom100ToolStripMenuItem) ZoomFactor = 1.0F;
            else if (sender == zoom50ToolStripMenuItem) ZoomFactor = 2.0F;
            else if (sender == zoom25ToolStripMenuItem) ZoomFactor = 4.0F;
            zoomImage();
        }

        private void OnPanClick(object sender, EventArgs e) {
            Panning = !Panning;
            if (Panning) {
                pictureBox.Cursor = Cursors.Hand;
            } else {
                pictureBox.Cursor = Cursors.Default;
            }
        }

        private void OnCropClick(object sender, EventArgs e) {
            Cropping = true;
            Panning = false;
            pictureBox.Cursor = Cursors.Cross;
        }

        private void OnCancelCropClick(object sender, EventArgs e) {
            if (ImageCrop != null) {
                resetCropping();
            } else {
                Utils.errMsg("Not cropping");
            }
        }

        private void OnDoCropClick(object sender, EventArgs e) {
            if (ImageCrop != null) {
                Crop();
            } else {
                Utils.errMsg("Not cropping");
            }
        }

        private void OnFullscreenClick(object sender, EventArgs e) {
            resetImage(null, true, ImageOrig);
        }

        private void OnResetClick(object sender, EventArgs e) {
            if (ImageOrig != null) {
                resetImage(null, true, ImageOrig);
            } else if (Image != null) {
                resetImage();
            }
        }

        private void OnFitClicked(object sender, EventArgs e) {
            resetViewToFit();
        }

        private void OnCutClick(object sender, EventArgs e) {
            if (Image == null) {
                Utils.errMsg("There is no image");
                return;
            }
            OnCopyClick(sender, e);
            Image = null;
            resetImage(null, true, null);
        }

        private void OnCopyClick(object sender, EventArgs e) {
            if (Image == null) {
                Utils.errMsg("There is no image");
                return;
            }
            try {
                Clipboard.SetImage(Image);
            } catch (Exception ex) {
                Utils.excMsg("Error copying image to clipboard", ex);
            }
        }

        private void OnPasteClick(object sender, EventArgs e) {
#if DEBUG_AVAILABLE_FORMATS
            // Debug
            showAvailableDataFormats(e);
#endif
            // FileDrop
            Image newImage = null;
            string[] files = (string[])Clipboard.GetDataObject().GetData((DataFormats.FileDrop));
            if (files != null && files.Length > 0) {
                // Use the first one
                string fileName = files[0];
                resetImage(fileName, true);
                return;
            }

            // PNG (Used by Chrome)
            object data = Clipboard.GetDataObject().GetData("PNG");
            if (data != null) {
                using (MemoryStream ms = (MemoryStream)data) {
                    ms.Position = 0;
                    newImage = (Bitmap)new Bitmap(ms);
                }
                if (newImage != null) {
                    resetImage(ClipboardDataTypes.CLIPBOARD.ToString(), true, newImage);
                    return;
                }
            }

            // Bitmap
            newImage = (Bitmap)(Clipboard.GetDataObject().GetData(DataFormats.Bitmap));
            if (newImage != null) {
                resetImage(ClipboardDataTypes.BITMAP.ToString(), true, newImage);
                return;
            }

            // General
            newImage = Clipboard.GetImage();
            if (newImage != null) {
                resetImage(ClipboardDataTypes.CLIPBOARD.ToString(), true, newImage);
                return;
            }
            Utils.errMsg("Clipboard does not contain an image");
        }

        private void OnHelpClick(object sender, EventArgs e) {
            string title = "OnHelpClick";
            string message = "Help: Not implemented yet";
            MessageBox.Show(message, title);
        }


        private void OnLandscapeClicked(object sender, EventArgs e) {
            string title = "OnLandscapeClick";
            string message = "Landscape: Not implemented yet";
            MessageBox.Show(message, title);
        }

        private void OnPortraitClicked(object sender, EventArgs e) {
            string title = "OnPortraitClick";
            string message = "Portrait: Not implemented yet";
            MessageBox.Show(message, title);
        }

        private void OnPictureBoxMouseDown(object sender, MouseEventArgs e) {
            if (Panning) PanStart = e.Location;
            else if (Cropping) {
                if (e.Button == System.Windows.Forms.MouseButtons.Left) {
                    pictureBox.Refresh();
                    if (ImageCrop != null) {
                        ImageCrop.Dispose();
                    }
                    Bitmap bm = new Bitmap(Image.Width, Image.Height);
                    //bm.MakeTransparent();
                    ImageCrop = bm;
                    CropX = e.X;
                    CropY = e.Y;
                    int newX = (int)Math.Round(CropX * ZoomFactor + ViewRectangle.X);
                    int newY = (int)Math.Round(CropY * ZoomFactor + ViewRectangle.Y);
                    CropRectangle = new Rectangle(newX, newY, 0, 0);
                    CropPen = new Pen(Color.Black, 1);
                    CropPen.DashStyle = DashStyle.Dash;
                    CropPen.Width = getDpiAdjustedCropLineWidth();
                    CropPen.Color = Color.Tomato;
                }
            }
        }

        private void OnPictureBoxMouseMove(object sender, MouseEventArgs e) {
            if (e.Button == MouseButtons.Left) {
                if (Panning) {
                    float deltaX = PanStart.X - e.X;
                    float deltaY = PanStart.Y - e.Y;
                    // Reset PanStart
                    PanStart = e.Location;
                    ViewRectangle = new RectangleF(ViewRectangle.X + deltaX,
                        ViewRectangle.Y + deltaY,
                        ViewRectangle.Width, ViewRectangle.Height);
                    Debug.WriteLine("OnPictureBoxMouseMove:"
                        + NL + " e=(" + e.X + "," + e.Y + ")"
                        + NL + " PanStart=(" + PanStart.X + "," + PanStart.Y + ")"
                        + NL + " delta=(" + deltaX + "," + deltaY + ")"
                        + NL + "    ViewRectangle=" + ViewRectangle);
                    pictureBox.Invalidate();
                } else if (Cropping) {
                    Debug.WriteLine("OnPictureBoxMouseMove: Cropping=" + Cropping
                        + $" CropX={CropX} CropY={CropY} CropPen={CropPen}");
                    CropWidth = e.X - CropX;
                    CropHeight = e.Y - CropY;
                    using (Graphics g = Graphics.FromImage(ImageCrop)) {
                        int newX = (int)Math.Round(CropX * ZoomFactor + ViewRectangle.X);
                        int newY = (int)Math.Round(CropY * ZoomFactor + ViewRectangle.Y);
                        int newWidth = (int)Math.Round(CropWidth * ZoomFactor);
                        int newHeight = (int)Math.Round(CropHeight * ZoomFactor);
                        CropRectangle = new Rectangle(newX, newY, newWidth, newHeight);
                        g.Clear(Color.Transparent);
                        g.DrawRectangle(CropPen, CropRectangle);
                    }
                    pictureBox.Invalidate();
                }
            }
        }

        private void OnPictureBoxMouseWheel(object sender, MouseEventArgs e) {
            Debug.WriteLine("OnPictureBoxMouseWheel: ZoomFactor=" + ZoomFactor);
            ZoomFactor *= 1 + e.Delta * MOUSE_WHEEL_ZOOM_FACTOR;
            zoomImage();
        }

        private void OnPictureBoxPaint(object sender, PaintEventArgs e) {
            if (Image == null) return;
            Graphics g = e.Graphics;
            g.Clear(pictureBox.BackColor);
            g.DrawImage(Image, pictureBox.ClientRectangle, ViewRectangle,
                GraphicsUnit.Pixel);
            if (ImageCrop != null) {
                g.DrawImage(ImageCrop, pictureBox.ClientRectangle, ViewRectangle,
                    GraphicsUnit.Pixel);
            }
        }

        private void OnInfoClicked(object sender, EventArgs e) {
            string msg = "Image Information" + NL + NL;
            if (Image == null) {
                msg += "Image Undefined" + NL;
            } else {
                msg += $"Width={Image.Width} Height={Image.Height}" + NL;
                msg += $"Horizontal Resolution={Image.HorizontalResolution} Vertical Resolution={Image.VerticalResolution}" + NL;
                msg += $"Pixel Format={Image.PixelFormat}" + NL;
            }
            if (Image.Tag != null) {
                msg += $"Tag={Image.Tag}" + NL;
            }
            msg += "Source" + NL + "    ";
            if (FileName == null) {
                msg += "FileName Undefined" + NL;
            } else {
                msg += FileName;
            }
            Utils.infoMsg(msg);
        }


        private void OnHelpAboutClicked(object sender, EventArgs e) {
            Assembly assembly = Assembly.GetExecutingAssembly();
            Image image = null;
            try {
                image = Image.FromFile(@".\Help\Image View.256x256.png");
            } catch (Exception ex) {
                Utils.excMsg("Failed to get AboutBox image", ex);
            }
            AboutBox dlg = new AboutBox("About Image View", image, assembly);
            dlg.ShowDialog();
        }

        private void OnDragEnter(object sender, DragEventArgs e) {
            if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
                e.Data.GetDataPresent(DataFormats.Bitmap)) {
                e.Effect = DragDropEffects.Copy;
            } else {
                e.Effect = DragDropEffects.None;
            }
            // Debug
            e.Effect = DragDropEffects.Copy;
        }

        private void OnDragDrop(object sender, DragEventArgs e) {
#if DEBUG_AVAILABLE_FORMATS
            // Debug
            showAvailableDataFormats(e);
#endif
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0) {
                // Use the first one
                string fileName = files[0];
                resetImage(fileName, true);
                return;
            }
            Image image = (Bitmap)(e.Data.GetData(DataFormats.Bitmap));
            if (image != null) {
                resetImage(ClipboardDataTypes.BITMAP.ToString(), true, image);
                return;
            }
            // Try it as text, assuming it is a URL
            String url;
            url = (string)(e.Data.GetData(DataFormats.Text));
            if (url != null) {
                image = getImageFromUrl(url);
                if (image != null) {
                    resetImage($"{ClipboardDataTypes.URL.ToString()} {url}", true, image);
                    return;
                }
            }
        }
    }
}


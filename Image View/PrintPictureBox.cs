using KEUtils.Utils;
using System;
using System.Drawing;
using System.Drawing.Printing;
using System.Windows.Forms;

namespace Image_View {
    public class PrintPictureBox {
        Image? PrintImage { get; set; }
        PrintDocument? PrintDocument { get; set; }
        PageSettings? PageSettings { get; set; }

        public PrintPictureBox(Image image) {
            if (image == null) {
                Utils.errMsg("No image");
                return;
            }
            PrintImage = image;
            PrintDocument = new PrintDocument();
            PrintDocument.PrintPage += new System.Drawing.Printing.PrintPageEventHandler(print);
            PageSettings = new PageSettings();
            PageSettings.Landscape = Properties.Settings.Default.Landscape;
            Margins margins = Properties.Settings.Default.Margins;
            if (margins != null) {
                PageSettings.Margins = Properties.Settings.Default.Margins;
            }

            //    PaperSize = new PaperSize("Custom", 100, 200);
            //    pd.Document = pdoc;
            //    pd.Document.DefaultPageSettings.PaperSize = psize;
            //    pdoc.DefaultPageSettings.PaperSize.Height = 320;
            //    pdoc.DefaultPageSettings.PaperSize.Width = 200;
        }

        public void showPrintDialog() {
            if (PrintDocument == null) {
                Utils.errMsg("showPrintDialog: PrintDocument is null");
                return;
            }
            PrintDialog printDialog = new PrintDialog();
            printDialog.Document = PrintDocument;
            if (PageSettings != null) {
                printDialog.Document.DefaultPageSettings = PageSettings;
            }
            if (printDialog.ShowDialog() == DialogResult.OK) {
                PrintDocument.Print();
            }
        }

        public DialogResult showPrintPreview() {
            if (PrintDocument == null) {
                Utils.errMsg("showPrintPreview: Invalid document");
                return DialogResult.Abort;
            }
            PrintPreviewDialog ppDialog = new PrintPreviewDialog();
            Control.ControlCollection controls = ppDialog.Controls;
            ToolStrip toolStrip = (ToolStrip)controls[1];
            ToolStripItemCollection items = toolStrip.Items;
            PointF toolBarDpi = getDpi(toolStrip);
            PointF dpiScale = new PointF(toolBarDpi.X / 96f, toolBarDpi.Y / 96f);
            int toolBarWidth = 0;
            int toolBarHeight = 0;
            foreach (ToolStripItem item in items) {
                item.AutoSize = false;
                Image image = item.Image;

                // Resize the image of the button to the new size
                item.Width = (int)Math.Round(dpiScale.X * item.Width);
                item.Height = (int)Math.Round(dpiScale.Y * item.Height);
                // Separators do not have an image
                if (image != null) {
                    int sourceWidth = image.Width;
                    int sourceHeight = image.Height;
                    int width = (int)(Math.Round(dpiScale.X * image.Width));
                    int height = (int)(Math.Round(dpiScale.Y * image.Height));
                    if (width > toolBarWidth) toolBarWidth = width;
                    if (height > toolBarHeight) toolBarHeight = height;
                    Bitmap bm = new Bitmap(width, height);
                    using (Graphics g = Graphics.FromImage((Image)bm)) {
                        // Should be the best
                        g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                        g.DrawImage(image, 0, 0, width, height);
                    }
                    // Put the resized image back to the button 
                    image = (Image)bm;
                }
            }
            toolStrip.AutoSize = false;
            toolStrip.ImageScalingSize = new Size(toolBarWidth, toolBarHeight);
            ppDialog.Document = PrintDocument;
            if (PageSettings != null) {
                ppDialog.Document.DefaultPageSettings = PageSettings;
            }
            DialogResult res = ppDialog.ShowDialog();
            return res;
        }

        public PointF getDpi(Control control) {
            float dx, dy;
            Graphics g = control.CreateGraphics();
            try {
                dx = g.DpiX;
                dy = g.DpiY;
            } finally {
                g.Dispose();
            }
            return new PointF(dx, dy);
        }

        public PageSettings? getDefaultPageSettings() {
            if (PrintDocument == null) {
                Utils.errMsg("getDefaultPageSettings: No PrintDocument");
                return null;
            }
            return PrintDocument.DefaultPageSettings;
        }

        public void showPageSetupDialog() {
            PageSetupDialog psDialog = new PageSetupDialog();
            // Initialize the dialog's PageSettings property to hold user
            // defined printer settings.
            psDialog.PageSettings = PageSettings;

            // Do not show the network in the printer dialog.
            psDialog.ShowNetwork = false;

            //Show the dialog storing the result.
            DialogResult result = psDialog.ShowDialog();
            if (result == DialogResult.OK) {
                PageSettings = psDialog.PageSettings;
                if (PageSettings != null) {
                    Properties.Settings.Default.Landscape = PageSettings.Landscape;
                    Properties.Settings.Default.Margins = PageSettings.Margins;
                    Properties.Settings.Default.Save(); 
                }
            }
        }

        private void print(System.Object sender, System.Drawing.Printing.PrintPageEventArgs e) {
            Rectangle drawingArea = e.MarginBounds;
            if (PrintImage == null) {
                Utils.errMsg("Printing: No image");
                return;
            }
            float aspect = (float)PrintImage.Height / PrintImage.Width;
            if (aspect == 0) {
                Utils.errMsg("Printing: Invalid image");
                return;
            }
            float printAspect = (float)drawingArea.Height / drawingArea.Width;
            // Adjust to fit drawing area
            if (aspect < printAspect) {
                drawingArea.Height = (int)Math.Round(drawingArea.Width * aspect);
            } else {
                drawingArea.Width = (int)Math.Round(drawingArea.Width / aspect);
            }
            if (e.Graphics != null) {
                e.Graphics.DrawImage(PrintImage, drawingArea);
            }
        }
    }
}

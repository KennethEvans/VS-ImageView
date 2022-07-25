using System.ComponentModel;
using System.Drawing.Printing;

namespace Image_View {
    /// <summary>
    /// This is a dialog to get and set Options. The property Options is
    /// only set in OnOkClick and hence only available if DialogResult is
    /// DialogResult.OK.
    /// </summary>
    public partial class OptionsDialog : Form {
        /// <summary>
        /// CTOR that initializes the controls from Settings.
        /// </summary>
        public OptionsDialog() {
            //// DEBUG
            //Properties.Settings.Default.Reset();
            InitializeComponent();
            Options options = getOptionsFromSavedSettings();
            setControlsFromOptions(options);
        }

        /// <summary>
        /// Gets the controls from the given Options.
        /// </summary>
        /// <param name="options">The Options to use.</param>
        private void setControlsFromOptions(Options options) {
            Margins margins = options.Margins;
            // Values are hundredths of an inch
            float left = margins.Left / 100.0f;
            float right = margins.Right / 100.0f;
            float top = margins.Top / 100.0f;
            float bottom = margins.Bottom / 100.0f;
            setValidFloat(textBoxLeft, left);
            setValidFloat(textBoxRight, right);
            setValidFloat(textBoxTop, top);
            setValidFloat(textBoxBottom, bottom);
            checkBoxLandscape.Checked = options.Landscape;
            setValidFloat(textBoxSelectionLineWidth, options.SelectionLineWidth);
            setValidString(textBoxSelectionLineColor, options.SelectionLineColor);
            setValidFloat(textBoxCustomZoom, options.CustomZoomPercent);
        }

        private Options getOptionsFromControls() {
            Options options = new Options();
            Margins margins = new Margins();
            // Values are hundredths of an inch
            margins.Left = (int)Math.Round(100 * getValidDouble(textBoxLeft));
            margins.Right = (int)Math.Round(100 * getValidDouble(textBoxRight));
            margins.Top = (int)Math.Round(100 * getValidDouble(textBoxTop));
            margins.Bottom = (int)Math.Round(100 * getValidDouble(textBoxBottom));
            options.Margins = margins;
            options.Landscape = checkBoxLandscape.Checked;
            options.SelectionLineWidth = getValidFloat(textBoxSelectionLineWidth);
            options.SelectionLineColor = textBoxSelectionLineColor.Text;
            options.CustomZoomPercent = (float)Math.Round(getValidDouble(textBoxCustomZoom));
            return options;
        }

        public static Options getOptionsFromSavedSettings() {
            Options options = new Options();
            options.Margins = Properties.Settings.Default.Margins;
            options.Landscape = Properties.Settings.Default.Landscape;
            options.SelectionLineWidth = Properties.Settings.Default.SelectionLineWidth;
            options.SelectionLineColor = Properties.Settings.Default.SelectionLineColor;
            options.CustomZoomPercent = Properties.Settings.Default.CustomZoomPercent;
            return options;
        }

        public static Options getOptionsFromDefaultSettings() {
            ;
            Options options = new Options();
            Margins? margins = GetDefaultValue("Margins") as Margins;
            if (margins != null) options.Margins = margins;
            bool? landscape = GetDefaultValue("Landscape") as bool?;
            if (landscape != null) options.Landscape = (bool)landscape;
            float? selectionLineWidth = GetDefaultValue("SelectionLineWidth") as float?;
            if (selectionLineWidth != null) options.SelectionLineWidth = (float)selectionLineWidth;
            string? selectionLineColor = GetDefaultValue("SelectionLineColor") as string;
            if (selectionLineColor != null) options.SelectionLineColor = selectionLineColor;
            float? customZoomPrecent = GetDefaultValue("CustomZoomPercent") as float?;
            if (customZoomPrecent != null) options.CustomZoomPercent = (float)customZoomPrecent;
            return options;
        }

        public static void setSettingsFromOptions(Options options) {
            Properties.Settings.Default.Margins = options.Margins;
            Properties.Settings.Default.Landscape = options.Landscape;
            Properties.Settings.Default.SelectionLineWidth = options.SelectionLineWidth;
            Properties.Settings.Default.SelectionLineColor = options.SelectionLineColor;
            Properties.Settings.Default.CustomZoomPercent = options.CustomZoomPercent;
        }

        private void setValidString(TextBox textBox, string strVal) {
            if (string.IsNullOrEmpty(strVal)) textBox.Text = "";
            else textBox.Text = strVal;
        }

        private string? getValidString(TextBox textBox) {
            if (string.IsNullOrEmpty(textBox.Text)) return null;
            else return textBox.Text;
        }

        private float getValidFloat(TextBox textBox) {
            if (string.IsNullOrEmpty(textBox.Text)) return float.NaN;
            try {
                return Convert.ToSingle(textBox.Text);
            } catch (Exception) {
                return float.NaN;
            }
        }

        private double getValidDouble(TextBox textBox) {
            if (string.IsNullOrEmpty(textBox.Text)) return double.NaN;
            try {
                return Convert.ToDouble(textBox.Text);
            } catch (Exception) {
                return double.NaN;
            }
        }

        private void setValidDouble(TextBox textBox, double val) {
            if (Double.IsNaN(val)) textBox.Text = "";
            else textBox.Text = val.ToString();
        }

        private void setValidFloat(TextBox textBox, float val) {
            if (float.IsNaN(val)) textBox.Text = "";
            else textBox.Text = val.ToString();
        }

        private void OnCancelClick(object sender, System.EventArgs e) {
            DialogResult = DialogResult.Cancel;
            Visible = false;
        }

        private void OnOkClick(object sender, System.EventArgs e) {
            Options options = getOptionsFromControls();
            setSettingsFromOptions(options);
            Properties.Settings.Default.Save();
            DialogResult = DialogResult.OK;
            Visible = false;
        }

        private void OnUseDefaultsClick(object sender, EventArgs e) {
            Options options = getOptionsFromDefaultSettings();
            setControlsFromOptions(options);
        }

        private void OnSaveClick(object sender, System.EventArgs e) {
            Options options = getOptionsFromControls();
            setSettingsFromOptions(options);
            Properties.Settings.Default.Save();
        }

        private void OnUseSavedClick(object sender, EventArgs e) {
            Options options = getOptionsFromSavedSettings();
            setControlsFromOptions(options);
        }

        /// <summary>
        /// Gets the original (default) Settings value for the given property name.
        /// </summary>
        /// <param name="propertyName"></param>
        /// <returns></returns>
        public static object? GetDefaultValue(string propertyName) {
            var property = Properties.Settings.Default.Properties[propertyName];
            var type = property.PropertyType;
            var defaultValue = property.DefaultValue;
            return TypeDescriptor.GetConverter(type).ConvertFrom(defaultValue);
        }

        private void OptionsDialog_Load(object sender, EventArgs e) {

        }

        private void toolTip_Popup(object sender, PopupEventArgs e) {

        }
    }

    public class Options {
        public Margins Margins { get; set; } = new Margins();
        public bool Landscape { get; set; } = false;
        public float SelectionLineWidth { get; set; } = 0;
        public string SelectionLineColor { get; set; } = "#FFFFFFFF";
        public float CustomZoomPercent { get; set; } = 0;
    }

    //public static class SettingsExtensions {
    //    public static object? GetDefaultValue(this ApplicationSettingsBase settings,
    //        string propertyName) {
    //        var property = settings.Properties[propertyName];
    //        var type = property.PropertyType;
    //        var defaultValue = property.DefaultValue;
    //        return TypeDescriptor.GetConverter(type).ConvertFrom(defaultValue);
    //    }
    //}

}

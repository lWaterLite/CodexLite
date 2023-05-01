using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Xml;
using Newtonsoft.Json;
using SoftCircuits.JavaScriptFormatter;
using Clipboard = System.Windows.Forms.Clipboard;
using Formatting = System.Xml.Formatting;

namespace CodexLite.Apps
{
    /// <summary>
    /// Formate.xaml 的交互逻辑
    /// </summary>
    public partial class Format : Page
    {
        public Format()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 判断输入的是否为合法数字，默认或不合法时为4
        /// </summary>
        private int IsIndentValid(string indent)
        {
            bool isValid = int.TryParse(indent, out var number);
            return isValid ? number : 4;
        }
        
        private void JsFormatting_Click(object sender, RoutedEventArgs e)
        {
            FormatOptions formatOptions = new FormatOptions
            {
                Tab = new string(' ', IsIndentValid(JsIndentTextBox.Text))
            };
            JavaScriptFormatter javaScriptFormatter = new JavaScriptFormatter(formatOptions);
            string result = javaScriptFormatter.Format(JsOriginTextBox.Text);
            JsFormattedTextBox.Text = result;
        }

        private void JsClear_Click(object sender, RoutedEventArgs e)
        {
            JsOriginTextBox.Clear();
            JsFormattedTextBox.Clear();
        }

        private void JsCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JsFormattedTextBox.Text);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
            
        }

        private void XmlFormatting_Click(object sender, RoutedEventArgs e)
        {
            XmlDocument document = new XmlDocument();
            try
            {
                document.LoadXml(XmlOriginTextBox.Text);
            }
            catch (XmlException exception)
            {
                XmlFormattedTextBox.Text = "XML内容有错误";
                Console.WriteLine(exception);
                return;
            }
            MemoryStream memoryStream = new MemoryStream();
            XmlTextWriter writer = new XmlTextWriter(memoryStream, null)
            {
                Formatting = Formatting.Indented,
                Indentation = IsIndentValid(XmlIndentTextBox.Text)
            };
            document.Save(writer);
            StreamReader streamReader = new StreamReader(memoryStream);
            memoryStream.Position = 0;
            XmlFormattedTextBox.Text = streamReader.ReadToEnd();
            streamReader.Close();
            memoryStream.Close();
        }

        private void XmlClear_Click(object sender, RoutedEventArgs e)
        {
            XmlOriginTextBox.Clear();
            XmlFormattedTextBox.Clear();
        }

        private void XmlCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JsFormattedTextBox.Text);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }

        private void JsonFormatting_Click(object sender, RoutedEventArgs e)
        {
            object obj;
            JsonSerializer jsonSerializer = new JsonSerializer();
            try
            {
                TextReader textReader = new StringReader(JsonOriginTextBox.Text);
                JsonTextReader jsonTextReader = new JsonTextReader(textReader);
                obj = jsonSerializer.Deserialize(jsonTextReader);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception.ToString());
                JsonFormattedTextBox.Text = "JSON内容有错误";
                return;
            }
            if (obj != null)
            {
                StringWriter stringWriter = new StringWriter();
                JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter)
                {
                    Indentation = IsIndentValid(JsonIndentTextBox.Text),
                    Formatting = Newtonsoft.Json.Formatting.Indented
                };
                jsonSerializer.Serialize(jsonTextWriter, obj);
                JsonFormattedTextBox.Text = stringWriter.ToString();
                return;
            }
            JsonFormattedTextBox.Text = "JSON内容有错误";
        }

        private void JsonClear_Click(object sender, RoutedEventArgs e)
        {
            JsonOriginTextBox.Clear();
            JsonFormattedTextBox.Clear();
        }

        private void JsonCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JsonFormattedTextBox.Text);
            }
            catch (Exception exception)
            {
                Console.WriteLine(exception);
            }
        }
    }
}

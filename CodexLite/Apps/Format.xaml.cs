using System;
using System.IO;
using System.Windows;
using System.Xml;
using Newtonsoft.Json;
using SoftCircuits.JavaScriptFormatter;
using Clipboard = System.Windows.Forms.Clipboard;
using Formatting = System.Xml.Formatting;

namespace CodexLite.Apps
{
    /// <summary>
    /// Format.xaml 的交互逻辑
    /// </summary>
    public partial class Format
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
        
        /// <summary>
        /// Js格式化，按钮响应事件。
        /// </summary>
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

        /// <summary>
        /// 清除文本框，按钮响应事件。
        /// </summary>
        private void JsClear_Click(object sender, RoutedEventArgs e)
        {
            JsOriginTextBox.Clear();
            JsFormattedTextBox.Clear();
        }

        /// <summary>
        /// 复制格式化后的Js到剪切板，按钮响应事件
        /// </summary>
        private void JsCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JsFormattedTextBox.Text);
            }
            catch (Exception)
            {
                //Pass
            }
            
        }

        /// <summary>
        /// XML格式化，按钮响应事件
        /// </summary>
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

        /// <summary>
        /// 文本框清除，按钮响应事件
        /// </summary>
        private void XmlClear_Click(object sender, RoutedEventArgs e)
        {
            XmlOriginTextBox.Clear();
            XmlFormattedTextBox.Clear();
        }

        /// <summary>
        /// 复制格式化后的XML到剪切板，按钮响应事件
        /// </summary>
        private void XmlCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JsFormattedTextBox.Text);
            }
            catch (Exception)
            {
                //Pass
            }
        }

        /// <summary>
        /// JSON格式化，按钮响应事件
        /// </summary>
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
            catch (Exception)
            {
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

        /// <summary>
        /// 文本框清除，按钮响应事件
        /// </summary>
        private void JsonClear_Click(object sender, RoutedEventArgs e)
        {
            JsonOriginTextBox.Clear();
            JsonFormattedTextBox.Clear();
        }

        /// <summary>
        /// 复制格式化后的JSON到剪切板，按钮响应事件
        /// </summary>
        private void JsonCopyToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(JsonFormattedTextBox.Text);
            }
            catch (Exception)
            {
                //Pass
            }
        }
    }
}

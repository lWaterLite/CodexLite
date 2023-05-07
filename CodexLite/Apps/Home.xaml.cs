using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;
using Newtonsoft.Json;

namespace CodexLite.Apps
{
    /// <summary>
    /// Home page的逻辑交互行为
    /// </summary>
    [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
    public partial class Home
    {
        /// <summary>
    /// 与UI线程同步的计时器，用于处理硬件信息更新的定时任务，设计为与UI线程同步的方式使其能安全访问UI内的变量。采用Dispatcher自动注入UI线程。
        /// </summary>
        private static DispatcherTimer _updateTimer;
        /// <summary>
        /// 用来保存全部硬件信息树，键为硬件类型枚举类，值为bindingSource，在内部值更新时可以进行更新触发保证绑定的ui同步更新。bindingSource的dataSource为PropertyNodeItem,
        /// 是用来渲染viewTree的树形类。
        /// </summary>
        private readonly Dictionary<HardwareType, BindingSource> _hardwareNodes;
        /// <summary>
        /// 专门用来渲染GPU的bindingSource，因为在引用库中将GPU设计为两类，有两种枚举类型，为了将两个树合并为一个添加了这个变量便于维护UI渲染。
        /// </summary>
        private static BindingSource _gpuNodes;

        public Home()
        {
            InitializeComponent();
            ComputerName.Text = SystemInformation.ComputerName;     // 获取计算机名称。
            Computer computer = InitializeComputer();       //初始化一个computer类进行硬件信息查询。
            _hardwareNodes = InitializeHardwareNodes(computer);     //第一次查询，同时初始化所有bindingSource的硬件树。
            InitializeSensorNodes(computer,_hardwareNodes);     //第一次查询，同时初始化硬件数下的所有传感器树。
            _gpuNodes = GpuHandler(_hardwareNodes);     // 第一次构建GPU硬件树。
            computer.Close();
            
            // ui数据绑定
            CpuTreeView.ItemsSource = _hardwareNodes[HardwareType.CPU];
            GpuTreeView.ItemsSource = _gpuNodes;
            RamTreeView.ItemsSource = _hardwareNodes[HardwareType.RAM];
            HddTreeView.ItemsSource = _hardwareNodes[HardwareType.HDD];

            // 初始化一个dispatcherTimer，并开始timer计时。
            _updateTimer = InitializeTimer();
            // Dispatcher.InvokeAsync(new Action(delegate { updateTimer.Start(); }));
            _updateTimer.Start();
        }
        
        ~Home()
        {
            _updateTimer.Stop();    // 停止timer，使得timer和其引用的资源能够GC，防止内存泄露。
        }

        
        /// <summary>
        /// 初始化一个computer类，启用部分硬件检测，尝试打开，然后将其返回。
        /// </summary>
        /// <returns>一个已经打开的computer实例，对于cpu, gpu, motherboard, ram, hdd的硬件检测已经启用。</returns>
        private static Computer InitializeComputer()
        {
            UpdateVisitor updateVisitor = new UpdateVisitor();
            Computer computer = new Computer()
            {
                CPUEnabled = true,
                GPUEnabled = true,
                MainboardEnabled = true,
                RAMEnabled = true,
                HDDEnabled = true
            };
            try
            {
                computer.Open();
            }
            catch (Exception)
            {
                return null;
            }
            computer.Accept(updateVisitor);
            return computer;
        }

        /// <summary>
        /// 遍历查询好的computer，并将其中的硬件信息转化为一个硬件树，再将资源转化为一个硬件类型与硬件树对应的字典返回
        /// </summary>
        /// <param name="computer">一个已经打开的computer实例</param>
        /// <returns>一个字典，键为硬件类型枚举，值为该硬件对应的硬件树，采用bindingsSource存储，用于后续触发更新信号</returns>
        private Dictionary<HardwareType, BindingSource> InitializeHardwareNodes(Computer computer)
        {
            List<HardwareType> hardwareRecord = new List<HardwareType>();
            Dictionary<HardwareType, BindingSource> hardwareNodes = new Dictionary<HardwareType, BindingSource>();
            foreach (IHardware hardware in computer.Hardware)
            {
                if (!hardwareRecord.Exists(h => h == hardware.HardwareType))
                {
                    hardwareRecord.Add(hardware.HardwareType);
                    hardwareNodes.Add(hardware.HardwareType, new BindingSource()
                    {
                        DataSource = new PropertyNodeItem(NodeType.Title, hardware.HardwareType.ToString())
                    });
                }
            }

            return hardwareNodes;
        }

        
        /// <summary>
        /// 初始化硬件树的子传感器结点。
        /// </summary>
        /// <param name="computer">一个已经打开的computer实例，应与上面方法的传入的一致</param>
        /// <param name="hardwareNodes">一个硬件树字典，应是上面方法运行的结果</param>
        private void InitializeSensorNodes(Computer computer, Dictionary<HardwareType, BindingSource> hardwareNodes)
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU && CpuName.Text == string.Empty) CpuName.Text = hardware.Name;
                if (hardware.HardwareType == HardwareType.Mainboard && MotherboardName.Text == string.Empty)
                    MotherboardName.Text = hardware.Name;
                if (hardware.HardwareType == HardwareType.GpuNvidia ||
                    hardware.HardwareType == HardwareType.GpuAti && 
                    GpuName.Text == string.Empty) GpuName.Text = hardware.Name;
                
                var nodeItem = (PropertyNodeItem)hardwareNodes[hardware.HardwareType].DataSource;
                var hw = new PropertyNodeItem(NodeType.Title, name: hardware.Name);
                nodeItem.Children.Add(hw);
                List<SensorType> sensorsRecords = new List<SensorType>();
                Dictionary<SensorType, PropertyNodeItem> propertyNodeItems =
                    new Dictionary<SensorType, PropertyNodeItem>();
                foreach (var sensor in hardware.Sensors)
                {
                    if (sensorsRecords.Exists(s => s == sensor.SensorType))
                    {
                        propertyNodeItems[sensor.SensorType].Children.Add(new PropertyNodeItem(NodeType.Source, sensor.Name, new HardwareInfo()
                        {
                            Name = sensor.Name,
                            SensorType = sensor.SensorType, 
                            MaxValue = sensor.Value
                        }));
                    }
                    else
                    {
                        sensorsRecords.Add(sensor.SensorType);
                        propertyNodeItems.Add(sensor.SensorType, new PropertyNodeItem(NodeType.Title, name:sensor.SensorType.ToString()));
                        propertyNodeItems[sensor.SensorType].Children.Add(new PropertyNodeItem(NodeType.Source, sensor.Name, new HardwareInfo()
                        {
                            Name = sensor.Name,
                            SensorType = sensor.SensorType, 
                            MaxValue = sensor.Value
                        }));
                    }
                }

                foreach (var value in propertyNodeItems.Values)
                {
                    hw.Children.Add(value);
                }
            }
        }

        /// <summary>
        /// 将硬件树的GPU树合并成一个树根
        /// </summary>
        /// <param name="hardwareNodes">一个硬件树字典</param>
        /// <returns>返回一个bindSource，数据资源是GPU树</returns>
        private BindingSource GpuHandler(Dictionary<HardwareType, BindingSource> hardwareNodes)
        {
            BindingSource gpuNodes = new BindingSource()
                {
                    DataSource = new PropertyNodeItem(NodeType.Title, name: "GPU")
                };
                var i = gpuNodes.DataSource as PropertyNodeItem;
                if (hardwareNodes.TryGetValue(HardwareType.GpuNvidia, out var node) && i != null)
                    i.Children.Add(node.DataSource as PropertyNodeItem);
            
                if (hardwareNodes.TryGetValue(HardwareType.GpuAti, out node) && i != null) 
                    i.Children.Add(node.DataSource as PropertyNodeItem);
                return gpuNodes;
        }

        
        /// <summary>
        /// 更新GPU树中的传感器信息。
        /// </summary>
        /// <param name="hardwareNodes">更新好的硬件树字典</param>
        /// <param name="gpuNodes">还未更新的gpu树</param>
        private void GpuUpdater(Dictionary<HardwareType, BindingSource> hardwareNodes, BindingSource gpuNodes)
        {
            if (gpuNodes.DataSource is PropertyNodeItem nodes)
            {
                nodes.Children.Clear();
                if (hardwareNodes.TryGetValue(HardwareType.GpuNvidia, out var node))
                    nodes.Children.Add(node.DataSource as PropertyNodeItem);

                if (hardwareNodes.TryGetValue(HardwareType.GpuAti, out node))
                    nodes.Children.Add(node.DataSource as PropertyNodeItem);
            }
        }

        /// <summary>
        /// 更新全部硬件的传感器信息。
        /// </summary>
        /// <param name="hardwareNodes">还未更新的硬件树字典</param>
        private void SensorInfoUpdater(Dictionary<HardwareType, BindingSource> hardwareNodes)
        {
            Computer computer = InitializeComputer();
            if (computer == null) return;

            foreach (var hardware in computer.Hardware)
            {
                if (hardwareNodes[hardware.HardwareType].DataSource is PropertyNodeItem firstNode) firstNode = firstNode.GetChildByName(hardware.Name);
                else return;
                foreach (var sensor in hardware.Sensors)
                {
                    if (firstNode != null)
                    {
                        var sensors = firstNode.GetChildByName(sensor.SensorType.ToString());
                        var sen = sensors.GetChildByName(sensor.Name);
                        sen.HardwareInfo.UpdateValue(sensor.Value);
                    }
                }
            }
            computer.Close();
        }

        // private void UpdateThread()
        // {
        //     Thread thread = new Thread(Updater_Tick);
        //     thread.Start();
        // }

        
        /// <summary>
        /// 更新硬件树，为了在timer中引用封装成一个函数
        /// </summary>
        private void Updater()
        {
            SensorInfoUpdater(_hardwareNodes);
            GpuUpdater(_hardwareNodes, _gpuNodes);
        }
        
        /// <summary>
        /// timer事件处理函数，将updater封装成task用于异步执行防止ui阻塞。
        /// </summary>
        /// <param name="sender">一定是timer</param>
        /// <param name="e">随timer触发时传入的事件参数</param>
        private async void Updater_Tick(object sender, EventArgs e)
        {
            await Task.Run(Updater);
        }

        /// <summary>
        /// 初始化dispatcherTimer。一个采用dispatcher将触发事件与ui同步的计时器。将updater作为触发事件。
        /// </summary>
        /// <returns>初始化完成的dispatcherTimer实例</returns>
        private DispatcherTimer InitializeTimer()
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 2)
            };
            timer.Tick += Updater_Tick;
            return timer;
        }

        /// <summary>
        /// 保存计算机信息报表，按钮点击事件。
        /// </summary>
        private void SaveComputerInfo_OnClick(object sender, RoutedEventArgs e)
        {
            ComputerInfo computerInfo = new ComputerInfo()
            {
                ComputerName = ComputerName.Text,
                Motherboard = MotherboardName.Text,
                Cpu = new List<string>(),
                Gpu = new List<string>(),
                Hdd = new List<string>()
            };
            var data = _hardwareNodes[HardwareType.CPU].DataSource as PropertyNodeItem;
            foreach (PropertyNodeItem propertyNodeItem in data.Children)
            {
                computerInfo.Cpu.Add(propertyNodeItem.Name);
            }
            data = _gpuNodes.DataSource as PropertyNodeItem;
            foreach (PropertyNodeItem propertyNodeItem in data.Children)
            {
                foreach (PropertyNodeItem nodeItem in propertyNodeItem.Children)
                {
                    computerInfo.Gpu.Add(nodeItem.Name);
                }
            }
            data = _hardwareNodes[HardwareType.HDD].DataSource as PropertyNodeItem;
            foreach (PropertyNodeItem propertyNodeItem in data.Children)
            {
                computerInfo.Hdd.Add(propertyNodeItem.Name);
            }
            string computerString = JsonConvert.SerializeObject(computerInfo, Formatting.Indented);
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = @"json文件(*.json)|*.json";
            saveFileDialog.AddExtension = true;
            saveFileDialog.FileName = DateTime.Now.ToString("yyyy_MM_dd") + "_computer_info";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllText(saveFileDialog.FileName, computerString);
            }
        }
    }


    /// <summary>
    /// Converter，将传感器类型SensorType枚举转化成对应单位，用在页面的数据绑定中动态生成数据的单位。继承了IValueConverter接口。
    /// <para>注意，因为调用的页面控件为隐式只读的，所以此Converter仅仅允许正向转化，尝试回转将会抛出异常。</para>
    /// <para>注意，传入的值允许是null，但是转化器将值自动更正为SensorType.Voltage，输出的将是单位 v</para>
    /// </summary>
    [ValueConversion(typeof(SensorType), typeof(string))]
    class SensorTypeToUnitsConverter: IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var type = value as SensorType? ?? SensorType.Voltage;
            string Converter(SensorType sensorType) =>
                sensorType switch
                {
                    SensorType.Voltage => "v",
                    SensorType.Clock => "MHz",
                    SensorType.Temperature => "℃",
                    SensorType.Load => "%",
                    SensorType.Fan => "r/s",
                    SensorType.Flow => "N",
                    SensorType.Control => "N",
                    SensorType.Level => "N",
                    SensorType.Factor => "N",
                    SensorType.Power => "W",
                    SensorType.Data => "GB",
                    SensorType.SmallData => "MB",
                    SensorType.Throughput => "MB/s",
                    _ => throw new ArgumentOutOfRangeException(nameof(sensorType), sensorType, null)
                };

            return Converter(type);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    
    /// <summary>
    /// DataTemplateSelector, 为ViewTree的结点选择数据模板的数据模板选择器。继承自DataTemplateSelector。
    /// <para>根据结点类PropertyNodeItem中的NodeType选择模板类型，分为Title模板和Source模板</para>
    /// <seealso cref="NodeType"/>
    /// </summary>
    internal class NodeTemplateSelector : DataTemplateSelector
    {
        public DataTemplate TitleDataTemplate { get; set; }
        public DataTemplate SourceDataTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if (item is PropertyNodeItem { NodeType: NodeType.Title }) return TitleDataTemplate;
            return SourceDataTemplate;
        }
    }

    /// <summary>
    /// 硬件信息类，存放了硬件传感器的各种信息。继承了INotifyPropertyChanged，用于通知数值变动。
    /// </summary>
    internal class HardwareInfo: INotifyPropertyChanged
    {
        /// <summary>
        /// PropertyChanged事件处理，用于发送PropertyChanged信号。
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        /// <summary>
        /// 传感器监测名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 传感器类型，枚举
        /// </summary>
        public SensorType SensorType { get; set; }
        /// <summary>
        /// 传感器当前数值, nullable
        /// </summary>
        private float? _instantValue;
        /// <summary>
        /// _instantValue的Attribution,其中set被加入了PropertyChanged事件，在_instantValue被改变的时候触发PropertyChanged信号，通知BindingSource重新渲染绑定的UI。
        /// </summary>
        public float? InstantValue
        {
            get => _instantValue;
            private set
            {
                _instantValue = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("InstantValue"));
            }
        }
        /// <summary>
        /// 传感器记录最大值，nullable
        /// </summary>
        private float? _maxValue;
        /// <summary>
        /// _maxValue的Attribution,其中set被加入了PropertyChanged事件，在_instantValue被改变的时候触发PropertyChanged信号，通知BindingSource重新渲染绑定的UI。
        /// </summary>
        public float? MaxValue
        {
            get => _maxValue;
            set 
            {
                _maxValue = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("MaxValue"));
            }
        }
        
        /// <summary>
        /// 更新传感器记录最大值
        /// </summary>
        /// <param name="instantValue">传感器当前值</param>
        public void UpdateValue(float? instantValue)
        {
            InstantValue = instantValue;
            MaxValue = instantValue > MaxValue ? instantValue : MaxValue;
        }
    }
    
    /// <summary>
    /// 节点类型
    /// </summary>
    internal enum NodeType
    {
        Title,
        Source
    }

    /// <summary>
    /// ViewTree结点类，此类构成了完整的硬件树结构。
    /// </summary>
    internal class PropertyNodeItem
    {
        /// <summary>
        /// 节点类型
        /// </summary>
        public NodeType NodeType { get; set; }
        /// <summary>
        /// 节点名称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 节点硬件信息，如果结点类型是title，此变量为null
        /// </summary>
        public HardwareInfo HardwareInfo { get; set; }
        /// <summary>
        /// 树状结构的孩子列表，not nullable
        /// </summary>
        public List<PropertyNodeItem> Children { get; set; }
        
        /// <summary>
        /// 构造函数，当NodeType为Title时，应保持hardwareInfo为空，即时填入数据也不会有任何效果，否则应填入HardwareInfo,否则ui渲染时会抛出异常。Children会初始化为一个空列表。
        /// </summary>
        /// <param name="nodeType">节点类型</param>
        /// <param name="name">节点名称</param>
        /// <param name="hardwareInfo">节点硬件信息</param>
        public PropertyNodeItem(NodeType nodeType, string name = null, HardwareInfo hardwareInfo = null)
        {
            NodeType = nodeType;
            Name = name;
            HardwareInfo = hardwareInfo;
            Children = new List<PropertyNodeItem>();
        }

        /// <summary>
        /// 根据节点名称寻找节点并返回
        /// </summary>
        /// <param name="name">待寻找的节点的名称</param>
        /// <returns>搜寻成功会返回找到的节点，失败则返回由默认构造函数构造的PropertyNodeItem，但是由于缺少name，在后续处理中大概率会抛出异常。</returns>
        public PropertyNodeItem GetChildByName(string name)
        {
            return Children.Find(c => c.Name == name);
        }
    }

    /// <summary>
    /// Computer的递归遍历器。用于遍历computer中的每个硬件和其中的传感器和传感器中的每个参数。
    /// <para><![CDATA[https://github.com/openhardwaremonitor/openhardwaremonitor]]></para>
    /// </summary>
    internal class UpdateVisitor : IVisitor
    {
        public void VisitComputer(IComputer computer)
        {
            computer.Traverse(this);
        }

        public void VisitHardware(IHardware hardware)
        {
            hardware.Update();
            foreach (IHardware subHardware in hardware.SubHardware) subHardware.Accept(this);
        }

        public void VisitSensor(ISensor sensor) { }
        public void VisitParameter(IParameter parameter) { }
    }

    /// <summary>
    /// 计算机硬件信息，用来生成信息报表
    /// </summary>
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    [SuppressMessage("ReSharper", "CollectionNeverQueried.Global")]
    internal class ComputerInfo
    {
        [JsonProperty("Computer")] public string  ComputerName { get; set; }
        public string Motherboard { get; set; } 
        [JsonProperty("CPU")] public List<string> Cpu { get; set; } 
        [JsonProperty("GPU")] public List<string> Gpu { get; set; }
        
        [JsonProperty("HDD")] public List<string> Hdd { get; set; }
    }
}

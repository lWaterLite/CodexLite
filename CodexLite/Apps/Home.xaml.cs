using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Threading;
using OpenHardwareMonitor.Hardware;

namespace CodexLite.Apps
{
    /// <summary>
    /// Page1.xaml 的交互逻辑
    /// </summary>
    public partial class Home
    {
        private static DispatcherTimer _updateTimer;
        private readonly Dictionary<HardwareType, BindingSource> _hardwareNodes;
        private static BindingSource _gpuNodes;

        public Home()
        {
            InitializeComponent();
            ComputerName.Text = SystemInformation.ComputerName;
            Computer computer = InitializeComputer();
            _hardwareNodes = InitializeHardwareNodes(computer);
            InitializeSensorNodes(computer,_hardwareNodes);
            _gpuNodes = GpuHandler(_hardwareNodes);
            computer.Close();
            
            CpuTreeView.ItemsSource = _hardwareNodes[HardwareType.CPU];
            GpuTreeView.ItemsSource = _gpuNodes;
            RamTreeView.ItemsSource = _hardwareNodes[HardwareType.RAM];
            HddTreeView.ItemsSource = _hardwareNodes[HardwareType.HDD];

            _updateTimer = InitializeTimer();
            // Dispatcher.InvokeAsync(new Action(delegate { updateTimer.Start(); }));
            _updateTimer.Start();
        }

        ~Home()
        {
            _updateTimer.Stop();
        }

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

        private void InitializeSensorNodes(Computer computer, Dictionary<HardwareType, BindingSource> hardwareNodes)
        {
            foreach (IHardware hardware in computer.Hardware)
            {
                if (hardware.HardwareType == HardwareType.CPU && CpuName.Text == string.Empty) CpuName.Text = hardware.Name;
                if (hardware.HardwareType == HardwareType.Mainboard && MotherboardName.Text == string.Empty)
                    MotherboardName.Text = hardware.Name;
                if (hardware.HardwareType == HardwareType.GpuNvidia ||
                    hardware.HardwareType == HardwareType.GpuAti && GpuName.Text ==
                    string.Empty) GpuName.Text = hardware.Name;
                
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

        private void Updater()
        {
            SensorInfoUpdater(_hardwareNodes);
            GpuUpdater(_hardwareNodes, _gpuNodes);
        }
        
        private async void Updater_Tick(object sender, EventArgs e)
        {
            await Task.Run(Updater);
        }

        private DispatcherTimer InitializeTimer()
        {
            DispatcherTimer timer = new DispatcherTimer
            {
                Interval = new TimeSpan(0, 0, 2)
            };
            timer.Tick += Updater_Tick;
            return timer;
        }
    }


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
    
    internal class HardwareInfo: INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Name { get; set; }
        public SensorType SensorType { get; set; }

        private float? _instantValue;
        public float? InstantValue
        {
            get => _instantValue;
            private set
            {
                _instantValue = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("InstantValue"));
            }
        }

        private float? _maxValue;
        public float? MaxValue
        {
            get => _maxValue;
            set 
            {
                _maxValue = value;
                if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs("MaxValue"));
            }
        }
        
        public void UpdateValue(float? instantValue)
        {
            InstantValue = instantValue;
            MaxValue = instantValue > MaxValue ? instantValue : MaxValue;
        }
    }
    
    internal enum NodeType
    {
        Title,
        Source
    }

    internal class PropertyNodeItem
    {
        public NodeType NodeType { get; set; }
        public string Name { get; set; }
        public HardwareInfo HardwareInfo { get; set; }
        public List<PropertyNodeItem> Children { get; set; }
        
        public PropertyNodeItem(NodeType nodeType, string name = null, HardwareInfo hardwareInfo = null)
        {
            NodeType = nodeType;
            Name = name;
            HardwareInfo = hardwareInfo;
            Children = new List<PropertyNodeItem>();
        }

        public PropertyNodeItem GetChildByName(string name)
        {
            return Children.Find(c => c.Name == name);
        }
    }

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
}
